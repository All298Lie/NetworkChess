using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.InputSystem;

public class BoardManager : MonoBehaviour
{
    public static BoardManager Instance { get; private set; }

    [Header("게임 에셋")]
    [SerializeField] GameObject piecePrefab;
    [SerializeField] PieceData[] pieceDatas;
    [SerializeField] GameObject boardPrefab;
    [SerializeField] GameObject tilePrefab;

    Dictionary<PieceType, PieceData> pieceDic;

    [Header("보드판 세팅")]
    [SerializeField] Vector2 a1Position;
    [SerializeField] float tileSize;

    public Piece[,] Board { get; private set; }

    public Vector2Int? enPassant;
    Vector2Int whiteKing;
    Vector2Int blackKing;

    // FEN 표기법을 통해 초기 보드판 세팅 상태 설정
    const string START_FEN = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR";

    [Header("Input 시스템")]
    private readonly Vector3 boardOffset = new Vector3(-3.5f, -3.5f, 0.0f);

    InputState inputState;

    private Piece selectedPiece;
    private Vector2Int dragStartTile;

    private Camera mainCamera;

    [Header("커서 설정")]
    [SerializeField] Texture2D defaultCursor; // 기본 동작
    [SerializeField] Texture2D hoverCursor; // 선택 가능 동작
    [SerializeField] Texture2D grabCursor; // 잡기 동작

    private readonly Vector2Int hotSpot = new Vector2Int(8, 8);

    // 오브젝트가 생성된 즉시 호출되는 함수
    void Awake()
    {
        if (Instance == null) // 싱글톤 패턴 디자인
        {
            Instance = this;
        }
        else
        {
            Debug.LogWarning("보드 매니저가 이미 존재합니다.");
            Destroy(gameObject);
        }

        mainCamera = Camera.main;

        inputState = InputState.None;

        this.Board = new Piece[8, 8];

        this.enPassant = null;

        this.pieceDic = new Dictionary<PieceType, PieceData>();
        foreach (PieceData pieceData in pieceDatas)
        {
            PieceType type = pieceData.type;
            if (this.pieceDic.ContainsKey(type) == false)
            {
                this.pieceDic.Add(type, pieceData);
            }
        }

        GenerateTiles();
        InitializeBoard(START_FEN);
    }

    void Update()
    {
        if (GameManager.Instance == null || GameManager.Instance.ActiveMode == null) return;

        HandleInput();
        UpdateCursorState();
    }

    // FEN 기보법을 통해 표기된 문자열을 통해 보드판 세팅 
    private void InitializeBoard(string fen)
    {
        int x = 0;
        int y = 7;

        GameObject piecesObject = new GameObject("Pieces");

        // FEN 기보법 확인
        foreach (char c in fen)
        {
            if (c == '/') // '/'의 경우, 다음 줄로 넘김 표시
            {
                x = 0;
                y = y - 1;
            }
            else if (char.IsDigit(c)) // 숫자일 경우 해당 칸만큼 빈 공간 표시
            {
                x = x + (c - '0');
            }
            else // 영문자일 경우 해당 기물 표시
            {
                bool isWhite = char.IsUpper(c); // 대문자일 경우 백 진영
                PieceType type = GetPieceTypeFromChar(c);

                SpawnPiece(piecesObject.transform, type, isWhite, x, y);
                x = x + 1;
            }
        }
    }

    // 기물에 맞는 열거형을 반환하는 함수
    private PieceType GetPieceTypeFromChar(char c)
    {
        switch (char.ToLower(c))
        {
            case 'n':
                return PieceType.Knight;

            case 'b':
                return PieceType.Bishop;

            case 'r':
                return PieceType.Rook;

            case 'q':
                return PieceType.Queen;

            case 'k':
                return PieceType.King;

            case 'p':
            default:
                return PieceType.Pawn;
        }
    }

