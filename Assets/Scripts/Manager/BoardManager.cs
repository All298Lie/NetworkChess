using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

using NetworkChess.Core;

public class BoardManager : MonoBehaviour
{
    public static BoardManager Instance { get; private set; }

    [Header("게임 에셋")]
    [SerializeField] private GameObject piecePrefab;
    [SerializeField] private PieceData[] pieceDatas;
    [SerializeField] private GameObject boardPrefab;
    [SerializeField] private GameObject tilePrefab;

    private Dictionary<PieceType, PieceData> pieceDic;

    [Header("보드판 세팅")]
    [SerializeField] private Vector2 a1Position;
    [SerializeField] private float tileSize;

    public CorePiece[,] Board { get; private set; }
    private Tile[,] tiles;
    private Dictionary<CorePiece, PieceView> pieceViewMap;

    // FEN 표기법을 통해 초기 보드판 세팅 상태 설정
    private const string START_FEN = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR";

    [Header("Input 시스템")]
    private BoardPos dragStartTile;
    private InputState inputState;

    private CorePiece selectedPiece;
    private Camera mainCamera;

    private bool isSelected;

    [Header("커서 설정")]
    [SerializeField] Texture2D defaultCursor; // 기본 동작
    [SerializeField] Texture2D hoverCursor; // 선택 가능 동작
    [SerializeField] Texture2D grabCursor; // 잡기 동작

    private readonly Vector2Int hotSpot = new Vector2Int(8, 8);

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

        this.mainCamera = Camera.main;

        this.inputState = InputState.None;

        this.Board = new CorePiece[8, 8];
        this.tiles = new Tile[8, 8];
        this.pieceViewMap = new Dictionary<CorePiece, PieceView>();

        this.pieceDic = new Dictionary<PieceType, PieceData>();
        foreach (PieceData pieceData in pieceDatas)
        {
            PieceType type = pieceData.type;
            if (this.pieceDic.ContainsKey(type) == false)
            {
                this.pieceDic.Add(type, pieceData);
            }
        }

        this.isSelected = false;

