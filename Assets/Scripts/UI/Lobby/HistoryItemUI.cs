using Cysharp.Threading.Tasks;
using NetworkChess.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HistoryItemUI : MonoBehaviour
{
    private ReplayUI replayUIManager;

    [Header("색상")]
    [SerializeField] private Color drawColor = new Color32(226, 232, 240, 255);
    [SerializeField] private Color winColor = new Color32(45, 212, 191, 255);
    [SerializeField] private Color loseColor = new Color32(251, 113, 133, 255);

    [Header("아이콘")]
    [SerializeField] private Image favoriteImage;
    [SerializeField] private Sprite favoriteSprite;
    [SerializeField] private Sprite removeSprite;
    private bool isFavorite;

    [Header("Top")]
    [SerializeField] private TMP_Text result;
    [SerializeField] private TMP_Text reason;
    [SerializeField] private Button favoriteBtn;

    [Header("Middle")]
    [SerializeField] private TMP_Text versus;
    [SerializeField] private TMP_Text playTime;

    [Header("Bottom")]
    [SerializeField] private TMP_Text replayCode;
    [SerializeField] private Button copyBtn;
    [SerializeField] private Button replayBtn;

    private string currentReplayCode;
    private LocalHistoryData data;

    #region 초기화 함수
    public void Setup(LocalHistoryData data, ReplayUI manager)
    {
        this.replayUIManager = manager;

        S2C_GameOverNoti matchData = data.MatchData;

        this.currentReplayCode = matchData.ReplayCode;
        this.data = data;

        // 1. 승패 텍스트 및 색상 설정
        if (matchData.Winner == "$Draw")
        {
            this.result.text = "무승부";
            this.result.color = this.drawColor;
        }
        else if (matchData.Winner == data.MyNickname)
        {
            this.result.text = "승리";
            this.result.color = this.winColor;
        }
        else if (matchData.BlackNickname == data.MyNickname || matchData.WhiteNickname == data.MyNickname)
        {
            this.result.text = "패배";
            this.result.color = this.loseColor;
        }
        else
        {
            this.result.text = $"{(matchData.Winner == matchData.WhiteNickname ? "백" : "흑")} 승리";
            this.result.color = this.drawColor;
        }

        // 2. 그 외 텍스트 데이터 설정
        this.versus.text = $"{matchData.WhiteNickname}{(matchData.WhiteNickname == data.MyNickname ? "(나)" : "")} vs {matchData.BlackNickname}{(matchData.BlackNickname == data.MyNickname ? "(나)" : "")}";
        this.reason.text = matchData.Reason;
        this.playTime.text = matchData.PlayTime.ToLocalTime().ToString("yyyy-MM-dd");
        this.replayCode.text = $"[공유코드 {matchData.ReplayCode}]";

        // 3. 해당 코드의 즐겨찾기 여부 확인
        this.isFavorite = LocalCacheManager.Instance.IsFavorite(this.currentReplayCode);

        UpdateFavoriteIcon();

        // 4. 버튼 이벤트 연결
        this.copyBtn.onClick.RemoveAllListeners();
        this.copyBtn.onClick.AddListener(OnCopy);

        this.replayBtn.onClick.RemoveAllListeners();
        this.replayBtn.onClick.AddListener(OnReplay);

        this.favoriteBtn.onClick.RemoveAllListeners();
        this.favoriteBtn.onClick.AddListener(OnFavoriteToggle);
    }
    #endregion

    #region 즐겨찾기 아이콘 업데이트 함수
    private void UpdateFavoriteIcon()
    {
        if (this.isFavorite == true)
        {
            this.favoriteImage.sprite = this.removeSprite;
        }
        else
        {
            this.favoriteImage.sprite = this.favoriteSprite;
        }
    }
    #endregion

    #region 코드 복사 버튼 클릭 시 작동하는 함수
    private void OnCopy()
    {
        GUIUtility.systemCopyBuffer = this.currentReplayCode;
        CLog.Log($"[시스템] 클립보드에 복사되었습니다: {this.currentReplayCode}");
    }
    #endregion

    #region 게임 리뷰 버튼 클릭 시 작동하는 함수
    private void OnReplay()
    {
        // 1. 서버에 FEN 기보 요청 패킷 발송
        C2S_ReplayReq req = new C2S_ReplayReq();

        req.ReplayCode = this.currentReplayCode;

        NetworkManager.Instance.SendPacket(req).Forget();
    }
    #endregion

    #region 즐겨찾기 버튼 클릭 시 작동하는 함수
    private void OnFavoriteToggle()
    {
        // 1. 현재 즐겨찾기 여부에 따라 즐겨찾기 처리
        if (this.isFavorite == true)
        {

            LocalCacheManager.Instance.RemoveFavorite(this.currentReplayCode);
        }
        else
        {
            LocalCacheManager.Instance.AddFavorite(this.data);
        }

        this.isFavorite = (this.isFavorite == false);

        // 2. 아이콘 업데이트
        UpdateFavoriteIcon();

        if (this.replayUIManager != null)
        {
            this.replayUIManager.OnItemFavoriteToggled(this.data, this.isFavorite);
        }
    }
    #endregion

    #region 아이템 아이콘을 갱신하게 해주는 함수
    public void ForceUpdateFavoriteState(bool isFavorite)
    {
        this.isFavorite = isFavorite;
        UpdateFavoriteIcon();
    }
    #endregion

    public string GetPlayCode() => this.currentReplayCode;
}
