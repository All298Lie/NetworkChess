using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

using NetworkChess.Core;
using System;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("게임 세팅")]
    [SerializeField] private Transform canvas;

    [Header("프로모션 UI")]
    [SerializeField] private GameObject promotionUIPrefab;
    public PromotionUIController PromotionUI { get; private set; }

    [Header("게임오버 UI")]
    [SerializeField] private GameObject gameOverUIPrefab;
    public GameOverUIController GameOverUI { get; private set; }

    public GameModeBase ActiveMode { get; private set; }

    public bool IsGameOver { get; private set; }

    public event Action<S2C_GameStateNoti> OnTurnEnded;

    #region Awake 함수
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            CLog.LogWarning("게임 매니저가 이미 존재합니다.");
            Destroy(gameObject);
        }
    }
    #endregion

    #region Start 함수
    void Start()
    {
        InitializePromotionUI();
        InitializeGameOverUI();

        InitializeGameMode();
    }
    #endregion

    #region OnDestroy 함수
    void OnDestroy()
    {
        if (this.ActiveMode != null)
        {
            this.ActiveMode.OnPieceCapturedEvent -= HandlePieceCaptured;
            this.ActiveMode.OnPawnPromotedEvent -= HandlePawnPromoted;
            this.ActiveMode.OnPieceMovedEvent -= HandlePieceMoved;
        }

        if (NetworkManager.Instance != null)
        {
            NetworkManager.OnGameOver -= HandleGameOver;
            NetworkManager.OnRoomLeave -= HandleRoomLeaveSuccess;

            NetworkManager.OnGameStateNotified -= HandleGameStateNotified;

            NetworkManager.OnReplayReceived -= HandleReplayReceived;
        }
    }
    #endregion

    #region 프로모션 UI 초기화
    private void InitializePromotionUI()
    {
        GameObject promotionUI = Instantiate(this.promotionUIPrefab, this.canvas);
        promotionUI.name = "PromotionUI";

        this.PromotionUI = promotionUI.GetComponent<PromotionUIController>();
    }
    #endregion

    #region 게임오버 UI 초기화
    private void InitializeGameOverUI()
    {
        GameObject gameOverUI = Instantiate(this.gameOverUIPrefab, this.canvas);
        gameOverUI.name = "GameOverUI";

        this.GameOverUI = gameOverUI.GetComponent<GameOverUIController>();
    }
    #endregion

    #region 게임모드 관련 초기화 함수
    private void InitializeGameMode()
    {
        Dictionary<PieceType, CorePieceData> dataDic = BoardManager.Instance.GetCorePieceDataDic();

        // 1. 게임모드에 따른 설정
        switch (GameData.CurrentMode)
        {
            case GameMode.Standard:
                // 순수 C# 코어 매니저 생성
                StandardChessMode standardMode = new StandardChessMode(dataDic);
                this.ActiveMode = standardMode;
                break;

            default:
                CLog.LogWarning("현재 추가되지 않은 게임모드로 설정되었습니다.");
                break;
        }

        // 2. 이벤트 구독
        this.ActiveMode.OnPieceCapturedEvent += HandlePieceCaptured;
        this.ActiveMode.OnPawnPromotedEvent += HandlePawnPromoted;
        this.ActiveMode.OnPieceMovedEvent += HandlePieceMoved;

        if (NetworkManager.Instance != null && GameData.IsReplay == false)
        {
            NetworkManager.OnGameOver += HandleGameOver;
            NetworkManager.OnRoomLeave += HandleRoomLeaveSuccess;

            NetworkManager.OnGameStateNotified += HandleGameStateNotified;

            NetworkManager.OnReplayReceived += HandleReplayReceived;
        }


        CLog.Log($"현재 활성화된 체스 모드: {GameData.CurrentMode}");

        // 5. 상태에 따른 분기 처리
        if (GameData.IsReplay == true)
        {
            SetupReplayMode();
        }
        else
        {
            this.ActiveMode.StartGame("w", "b", GameData.StartingFEN);
        }

        // 4. 뷰어 세팅
        BoardManager.Instance.SetupBoard(this.ActiveMode);
    }
    #endregion

    private void SetupReplayMode()
    {
        CLog.Log("[게임 리뷰] 리플레이 환경 구성 완료");
    }

    #region + 이벤트 호출 함수

    #region 게임 종료 시 호출되는 함수
    private void HandleGameOver(string winnerName, string reason, string replayCode)
    {
        this.IsGameOver = true;
        this.GameOverUI.ShowGameOver(winnerName, reason, replayCode);

        CLog.Log("게임 종료 이벤트 감지");
    }
    #endregion

    #region 기물을 잡을 시 호출되는 함수
    private void HandlePieceCaptured(CorePiece capturedPiece)
    {
        BoardManager.Instance.DeactivatePiece(capturedPiece);
    }
    #endregion

    #region 폰 프로모션 시 호출되는 함수
    private void HandlePawnPromoted(CorePiece pawn, PieceType newType)
    {
        BoardManager.Instance.PromotePawnView(pawn, newType);
    }
    #endregion

    #region 기물 이동 시 호출되는 함수
    private void HandlePieceMoved(CorePiece piece, BoardPos newPos)
    {
        BoardManager.Instance.UpdatePieceVisualPosition(piece, newPos);
    }
    #endregion

    #region 게임 나가기 성공 시 호출되는 함수
    private void HandleRoomLeaveSuccess()
    {
        SceneManager.LoadScene("LobbyScene");
    }
    #endregion

    #region 게임 리뷰 요청 시 호출되는 함수
    private void HandleReplayReceived(S2C_ReplayRes res)
    {
        GameData.IsReplay = true;
        GameData.FENHistory = res.FENHistory;

        this.GameOverUI.CloseGameOverUI();
    }
    #endregion

    #region 게임 상태 통보 수신시 호출되는 함수
    private void HandleGameStateNotified(S2C_GameStateNoti noti)
    {
        // 1. 코어 데이터 처리
        bool didIMove = (GameData.IsWhite != noti.IsWhiteTurn) && (GameData.IsSpectator == false);

        if (didIMove == false)
        {
            CorePiece movedPiece = this.ActiveMode.Board[noti.StartPos.x, noti.StartPos.y];

            if (movedPiece != null)
            {
                this.ActiveMode.HandlePieceMoveRequest(movedPiece, noti.EndPos, noti.PromotionType);
            }
        }

        this.ActiveMode.IsWhiteTurn = noti.IsWhiteTurn;

        // 2. 비주얼 및 하이라이트 매니저 동기화
        BoardManager.Instance.SyncVisualsWithCore(this.ActiveMode);
        HighlightManager.Instance.UpdateLastMoveHighlight(noti.StartPos, noti.EndPos);

        // 3. UI 갱신
        OnTurnEnded?.Invoke(noti);

        CLog.Log($"[기물 이동] {noti.StartPos} -> {noti.EndPos} / 다음 턴 : {(noti.IsWhiteTurn == true ? "백" : "흑")}");
    }
    #endregion

    #endregion - 이벤트 호출 함수
}
