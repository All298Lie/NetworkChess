using NetworkChess.Core;
using Newtonsoft.Json;
using NUnit.Framework;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

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
    public void SaveToRecentHistory(S2C_GameOverNoti noti)
    {
        List<S2C_GameOverNoti> historyList = LoadRecentHistory();

        historyList.Insert(0, noti);

        if (historyList.Count > MAT_HISTORY_COUNT)
        {
            historyList.RemoveAt(MAT_HISTORY_COUNT - 1);
        }
    }
    #endregion

    #region 저장된 파일 읽어오기
    public List<S2C_GameOverNoti> LoadRecentHistory()
    {
        // 1. 파일이 없으면 빈 리스트 반환
        if (File.Exists(recentHistoryFilePath) == false)
        {
            return new List<S2C_GameOverNoti>();
        }

        // 2. 파일이 있으면 읽어서 리스트로 역직렬화
        string json = File.ReadAllText(recentHistoryFilePath);
        List<S2C_GameOverNoti> historyList = JsonConvert.DeserializeObject<List<S2C_GameOverNoti>>(json);

        return historyList ?? new List<S2C_GameOverNoti>();
    }
    #endregion
}
