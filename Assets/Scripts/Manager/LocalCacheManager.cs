using NetworkChess.Core;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
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
    private const int MAT_HISTORY_COUNT = 20;

    #region Awake 함수
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            this.recentHistoryFilePath = Path.Combine(Application.persistentDataPath, "RecentHistory.json");
            CLog.Log($"[로컬 캐시] 전적 저장 경로 : {this.recentHistoryFilePath}");
        }
        else
        {
            Destroy(gameObject);
        }
    }
    #endregion

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

        if (historyList.Count > MAT_HISTORY_COUNT)
        {
            historyList.RemoveAt(MAT_HISTORY_COUNT - 1);
        }

        // 4. 파일 저장
        string json = JsonConvert.SerializeObject(historyList, Formatting.Indented);
        File.WriteAllText(recentHistoryFilePath, json);
    }
    #endregion

    #region 저장된 파일 읽어오기
    public List<LocalHistoryData> LoadRecentHistory()
    {
        // 1. 파일이 없으면 빈 리스트 반환
        if (File.Exists(recentHistoryFilePath) == false)
        {
            return new List<LocalHistoryData>();
        }

        // 2. 파일이 있으면 읽어서 리스트로 역직렬화
        string json = File.ReadAllText(recentHistoryFilePath);
        List<LocalHistoryData> historyList = JsonConvert.DeserializeObject<List<LocalHistoryData>>(json);

        return historyList ?? new List<LocalHistoryData>();
    }
    #endregion
}
