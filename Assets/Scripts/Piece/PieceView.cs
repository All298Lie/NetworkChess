using UnityEngine;

using NetworkChess.Core;

public class PieceView : MonoBehaviour
{
    [Header("기물 속성")]
    [SerializeField] private SpriteRenderer spriteRenderer;

    [HideInInspector] public CorePiece LogicPiece;

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
        if (this.LogicPiece.Data == null || ThemeManager.Instance == null) return;

        PieceThemeData activeTheme = ThemeManager.Instance.CurrentPieceTheme;

        if (activeTheme != null)
        {
            this.spriteRenderer.sprite = activeTheme.GetSprite(this.LogicPiece.Data.type, this.LogicPiece.IsWhite);
        }
    }

    // BoardManager가 기물을 스폰할 때 딱 한 번 호출하는 함수
    public void Initialize(CorePiece logicPiece)
    {
        this.LogicPiece = logicPiece;

        RefreshSprite(); // 생성 즉시 테마에 맞는 기물 Sprite 입히기
    }

    // 기물을 이동할 때 호출되는 함수
    public void MoveTo(Vector3 newWorldPos)
    {
        transform.position = newWorldPos;
    }

    public void GrabPiece(bool isGrab)
    {
        if (isGrab == true)
        {
            spriteRenderer.sortingOrder = 3;
        }
        else
        {
            spriteRenderer.sortingOrder = 0;
        }
    }
}
