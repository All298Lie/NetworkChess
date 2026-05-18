using System.Collections.Generic;
using TMPro;
using UnityEngine;
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
    [Header("상단 탭 버튼")]
    [SerializeField] private ButtonTextColor recentTabBtn;
    [SerializeField] private ButtonTextColor favoriteTabBtn;
    [SerializeField] private ButtonTextColor searchTabBtn;

    [Header("Recent 탭")]
    [SerializeField] private GameObject recentUI;
    [SerializeField] private Transform recentContent;

    [Header("Favorite 탭")]
    [SerializeField] private GameObject favoriteUI;
    [SerializeField] private Transform favoriteContent;

    [Header("Search 탭")]
    [SerializeField] private GameObject searchUI;
    [SerializeField] private TMP_InputField search;
    [SerializeField] private Button searchBtn;

    [Header("프리팹")]
    [SerializeField] private GameObject historyItemPrefab;

    private List<LocalHistoryData> cachedRecentList;
    private List<LocalHistoryData> cachedFavoriteList;

    private ReplayTab currentTab = ReplayTab.None;

    void Start()
    {
        // 1. 파일 불러오기
        this.cachedRecentList = new List<LocalHistoryData>();

        // 2. 버튼 연결
        this.recentTabBtn.button.onClick.AddListener(() => SetTab(ReplayTab.Recent));
        this.favoriteTabBtn.button.onClick.AddListener(() => SetTab(ReplayTab.Favorite));
        this.searchTabBtn.button.onClick.AddListener(() => SetTab(ReplayTab.Search));

        SetTab(ReplayTab.Recent);
    }

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
}
