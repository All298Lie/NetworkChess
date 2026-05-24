using Cysharp.Threading.Tasks;
using NetworkChess.Core;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.UI;

public enum ReplayTab
{
    None,
    Recent,
    Favorite,
    Search
}

public class ReplayUI : PopUpUI
{
    private readonly Regex replayCodeRegex = new Regex("^[a-zA-Z0-9]{8}$", RegexOptions.Compiled);

    private AlertPopUpUI alert;

    private PopUpUI loadingUI;
    private LoadingUI loading;

    [Header("상단 탭 버튼")]
    [SerializeField] private ButtonTextEffect recentTabBtn;
    [SerializeField] private ButtonTextEffect favoriteTabBtn;
    [SerializeField] private ButtonTextEffect searchTabBtn;

    [Header("Recent 탭")]
    [SerializeField] private GameObject recentUI;
    [SerializeField] private Transform recentContent;

    [Header("Favorite 탭")]
    [SerializeField] private GameObject favoriteUI;
    [SerializeField] private Transform favoriteContent;

    [Header("Search 탭")]
    [SerializeField] private GameObject searchUI;
    [SerializeField] private Transform searchContent;
    [SerializeField] private TMP_InputField search;
    [SerializeField] private Button searchBtn;
    private HistoryItemUI searchItem;
    private bool cancelSearch;

    [Header("프리팹")]
    [SerializeField] private GameObject historyItemPrefab;

    private ObjectPool<HistoryItemUI> itemPool;

    private List<LocalHistoryData> cachedRecentList;
    private List<LocalHistoryData> cachedFavoriteList;

    private List<HistoryItemUI> activeRecentItems = new List<HistoryItemUI>();
    private List<HistoryItemUI> activeFavoriteItems = new List<HistoryItemUI>();

    private ReplayTab currentTab = ReplayTab.None;

    #region + 유니티 함수

    #region Start 함수
    void Start()
    {
        // 1. 파일 불러오기
        this.cachedRecentList = LocalCacheManager.Instance.LoadRecentHistory();
        this.cachedFavoriteList = LocalCacheManager.Instance.LoadFavoriteHistory();

        // 2. 버튼 연결
        this.recentTabBtn.Button.onClick.AddListener(() => SetTab(ReplayTab.Recent));
        this.favoriteTabBtn.Button.onClick.AddListener(() => SetTab(ReplayTab.Favorite));
        this.searchTabBtn.Button.onClick.AddListener(() => SetTab(ReplayTab.Search));
        this.searchBtn.onClick.AddListener(OnSearchClick);

        this.cancelSearch = false;

        InitializeObjectPool();

        InitializeRecentTab();
        InitializeFavoriteTab();
        InitializeSearchTab();

        SetTab(ReplayTab.Recent);
    }
    #endregion

    #region OnEnable 함수
    void OnEnable()
    {
        if (NetworkManager.Instance != null)
        {
            NetworkManager.OnReplayCodeReceived += ReplayCodeReceived;
        }

        if (this.loading != null)
        {
            this.loading.OnCancelLoading += HandleCancelSearchReplayCode;
        }
    }
    #endregion

    #region OnDisable 함수
    void OnDisable()
    {
        if (NetworkManager.Instance != null)
        {
            NetworkManager.OnReplayCodeReceived -= ReplayCodeReceived;
        }

        this.loading.OnCancelLoading -= HandleCancelSearchReplayCode;
    }
    #endregion

    #endregion - 유니티 함수

    #region + 초기화 관련 함수

    #region 로비UI매니저 주입 함수
    public void Setup(AlertPopUpUI alert, LoadingUI loading, PopUpUI loadingUI)
    {
        this.alert = alert;

        this.loadingUI = loadingUI;
        this.loading = loading;

        this.loading.OnCancelLoading += HandleCancelSearchReplayCode;
    }
    #endregion

    #region 오브젝트풀링 초기화 함수
    private void InitializeObjectPool()
    {
        this.itemPool = new ObjectPool<HistoryItemUI>(
            createFunc: CreateHistoryItem,
            actionOnGet:GetHistoryItem,
            actionOnRelease: ReleaseHistoryItem,
            actionOnDestroy: DestroyHistoryItem,
            collectionCheck: true,
            defaultCapacity: 20,
            maxSize: 100
        );
    }
    #endregion

    #region 최근 게임 탭 초기화 함수
    private void InitializeRecentTab()
    {
        foreach (LocalHistoryData data in this.cachedRecentList)
        {
            HistoryItemUI item = GetItemForTab(this.recentContent);

            item.Setup(data, this);

            this.activeRecentItems.Add(item);
        }
    }
    #endregion

    #region 즐겨찾기 탭 초기화 함수
    private void InitializeFavoriteTab()
    {
        foreach (LocalHistoryData data in this.cachedFavoriteList)
        {
            HistoryItemUI item = GetItemForTab(this.favoriteContent);

            item.Setup(data, this);

            this.activeFavoriteItems.Add(item);
        }
    }
    #endregion

    #region 검색 탭 초기화 함수
    private void InitializeSearchTab()
    {
        this.searchItem = GetItemForTab(this.searchContent);

        this.searchItem.gameObject.SetActive(false);
    }
    #endregion

