using System.Collections.Generic;
using UnityEngine;

public class GlobalSettingsManager : MonoBehaviour
{
    public static GlobalSettingsManager Instance { get; private set; }

    [Header("비디오 설정")]
    private readonly int[] widthList = {1920, 1600, 1280};
    private readonly int[] heightList = { 1080, 900, 720};

    [Header("테마 설정")]
    [SerializeField] private BoardThemeData[] boardThemeData;
    [SerializeField] private PieceThemeData[] pieceThemeData;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(transform.root.gameObject);
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
        Screen.SetResolution(this.widthList[index], this.heightList[index], isFullscreen);

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
