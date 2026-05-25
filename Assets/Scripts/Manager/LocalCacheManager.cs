using NetworkChess.Core;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

[System.Serializable]
public class LocalHistoryData
{
    public string MyNickname;
    public S2C_GameOverNoti MatchData;
}

public class LocalCacheManager : MonoBehaviour
{
    public static LocalCacheManager Instance { get; private set; }

    private string recentHistoryFilePath;
    private string favoriteHistoryFilePath;

    private const int MAX_HISTORY_COUNT = 20;
    private const int MAX_FAVORITE_COUNT = 50;

    #region Awake 함수
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(transform.root.gameObject);

            this.recentHistoryFilePath = Path.Combine(Application.persistentDataPath, "RecentHistory.json");
            this.favoriteHistoryFilePath = Path.Combine(Application.persistentDataPath, "FavoriteHistory.json");

            CLog.Log($"[로컬 캐시] 전적 저장 경로 : {this.recentHistoryFilePath}");
            CLog.Log($"[로컬 캐시] 즐겨찾기 저장 경로 : {this.favoriteHistoryFilePath}");
        }
        else
        {
            Destroy(gameObject);
        }
    }
    #endregion

    #region + 게임 전적 관련 함수

    #region 게임 결과 로컬 저장
    public void SaveToRecentHistory(S2C_GameOverNoti noti, string currentMyNickname)
    {
        // 1. 파일 불러오기
        List<LocalHistoryData> historyList = LoadRecentHistory();

        // 2. 데이터 랩핑
        LocalHistoryData newData = new LocalHistoryData();

        newData.MyNickname = currentMyNickname;
        newData.MatchData = noti;

        // 3. 리스트에 추가 및 개수 제한 적용
        historyList.Insert(0, newData);

        if (historyList.Count > MAX_HISTORY_COUNT)
        {
            historyList.RemoveAt(historyList.Count - 1);
        }

        // 4. 파일 저장
        string json = JsonConvert.SerializeObject(historyList, Formatting.Indented);
        File.WriteAllText(this.recentHistoryFilePath, json);
    }
    #endregion

    #region 저장된 전적 파일 읽어오기
    public List<LocalHistoryData> LoadRecentHistory()
    {
        // 1. 파일이 없으면 빈 리스트 반환
        if (File.Exists(this.recentHistoryFilePath) == false)
        {
            return new List<LocalHistoryData>();
        }

        // 2. 파일이 있으면 읽어서 리스트로 역직렬화
        string json = File.ReadAllText(this.recentHistoryFilePath);
        List<LocalHistoryData> historyList = JsonConvert.DeserializeObject<List<LocalHistoryData>>(json);

        return historyList ?? new List<LocalHistoryData>();
    }
    #endregion

    #endregion - 게임 전적 관련 함수

    #region 즐겨찾기 로컬 저장
    public void SaveFavoriteToFile(List<LocalHistoryData> favoriteList)
    {
        // 1. 파일 저장
        string json = JsonConvert.SerializeObject(favoriteList, Formatting.Indented);
        File.WriteAllText(this.favoriteHistoryFilePath, json);
    }
    #endregion

    #region 저장된 즐겨찾기 파일 읽어오기
    public List<LocalHistoryData> LoadFavoriteHistory()
    {
        // 1. 파일이 없으면 빈 리스트 반환
        if (File.Exists(this.favoriteHistoryFilePath) == false)
        {
            return new List<LocalHistoryData>();
        }

        // 2. 파일이 있으면 읽어서 리스트로 역직렬화
        string json = File.ReadAllText(this.favoriteHistoryFilePath);
        List<LocalHistoryData> favoriteList = JsonConvert.DeserializeObject<List<LocalHistoryData>>(json);

        return favoriteList ?? new List<LocalHistoryData>();
    }
    #endregion

    #region 즐겨찾기에 등록되어있는지 확인하는 함수
    public bool IsFavorite(string replayCode)
    {
        List<LocalHistoryData> favoriteList = LoadFavoriteHistory();
        return favoriteList.Any(x => x.MatchData.ReplayCode == replayCode);
    }
    #endregion

    #region 즐겨찾기 추가
    public void AddFavorite(LocalHistoryData data)
    {
        // 1. 즐겨찾기 불러오기
        List<LocalHistoryData> favoriteList = LoadFavoriteHistory();

        string code = data.MatchData.ReplayCode;

        // 2. 즐겨찾기에 이미 포함되어있는지 확인
        if (favoriteList.Any(x => x.MatchData.ReplayCode == code) == true) return;

        favoriteList.Insert(0, data);

        if (favoriteList.Count > MAX_FAVORITE_COUNT)
        {
            favoriteList.RemoveAt(favoriteList.Count - 1);
        }

        SaveFavoriteToFile(favoriteList);
        CLog.Log("[로컬 캐시] 즐겿자기 추가 완료");
    }
    #endregion

    #region 즐겨찾기 제거
    public void RemoveFavorite(string replayCode)
    {
        List<LocalHistoryData> favoriteList = LoadFavoriteHistory();

        // 1. 해당 코드를 가진 데이터를 리스트에서 제거
        int removedCount = favoriteList.RemoveAll(x => x.MatchData.ReplayCode == replayCode);

        if (removedCount > 0)
        {
            SaveFavoriteToFile(favoriteList);
            CLog.Log($"[로컬 캐시] 즐겨찾기 삭제 완료 : {replayCode}");
        }
    }
    #endregion
}
