using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

using NetworkChess.Core;
using System;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public GameModeBase ActiveMode { get; private set; }

    public bool IsGameOver { get; private set; }

    public event Action<S2C_GameStateNoti> OnTurnEnded;
    public event Action<List<ChessMoveEntry>> OnReplayStarted;
    public event Action<string, bool> OnPieceMoveSound;

    public event Action<string, string, string> OnGameOverEvent;
    public event Action OnCloseGameOverUI;
    public event Action<bool> OnChangeGameUIState;

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
            ReplayManager.Instance.SetupTimeline(GameData.Entries);
            ReplayManager.Instance.JumpToPly(0);

            OnReplayStarted?.Invoke(GameData.Entries);

            CLog.Log("[게임 리뷰] 리플레이 환경 구성 완료");

            OnChangeGameUIState?.Invoke(false);
        }
        else if (GameData.IsSpectator == true)
        {
            ChessMoveEntry entry = GameData.Entries[GameData.Entries.Count - 1];

            string currentFEN = entry.FEN;

            this.ActiveMode.StartGame("w", "b", currentFEN);

            ReplayManager.Instance.SetupTimeline(GameData.Entries);
            ReplayManager.Instance.JumpToPly(GameData.Entries.Count - 1);

            OnReplayStarted?.Invoke(GameData.Entries);

            OnChangeGameUIState?.Invoke(false);
        }
        else
        {
            this.ActiveMode.StartGame("w", "b", GameData.StartingFEN);

            ChessMoveEntry entry = new ChessMoveEntry();
            entry.FEN = GameData.StartingFEN;
            entry.StartPos = new BoardPos(-1, -1);
            entry.EndPos = new BoardPos(-1, -1);

            List<ChessMoveEntry> initialTimeLine = new List<ChessMoveEntry>();
            initialTimeLine.Add(entry);

            ReplayManager.Instance.SetupTimeline(initialTimeLine);

            OnChangeGameUIState?.Invoke(true);
        }

        // 4. 뷰어 세팅
        BoardManager.Instance.SetupBoard(this.ActiveMode);
    }
    #endregion

    #region + 이벤트 호출 함수

    #region 게임 종료 시 호출되는 함수
    private void HandleGameOver(string winnerName, string reason, string replayCode)
    {
        this.IsGameOver = true;

        OnGameOverEvent?.Invoke(winnerName, reason, replayCode);

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
    private void HandleRoomLeaveSuccess(RoomLeaveReason reason)
    {
        switch (reason)
        {
            case RoomLeaveReason.Resign:
                CLog.Log($"<color=green>[네트워크]</color> 기권 처리 완료. 게임오버 화면 대기 중...");
                break;

            case RoomLeaveReason.Disconnect:
                CLog.Log($"<color=red>[네트워크]</color> 연결이 끊어졌습니다.");
                SceneManager.LoadScene("TitleScene");
                break;

            case RoomLeaveReason.PostGameExit:
            case RoomLeaveReason.CancelWaiting: // 예외 처리용
            default: // 예외 처리용
                SceneManager.LoadScene("LobbyScene");
                break;
        }
    }
    #endregion

    #region 게임 리뷰 요청 시 호출되는 함수
    private void HandleReplayReceived(S2C_ReplayRes res)
    {
        if (res.IsSuccess == true)
        {
            HighlightManager.Instance.HideMoveHighlights();

            GameData.IsReplay = true;

            // 게임오버 UI 닫기
            OnCloseGameOverUI?.Invoke();

            // 버튼 상태 변경
            OnChangeGameUIState?.Invoke(false);

            // 리플레이 설정 후, 시작점으로 이동
            ReplayManager.Instance.SetupTimeline(res.Entries);
            ReplayManager.Instance.JumpToPly(0);
        }
    }
    #endregion

    #region 게임 상태 통보 수신시 호출되는 함수
    private void HandleGameStateNotified(S2C_GameStateNoti noti)
    {
        ChessMoveEntry entry = noti.Entry;

        bool didIMove = (GameData.IsWhite != noti.IsWhiteTurn) && (GameData.IsSpectator == false);

        string SAN = entry.MoveNotation;

        OnPieceMoveSound?.Invoke(SAN, didIMove);

        // 1. 코어 데이터 처리
        if (didIMove == false)
        {

            CorePiece movedPiece = this.ActiveMode.Board[entry.StartPos.x, entry.StartPos.y];

            if (movedPiece != null)
            {
                this.ActiveMode.HandlePieceMoveRequest(movedPiece, entry.EndPos, noti.PromotionType);
            }
        }

        this.ActiveMode.IsWhiteTurn = noti.IsWhiteTurn;

        ReplayManager.Instance.UpdateTimeLine(entry);

        // 2. UI 갱신
        OnTurnEnded?.Invoke(noti);

        CLog.Log($"[기물 이동] {entry.StartPos} -> {entry.EndPos} / 다음 턴 : {(noti.IsWhiteTurn == true ? "백" : "흑")}");
    }
    #endregion

    #endregion - 이벤트 호출 함수
}
