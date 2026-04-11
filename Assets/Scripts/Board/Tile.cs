using UnityEngine;

public class Tile : MonoBehaviour
{
    Vector2Int arrayPosition;
    public Vector2Int ArrayPosition
    {
        get { return arrayPosition; }
    }

    // 보드매니저로 생성할 때 딱 한 번 호출하는 함수
    public void Setup(int x, int y)
    {
        this.arrayPosition = new Vector2Int(x, y);
        gameObject.name = $"Tile_{x}_{y}";
    }
}
