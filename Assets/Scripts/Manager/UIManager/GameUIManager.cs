using Cysharp.Threading.Tasks;
using NetworkChess.Core;
using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.SceneManagement;
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
    [SerializeField] private GameObject requestTab;
    [SerializeField] private GameObject reviewTab;

    private GameHistoryItemUI lastHistoryItem;

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

    [Header("버튼")]
    [SerializeField] private Button resignBtn; // 기권
    [SerializeField] private Button drawReqBtn; // 무승부 요청
    [SerializeField] private Button takebackReqBtn; // 무르기 요청
    [SerializeField] private Button acceptBtn; // 수락
    [SerializeField] private Button denyBtn; // 거절

    [SerializeField] private Button exitBtn; // 로비로 나가기

    private ProposalType? proposalType;

    private bool isNetworkProcessing = false;
    private bool isProposalPending = false;
    private CancellationTokenSource timeoutCts;
    private const int TIMEOUT_SECONDS = 5;

    #region Start 함수
    void Start()
    {
        InitializeButton();

        InitializeAlertPopUpUI();

        InitializePromotionUI();
        InitializeGameOverUI();

        this.alertPopUpUI.transform.SetAsLastSibling();

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnReplayStarted += PopulateReplayHistory;
            GameManager.Instance.OnTurnEnded += HandleTurnEnded;
            GameManager.Instance.OnGameOverEvent += ShowGameOverUI;
            GameManager.Instance.OnCloseGameOverUI += CloseGameOverUI;
            GameManager.Instance.OnChangeGameUIState += SetButtonView;
        }

        SetProposalButtonView(false, null);

        InitializePlayerInfo();
    }
    #endregion

    #region OnDestroy 함수
    void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnReplayStarted -= PopulateReplayHistory;
            GameManager.Instance.OnTurnEnded -= HandleTurnEnded;
            GameManager.Instance.OnGameOverEvent -= ShowGameOverUI;
            GameManager.Instance.OnCloseGameOverUI -= CloseGameOverUI;
            GameManager.Instance.OnChangeGameUIState -= SetButtonView;
        }
    }
    #endregion

    #region OnEnable 함수
    void OnEnable()
    {
        if (NetworkManager.Instance != null)
        {
            NetworkManager.OnRemoveLastHistory += RemoveLastHistoryItem;
            NetworkManager.OnSetProposalUI += SetProposalButtonView;
            NetworkManager.OnCancelNetworkTimer += CancelTimer;
        }
    }
    #endregion

    #region OnDisable 함수
    void OnDisable()
    {
        if (NetworkManager.Instance != null)
        {
            NetworkManager.OnRemoveLastHistory -= RemoveLastHistoryItem;
            NetworkManager.OnSetProposalUI -= SetProposalButtonView;
            NetworkManager.OnCancelNetworkTimer -= CancelTimer;
        }
    }
    #endregion

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

    #region 버튼 초기화
    private void InitializeButton()
    {
        this.resignBtn.onClick.AddListener(OnResignClick);
        this.drawReqBtn.onClick.AddListener(() => OnProposalClick(ProposalType.Draw));
        this.takebackReqBtn.onClick.AddListener(() => OnProposalClick(ProposalType.Takeback));

        this.acceptBtn.onClick.AddListener(() => OnProposalReplyClick(true));
        this.denyBtn.onClick.AddListener(() => OnProposalReplyClick(false));

        this.exitBtn.onClick.AddListener(OnExitClick);
    }
    #endregion

    #region 플레이어 정보 초기화
    private void InitializePlayerInfo()
    {
        if (GameData.IsReplay == true || GameData.IsSpectator == true)
        {
            this.opponentInfo.Setup(GameData.ReplayTopNickname, GameData.IsWhite == false);
            this.myInfo.Setup(GameData.ReplayBottomNickname, GameData.IsWhite == true);
        }
        else
        {
            this.opponentInfo.Setup(GameData.OpponentNickname, GameData.IsWhite == false);
            this.myInfo.Setup(NetworkManager.Instance.MyNickname, GameData.IsWhite == true);
        }
    }
    #endregion

    #endregion - 초기화 관련 함수

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

    #region 마지막 대수 기보 표기를 삭제하는 함수
    public void RemoveLastHistoryItem()
    {
        if (this.lastHistoryItem != null)
        {
            bool isBlackMoveEmpty = this.lastHistoryItem.RemoveBlackMove();

            if (isBlackMoveEmpty == false)
            {
                Destroy(this.lastHistoryItem.gameObject);

                this.lastHistoryItem = this.historyContent.GetChild(this.historyContent.childCount - 1).GetComponent<GameHistoryItemUI>();
            }
        }
    }
    #endregion

    #region 보이는 버튼을 바꾸는 함수
    private void SetButtonView(bool isPlay)
    {
        this.requestTab.SetActive(isPlay == true);
        this.reviewTab.SetActive(isPlay == false);
    }
    #endregion

    #region 제안 시 수락/거절 버튼을 띄우는 함수
    private void SetProposalButtonView(bool isProposal, ProposalType? type)
    {
        this.proposalType = type;

        this.acceptBtn.gameObject.SetActive(isProposal == true);
        this.denyBtn.gameObject.SetActive(isProposal == true);

        this.drawReqBtn.gameObject.SetActive(isProposal == false);
        this.takebackReqBtn.gameObject.SetActive(isProposal == false);
    }
    #endregion

    #region + 버튼 함수

    #region 기권 버튼을 누를 시 작동되는 함수
    private void OnResignClick()
    {
        // 1. 패킷 송신 중인지 확인
        if (this.isNetworkProcessing == true) return;

        // 2. 버튼 잠금
        this.isNetworkProcessing = true;
        this.resignBtn.interactable = false;

        // 3. 패킷 전송
        C2S_RoomLeaveReq req = new C2S_RoomLeaveReq();

        _ = NetworkManager.Instance.SendPacket(req);

        // 4. 잠금 타이머 가동
        StartNetworkTimer().Forget();
    }
    #endregion

    #region 제안 버튼을 누를 시 작동되는 함수
    private void OnProposalClick(ProposalType type)
    {
        // 1. 패킷 송신 중이거나 다른 제안을 기다리는 중인지 확인
        if (this.isNetworkProcessing == true || this.isProposalPending == true) return;

        // 2. 버튼 잠금
        this.isNetworkProcessing = true;
        this.isProposalPending = true;

        this.drawReqBtn.interactable = false;
        this.takebackReqBtn.interactable = false;

        // 3. 패킷 전송
        C2S_ProposalReq req = new C2S_ProposalReq();
        req.ProposalType = type;

        _ = NetworkManager.Instance.SendPacket(req);

        // 4. 잠금 타이머 가동
        StartNetworkTimer().Forget();
    }
    #endregion

    #region 제안 답장 버튼을 누를 시 작동되는 함수
    private void OnProposalReplyClick(bool isAccept)
    {
        C2S_ProposalReplyReq req = new C2S_ProposalReplyReq();

        if (this.proposalType != null ) req.ProposalType = this.proposalType.Value;
        req.IsAccepted = isAccept;

        _ = NetworkManager.Instance.SendPacket(req);

        SetProposalButtonView(false, null);
    }
    #endregion

    #region 로비로 나가기 버튼을 누를 시 작동되는 함수
    private void OnExitClick()
    {

        if (GameData.IsReplay == true) // 게임 리뷰일 경우
        {
            GameData.Clear();

            SceneManager.LoadScene("LobbyScene");
        }
        else if (GameData.IsSpectator == true) // 관전일 경우
        {
            GameData.Clear();

            C2S_RoomLeaveReq req = new C2S_RoomLeaveReq();

            _ = NetworkManager.Instance.SendPacket(req);
        }
    }
    #endregion

    #region 타임아웃 타이머 비동기 함수
    private async UniTaskVoid StartNetworkTimer()
    {
        this.timeoutCts?.Cancel();
        this.timeoutCts?.Dispose();
        this.timeoutCts = new CancellationTokenSource();

        try
        {
            await UniTask.Delay(TimeSpan.FromSeconds(TIMEOUT_SECONDS), cancellationToken: this.timeoutCts.Token);

            Debug.LogWarning($"<color=red>[네트워크]</color> 서버 응답 시간 초과 (요청 버튼)");

            // 타임아웃 발생 시 다시 누를 수 있도록 잠금 해제
            this.isNetworkProcessing = false;
            this.isProposalPending = false;

            if (this.resignBtn != null) this.resignBtn.interactable = true;
            if (this.drawReqBtn != null) this.drawReqBtn.interactable = true;
            if (this.takebackReqBtn != null) this.takebackReqBtn.interactable = true;
        }
        catch (OperationCanceledException)
        {
            // 정상적으로 서버 응답을 받아 타이머가 취소된 경우
            Debug.Log($"<color=green>[네트워크]</color> 버튼 요청 타임아웃 타이머 정상 안전 종료");
        }
    }
    #endregion

    #region 타이머 취소 함수
    public void CancelTimer(bool isProposalPending)
    {
        this.timeoutCts?.Cancel();
        this.timeoutCts?.Dispose();
        this.timeoutCts = null;

        this.isNetworkProcessing = false;
        if (isProposalPending == true)
        {
            this.isProposalPending = false;

            if (this.drawReqBtn != null) this.drawReqBtn.interactable = true;
            if (this.takebackReqBtn != null) this.takebackReqBtn.interactable = true;
        }
    }
    #endregion

    #endregion - 버튼함수
}