        GenerateTiles();
        InitializeBoard(START_FEN);
    }

    void Update()
    {
        if (GameManager.Instance == null || GameManager.Instance.ActiveMode == null) return;

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

    // PieceData(SO)를 CorePieceData로 변환해주는 함수
    private CorePieceData ConvertToCoreData(PieceData data)
    {
        List<BoardPos> moveOffsets = new List<BoardPos>();
        foreach (Vector2Int offset in data.moveOffsets)
        {
            moveOffsets.Add(new BoardPos(offset.x, offset.y));
        }

        List<BoardPos> attackOffsets = new List<BoardPos>();
        foreach (Vector2Int offset in data.attackOffsets)
        {
            attackOffsets.Add(new BoardPos(offset.x, offset.y));
        }

        List<BoardPos> slideDirections = new List<BoardPos>();
        foreach (Vector2Int offset in data.slideDirections)
        {
            slideDirections.Add(new BoardPos(offset.x, offset.y));
        }

        return new CorePieceData(data.type, moveOffsets, attackOffsets, slideDirections);
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
        BoardPos boardPos = new BoardPos(x, y);

        // 1. 생성 및 보드판 등록
        CorePieceData coreData = ConvertToCoreData(data);
        CorePiece logicPiece = new CorePiece(coreData)
        {
            IsWhite = isWhite,
            CurrentPosition = boardPos,
            HasMoved = false
        };
        this.Board[x, y] = logicPiece;

        // 2. 유니티 오브젝트 생성
        Vector3 worldPos = GetWorldPosition(x, y);

        GameObject pieceObject = Instantiate(piecePrefab, worldPos, Quaternion.identity, parent);
        pieceObject.name = $"{(isWhite ? "White" : "Black")}_{data.name}";
        
        PieceView newPieceView = pieceObject.GetComponent<PieceView>();

        // 3. 오브젝트에 CorePiece 등록 및 기물 맵핑
        newPieceView.Initialize(logicPiece);
        this.pieceViewMap.Add(logicPiece, newPieceView);
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

                this.tiles[x, y] = tile;
            }
        }
    }

    // 마우스 커서 상태를 업데이트하는 함수
    private void UpdateCursorState()
    {
        // 1. 기물을 잡고 드래그 중인 상태일 경우 (잡는 형태의 커서)
        if (this.inputState == InputState.Dragging)
        {
            Cursor.SetCursor(grabCursor, hotSpot, CursorMode.ForceSoftware);

            return;
        }

        Vector2 screenPos = Mouse.current.position.ReadValue();
        BoardPos tilePos = GetTilePosFromMouse(screenPos);

        // 2. 보드판 안에서 이동 시킬 수 있는 기물에 마우스 커서를 올려놨을 경우 (잡을 수 있는 상태의 커서)
        if (MoveValidator.IsOnBoard(tilePos) == true)
        {
            CorePiece hoveredPiece = Board[tilePos.x, tilePos.y];

            if (this.inputState == InputState.Selected || (hoveredPiece != null && hoveredPiece.IsWhite == GameManager.Instance.ActiveMode.IsWhiteTurn))
            {
                Cursor.SetCursor(hoverCursor, hotSpot, CursorMode.ForceSoftware);

                return;
            }
        }

        // 3. 평상 시 상태일 경우 (기본 커서)
        Cursor.SetCursor(defaultCursor, hotSpot, CursorMode.ForceSoftware);
    }

    // 기물 이동을 시도하는 함수
    private async UniTaskVoid TryMovePiece(CorePiece piece, BoardPos targetPos)
    {
        ClearSelection();

        BoardPos originalPos = piece.CurrentPosition;

        // 1. 보드 위인지 확인
        if (MoveValidator.IsOnBoard(targetPos) == false)
        {
            CancelPieceMove(piece);
            return;
        }

        // 2. 클라이언트 예측(프로모션 가능한지 확인)
        PieceType? selectedPromotionType = null;

        if (piece.Data.type == PieceType.Pawn)
        {
            int promotionY = piece.IsWhite ? 7 : 0;
            if (targetPos.y == promotionY) // 폰의 위치가 프로모션 위치일 경우
            {
                // 비동기 상태로 프로모션UI 팝업
                PromotionUIController.Instance.IsWhite = piece.IsWhite;
                selectedPromotionType = await PromotionUIController.Instance.SelectPieceAsync(targetPos, piece.IsWhite);

                if (selectedPromotionType == null)
                {
                    CancelPieceMove(piece);
                    return;
                }
            }
        }

        // 3. 유저 선택이 완료되었거나 일반 이동일 경우 서버로 요청 전송(예정)
        bool isMoveValid = GameManager.Instance.ActiveMode.HandlePieceMoveRequest(piece, targetPos, selectedPromotionType);

        if (isMoveValid == true)
        {
            HighlightManager.Instance.UpdateLastMoveHighlight(originalPos, targetPos);
            UpdatePieceVisualPosition(piece, targetPos);
        }
        else
        {
            CancelPieceMove(piece);
        }
    }

    // 기물 이동 가능 타일 표현 및 선택 판정 기물을 초기화하는 함수
    private void ClearSelection()
    {
        if (this.selectedPiece != null && pieceViewMap.ContainsKey(this.selectedPiece) == true)
        {
            HighlightManager.Instance.HideMoveHighlights();
            pieceViewMap[this.selectedPiece].GrabPiece(false);
        }

        this.isSelected = false;
        this.selectedPiece = null;
        this.inputState = InputState.None;
    }

    // 마우스 좌표를 월드 좌표로 치환해주는 함수
    private Vector3 GetMouseWorldPosition(Vector2 screenPos)
    {
        Vector3 mouseScreenPos = new Vector3(screenPos.x, screenPos.y, 0.0f);
        mouseScreenPos.z = Mathf.Abs(this.mainCamera.transform.position.z);

        return this.mainCamera.ScreenToWorldPoint(mouseScreenPos);
    }

    // 마우스 위치를 통해 타일 좌표를 얻는 함수
    public BoardPos GetTilePosFromMouse(Vector2 screenPos)
    {
        Vector3 worldPos = GetMouseWorldPosition(screenPos);

        int x = Mathf.RoundToInt((worldPos.x - this.a1Position.x) / this.tileSize);
        int y = Mathf.RoundToInt((worldPos.y - this.a1Position.y) / this.tileSize);

        return new BoardPos(x, y);
    }

    // 외부 매니저가 특정 좌표의 Tile 컴포넌트를 가져갈 수 있게 하는 함수
    public Tile GetTile(BoardPos pos)
    {
        if (MoveValidator.IsOnBoard(pos) == true)
        {
            return this.tiles[pos.x, pos.y];
        }

        return null;
    }

    // x, y 값 기준 객체가 존재해야할 월드 포지션을 가져오는 함수
    public Vector3 GetWorldPosition(int x, int y)
    {
        return new Vector3(this.a1Position.x + (x * this.tileSize), this.a1Position.y + (y * this.tileSize), 0.0f);
    }

    // 폰을 프로모션 처리하는 함수
    public void PromotePawnView(CorePiece pawn, PieceType type)
    {
        if (this.pieceDic.ContainsKey(type) == true)
        {
            // PieceView 갱신
            if (this.pieceViewMap.TryGetValue(pawn, out PieceView view) == true)
            {
                view.Initialize(pawn);
                view.gameObject.name = $"{(pawn.IsWhite ? "White" : "Black")}_{this.pieceDic[type].name}";
            }
        }
    }

    // 기물 이동을 취소 처리하는 함수
    public void CancelPieceMove(CorePiece piece)
    {
        Vector3 originalWorldPos = GetWorldPosition(piece.CurrentPosition.x, piece.CurrentPosition.y);
        pieceViewMap[piece]?.MoveTo(originalWorldPos);
    }

    // 좌클릭 드래그 시 실행되는 함수
    public void OnDragPiece(Vector2 mousePos)
    {
        if (this.selectedPiece != null && this.pieceViewMap.ContainsKey(this.selectedPiece) == true)
        {
            Vector3 mouseWorldPos = GetMouseWorldPosition(mousePos);

            pieceViewMap[this.selectedPiece].transform.position = mouseWorldPos;
        }
    }

    // 좌클릭 시작 시 실행되는 함수
    public bool OnLeftClickStarted(Vector2 mousePos)
    {
        // 1. 마우스가 올려져있는 타일 좌표 가져오기
        BoardPos tilePos = GetTilePosFromMouse(mousePos);

        // 2. 마우스 위에 있는 타일이 보드 위인지 확인
        if (MoveValidator.IsOnBoard(tilePos) == true)
        {
            CorePiece clickedPiece = this.Board[tilePos.x, tilePos.y];

            if (clickedPiece != null && clickedPiece.IsWhite == GameManager.Instance.ActiveMode.IsWhiteTurn) // 클릭한 기물 진영의 턴이 아닐 경우(싱글플레이)
            {
                if (this.selectedPiece != null && this.selectedPiece != clickedPiece)
                {
                    ClearSelection();
                }

                this.selectedPiece = clickedPiece;
                this.dragStartTile = tilePos;
                this.inputState = InputState.Dragging;

                PieceView selectedPieceView = this.pieceViewMap[this.selectedPiece];
                selectedPieceView.GrabPiece(true);

                // 이동할 수 있는 기물일 경우, 이동 가능한 타일에 하이라이트 표시
                if (GameManager.Instance.ActiveMode.LegalMovesCache.ContainsKey(this.selectedPiece) == true)
                {
                    List<BoardPos> legalMoves = GameManager.Instance.ActiveMode.LegalMovesCache[this.selectedPiece];

                    HighlightManager.Instance.ShowMoveHighlights(this.selectedPiece, legalMoves);
                }

                return true;
            }
            else if (this.inputState == InputState.Selected) // 기존에 클릭-클릭 방식으로 선택되어있는 기물이 존재할 경우
            {
                TryMovePiece(this.selectedPiece, tilePos).Forget();

                return false;
            }
            else
            {
                ClearSelection();

                return false;
            }
        }
        else
        {
            ClearSelection();

            return false;
        }
    }

    // 좌클릭 취소 시 실행되는 함수
    public void OnLeftClickCanceled(Vector2 mousePos)
    {
        // 1. 예외 처리
        if (inputState != InputState.Dragging) return;

        // 2. 마우스가 올려져있는 타일 좌표 가져오기
        BoardPos tilePos = GetTilePosFromMouse(mousePos);

        if (tilePos == this.dragStartTile)
        {
            CancelPieceMove(this.selectedPiece);

            if (this.isSelected == true) // 기존 기물을 두번 들었다 놨을 경우, 선택 취소
            {
                ClearSelection();
            }
            else // 처음 기물을 들었다 놨을 경우, 선택 모드
            {
                this.isSelected = true;

                this.inputState = InputState.Selected;
            }
        }
        else
        {
            TryMovePiece(this.selectedPiece, tilePos).Forget();
        }
    }

    // 우클릭 시작 시 실행되는 함수
    public bool OnRightClickStarted()
    {
        // 1. 예외 처리
        if (this.inputState == InputState.None) return false;

        // 2. 작업 취소
        CancelPieceMove(this.selectedPiece);
        ClearSelection();

        return true;
    }

    public void DestroyPiece(CorePiece capturedPiece)
    {
        if (this.pieceViewMap.TryGetValue(capturedPiece, out PieceView pieceView) == true)
        {
            Destroy(pieceView.gameObject);

            pieceViewMap.Remove(capturedPiece);
        }
    }

    public Dictionary<PieceType, CorePieceData> GetCorePieceDataDic()
    {
        Dictionary<PieceType, CorePieceData> dic = new Dictionary<PieceType, CorePieceData>();
        foreach (KeyValuePair<PieceType, PieceData> keyValuePair in this.pieceDic)
        {
            dic.Add(keyValuePair.Key, ConvertToCoreData(keyValuePair.Value));
        }

        return dic;
    }

    public void UpdatePieceVisualPosition(CorePiece piece, BoardPos newPos)
    {
        if (this.pieceViewMap.TryGetValue(piece, out PieceView view) == true)
        {
            view.MoveTo(GetWorldPosition(newPos.x, newPos.y));
        }
    }
}
