using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

public class HighlightManager : MonoBehaviour
{
    public static HighlightManager Instance { get; private set; }

    [Header("프리팹")]
    [SerializeField] private GameObject arrowPrefab; 

    private List<Vector2Int> highlightedTiles; // 이동  가능 영역
    private List<Vector2Int> lastMoveTiles; // 최근 이동 흔적
    private List<Vector2Int> selectHighlightTiles; // 선택 하이라이트
    private Dictionary<(Vector2Int, Vector2Int), Arrow> activeArrows; // 화살표 어노테이션

    private ObjectPool<Arrow> arrowPool;
    
    private Vector2Int startPos;
    
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
        this.selectHighlightTiles = new List<Vector2Int>();
        this.activeArrows = new Dictionary<(Vector2Int, Vector2Int), Arrow>();

        this.arrowPool = new ObjectPool<Arrow>(OnCreateArrow, OnGetArrow, OnReleaseArrow);

        this.startPos = new Vector2Int(-1, -1);
    }

    // 오브젝트 풀링 : 가져오기 함수
    private void OnGetArrow(Arrow arrow)
    {
        arrow.gameObject.SetActive(true);
    }

    // 오브젝트 풀링 : 반환 함수
    private void OnReleaseArrow(Arrow arrow)
    {
        arrow.Clear();
    }

    // 오브젝트 풀링 : 생성 함수
    private Arrow OnCreateArrow()
    {
        GameObject arrowObject = Instantiate(this.arrowPrefab, Vector3.zero, Quaternion.identity, transform);
        arrowObject.name = "AnnotationArrow";
        Arrow arrow = arrowObject.GetComponent<Arrow>();

        return arrow;
    }

    // 이동/공격 하이라이트를 상태에 맞게 켜주는 함수
    private void SetMoveHighlight(Vector2Int tilePos, bool show, bool isCapture)
    {
        if (MoveValidator.IsOnBoard(tilePos) == false) return;

        Tile tile = BoardManager.Instance.GetTile(tilePos);

        if (tile != null)
        {
            tile.SetMoveHighlight(show, isCapture);
        }
    }

    // 선택 하이라이트 토글 함수
    private void ToggleSelectHighlight(Vector2Int tilePos)
    {
        if (MoveValidator.IsOnBoard(tilePos) == false) return;

        Tile tile = BoardManager.Instance.GetTile(tilePos);

        if (tile != null)
        {
            tile.ToggleSelectHighlight();
        }
    }

    // 타일 하이라이트를 숨기는 함수
    private void HideSelectHighlight(Vector2Int tilePos)
    {
        if (MoveValidator.IsOnBoard(tilePos) == false) return;

        Tile tile = BoardManager.Instance.GetTile(tilePos);

        if (tile != null)
        {
            tile.HideSelectHighlight();
        }
    }

    // 타일 하이라이트, 어노테이션 화살표 초기화하는 함수
    private void ClearHighlight()
    {
        // 1. 선택 하이라이트 제거
        foreach (Vector2Int tilePos in this.selectHighlightTiles)
        {
            HideSelectHighlight(tilePos);
        }
        this.selectHighlightTiles.Clear();

        // 2. 어노테이션 화살표 제거
        foreach (Arrow arrow in this.activeArrows.Values)
        {
            this.arrowPool.Release(arrow);
        }
        this.activeArrows.Clear();
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

    // 어노테이션 화살표를 업데이트하는 함수
    public void UpdateAnnotationArrow(Vector2Int startPos, Vector2Int endPos)
    {
        // 이미 동일한 시작점, 끝점에 화살표가 있을 경우, 화살표 삭제
        if (this.activeArrows.ContainsKey((startPos, endPos)) == true)
        {
            this.arrowPool.Release(this.activeArrows[(startPos, endPos)]);
            this.activeArrows.Remove((startPos, endPos));

            return;
        }

        Arrow arrow = this.arrowPool.Get();
        this.activeArrows.Add((startPos, endPos), arrow);

        Vector3 startWorldPos = BoardManager.Instance.GetWorldPosition(startPos.x, startPos.y);
        Vector3 endWorldPos = BoardManager.Instance.GetWorldPosition(endPos.x, endPos.y);

        int dx = Mathf.Abs(endPos.x - startPos.x);
        int dy = Mathf.Abs(endPos.y - startPos.y);

        if ((dx == 2 && dy == 1) || (dx == 1 && dy == 2)) // 나이트 행마법일 경우
        {
            bool isFirstX = dx > dy;

            Vector2Int middlePos;
            if (isFirstX == true)
            {
                middlePos = new Vector2Int(endPos.x, startPos.y);
            }
            else
            {
                middlePos = new Vector2Int(startPos.x, endPos.y);
            }

            Vector3 middleWorldPos = BoardManager.Instance.GetWorldPosition(middlePos.x, middlePos.y);


            arrow.DrawArrow(new Vector3[] { startWorldPos, middleWorldPos, endWorldPos });
        }
        else // 일반 어노테이션 화살표
        {
            arrow.DrawArrow(new Vector3[] { startWorldPos, endWorldPos });
        }
    }

    // 좌클릭 시작 시 작동하는 함수
    public void OnLeftClickStarted(Vector2 screenPos)
    {
        Vector2Int tilePos = BoardManager.Instance.GetTilePosFromMouse(screenPos);

        if (MoveValidator.IsOnBoard(tilePos) == true)
        {
            ClearHighlight(); // 하이라이트, 어노테이션 화살표 초기화
        }
    }

    // 우클릭 시작 시 작동하는 함수
    public void OnRightClickStarted(Vector2 screenPos)
    {
        Vector2Int tilePos = BoardManager.Instance.GetTilePosFromMouse(screenPos);

        if (MoveValidator.IsOnBoard(tilePos) == true)
        {
            this.startPos = tilePos; // 화살표 시작지점 지정
        }
    }

    // 우클릭 취소 시 작동하는 함수
    public void OnRightClickCanceled(Vector2 screenPos)
    {
        Vector2Int tilePos = BoardManager.Instance.GetTilePosFromMouse(screenPos);

        if (MoveValidator.IsOnBoard(tilePos) == true)
        {
            if (this.startPos == tilePos) // 우클릭 시작점과 끝점이 같은 경우, 현재 위치 타일 하이라이트 활성화
            {
                ToggleSelectHighlight(tilePos);

                if (this.selectHighlightTiles.Contains(tilePos) == true)
                {
                    this.selectHighlightTiles.Remove(tilePos);
                }
                else
                {
                    this.selectHighlightTiles.Add(tilePos);
                }
            }
            else // 다를 경우, 시작점과 끝점을 잇는 어노테이션 화살표
            {
                UpdateAnnotationArrow(this.startPos, tilePos);
            }
        }
    }
}
