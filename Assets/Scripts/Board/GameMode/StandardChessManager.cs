using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;

public class StandardChessManager : GameModeBase
{
    private Dictionary<string, int> stateHistory;
    private Piece selectedPiece;

    private string currentTurnFEN;
    private int halfMoveClock = 0;
    private bool isProcessingMove;

    void Start()
    {
        this.LegalMovesCache = new Dictionary<Piece, List<Vector2Int>>();
        this.stateHistory = new Dictionary<string, int>();
        this.selectedPiece = null;

        this.isProcessingMove = false;

        GameManager.Instance.RegisterModeManager(this);
    }

    // 게임 시작 시 실행되는 함수
    public override void StartGame()
    {
        this.IsWhiteTurn = true;
        BoardManager.Instance.EnPassant = null;
        CalculateLegalMovesForTurn();
    }

    // 기물 이동 리퀘스트 관련 처리를 하는 함수
    public override void HandlePieceMoveRequest(Piece piece, Vector2Int targetPos)
    {
        if (isProcessingMove == true) return;

        // 잘못된 기물 이동 방식일 경우, 취소 후 리턴
        if (piece.IsWhite != this.IsWhiteTurn || this.LegalMovesCache.ContainsKey(piece) == false || this.LegalMovesCache[piece].Contains(targetPos) == false)
        {
            BoardManager.Instance.CancelPieceMove(piece);
            this.selectedPiece = null;
            return;
        }

        // 정상적인 기물 이동 방식일 경우, 기물 이동 처리
        isProcessingMove = true;
        FinalizeMove(piece, targetPos).Forget();
    }

    // 승리/종료 판정을 내리는 함수
    protected override void CheckWinCondition()
    {
        // 50수 규칙
        if (halfMoveClock >= 100)
        {
            Debug.Log("스테일메이트! 무승부! (50수 규칙)");

            // TODO : UI 띄우기

            return;
        }

        // 3회 동형 상황일 경우
        if (this.stateHistory.ContainsKey(this.currentTurnFEN) == true && this.stateHistory[this.currentTurnFEN] >= 3)
        {
            Debug.Log("스테일메이트! 무승부! (3회 동형 반복)");

            // TODO : UI 띄우기

            return;
        }

        // 스테일메이트 또는 체크메이트 상황일 경우
        if (this.LegalMovesCache.Count == 0)
        {
            Vector2Int myKingPos = BoardManager.Instance.GetKingPosition(this.IsWhiteTurn);
            bool inCheck = MoveValidator.IsKingInCheck(BoardManager.Instance.Board, this.IsWhiteTurn, myKingPos);

            if (inCheck == true)
            {
                Debug.Log($"체크메이트! {(this.IsWhiteTurn ? "흑" : "백")} 승리 !");
            }
            else
            {
                Debug.Log("스테일메이트! 무승부!");
            }

            // TODO : UI 띄우기

            return;
        }

        // 기물 부족으로 인한 스테일메이트 (킹vs킹, 킹+나이트vs킹, 킹+비숍vs킹)
        if (IsInsufficientMaterial() == true)
        {
            Debug.Log("무승부! (기물 부족으로 인한 체크메이트 불가)");
            return;
        }
    }

    // 내 기물의 이동 범위를 미리 계산하는 함수
    private void CalculateLegalMovesForTurn()
    {
        this.LegalMovesCache.Clear();

        // 보드판을 탐색하여 내 기물의 이동 범위 계산
        for (int x = 0; x < 8; x++)
        {
            for (int y = 0; y < 8; y++)
            {
                Piece piece = BoardManager.Instance.Board[x, y];
                if (piece != null && piece.IsWhite == this.IsWhiteTurn)
                {
                    List<Vector2Int> moves = MoveValidator.GetLegalMoves(BoardManager.Instance.Board, piece);

                    if (moves.Count > 0)
                    {
                        this.LegalMovesCache.Add(piece, moves);
                    }
                }
            }
        }

        // 현재까지의 게임 진행 상황을 FEN 형식으로 생성해 저장
        this.currentTurnFEN = FENUtility.GeneratingStateString(BoardManager.Instance.Board, IsWhiteTurn);

        if (this.stateHistory.ContainsKey(this.currentTurnFEN) == true) // 이미 존재하는 FEN 형식일 경우
        {
            this.stateHistory[this.currentTurnFEN]++;
        }
        else // 존재하지 않는 FEN 형식일 경우
        {
            this.stateHistory.Add(this.currentTurnFEN, 1);
        }

        CheckWinCondition();
    }

