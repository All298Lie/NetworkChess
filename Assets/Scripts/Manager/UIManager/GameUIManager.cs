using NetworkChess.Core;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameUIManager : MonoBehaviour
{
    [Header("플레이어 정보 UI")]
    [SerializeField] private PlayerInfoUI opponentInfo;
    [SerializeField] private PlayerInfoUI myInfo;

    [Header("기보 스크롤 뷰 관련")]
    [SerializeField] private Transform historyContent;
    [SerializeField] private ScrollRect historyScrollRect;
    [SerializeField] private GameObject historyItemPrefab;

    [Header("프로모션 UI")]
    [SerializeField] private GameObject promotionUIPrefab;
    public PromotionUIController PromotionUI { get; private set; }

    [Header("게임오버 UI")]
    [SerializeField] private GameObject gameOverUIPrefab;
    public GameOverUIController GameOverUI { get; private set; }

    [Header("알림 UI")]
    [SerializeField] private GameObject alertPopUpUIPrefab;
    private PopUpUI alertPopUpUI;
    private AlertPopUpUI alert;

    private GameHistoryItemUI lastHistoryItem;

    [Header("버튼")]
    [SerializeField] private Button resignBtn;

    void Start()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnReplayStarted += PopulateReplayHistory;
            GameManager.Instance.OnTurnEnded += HandleTurnEnded;
        }

        InitializeButton();

        InitializeAlertPopUpUI();

        InitializePromotionUI();
        InitializeGameOverUI();

        this.alertPopUpUI.transform.SetAsLastSibling();

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameOverEvent += ShowGameOverUI;
            GameManager.Instance.OnCloseGameOverUI += CloseGameOverUI;
        }
    }
    
    void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnReplayStarted -= PopulateReplayHistory;
            GameManager.Instance.OnTurnEnded -= HandleTurnEnded;
            GameManager.Instance.OnGameOverEvent -= ShowGameOverUI;
            GameManager.Instance.OnCloseGameOverUI -= CloseGameOverUI;
        }
    }

    #region + 초기화 관련 함수

    #region 프로모션 UI 초기화
    private void InitializePromotionUI()
    {
        GameObject promotionUI = Instantiate(this.promotionUIPrefab, transform);
        promotionUI.name = "PromotionUI";

        this.PromotionUI = promotionUI.GetComponent<PromotionUIController>();
    }
    #endregion

    #region 게임오버 UI 초기화
    private void InitializeGameOverUI()
    {
        GameObject gameOverUI = Instantiate(this.gameOverUIPrefab, transform);
        gameOverUI.name = "GameOverUI";

        this.GameOverUI = gameOverUI.GetComponent<GameOverUIController>();
        this.GameOverUI.Setup(this.alert);
    }
    #endregion

    #region AlertPopUpUI 초기화 함수
    private void InitializeAlertPopUpUI()
    {
        // 1. 프리팹을 통한 생성
        GameObject alertPopUpUI = Instantiate(this.alertPopUpUIPrefab, transform);
        alertPopUpUI.name = "AlertPopUpUI";

        // 2. 알림 팝업 UI 변수에 담기
        this.alertPopUpUI = alertPopUpUI.GetComponent<PopUpUI>();
        this.alert = alertPopUpUI.GetComponent<AlertPopUpUI>();
    }
    #endregion

    #endregion - 초기화 관련 함수

    private void InitializeButton()
    {
        this.resignBtn.onClick.AddListener(OnResignClick);
    }

    private void ShowGameOverUI(string winner, string reason, string code) => this.GameOverUI.ShowGameOver(winner, reason, code);

    private void CloseGameOverUI() => this.GameOverUI.CloseGameOverUI();

    #region 해당 턴에서 이뤄진 동작을 기록하는 함수
    private void HandleTurnEnded(S2C_GameStateNoti noti)
    {
        bool didWhiteJustMove = (noti.IsWhiteTurn == false);

        ChessMoveEntry entry = noti.Entry;

        int ply = ReplayManager.Instance.LatestIndex;

        if (didWhiteJustMove == true)
        {
            GameObject history = Instantiate(this.historyItemPrefab, this.historyContent);
            this.lastHistoryItem = history.GetComponent<GameHistoryItemUI>();

            this.lastHistoryItem.SetWhiteMove(noti.FullMoveNumber, entry.MoveNotation, ply);
        }
        else
        {
            if (this.lastHistoryItem != null)
            {
                this.lastHistoryItem.UpdateBlackMove(entry.MoveNotation, ply);
            }
        }

        ScrollToBottom();
    }
    #endregion

    #region 리플레이 로드 시 기보 목록을 띄우는 함수
    public void PopulateReplayHistory(List<ChessMoveEntry> entries)
    {
        if (entries == null || entries.Count < 1) return;

        for (int i = 1; i < entries.Count; i++)
        {
            ChessMoveEntry entry = entries[i];
            bool isWhiteTurn = (i % 2 != 0);

            if (isWhiteTurn == true)
            {
                GameObject history = Instantiate(this.historyItemPrefab, this.historyContent);

                this.lastHistoryItem = history.GetComponent<GameHistoryItemUI>();
                this.lastHistoryItem.SetWhiteMove(i / 2 + 1, entry.MoveNotation, i);
            }
            else
            {
                this.lastHistoryItem?.UpdateBlackMove(entry.MoveNotation, i);
            }
        } // for 문
    }
    #endregion

    #region 맨 아래로 스크롤을 내리는 함수
    private void ScrollToBottom()
    {
        Canvas.ForceUpdateCanvases();

        this.historyScrollRect.verticalNormalizedPosition = 0.0f;
    }
    #endregion

    #region + 버튼 함수

    #region 기권 버튼을 누를 시 작동되는 함수
    private void OnResignClick()
    {
        C2S_RoomLeaveReq req = new C2S_RoomLeaveReq();

        _ = NetworkManager.Instance.SendPacket(req);
    }
    #endregion

    #endregion
}
