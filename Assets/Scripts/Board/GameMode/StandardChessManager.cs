using System.Collections.Generic;
using UnityEngine;

public class StandardChessManager : GameModeBase
{
    private Dictionary<Piece, List<Vector2Int>> legalMovesCache;
    private Piece selectedPiece;

    void Start()
    {
        this.legalMovesCache = new Dictionary<Piece, List<Vector2Int>>();
        this.selectedPiece = null;

        GameManager.Instance.RegisterModeManager(this);
    }

    // 게임 시작 시 실행되는 함수
    public override void StartGame()
    {
        this.isWhiteTurn = true;
        BoardManager.Instance.enPassant = null;
        CalculateLegalMovesForTurn();
    }

    // 기물 이동 리퀘스트 관련 처리를 하는 함수
    public override void HandlePieceMoveRequest(Piece piece, Vector2Int targetPos)
    {
        // 잘못된 기물 이동 방식일 경우, 취소 후 리턴
        if (piece.IsWhite != this.isWhiteTurn || this.legalMovesCache.ContainsKey(piece) == false || this.legalMovesCache[piece].Contains(targetPos) == false)
        {
            BoardManager.Instance.CancelPieceMove(piece);
            this.selectedPiece = null;
            return;
        }

        // 정상적인 기물 이동 방식일 경우, 기물 이동 처리
        FinalizeMove(piece, targetPos);
    }

    // 승리/종료 판정을 내리는 함수
    protected override void CheckWinCondition()
    {
        if (this.legalMovesCache.Count == 0) // 스테일 메이트 또는 체크메이트 상황일 경우
        {
            Vector2Int myKingPos = BoardManager.Instance.GetKingPosition(isWhiteTurn);
            bool inCheck = MoveValidator.IsKingInCheck(BoardManager.Instance.Board, isWhiteTurn, myKingPos);

            if (inCheck == true)
            {
                Debug.Log($"체크메이트! {(this.isWhiteTurn ? "흑" : "백")} 승리 !");
            }
            else
            {
                Debug.Log($"스테일메이트! 무승부!");
            }

            // TODO : UI 띄우기
        }
    }

    // 내 기물의 이동 범위를 미리 계산하는 함수
    private void CalculateLegalMovesForTurn()
    {
        this.legalMovesCache.Clear();

        // 보드판을 탐색하여 내 기물의 이동 범위 계산
        for (int x = 0; x < 8; x++)
        {
            for (int y = 0; y < 8; y++)
            {
                Piece piece = BoardManager.Instance.Board[x, y];
                if (piece != null && piece.IsWhite == this.isWhiteTurn)
                {
                    List<Vector2Int> moves = MoveValidator.GetLegalMoves(BoardManager.Instance.Board, piece);

                    if (moves.Count > 0)
                    {
                        this.legalMovesCache.Add(piece, moves);
                    }
                }
            }
        }
    }

    // 기물의 이동을 처리하는 함수
    private void FinalizeMove(Piece piece, Vector2Int targetPos)
    {
        Vector2Int originalPos = piece.CurrentPosition;

        BoardManager.Instance.ExecuteMoveOnBoard(piece, targetPos);

        // 1. 캐슬링 처리
        if (piece.Data.type == PieceType.King && Mathf.Abs(targetPos.x - originalPos.x) > 1)
        {
            HandleCastling(piece, targetPos.x > originalPos.x);
        }

        // 2. 앙파상 처리
        HandleEnPassant(piece, originalPos, targetPos);

        // 3. 승급 처리
        HandlePromotion(piece, targetPos);

        // 4. 턴 넘기기
        piece.hasMoved = true;
        selectedPiece = null;
        isWhiteTurn = !isWhiteTurn;

        CalculateLegalMovesForTurn();
    }

    // 캐슬링 관련 처리를 하는 함수
    private void HandleCastling(Piece king, bool isKingSide)
    {
        int y = king.CurrentPosition.y;
        int kingX = king.CurrentPosition.x;

        // 룩의 출발점, 도착점 x값 찾기
        int oldRookX = isKingSide ? 7 : 0;
        int newRookX = isKingSide ? 5 : 3;

        Piece rook = BoardManager.Instance.Board[oldRookX, y];

        if (rook != null && rook.Data.type == PieceType.Rook)
        {
            BoardManager.Instance.ExecuteMoveOnBoard(rook, new Vector2Int(newRookX, y));
            rook.hasMoved = true;
        }
    }

    // 앙파상 관련 처리를 하는 함수
    private void HandleEnPassant(Piece piece, Vector2Int originalPos, Vector2Int targetPos)
    {
        BoardManager.Instance.enPassant = null;

        if (piece.Data.type != PieceType.Pawn) return;

        if (Mathf.Abs(targetPos.y - originalPos.y) == 2) // 폰이 2칸 이동했을 경우, 건너 뛴 위치를 앙파상 위치로 설정
        {
            int direction = piece.IsWhite ? 1 : -1;

            BoardManager.Instance.enPassant = new Vector2Int(originalPos.x, originalPos.y + direction);
        }
        else if (originalPos.x != targetPos.x && BoardManager.Instance.Board[targetPos.x, targetPos.y] == piece) // 내가 대각선으로 이동했는데 도착한 칸에 아무도 없을 경우, 앙파상으로 처리한 상대 폰 삭제
        {
            int direction = piece.IsWhite ? 1 : -1;

            Vector2Int enemyPawnPos = new Vector2Int(targetPos.x, targetPos.y - direction);
            Piece enemyPawn = BoardManager.Instance.Board[enemyPawnPos.x, enemyPawnPos.y];

            if (enemyPawn != null && enemyPawn.Data.type == PieceType.Pawn)
            {
                BoardManager.Instance.Board[enemyPawnPos.x, enemyPawnPos.y] = null;
                Destroy(enemyPawn.gameObject);
            }
        }
    }

    // 프로모션 관련 처리를 하는 함수
    private void HandlePromotion(Piece piece, Vector2Int targetPos)
    {
        if (piece.Data.type != PieceType.Pawn) return;

        int promotionY = piece.IsWhite ? 7 : 0;

        if (targetPos.y == promotionY)
        {
            // TODO : UI를 띄워 마이너/메이저 피스 중에 고를 수 있게하여, 고른 기물로 승급하도록 하기

            // 현재는 퀸으로 자동승급

            BoardManager.Instance.promotePawn(piece, PieceType.Queen);

            Debug.Log("폰이 퀸으로 승급");
        }
    }
}
