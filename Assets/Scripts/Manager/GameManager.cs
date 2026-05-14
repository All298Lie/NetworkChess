using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

using NetworkChess.Core;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("게임 세팅")]
    [SerializeField] private Transform canvas;

    [Header("프리팹")]
    [SerializeField] private GameObject gameOverUIPrefab;

    public GameOverUIController GameOverUI { get; private set; }
    public GameModeBase ActiveMode { get; private set; }

    public bool IsGameEnd { get; private set; }

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
        GameObject gameOverUI = Instantiate(gameOverUIPrefab, canvas);
        gameOverUI.name = "GameOverUI";

        this.GameOverUI = gameOverUI.GetComponent<GameOverUIController>();

        InitializeGameMode();
    }
    #endregion

    #region OnDestroy 함수
    void OnDestroy()
    {
        if (this.ActiveMode != null)
        {
            this.ActiveMode.OnGameOverEvent -= HandleGameOver;
            this.ActiveMode.OnPieceCapturedEvent -= HandlePieceCaptured;
            this.ActiveMode.OnPawnPromotedEvent -= HandlePawnPromoted;
            this.ActiveMode.OnPieceMovedEvent -= HandlePieceMoved;
        }
    }
    #endregion

    #region 게임모드 관련 초기화 함수
    private void InitializeGameMode()
    {
        if (GameData.CurrentMode == GameMode.Standard)
        {
            // 1. 순수 C# 코어 매니저 생성
            StandardChessMode standardMode = new StandardChessMode();
            this.ActiveMode = standardMode;

            // 2. 이벤트 구독
            this.ActiveMode.OnGameOverEvent += HandleGameOver;
            this.ActiveMode.OnPieceCapturedEvent += HandlePieceCaptured;
            this.ActiveMode.OnPawnPromotedEvent += HandlePawnPromoted;
            this.ActiveMode.OnPieceMovedEvent += HandlePieceMoved;

            // 3. 보드 데이터 및 규칙 주입
            CorePiece[, ] coreBoard = BoardManager.Instance.Board;
            Dictionary<PieceType, CorePieceData> dataDic = BoardManager.Instance.GetCorePieceDataDic();

            standardMode.Initialize(coreBoard, dataDic);
            CLog.Log($"현재 활성화된 체스 모드: {GameData.CurrentMode}");

            // 4. 게임 시작
            this.ActiveMode.StartGame();
        }
    }
    #endregion

    #region + 이벤트 호출 함수

    #region 게임 종료 시 호출되는 함수
    private void HandleGameOver(string winnerName, string reason)
    {
        this.IsGameEnd = true;
        this.GameOverUI.ShowGameOver(winnerName, reason);
    }
    #endregion

    #region 기물을 잡을 시 호출되는 함수
    private void HandlePieceCaptured(CorePiece capturedPiece)
    {
        BoardManager.Instance.DestroyPiece(capturedPiece);
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

    #endregion - 이벤트 호출 함수

    #region 게임 나가기 버튼 클릭 시 작동되는 함수
    public void OnClickExitButton()
    {
        SceneManager.LoadScene("LobbyScene");
    }
    #endregion
}
