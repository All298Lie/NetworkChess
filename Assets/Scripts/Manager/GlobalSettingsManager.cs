using System.Collections.Generic;
using UnityEngine;

public class GlobalSettingsManager : MonoBehaviour
{
    public static GlobalSettingsManager Instance { get; private set; }

    [Header("비디오 설정")]
    public List<Resolution> AvilableResolutions { get; private set; }

    [Header("테마 설정")]
    [SerializeField] private BoardThemeData[] boardThemeData;
    [SerializeField] private PieceThemeData[] pieceThemeData;

    #region + 유니티 함수

    #region Awake 함수
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(transform.root.gameObject);

            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = 60;

            this.AvilableResolutions = new List<Resolution>();

            InitResolutions();
            LoadAndApplyVideoSettings();
        }
        else
        {
            Destroy(gameObject);
        }
    }
    #endregion

    #region Start 함수
    void Start()
    {
        LoadAndApplyThemeSettings();
    }
    #endregion

    #endregion - 유니티 함수

    #region 해상도 초기화
    private void InitResolutions()
    {
        Resolution[] allResolutions = Screen.resolutions;
        this.AvilableResolutions.Clear();

        // 1. 현재 해상도 확인
        int currentMonitorWidth = Screen.currentResolution.width;
        int currentMonitorHeight = Screen.currentResolution.height;
        bool isVerticalMonitor = currentMonitorWidth < currentMonitorHeight;

        // 2. 유니티에서 지원하는 16:9 해상도 추출
        float targetRatio = 16f / 9f;
        int minWidth = isVerticalMonitor ? 800 : 1200;

        foreach (Resolution res in allResolutions)
        {
            if (res.width > currentMonitorWidth) continue;

            if (res.width >= minWidth) // 창 최소 크기
            {
                // 오차범위 0.05 이내 16:9 비율이 아닌 해상도도 포함
                float currentRatio = (float)res.width / res.height;
                if (Mathf.Abs(currentRatio - targetRatio) < 0.05f)
                {
                    if (this.AvilableResolutions.Exists(x => x.width == res.width && x.height == res.height) == false)
                    {
                        this.AvilableResolutions.Add(res);
                    }
                }
            }
        } // foreach 문 끝점

        // 3. 울트라와이드 모니터를 위해 표준 16:9 해상도 수동 주입
        int[] standardWidths = { 1280, 1366, 1600, 1920, 2560 };
        foreach (int width in standardWidths)
        {
            int height = Mathf.RoundToInt(width / targetRatio);
            if (width <= currentMonitorWidth && height <= currentMonitorHeight)
            {
                if (this.AvilableResolutions.Exists(x => (x.width == width && x.height == height)) == false)
                {
                    Resolution res = new Resolution();

                    res.width = width;
                    res.height = height;

                    this.AvilableResolutions.Add(res);
                }
            }
        }

        // 4. 사용가능한 해상도 목록이 없을 경우, 높이를 기준으로 생성
        if (this.AvilableResolutions.Count == 0)
        {
            Resolution fallbackRes = new Resolution();

            fallbackRes.width = Mathf.Max(800, currentMonitorWidth - 100);
            fallbackRes.height = Mathf.RoundToInt(fallbackRes.width / targetRatio);

            if (fallbackRes.width > currentMonitorWidth)
            {
                fallbackRes.width = currentMonitorWidth - 50;
                fallbackRes.height = Mathf.RoundToInt(fallbackRes.width / targetRatio);
            }

            this.AvilableResolutions.Add(fallbackRes);

            CLog.Log($"[비디오 설정] 지원하는 16:9 해상도가 없어 {fallbackRes.width}x{fallbackRes.height} 강제 추가");
        }

        // 해상도 크기에 맞게 정렬
        this.AvilableResolutions.Sort((a, b) => b.width.CompareTo(a.width));
    }
    #endregion

    #region 기존에 설정한 환경설정을 불러오고 적용하는 함수
    private void LoadAndApplyVideoSettings()
    {
        int resIndex = PlayerPrefs.GetInt("ResIndex", 0);
        bool isFullscreen = PlayerPrefs.GetInt("Fullscreen", 0) == 1;

        ApplyResolution(resIndex, isFullscreen);
    }
    #endregion

    #region 기존에 설정한 테마를 불러오고 적용하는 함수
    private void LoadAndApplyThemeSettings()
    {
        int boardIndex = PlayerPrefs.GetInt("BoardTheme", 0);
        int pieceIndex = PlayerPrefs.GetInt("PieceTheme", 0);

        ApplyTheme(boardIndex, pieceIndex);
    }
    #endregion

    #region 해상도 조정하는 함수
    public void ApplyResolution(int index, bool isFullscreen)
    {
        if (index < 0 || index >= this.AvilableResolutions.Count)
        {
            index = 0;
        }

        int currentMonitorWidth = Screen.currentResolution.width;
        int currentMonitorHeight = Screen.currentResolution.height;

        bool isVerticalMonitor = currentMonitorWidth < currentMonitorHeight;
        bool isUltraWideMonitor = ((float)currentMonitorWidth / currentMonitorHeight) >= 2.0f;

        if (isVerticalMonitor == true || isUltraWideMonitor == true)
        {
            isFullscreen = false;
        }

        Resolution selectedRes = this.AvilableResolutions[index];
        FullScreenMode mode = isFullscreen == true ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;

        Screen.SetResolution(selectedRes.width, selectedRes.height, mode);

        PlayerPrefs.SetInt("ResIndex", index);
        PlayerPrefs.SetInt("Fullscreen", isFullscreen ? 1 : 0);
        PlayerPrefs.Save();
    }
    #endregion

    #region 테마 적용 함수 추가
    public void ApplyTheme(int boardThemeIndex, int pieceThemeIndex)
    {
        if (ThemeManager.Instance != null)
        {
            if (this.boardThemeData.Length > boardThemeIndex)
            {
                ThemeManager.Instance.ChangeBoardTheme(this.boardThemeData[boardThemeIndex]);
            }
            
            if (this.pieceThemeData.Length > pieceThemeIndex)
            {
                ThemeManager.Instance.ChangePieceTheme(this.pieceThemeData[pieceThemeIndex]);
            }
        }

        PlayerPrefs.SetInt("BoardTheme", boardThemeIndex);
        PlayerPrefs.SetInt("PieceTheme", pieceThemeIndex);
        PlayerPrefs.Save();
    }
    #endregion
}
