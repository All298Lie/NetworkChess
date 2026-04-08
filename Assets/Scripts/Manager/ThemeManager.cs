using System;
using UnityEngine;

public class ThemeManager : MonoBehaviour
{
    public static ThemeManager Instance;

    [Header("현재 적용된 테마")]
    [SerializeField] PieceThemeData currentPieceTheme;
    [SerializeField] BoardThemeData currentBoardTheme;

    public PieceThemeData CurrentPieceTheme
    {
        get { return currentPieceTheme; }
    }

    public BoardThemeData CurrentBoardTheme
    {
        get { return currentBoardTheme; }
    }

    public static event Action OnPieceThemeChanged;
    public static event Action OnBoardThemeChanged;

    // 오브젝트가 생성된 즉시 호출되는 함수
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Debug.LogWarning("테마매니저가 이미 존재합니다.");
            Destroy(this);
        }
    }

    // 설정 화면에서 기물 테마를 바꿀 때 호출되는 함수
    public void ChangePieceTheme(PieceThemeData newTheme)
    {
        this.currentPieceTheme = newTheme;

        OnPieceThemeChanged?.Invoke();
    }

    // 설정 화면에서 보드 테마를 바꿀 때 호출되는 함수
    public void ChangeBoardTheme(BoardThemeData newTheme)
    {
        this.currentBoardTheme = newTheme;

        OnBoardThemeChanged?.Invoke();
    }
}
