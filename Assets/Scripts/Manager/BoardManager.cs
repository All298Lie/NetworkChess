using Cysharp.Threading.Tasks;
using DG.Tweening;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

using NetworkChess.Core;
using UnityEngine.Pool;
using System;

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

    private Tile[,] tiles;
    private Dictionary<CorePiece, PieceView> pieceViewMap;

    [Header("Input 시스템")]
    private BoardPos dragStartTile;
    private InputState inputState;

    private CorePiece selectedPiece;
    private Camera mainCamera;

    private bool isSelected;

    [Header("애니메이션 설정")]
    [SerializeField] public float moveDuration = 0.1f;
    [SerializeField] private Ease moveEase = Ease.OutQuad; // 부드러운 감속 곡선

    [Header("오브젝트 풀")]
    private ObjectPool<PieceView> piecePool;
    private List<PieceView> replayTempPieces = new List<PieceView>();
    private Transform pieceParent;

    #region + 유니티 함수

    #region Awake 함수
    void Awake()
    {
        if (Instance == null) // 싱글톤 패턴 디자인
        {
            Instance = this;
        }
        else
        {
            CLog.LogWarning("보드 매니저가 이미 존재합니다.");
            Destroy(gameObject);
        }

        this.mainCamera = Camera.main;

        this.inputState = InputState.None;

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

        this.piecePool = new ObjectPool<PieceView>(
            createFunc: CreatePiece,
            actionOnGet: GetPiece,
            actionOnRelease: ReleasePiece,
            actionOnDestroy: DestroyPiece,
            defaultCapacity: 32,
            maxSize: 64
        );

        GenerateTiles();
    }
    #endregion

    #region Update 함수
    void Update()
    {
        if (GameManager.Instance == null || GameManager.Instance.ActiveMode == null) return;

        UpdateCursorState();
    }
    #endregion

    #endregion - 유니티 함수

    #region + 초기화 함수

    #region PieceData(SO)를 CorePieceData로 변환해주는 함수
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
    #endregion

    #region 타일을 생성하는 함수
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
    #endregion

    #region CorePiece 데이터를 담은 Dictionary 컨테이너를 가져오는 함수
    public Dictionary<PieceType, CorePieceData> GetCorePieceDataDic()
    {
        Dictionary<PieceType, CorePieceData> dic = new Dictionary<PieceType, CorePieceData>();
        foreach (KeyValuePair<PieceType, PieceData> keyValuePair in this.pieceDic)
        {
            dic.Add(keyValuePair.Key, ConvertToCoreData(keyValuePair.Value));
        }

        return dic;
    }
    #endregion

    #region 비정상 상태 복구를 위한 하드 리셋 함수
    public void HardResetBoard(GameModeBase activeMode)
    {
        ClearSelection();

        foreach (PieceView temp in this.replayTempPieces) this.piecePool.Release(temp);
        this.replayTempPieces.Clear();

        foreach (PieceView view in this.pieceViewMap.Values) piecePool.Release(view);
        this.pieceViewMap.Clear();

        SetupBoard(activeMode);
    }
    #endregion

    #endregion - 초기화 함수

    #region + 계산 관련 함수

    #region 마우스 좌표를 월드 좌표로 치환해주는 함수
    private Vector3 GetMouseWorldPosition(Vector2 screenPos)
    {
        Vector3 mouseScreenPos = new Vector3(screenPos.x, screenPos.y, 0.0f);
        mouseScreenPos.z = Mathf.Abs(this.mainCamera.transform.position.z);

        return this.mainCamera.ScreenToWorldPoint(mouseScreenPos);
    }
    #endregion

    #region 마우스 위치를 통해 타일 좌표를 얻는 함수
    public BoardPos GetTilePosFromMouse(Vector2 screenPos)
    {
        Vector3 worldPos = GetMouseWorldPosition(screenPos);

        int x = Mathf.RoundToInt((worldPos.x - this.a1Position.x) / this.tileSize);
        int y = Mathf.RoundToInt((worldPos.y - this.a1Position.y) / this.tileSize);

        if (GameData.IsWhite == false)
        {
            x = 7 - x;
            y = 7 - y;
        }

        return new BoardPos(x, y);
    }
    #endregion

    #region 외부 매니저가 특정 좌표의 Tile 컴포넌트를 가져갈 수 있게 하는 함수
    public Tile GetTile(BoardPos pos)
    {
        if (MoveValidator.IsOnBoard(pos) == true)
        {
            return this.tiles[pos.x, pos.y];
        }

        return null;
    }
    #endregion

    #region x, y 값 기준 객체가 존재해야할 월드 포지션을 가져오는 함수
    public Vector3 GetWorldPosition(int x, int y)
    {
        if (GameData.IsWhite == false)
        {
            x = 7 - x;
            y = 7 - y;
        }

        float worldX = this.a1Position.x + x * this.tileSize;
        float worldY = this.a1Position.y + y * this.tileSize;

        return new Vector3(worldX, worldY, 0.0f);
    }
    #endregion

    #endregion - 계산 관련 함수

    #region + 뷰어 관련 함수

    #region 게임 시작 시 GameManager에서 호출해 줄 초기화 함수
    public void SetupBoard(GameModeBase currentMode)
    {
        GenerateVisualBoard(currentMode);
    }
    #endregion

    #region 코어의 논리 보드를 기반으로 유니티 프리팹 껍데기를 씌우는 함수
    private void GenerateVisualBoard(GameModeBase currentMode)
    {
        this.pieceParent = new GameObject("Pieces").transform;
        CorePiece[,] coreBoard = currentMode.Board;

        for (int x = 0; x < 8; x++)
        {
            for (int y = 0; y < 8; y++)
            {
                CorePiece logicPiece = coreBoard[x, y];

                if (logicPiece != null)
                {
                    SpawnPieceVisual(this.pieceParent, logicPiece, x, y);
                }
            }
        }
    }
    #endregion

    #region 기물을 보드에 생성하는 뷰어 함수
    private void SpawnPieceVisual(Transform parent, CorePiece logicPiece, int x, int y)
    {
        PieceType type = logicPiece.Data.type;
        if (this.pieceDic.TryGetValue(type, out PieceData data) == true)
        {
            Vector3 worldPos = GetWorldPosition(x, y);

            // 1. 유니티 오브젝트 생성
            PieceView newPieceView = this.piecePool.Get();
            newPieceView.gameObject.name = $"{(logicPiece.IsWhite ? "White" : "Black")}_{ data.name}";

            newPieceView.Initialize(logicPiece);
            newPieceView.MoveTo(worldPos);

            this.pieceViewMap.Add(logicPiece, newPieceView);
        }
    }
    #endregion

    #region 기물 이동 가능 타일 표현 및 선택 판정 기물을 초기화하는 함수
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
    #endregion

    #region 기물 이동을 시도하는 함수
    private async UniTaskVoid TryMovePiece(CorePiece piece, BoardPos targetPos, bool isInstant = false)
    {
        ClearSelection();

        BoardPos originalPos = piece.CurrentPosition;

        // 1. 보드 위인지 확인
        if (MoveValidator.IsOnBoard(targetPos) == false)
        {
            CancelPieceMove(piece);
            return;
        }

        BoardPos requestPos = targetPos;
        bool isCastling = false;

        if (piece.Data.type == PieceType.King && GameManager.Instance.ActiveMode.LegalMovesCache.TryGetValue(piece, out List<BoardPos> kingLegalMoves))
        {
            foreach (BoardPos legalPos in kingLegalMoves)
            {
                CorePiece targetRook = GameManager.Instance.ActiveMode.Board[legalPos.x, legalPos.y];

                // 합법적 이동 경로 중에 아군 룩이 있다면? (캐슬링 후보지)
                if (targetRook != null && targetRook.IsWhite == piece.IsWhite && targetRook.Data.type == PieceType.Rook)
                {
                    bool isKingSide = legalPos.x > piece.CurrentPosition.x;
                    int finalKingX = isKingSide ? 6 : 2;

                    // 유저가 스탠다드처럼 최종 위치(finalKingX)에 놓았거나, 피셔 랜덤처럼 룩 머리 위(legalPos)에 직접 놓았을 경우
                    if ((targetPos.x == finalKingX && targetPos.y == piece.CurrentPosition.y) || targetPos == legalPos)
                    {
                        // 실제 서버(코어) 로직이 요구하는 '룩의 좌표'로 요청 위치를 덮어씌웁니다!
                        requestPos = legalPos;
                        isCastling = true;
                        break;
                    }
                }
            }
        }

        // 2. 클라이언트 예측(프로모션 가능한지 확인)
        PieceType? selectedPromotionType = null;

        if (piece.Data.type == PieceType.Pawn)
        {
            int promotionY = piece.IsWhite ? 7 : 0;

            // targetPos 대신 requestPos로 일관성을 맞춰주는 것이 좋습니다.
            if (requestPos.y == promotionY)
            {
                bool isLegalMove = false;
                if (GameManager.Instance.ActiveMode.LegalMovesCache.TryGetValue(piece, out List<BoardPos> pawnLegalMoves))
                {
                    if (pawnLegalMoves.Contains(requestPos) == true)
                    {
                        isLegalMove = true;
                    }
                }

                // 갈 수 없는 위치일 경우, 팝업을 띄우지 않고 곧바로 이동 취소
                if (isLegalMove == false)
                {
                    CancelPieceMove(piece);
                    return;
                }

                // 비동기 상태로 프로모션UI 팝업
                PromotionUIController.Instance.IsWhite = piece.IsWhite;
                selectedPromotionType = await PromotionUIController.Instance.SelectPieceAsync(targetPos, true); // true : 프로모션 UI 위치 상단으로 고정

                if (selectedPromotionType == null)
                {
                    CancelPieceMove(piece);
                    return;
                }
            }
        }

        // 3. 유저 선택이 완료되었거나 일반 이동일 경우 서버로 요청 전송(예정)
        bool isMoveValid = GameManager.Instance.ActiveMode.HandlePieceMoveRequest(piece, requestPos, selectedPromotionType);

        if (isMoveValid == true)
        {
            if (isCastling == true)
            {
                UpdatePieceVisualPosition(piece, piece.CurrentPosition, true);
            }
            else
            {
                UpdatePieceVisualPosition(piece, targetPos, isInstant);
            }

            // 서버로 이동 요청 패킷 발송
            C2S_GameMoveReq moveReq = new C2S_GameMoveReq();

            moveReq.StartPos = originalPos;
            moveReq.EndPos = requestPos;
            moveReq.PromotionType = selectedPromotionType;

            NetworkManager.Instance.SendPacket(moveReq).Forget();
        }
        else
        {
            CancelPieceMove(piece);
        }
    }
    #endregion

    #region 폰을 프로모션 처리하는 함수
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
    #endregion

    #region 기물 이동을 취소 처리하는 함수
    public void CancelPieceMove(CorePiece piece)
    {
        Vector3 originalWorldPos = GetWorldPosition(piece.CurrentPosition.x, piece.CurrentPosition.y);
        pieceViewMap[piece]?.MoveTo(originalWorldPos);
    }
    #endregion

    #region 기물 파괴 처리 시 뷰어에서도 없어지도록 하는 함수
    public void DeactivatePiece(CorePiece capturedPiece)
    {
        if (this.pieceViewMap.TryGetValue(capturedPiece, out PieceView pieceView) == true)
        {
            pieceView.gameObject.SetActive(false);
        }
    }
    #endregion

    #region 기물 이동 시 뷰어에서도 반영되도록 하는 함수
    public void UpdatePieceVisualPosition(CorePiece piece, BoardPos newPos, bool isInstant = false)
    {
        if (this.pieceViewMap.TryGetValue(piece, out PieceView view) == true)
        {
            if (isInstant == true)
            {
                view.MoveTo(GetWorldPosition(newPos.x, newPos.y));
            }
            else
            {
                AnimatePieceMove(piece.CurrentPosition, newPos);
            }
        }
    }
    #endregion

    #region 로직 보드에 맞게 비주얼 보드 동기화 작업을 진행하는 함수 (최신 상태)
    public void SyncVisualsWithCore(GameModeBase activeMode)
    {
        // 1. 기존에 화면에 있는 모든 기물들의 매핑을 확인
        foreach (PieceView temp in this.replayTempPieces) piecePool.Release(temp);
        this.replayTempPieces.Clear();

        foreach (KeyValuePair<CorePiece, PieceView> pair in this.pieceViewMap)
        {
            CorePiece logicPiece = pair.Key;
            PieceView view = pair.Value;

            // 2. 논리 보드에서 이 기물이 파괴된 상태일 경우
            bool isAlive = false;
            BoardPos pos = logicPiece.CurrentPosition;

            if (MoveValidator.IsOnBoard(pos) == true && activeMode.Board[pos.x, pos.y] == logicPiece)
            {
                isAlive = true;
            }

            // 3. 살아있는 기물이면 키고 죽었을 경우 끄기
            if (isAlive == true)
            {
                view.gameObject.SetActive(true);
                view.MoveTo(GetWorldPosition(pos.x, pos.y));
            }
            else
            {
                view.gameObject.SetActive(false);
            }
        }
    }
    #endregion

    #region FEN 문자열을 읽어서 비주얼 보드를 과거 시점으로 강제 동기화하는 함수
    public void SyncVisualsWithFEN(string FEN)
    {
        // 1. 임시 기물 반납
        foreach (PieceView temp in this.replayTempPieces) this.piecePool.Release(temp);
        this.replayTempPieces.Clear();

        // 2. 라이브 기물 숨기기
        foreach (PieceView view in this.pieceViewMap.Values) view.gameObject.SetActive(false);

        // 3. FEN에서 보드 배치 부분만 잘라내기
        string piecePlacement = FEN.Split(' ')[0];
        int x = 0;
        int y = 7;

        Dictionary<PieceType, CorePieceData> coreDataDic = GetCorePieceDataDic();

        HashSet<PieceView> usedLiveViews = new HashSet<PieceView>();
        foreach (char c in piecePlacement)
        {
            if (c == '/')
            {
                x = 0;
                y--;
                continue;
            }

            if (char.IsDigit(c) == true)
            {
                x += (int)char.GetNumericValue(c);
                continue;
            }

            bool isWhite = char.IsUpper(c);
            PieceType type = GetPieceTypeFromChar(c);

            PieceView matchedView = null;
            foreach (PieceView view in this.pieceViewMap.Values)
            {
                if (usedLiveViews.Contains(view) == false && view.LogicPiece.IsWhite == isWhite && view.LogicPiece.Data.type == type)
                {
                    matchedView = view;
                    usedLiveViews.Add(view);
                    break;
                }
            }

            if (matchedView == null)
            {
                matchedView = this.piecePool.Get();

                CorePiece tempPiece = new CorePiece(coreDataDic[type]);
                tempPiece.IsWhite = isWhite;

                matchedView.Initialize(tempPiece);
                matchedView.gameObject.name = $"{(isWhite ? "White" : "Black")}_{this.pieceDic[type].name}_ReplayTemp";

                this.replayTempPieces.Add(matchedView);
            }

            matchedView.gameObject.SetActive(true);
            matchedView.MoveTo(GetWorldPosition(x, y));

            x++;
        }
    }
    #endregion

    #region FEN 문자를 PieceType으로 변환하는 헬퍼 함수
    private PieceType GetPieceTypeFromChar(char c)
    {
        switch (char.ToLower(c))
        {
            case 'p':
                return PieceType.Pawn;

            case 'b':
                return PieceType.Bishop;

            case 'n':
                return PieceType.Knight;

            case 'r':
                return PieceType.Rook;

            case 'q':
                return PieceType.Queen;

            case 'k': 
                return PieceType.King;

            default:
                return PieceType.Pawn;
        }
    }
    #endregion

    #region 기물 이동에 애니메이션을 넣어주는 함수
    public void AnimatePieceMove(BoardPos startPos, BoardPos endPos, Action onComplete = null)
    {
        Vector3 startWorldPos = GetWorldPosition(startPos.x, startPos.y);
        Vector3 endWorldPos = GetWorldPosition(endPos.x, endPos.y);

        PieceView targetView = null;
        PieceView capturedTempView = null;

        // 1. 시작 위치에 있는 기물과 끝 위치에 있는 기물 탐색
        foreach (PieceView view in this.pieceViewMap.Values) // 실시간 대전 기물 탐색
        {
            if (view.gameObject.activeSelf == false) continue;

            if (Vector3.Distance(view.transform.position, startWorldPos) < 0.1f) targetView = view;
        }

        if (targetView == null)
        {
            foreach (PieceView view in this.replayTempPieces) // 리플레이 임시 기물 탐색
            {
                if (view.gameObject.activeSelf == false) continue;

                if (Vector3.Distance(view.transform.position, startWorldPos) < 0.1f) targetView = view;
                if (Vector3.Distance(view.transform.position, endWorldPos) < 0.1f) capturedTempView = view;
            }
        }

        // 2. 애니메이션 실행
        if (targetView != null)
        {
            targetView.GetComponent<SpriteRenderer>().sortingOrder = 100;

            targetView.transform.DOKill();
            targetView.transform.DOMove(endWorldPos, this.moveDuration)
                .SetEase(this.moveEase)
                .OnComplete(() =>
                {
                    if (capturedTempView != null && this.replayTempPieces.Contains(capturedTempView) == true)
                    {
                        this.replayTempPieces.Remove(capturedTempView);
                        this.piecePool.Release(capturedTempView);
                    }

                    targetView.GetComponent<SpriteRenderer>().sortingOrder = 0;

                    onComplete?.Invoke();
                });
        }
        else
        {
            onComplete?.Invoke();
        }
    }
    #endregion

    #endregion - 뷰어 관련 함수

    #region + 오브젝트 풀 함수

    private PieceView CreatePiece()
    {
        GameObject piece = Instantiate(this.piecePrefab, this.pieceParent);

        return piece.GetComponent<PieceView>();
    }

    private void GetPiece(PieceView view) => view.gameObject.SetActive(true);

    private void ReleasePiece(PieceView view)
    {
        view.transform.DOKill();

        view.gameObject.SetActive(false);
    }

    private void DestroyPiece(PieceView view) => Destroy(view.gameObject);

    #endregion - 오브젝트 풀 함수

    #region + 마우스 조작 관련 함수

    #region 좌클릭 드래그 시 실행되는 함수
    public void OnDragPiece(Vector2 mousePos)
    {
        if (this.selectedPiece != null && this.pieceViewMap.ContainsKey(this.selectedPiece) == true)
        {
            Vector3 mouseWorldPos = GetMouseWorldPosition(mousePos);

            pieceViewMap[this.selectedPiece].transform.position = mouseWorldPos;
        }
    }
    #endregion

    #region 좌클릭 시작 시 실행되는 함수
    public bool OnLeftClickStarted(Vector2 mousePos)
    {
        if (ReplayManager.Instance.IsViewingLatest == false) return false;

        // 1. 마우스가 올려져있는 타일 좌표 가져오기
        BoardPos tilePos = GetTilePosFromMouse(mousePos);

        // 2. 마우스 위에 있는 타일이 보드 위인지 확인
        if (MoveValidator.IsOnBoard(tilePos) == true)
        {
            CorePiece clickedPiece = GameManager.Instance.ActiveMode.Board[tilePos.x, tilePos.y];

            bool isMyPiece = (clickedPiece != null && clickedPiece.IsWhite == GameData.IsWhite); // 내 기물인지 확인

            bool isMyTurn = (GameData.IsWhite == GameManager.Instance.ActiveMode.IsWhiteTurn); // 내 턴인지 확인

            if (clickedPiece != null && isMyPiece == true && isMyTurn == true) // 내 턴, 내 기물을 클릭한 경우
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
    #endregion

    #region 좌클릭 취소 시 실행되는 함수
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
            TryMovePiece(this.selectedPiece, tilePos, true).Forget();
        }
    }
    #endregion

    #region 우클릭 시작 시 실행되는 함수
    public bool OnRightClickStarted()
    {
        // 1. 예외 처리
        if (this.inputState == InputState.None) return false;

        // 2. 작업 취소
        CancelPieceMove(this.selectedPiece);
        ClearSelection();

        return true;
    }
    #endregion

    #region 마우스 커서 상태를 업데이트하는 함수
    private void UpdateCursorState()
    {
        // 1. 기물을 잡고 드래그 중인 상태일 경우 (잡는 형태의 커서)
        if (this.inputState == InputState.Dragging)
        {
            CursorManager.Instance.SetGrabCursor();

            return;
        }

        Vector2 screenPos = Mouse.current.position.ReadValue();
        BoardPos tilePos = GetTilePosFromMouse(screenPos);

        // 2. 보드판 안에서 이동 시킬 수 있는 기물에 마우스 커서를 올려놨을 경우 (잡을 수 있는 상태의 커서)
        if (MoveValidator.IsOnBoard(tilePos) == true)
        {
            CorePiece hoveredPiece = GameManager.Instance.ActiveMode.Board[tilePos.x, tilePos.y];

            bool isMyPiece = (hoveredPiece != null && hoveredPiece.IsWhite == GameData.IsWhite); // 내 기물인지 확인

            bool isMyTurn = (GameData.IsWhite == GameManager.Instance.ActiveMode.IsWhiteTurn); // 내 턴인지 확인(프리무브 구현 시 제거)

            if (this.inputState == InputState.Selected || (hoveredPiece != null && isMyPiece == true && isMyTurn == true))
            {
                CursorManager.Instance.SetHoverCursor();

                return;
            }
        }

        // 3. 평상 시 상태일 경우 (기본 커서)
        CursorManager.Instance.SetDefaultCursor();
    }
    #endregion

    #endregion - 마우스 조작 관련 함수
}