    // 기물의 이동을 처리하는 함수
    private async UniTaskVoid FinalizeMove(Piece piece, Vector2Int targetPos)
    {
        Piece targetPiece = BoardManager.Instance.Board[targetPos.x, targetPos.y];
        Vector2Int originalPos = piece.CurrentPosition;

        BoardManager.Instance.ExecuteMoveOnBoard(piece, targetPos);

        // 1. 캐슬링 처리
        if (piece.Data.type == PieceType.King && Mathf.Abs(targetPos.x - originalPos.x) > 1)
        {
            HandleCastling(piece, targetPos.x > originalPos.x);
        }

        // 2. 앙파상 처리
        Piece enPassantTarget = HandleEnPassant(piece, originalPos, targetPos);

        // 3. 승급 처리
        bool needRollback = await HandlePromotion(piece, targetPos);

        // 4. 롤백 필요 여부 처리
        if (needRollback == true)
        {
            BoardManager.Instance.Board[targetPos.x, targetPos.y] = null;

            BoardManager.Instance.Board[originalPos.x, originalPos.y] = piece;
            piece.MoveTo(originalPos, BoardManager.Instance.GetWorldPosition(originalPos.x, originalPos.y));
            
            if (targetPiece != null)
            {
                BoardManager.Instance.Board[targetPos.x, targetPos.y] = targetPiece;
                targetPiece.gameObject.SetActive(true);
            }

            isProcessingMove = false;

            return;
        }

        // 6. 50수 규칙과 3회 동형 관련 로직
        bool isCapture = (targetPiece != null);
        bool isPawnMove = (piece.Data.type == PieceType.Pawn);

        if (isCapture == true || isPawnMove == true) // 기물을 먹거나 폰을 움직였을 경우
        {
            halfMoveClock = 0;

            stateHistory.Clear();
        }
        else
        {
            halfMoveClock = halfMoveClock + 1;
        }

        // 7. 롤백 때 파괴되지 않도록 기물 지연 파괴 작업
        if (targetPiece != null)
        {
            Destroy(targetPiece.gameObject);
        }
        if (enPassantTarget != null)
        {
            Destroy(enPassantTarget.gameObject);
        }

        // 8. 턴 넘기기
        piece.HasMoved = true;
        selectedPiece = null;
        IsWhiteTurn = !IsWhiteTurn;

        CalculateLegalMovesForTurn();

        isProcessingMove = false;
    }

    // 캐슬링 관련 처리를 하는 함수
    private void HandleCastling(Piece king, bool isKingSide)
    {
        int y = king.CurrentPosition.y;

        // 룩의 출발점, 도착점 x값 찾기
        int oldRookX = isKingSide ? 7 : 0;
        int newRookX = isKingSide ? 5 : 3;

        Piece rook = BoardManager.Instance.Board[oldRookX, y];

        if (rook != null && rook.Data.type == PieceType.Rook)
        {
            BoardManager.Instance.ExecuteMoveOnBoard(rook, new Vector2Int(newRookX, y));
            rook.HasMoved = true;
        }
    }

    // 앙파상 관련 처리를 하는 함수
    private Piece HandleEnPassant(Piece piece, Vector2Int originalPos, Vector2Int targetPos)
    {
        Vector2Int? previousEnPassantTarget = BoardManager.Instance.EnPassant;

        BoardManager.Instance.EnPassant = null;

        if (piece.Data.type != PieceType.Pawn) return null;

        if (originalPos.x != targetPos.x && previousEnPassantTarget == targetPos) // 내가 대각선으로 이동했는데 도착한 칸에 아무도 없을 경우, 앙파상으로 처리한 상대 폰 삭제
        {
            int direction = piece.IsWhite ? 1 : -1;

            Vector2Int enemyPawnPos = new Vector2Int(targetPos.x, targetPos.y - direction);
            Piece enemyPawn = BoardManager.Instance.Board[enemyPawnPos.x, enemyPawnPos.y];

            if (enemyPawn != null)
            {
                BoardManager.Instance.Board[enemyPawnPos.x, enemyPawnPos.y] = null;
                enemyPawn.gameObject.SetActive(false);

                return enemyPawn;
            }
        }
        else if (Mathf.Abs(targetPos.y - originalPos.y) == 2) // 폰이 2칸 이동했을 경우, 건너 뛴 위치를 앙파상 위치로 설정
        {
            int direction = piece.IsWhite ? 1 : -1;

            BoardManager.Instance.EnPassant = new Vector2Int(originalPos.x, originalPos.y + direction);
        }

        return null;
    }

    // 프로모션 관련 처리를 하는 함수
    private async UniTask<bool> HandlePromotion(Piece piece, Vector2Int targetPos)
    {
        if (piece.Data.type != PieceType.Pawn) return false;

        int promotionY = piece.IsWhite ? 7 : 0;

        if (targetPos.y == promotionY)
        {
            PromotionUI.Instance.IsWhite = piece.IsWhite;

            // UI를 통해 승급할 기물 선택
            PieceType? selectedType = await PromotionUI.Instance.SelectPieceAsync(targetPos, piece.IsWhite); // 현재 isTopRank를 IsWhite로 판정. 나중에 보드를 뒤집을 수 있게 할경우 변경해야함

            if (selectedType == null) return true;

            BoardManager.Instance.PromotePawn(piece, selectedType.Value);

            Debug.Log($"폰이 {selectedType}으로 승급");
        }

        return false;
    }

    // 기물 부족 상황인지 확인하는 함수
    private bool IsInsufficientMaterial()
    {
        int minorPieceCount = 0; // 마이너 피스 수를 세는 함수

        // 보드판 순회
        for (int x = 0; x < 8; x++)
        {
            for (int y = 0; y < 8; y++)
            {
                // 기물 체크
                Piece p = BoardManager.Instance.Board[x, y];

                // 1. 킹이거나 기물이 존재하지 않을 경우, 다음 보드판 확인
                if (p == null || p.Data.type == PieceType.King) continue;

                // 2. 마이너 기물 외에 다른 기물이 존재할 경우, 기물 부족이 아니므로 false 리턴
                if (p.Data.type == PieceType.Pawn || p.Data.type == PieceType.Rook || p.Data.type == PieceType.Queen) return false;

                // 3. 마이너 기물이 존재할 경우, 카운트 증가
                if (p.Data.type == PieceType.Knight || p.Data.type == PieceType.Bishop)
                {
                    minorPieceCount = minorPieceCount + 1;
                }
            }
        }

        // 4. 마이너 기물이 1개 이하일 경우, 기물 부족이므로 true 리턴
        if (minorPieceCount <= 1) return true;

        return false;
    }
}
