using UnityEngine;

public class Board : MonoBehaviour
{
    [SerializeField] private SpriteRenderer spriteRenderer;

    // 객체가 활성화될 때 작동하는 함수
    void OnEnable()
    {
        if (ThemeManager.Instance != null) // 테마매니저가 존재할 경우, 이벤트 구독
        {
            ThemeManager.OnBoardThemeChanged += RefreshSprite;
        }

        RefreshSprite();
    }

    // 객체가 비활성화될 때 작동하는 함수
    void OnDisable()
    {
        if (ThemeManager.Instance != null) // 테마매니저가 존재할 경우, 이벤트 구독 해제
        {
            ThemeManager.OnBoardThemeChanged -= RefreshSprite;
        }
    }

    // 보드 sprite 새로고침하는 함수
    private void RefreshSprite()
    {
        if (ThemeManager.Instance == null) return;

        BoardThemeData currentTheme = ThemeManager.Instance.CurrentBoardTheme;

        if (currentTheme != null)
        {
            this.spriteRenderer.sprite = currentTheme.boardSprite;
        }
    }
}