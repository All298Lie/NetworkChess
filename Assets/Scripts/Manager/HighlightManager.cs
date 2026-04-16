using System.Collections.Generic;
using UnityEngine;

public class HighlightManager : MonoBehaviour
{
    public static HighlightManager Instance { get; private set; }

    private List<Vector2Int> highlightedTiles; // 이동  가능 영역
    private List<Vector2Int> lastMoveTiles; // 최근 이동 흔적
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Debug.LogWarning("이미 하이라이트 매니저가 존재합니다.");
            Destroy(gameObject);
        }

        this.highlightedTiles = new List<Vector2Int>();
        this.lastMoveTiles = new List<Vector2Int>();
    }

    private void SetMoveHighlight(Vector2Int tilePos, bool show, bool isCapture)
    {
        if (MoveValidator.IsOnBoard(tilePos) == false) return;

        Tile tile = BoardManager.Instance.GetTile(tilePos);

        if (tile != null)
        {
            tile.SetMoveHighlight(show, isCapture);
        }
    }

    // 이동/공격 하이라이트를 켜주는 함수
    public void ShowMoveHighlights(Piece piece, List<Vector2Int> legalMoves)
    {
        foreach (Vector2Int pos in legalMoves)
        {
            bool isCapture = (BoardManager.Instance.Board[pos.x, pos.y] != null);

            if (BoardManager.Instance.EnPassant.HasValue && BoardManager.Instance.EnPassant.Value == pos && piece.Data.type == PieceType.Pawn)
            {
                isCapture = true;
            }

            SetMoveHighlight(pos, true, isCapture);

            this.highlightedTiles.Add(pos);
        }
    }

    // 이동/공격 하이라이트를 꺼주는 함수
    public void HideMoveHighlights()
    {
        foreach (Vector2Int pos in this.highlightedTiles)
        {
            SetMoveHighlight(pos, false, false);
        }

        this.highlightedTiles.Clear();
    }

    // 최근 이동 위치를 나타내는 하이라이트를 업데이트 해주는 함수
    public void UpdateLastMoveHighlight(Vector2Int fromPos, Vector2Int toPos)
    {
        // 1. 기존 흔적 지우기
        foreach (Vector2Int pos in this.lastMoveTiles)
        {
            Tile tile = BoardManager.Instance.GetTile(pos);

            if (tile != null)
            {
                tile.SetLastMoveHighlight(false);
            }
        }

        this.lastMoveTiles.Clear();

        // 2. 새로운 흔적 표시
        Tile fromTile = BoardManager.Instance.GetTile(fromPos);
        Tile toTile = BoardManager.Instance.GetTile(toPos);

        if (fromTile != null)
        {
            fromTile.SetLastMoveHighlight(true);
            this.lastMoveTiles.Add(fromPos);
        }

        if (toTile != null)
        {
            toTile.SetLastMoveHighlight(true);
            this.lastMoveTiles.Add(toPos);
        }
    }
}