    // 기물을 보드판에 배치하는 함수
    private void SpawnPiece(Transform parent, PieceType type, bool isWhite, int x, int y)
    {
        if (this.pieceDic.ContainsKey(type) == false) return;

        PieceData data = this.pieceDic[type];

        Vector3 worldPos = GetWorldPosition(x, y);

        GameObject pieceObject = Instantiate(piecePrefab, worldPos, Quaternion.identity, parent);
        pieceObject.name = $"{(isWhite ? "White" : "Black")}_{data.name}";
        Piece newPiece = pieceObject.GetComponent<Piece>();

        newPiece.Setup(data, isWhite, new Vector2Int(x, y));

        this.Board[x, y] = newPiece;

        if (type == PieceType.King)
        {
            if (isWhite == true)
            {
                this.whiteKing = new Vector2Int(x, y);
            }
            else
            {
                this.blackKing = new Vector2Int(x, y);
            }
        }
    }

    // 타일을 생성하는 함수
    private void GenerateTiles()
    {
        GameObject boardObject = Instantiate(this.boardPrefab, Vector3.zero, Quaternion.identity);

        boardObject.name = "Board";

        for (int x = 0; x < 8; x++)
        {
            for (int y = 0; y < 8; y++)
            {
                GameObject tileObject = Instantiate(this.tilePrefab, GetWorldPosition(x, y), Quaternion.identity, boardObject.transform);
                
                Tile tile = tileObject.GetComponent<Tile>();
                tile.Setup(x, y);
            }
        }
    }

    private void UpdateCursorState()
    {
        if (this.inputState == InputState.Selected || Mouse.current.leftButton.isPressed == true)
        {
            Cursor.SetCursor(grabCursor, hotSpot, CursorMode.Auto);

            return;
        }

        Vector2Int tilePos = GetTilePosFromMouse();

        if (MoveValidator.IsOnBoard(tilePos) == true)
        {
            Piece hoveredPiece = Board[tilePos.x, tilePos.y];

            if (hoveredPiece != null && hoveredPiece.IsWhite == GameManager.Instance.ActiveMode.IsWhiteTurn)
            {
                Cursor.SetCursor(hoverCursor, hotSpot, CursorMode.Auto);

                return;
            }
        }

        Cursor.SetCursor(defaultCursor, hotSpot, CursorMode.Auto);
    }

    private void HandleInput()
    {
        Vector2Int tilePos = GetTilePosFromMouse();

        // 1. 마우스 클릭을 한 상태
        if (Mouse.current.leftButton.wasPressedThisFrame == true)
        {
            if (MoveValidator.IsOnBoard(tilePos) == true)
            {
                Piece clickedPiece = this.Board[tilePos.x, tilePos.y];

                if (clickedPiece != null && clickedPiece.IsWhite == GameManager.Instance.ActiveMode.IsWhiteTurn)
                {
                    this.selectedPiece = clickedPiece;
                    this.dragStartTile = tilePos;
                    this.inputState = InputState.Dragging;

                    //  TODO : 이동 가능 경로 하이라이트 켜기
                }
                else if (this.inputState == InputState.Selected)
                {
                    TryMovePiece(this.selectedPiece, tilePos);
                }
                else
                {
                    ClearSelection();
                }
            }
            else
            {
                ClearSelection();
            }
        }

        // 2. 마우스 클릭을 누르고 있는 상태
        if (inputState == InputState.Dragging && Mouse.current.leftButton.isPressed == true)
        {
            Vector3 mouseWorldPos = GetMouseWorldPosition();

            this.selectedPiece.transform.position = new Vector3(mouseWorldPos.x, mouseWorldPos.y, -1f);
        }

        // 3. 마우스 클릭을 뗀 상태
        if (Mouse.current.leftButton.wasReleasedThisFrame == true && inputState == InputState.Dragging)
        {
            if (tilePos == this.dragStartTile)
            {
                this.inputState = InputState.Selected;

                CancelPieceMove(this.selectedPiece);
            }
            else
            {
                TryMovePiece(this.selectedPiece, tilePos);
            }
        }
    }

