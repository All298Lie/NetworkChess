using UnityEngine;

public class Piece : MonoBehaviour
{
    [SerializeField] SpriteRenderer renderer;

    PieceData data;

    Vector2Int currentPosition;

    bool isWhite;
    bool hasMoved;


    // 기물 Sprite를 새로고침 하는 함수
    //private void RefreshSprite()
    //{
    //    if (data == null || ThemeManager.Instance == null) return;
        
    //    PieceThemeData activeTheme = ThemeManager.Instance.currentPieceTheme;
    //    if (activeTheme != null)
    //    {
    //        renderer.sprite = activeTheme.GetSprite(this.data.type, this.isWhite);
    //    }
    //}

    // BoardManager가 기물을 스폰할 때 딱 한 번 호출하는 함수
    public void Setup(PieceData data, bool isWhite, Vector2Int startPos)
    {
        this.data = data;
        this.isWhite = isWhite;
        this.currentPosition = startPos;
        this.hasMoved = false;

        // RefreshSprite(); // 생성 즉시 테마에 맞는 기물 Sprite 입히기
    }

    // 기물을 이동할 때 호출되는 함수
    public void MoveTo(Vector2Int newArrayPos, Vector3 newWorldPos)
    {
        this.currentPosition = newArrayPos;
        hasMoved = true;

        transform.position = newWorldPos;
    }
}
