using UnityEngine;

public class Piece : MonoBehaviour
{
    [Header("기물 속성")]
    [SerializeField] SpriteRenderer spriteRenderer;

    public PieceData Data { get; private set; }

    public Vector2Int CurrentPosition { get; private set; }

    public bool IsWhite { get; private set; }
    public bool hasMoved;

    // 객체가 활성화되면 작동하는 함수
    void OnEnable()
    {
        if (ThemeManager.Instance != null) // 테마 매니저가 존재할 경우, 이벤트 구독
        {
            ThemeManager.OnPieceThemeChanged += RefreshSprite;
        }
    }

    // 객체가 비활성화되면 작동하는 함수
    void OnDisable()
    {
        if (ThemeManager.Instance != null) // 테마 매니저가 존재할 경우, 이벤트 구독 해제
        {
            ThemeManager.OnPieceThemeChanged -= RefreshSprite;
        }
    }

    // 기물 Sprite를 새로고침 하는 함수
    private void RefreshSprite()
    {
        if (this.Data == null || ThemeManager.Instance == null) return;

        PieceThemeData activeTheme = ThemeManager.Instance.CurrentPieceTheme;

        if (activeTheme != null)
        {
            spriteRenderer.sprite = activeTheme.GetSprite(this.Data.type, this.IsWhite);
        }
    }

    // BoardManager가 기물을 스폰할 때 딱 한 번 호출하는 함수
    public void Setup(PieceData data, bool isWhite, Vector2Int startPos)
    {
        this.Data = data;
        this.IsWhite = isWhite;
        this.CurrentPosition = startPos;
        this.hasMoved = false;

        RefreshSprite(); // 생성 즉시 테마에 맞는 기물 Sprite 입히기
    }

    // 기물을 이동할 때 호출되는 함수
    public void MoveTo(Vector2Int newArrayPos, Vector3 newWorldPos)
    {
        this.CurrentPosition = newArrayPos;

        transform.position = newWorldPos;
    }
}