    // 기물 이동을 시도하는 함수
    private void TryMovePiece(Piece piece, Vector2Int targetPos)
    {
        if (MoveValidator.IsOnBoard(targetPos) == true)
        {
            GameManager.Instance.ActiveMode.HandlePieceMoveRequest(piece, targetPos);
        }
        else
        {
            CancelPieceMove(piece);
        }

        ClearSelection();
    }

    // 기물 이동 가능 타일 표현 및 선택 판정 기물을 초기화하는 함수
    private void ClearSelection()
    {
        this.selectedPiece = null;
        this.inputState = InputState.None;

        // TODO : 켜져있는 타일 하이라이트 끄기
    }

    private Vector3 GetMouseWorldPosition()
    {
        Vector3 mouseScreenPos = Mouse.current.position.ReadValue();
        mouseScreenPos.z = Mathf.Abs(this.mainCamera.transform.position.z);

        return this.mainCamera.ScreenToWorldPoint(mouseScreenPos);
    }

    private Vector2Int GetTilePosFromMouse()
    {
        Vector3 worldPos = GetMouseWorldPosition();

        int x = Mathf.FloorToInt(worldPos.x - this.boardOffset.x + 0.5f);
        int y = Mathf.FloorToInt(worldPos.y - this.boardOffset.y + 0.5f);

        return new Vector2Int(x, y);
    }

    // x, y 값 기준 객체가 존재해야할 월드 포지션을 가져오는 함수
    public Vector3 GetWorldPosition(int x, int y)
    {
        return new Vector3(this.a1Position.x + (x * this.tileSize), this.a1Position.y + (y * this.tileSize), 0.0f);
    }

    // 킹 좌표가 갱신되었을 때 작동하는 함수
    public void UpdateKingPosition(Vector2Int pos, bool isWhite)
    {
        if (isWhite == true)
        {
            this.whiteKing = pos;
        }
        else
        {
            this.blackKing = pos;
        }
    }

    // 킹의 현재 좌표를 확인하는 함수
    public Vector2Int GetKingPosition(bool isWhite)
    {
        return isWhite ? this.whiteKing : this.blackKing;
    }

    // 보드에서 기물을 이동 처리하는 함수
    public void ExecuteMoveOnBoard(Piece piece, Vector2Int targetPos)
    {
        // 1. 기존 타일에 기물 비우기
        Vector2Int originPos = piece.CurrentPosition;

        this.Board[originPos.x, originPos.y] = null;

        // 2. 도착지에 적 기물이 있으면 파괴
        Piece targetPiece = this.Board[targetPos.x, targetPos.y];
        if (targetPiece != null)
        {
            Destroy(targetPiece.gameObject);
        }

        // 3. 배열 데이터 갱신
        this.Board[targetPos.x, targetPos.y] = piece;

        // 4. 기물의 실제 이동 처리
        piece.MoveTo(targetPos, GetWorldPosition(targetPos.x, targetPos.y));

        // 5. 해당 기물이 킹일 경우, 킹 위치 갱신
        if (piece.Data.type == PieceType.King)
        {
            UpdateKingPosition(targetPos, piece.IsWhite);
        }
    }

    // 폰을 프로모션 처리하는 함수
    public void promotePawn(Piece pawn, PieceType type)
    {
        if (this.pieceDic.ContainsKey(type) == true)
        {
            pawn.Setup(this.pieceDic[type], pawn.IsWhite, pawn.CurrentPosition);

            pawn.gameObject.name = $"{(pawn.IsWhite ? "White" : "Black")}_{this.pieceDic[type].name}";

            Debug.Log("폰이 무사히 승급했습니다!");
        }
    }

    // 기물 이동을 취소 처리하는 함수
    public void CancelPieceMove(Piece piece)
    {
        Vector3 originalWorldPos = GetWorldPosition(piece.CurrentPosition.x, piece.CurrentPosition.y);
        piece.MoveTo(piece.CurrentPosition, originalWorldPos);
    }
}
