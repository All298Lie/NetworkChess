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

    void Start()
    {
        LoadAndApplyThemeSettings();
    }

    private void InitResolutions()
    {
        Resolution[] allResolutions = Screen.resolutions;
        this.AvilableResolutions.Clear();

        // 16 : 9 비율 계산
        float targetRatio = 16f / 9f;

        foreach (Resolution res in allResolutions)
        {
            if (res.width >= 1280) // 창 최소 크기
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

        // 해상도 크기에 맞게 정렬
        this.AvilableResolutions.Sort((a, b) => b.width.CompareTo(a.width));
    }
    
    // 기존에 설정한 환경설정을 불러오고 적용하는 함수
    private void LoadAndApplyVideoSettings()
    {
        int resIndex = PlayerPrefs.GetInt("ResIndex", 0);
        bool isFullscreen = PlayerPrefs.GetInt("Fullscreen", 1) == 1;

        ApplyResolution(resIndex, isFullscreen);
    }

    // 기존에 설정한 테마를 불러오고 적용하는 함수
    private void LoadAndApplyThemeSettings()
    {
        int boardIndex = PlayerPrefs.GetInt("BoardTheme", 0);
        int pieceIndex = PlayerPrefs.GetInt("PieceTheme", 0);

        ApplyTheme(boardIndex, pieceIndex);
    }

    // 해상도 조정하는 함수
    public void ApplyResolution(int index, bool isFullscreen)
    {
        if (index < 0 || index >= this.AvilableResolutions.Count) return;

        Resolution selectedRes = this.AvilableResolutions[index];
        FullScreenMode mode = isFullscreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;

        Screen.SetResolution(selectedRes.width, selectedRes.height, mode);

        PlayerPrefs.SetInt("ResIndex", index);
        PlayerPrefs.SetInt("Fullscreen", isFullscreen ? 1 : 0);
        PlayerPrefs.Save();
    }

    // TODO : 사운드, 효과음 추가 시 오디오 설정하는 함수 및 객체 추가

    // 테마 적용 함수 추가
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
}
