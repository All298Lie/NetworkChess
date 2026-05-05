using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

using NetworkChess.Core;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("게임 세팅")]
    [SerializeField] private Transform canvas;
    [SerializeField] private GameMode currentMode = GameMode.Standard;

    [Header("프리팹")]
    [SerializeField] private GameObject gameOverUIPrefab;

    public GameOverUIController GameOverUI { get; private set; }
    public GameModeBase ActiveMode { get; private set; }

    public bool IsGameEnd { get; private set; }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Debug.LogWarning("게임 매니저가 이미 존재합니다.");
            Destroy(gameObject);
        }
    }

    void Start()
    {
        GameObject gameOverUI = Instantiate(gameOverUIPrefab, canvas);
        gameOverUI.name = "GameOverUI";

        this.GameOverUI = gameOverUI.GetComponent<GameOverUIController>();

        InitializeGameMode();
    }

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

    private void InitializeGameMode()
    {
        if (this.currentMode == GameMode.Standard)
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
            Debug.Log($"현재 활성화된 체스 모드: {currentMode}");

            // 4. 게임 시작
            this.ActiveMode.StartGame();
        }
    }

    private void HandleGameOver(string winnerName, string reason)
    {
        this.IsGameEnd = true;
        this.GameOverUI.ShowGameOver(winnerName, reason);
    }

    private void HandlePieceCaptured(CorePiece capturedPiece)
    {
        BoardManager.Instance.DestroyPiece(capturedPiece);
    }

    private void HandlePawnPromoted(CorePiece pawn, PieceType newType)
    {
        BoardManager.Instance.PromotePawnView(pawn, newType);
    }

    private void HandlePieceMoved(CorePiece piece, BoardPos newPos)
    {
        BoardManager.Instance.UpdatePieceVisualPosition(piece, newPos);
    }

    public void OnClickExitButton()
    {
        SceneManager.LoadScene("LobbyScene");
    }
}
