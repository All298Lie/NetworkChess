using NetworkChess.Core;
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

    private GameHistoryItemUI lastHistoryItem;

    [Header("버튼")]
    [SerializeField] private Button resignBtn;

    void Start()
    {
        if (GameManager.Instance != null)
        {
            // GameManager.Instance.OnGameStarted += HandleGameStarted;
            GameManager.Instance.OnTurnEnded += HandleTurnEnded;
        }

        InitializeButton();
    }
    
    void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            // GameManager.Instance.OnGameStarted -= HandleGameStarted;
            GameManager.Instance.OnTurnEnded -= HandleTurnEnded;
        }
    }

    private void InitializeButton()
    {
        this.resignBtn.onClick.AddListener(OnResignClick);
    }

    private void HandleGameStarted()
    {

    }

    #region 해당 턴에서 이뤄진 동작을 기록하는 함수
    private void HandleTurnEnded(S2C_GameStateNoti noti)
    {
        bool didWhiteJustMove = (noti.IsWhiteTurn == false);

        if (didWhiteJustMove == true)
        {
            GameObject history = Instantiate(this.historyItemPrefab, this.historyContent);
            this.lastHistoryItem = history.GetComponent<GameHistoryItemUI>();

            this.lastHistoryItem.SetWhiteMove(noti.FullMoveNumber, noti.MoveNotation, noti.CurrentFEN);
        }
        else
        {
            if (this.lastHistoryItem != null)
            {
                this.lastHistoryItem.UpdateBlackMove(noti.MoveNotation, noti.CurrentFEN);
            }
        }

        ScrollToBottom();
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