    #endregion - 초기화 관련 함수

    #region 탭 지정 함수
    private void SetTab(ReplayTab tab)
    {
        if (this.currentTab == tab) return;

        this.currentTab = tab;

        // 2. 버튼 활성화 설정
        this.recentTabBtn.SetSelected(tab == ReplayTab.Recent);
        this.favoriteTabBtn.SetSelected(tab == ReplayTab.Favorite);
        this.searchTabBtn.SetSelected(tab == ReplayTab.Search);

        // 3. 탭 활성화 설정
        this.recentUI.SetActive(tab == ReplayTab.Recent);
        this.favoriteUI.SetActive(tab == ReplayTab.Favorite);
        this.searchUI.SetActive(tab == ReplayTab.Search);
    }
    #endregion

    #region 아이템 간 상태 동기화
    public void OnItemFavoriteToggled(LocalHistoryData data, bool isFavorite)
    {
        string replayCode = data.MatchData.ReplayCode;

        if (isFavorite == true)
        {
            HistoryItemUI newItem = GetItemForTab(this.favoriteContent);
            newItem.Setup(data, this);

            newItem.transform.SetAsFirstSibling();
            activeFavoriteItems.Add(newItem);

            SyncIconState(this.activeRecentItems, replayCode, true);
        }
        else
        {
            HistoryItemUI itemToRemove = this.activeFavoriteItems.Find(x => x.GetPlayCode() == replayCode);
            if (itemToRemove != null)
            {
                this.activeFavoriteItems.Remove(itemToRemove);
                itemPool.Release(itemToRemove);
            }

            SyncIconState(this.activeRecentItems, replayCode, false);
        }
    }
    #endregion

    #region 아이콘 상태 동기화 함수
    private void SyncIconState(List<HistoryItemUI> targetList, string replayCode, bool isFavorite)
    {
        HistoryItemUI targetItem = targetList.Find(x => x.GetPlayCode() == replayCode);
        if (targetItem != null)
        {
            targetItem.ForceUpdateFavoriteState(isFavorite);
        }
    }
    #endregion

    #region 검색 버튼 함수
    private void OnSearchClick()
    {
        // 1. 내용을 입력했는지 확인
        if (string.IsNullOrEmpty(this.search.text) == true)
        {
            this.alert.ShowPopup("게임 리뷰", "리플레이 코드 8자를 입력해야 합니다.");
            return;
        }

        // 2. 기보 코드 형식인지 확인
        if (this.replayCodeRegex.IsMatch(this.search.text) == false)
        {
            this.alert.ShowPopup("게임 리뷰", "잘못된 리플레이 코드 형식입니다.");
            return;
        }

        this.searchItem.gameObject.SetActive(false);

        this.cancelSearch = false;

        // 3. 기보코드를 통해 게임 리뷰 요청
        C2S_FindReplayCodeReq req = new C2S_FindReplayCodeReq();

        req.ReplayCode = this.search.text;

        NetworkManager.Instance.SendPacket(req).Forget();

        // 4. 로딩 UI 팝업
        this.loading.ShowWaiting("리플레이 기록을 검색 중입니다...", true);
    }
    #endregion

    #region 리플레이 코드 검색 결과가 반환왔을 때 작동하는 함수
    private void ReplayCodeReceived(S2C_FindReplayCodeRes res)
    {
        // 1. 취소했는지 확인
        if (this.cancelSearch == true) return;

        // 2. 로딩 창 닫기
        this.loadingUI.ClosePopUpUI();

        // 3. 탐색에 성공했는지 확인
        if (res.IsSuccess == false)
        {
            this.alert.ShowPopup("리플레이 코드", res.Message);

            return;
        }

        LocalHistoryData data = new LocalHistoryData();
        data.MyNickname = "$Unknown";
        data.MatchData = res.MatchData;

        this.searchItem.Setup(data, this);

        this.searchItem.gameObject.SetActive(true);
    }
    #endregion

    #region 리플레이 코드 검색 중 취소 했을 경우 작동하는 함수
    private void HandleCancelSearchReplayCode() => this.cancelSearch = true;
    #endregion

    #region + 오브젝트 풀링 함수

    #region 오브젝트 생성
    private HistoryItemUI CreateHistoryItem()
    {
        GameObject historyItem = Instantiate(this.historyItemPrefab);

        return historyItem.GetComponent<HistoryItemUI>();
    }
    #endregion

    #region 오브젝트 반환
    private void ReleaseHistoryItem(HistoryItemUI item)
    {
        item.gameObject.SetActive(false);
        item.transform.SetParent(this.transform, false);
    }
    #endregion

    #region 오브젝트 획득
    private void GetHistoryItem(HistoryItemUI item)
    {
        item.gameObject.SetActive(true);
    }
    #endregion

    #region 오브젝트 파괴
    private void DestroyHistoryItem(HistoryItemUI item)
    {
        Destroy(item.gameObject);
    }
    #endregion

    #region 오브젝트 획득(콘텐츠 위치 이동 포함)
    private HistoryItemUI GetItemForTab(Transform parentContent)
    {
        HistoryItemUI item = itemPool.Get();

        item.transform.SetParent(parentContent, false);

        return item;
    }
    #endregion

    #endregion - 오브젝트 풀링 함수
}
