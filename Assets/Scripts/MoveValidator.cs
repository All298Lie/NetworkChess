using System.Collections.Generic;
using UnityEngine;

public static class MoveValidator // 서버, 클라이언트에 모두 사용할 기물이 이동 가능한 좌표를 뱉는 함수
{
    // 기물이 이동가능한 위치 배열을 반환하는 함수 (킹이 안전한지 체크 o)
    public static List<Vector2Int> GetLegalMoves(Piece[,] board, Piece piece)
    {
        List<Vector2Int> moves = new List<Vector2Int>();

        // 1. 기물이 이동 가능한 가상 위치 가져오기
        List<Vector2Int> pseudoMoves = GetPseudoLegalMoves(board, piece);

        // 2. 기물이 이동했을 때 킹이 안전할 경우, 이동 가능한 위치 추가
        foreach (Vector2Int target in pseudoMoves)
        {
            if (IsMoveSafeForKing(board, piece, target) == true)
            {
                moves.Add(target);
            }
        }

        // 3. 기물이 킹일 경우, 캐슬링 이동도 추가
        if (piece.Data.type == PieceType.King && piece.HasMoved == false)
        {
            moves.AddRange(GetCastlingMoves(board, piece));
        }

        return moves;
    }

    // 기물이 이동가능한 가상 위치 배열을 반환하는 함수 (킹이 안전한지 체크 x)
    private static List<Vector2Int> GetPseudoLegalMoves(Piece[,] board, Piece piece)
    {
        List<Vector2Int> moves = new List<Vector2Int>();

        switch (piece.Data.type)
        {
            case PieceType.Pawn: // 기물이 폰일 경우
                moves = GetPawnMoves(board, piece);
                break;

            case PieceType.Knight: // 기물이 나이트일 경우
                moves = GetStepMoves(board, piece);
                break;

            case PieceType.Bishop: // 기물이 비숍일 경우
                moves = GetSlideMoves(board, piece);
                break;

            case PieceType.Rook: // 기물이 룩일 경우
                moves = GetSlideMoves(board, piece);
                break;

            case PieceType.King: // 기물이 킹일 경우
                moves = GetStepMoves(board, piece);
                break;

            case PieceType.Queen: // 기물이 퀀일 경우
                moves = GetSlideMoves(board, piece);
                break;

            default:
                return moves;
        }

        return moves;
    }

    // 폰이 이동할 수 있는 위치를 받아오는 함수
    private static List<Vector2Int> GetPawnMoves(Piece[,] board, Piece piece)
    {
        List<Vector2Int> moves = new List<Vector2Int>();
        Vector2Int pos = piece.CurrentPosition;

        // 이동 오프셋
        List<Vector2Int> pieceMoveOffsets = new List<Vector2Int>();
        pieceMoveOffsets.AddRange(piece.Data.moveOffsets);

        if (piece.Data.type == PieceType.Pawn && piece.HasMoved == false) // 폰이 2칸 이동이 가능한지 확인
        {
            Vector2Int target = pos + Vector2Int.up * (piece.IsWhite ? 1 : -1); // 진영에 따라 이동하는 방향이 다름

            Piece targetPiece = board[target.x, target.y];
            if (targetPiece == null) // 1칸 앞에 폰이 없을 경우, 이동 가능하므로 오프셋에 추가
            {
                pieceMoveOffsets.Add(new Vector2Int(0, 2));
            } 
        }

        foreach (Vector2Int offset in pieceMoveOffsets)
        {
            Vector2Int target = pos + offset * (piece.IsWhite ? 1 : -1); // 진영에 따라 이동하는 방향이 다름

            // 1. 보드 밖으로 나가는지 검사
            if (IsOnBoard(target) == false) continue;

            // 2. 기물로 가로막혀있는지 검사
            Piece targetPiece = board[target.x, target.y];
            if (targetPiece != null) continue;

            moves.Add(target);
        }

        // 공격 오프셋
        foreach (Vector2Int offset in piece.Data.attackOffsets)
        {
            Vector2Int target = pos + offset * (piece.IsWhite ? 1 : -1); // 진영에 따라 이동하는 방향이 다름

            // 1. 보드 밖으로 나가는지 검사
            if (IsOnBoard(target) == false) continue;

            // 2. 적 기물이 위치해있거나 앙파상 가능한 위치인지 검사
            Piece targetPiece = board[target.x, target.y];
            if (targetPiece == null)
            {
                if (BoardManager.Instance.EnPassant != target) continue;
            }
            else
            {
                if (targetPiece.IsWhite == piece.IsWhite) continue;
            }

            moves.Add(target);
        }

        return moves;
    }

    // moveOffset을 통해 이동하는 기물(킹, 나이트)이 이동할 수 있는 위치를 받아오는 함수
    private static List<Vector2Int> GetStepMoves(Piece[,] board, Piece piece)
    {
        List<Vector2Int> moves = new List<Vector2Int>();
        Vector2Int pos = piece.CurrentPosition;

        foreach (Vector2Int offset in piece.Data.moveOffsets)
        {
            Vector2Int target = pos + offset;

            // 1. 보드 밖으로 나가는지 검사
            if (IsOnBoard(target) == false) continue;

            // 2. 해당 위치에 기물이 있을 경우, 아군 기물인지 검사
            Piece targetPiece = board[target.x, target.y];
            if (targetPiece != null && targetPiece.IsWhite == piece.IsWhite) continue;

            moves.Add(target);
        }

        return moves;
    }

    // slideDiraction을 통해 이동하는 기물(퀸, 룩, 비숍)이 이동할 수 있는 위치를 받아오는 함수
    private static List<Vector2Int> GetSlideMoves(Piece[,] board, Piece piece)
    {
        List<Vector2Int> moves = new List<Vector2Int>();
        Vector2Int pos = piece.CurrentPosition;

        foreach(Vector2Int dir in piece.Data.slideDirections)
        {
            Vector2Int target = pos + dir;

            while (IsOnBoard(target) == true) // 보드판에 존재하는 동안 반복
            {
                Piece targetPiece = board[target.x, target.y];

                if (targetPiece == null) // 해당 위치에 기물이 존재하지 않을 경우
                {
                    moves.Add(target);

                    target = target + dir; // 계속 슬라이드(이동)하기
                }
                else // 해당 위치에 기물이 존재할 경우
                {
                    if (targetPiece.IsWhite != piece.IsWhite) // 기물이 적일 경우 이 칸까지 이동 가능처리
                    {
                        moves.Add(target);
                    }

                    break; // 막혀있으므로 이후로 이동 불가
                }
            }
        }

        return moves;
    }

    // 해당 위치가 보드 안인지 확인하는 함수
    public static bool IsOnBoard(Vector2Int pos)
    {
        if (pos.x < 0 || pos.x > 7) return false;
        if (pos.y < 0 || pos.y > 7) return false;

        return true;
    }

    // 해당 위치로 기물이 이동할 때 킹이 안전한지 확인하는 함수
    private static bool IsMoveSafeForKing(Piece[,] board, Piece piece, Vector2Int target)
    {
        Vector2Int originPos = piece.CurrentPosition;
        Piece targetPiece = board[target.x, target.y];

        // 기물을 가상 이동
        board[originPos.x, originPos.y] = null;
        board[target.x, target.y] = piece;

        // 기물 진영의 킹 좌표 가져오기
        Vector2Int kingPos;
        if (piece.Data.type == PieceType.King) // 해당 기물이 킹일 경우, 가상 이동한 좌표로 설정
        {
            kingPos = target;
        }
        else // 해당 기물이 킹이 아닐 경우, 보드매니저에서 킹 좌표 가져오기
        {
            kingPos = BoardManager.Instance.GetKingPosition(piece.IsWhite);
        }

        // 기물이 이동했을 때 기준으로 킹이 체크를 당하는 상태인지 확인
        bool isSafe = (IsKingInCheck(board, piece.IsWhite, kingPos) == false);

        // 확인이 끝난 후 기물 위치 원상 복귀(백트래킹)
        board[originPos.x, originPos.y] = piece;
        board[target.x, target.y] = targetPiece;

        return isSafe;
    }
    
    // 킹이 체크상태인지 확인하는 함수
    public static bool IsKingInCheck(Piece[,] board, bool isWhite, Vector2Int kingPos)
    {
        // 적군 기물들이 내 킹을 때릴 수 있는지 검사
        for (int x = 0; x < 8; x++)
        {
            for (int y = 0; y < 8; y++)
            {
                Piece enemyPiece = board[x, y];
                if (enemyPiece != null && enemyPiece.IsWhite != isWhite)
                {
                    List<Vector2Int> enemyMoves = GetPseudoLegalMoves(board, enemyPiece);

                    if (enemyMoves.Contains(kingPos)) return true; // 적의 공격 범위에 내 킹이 존재할 경우, true 리턴
                }
            }
        }

        return false; // 모든 적 공격 범위에 내 킹이 존재하지 않으므로 false 리턴
    }

    // 킹의 캐슬링 가능여부에 따라 이동할 수 있는 위치를 반환받는 함수
    private static List<Vector2Int> GetCastlingMoves(Piece[,] board, Piece king)
    {
        List<Vector2Int> moves = new List<Vector2Int>();

        // 1. 현재 킹이 체크 상태일 경우, 리턴
        if (IsKingInCheck(board, king.IsWhite, king.CurrentPosition) == true) return moves;

        // 2. 룩 위치 스캔
        int y = king.CurrentPosition.y;
        int leftRookX = -1;
        int rightRookX = -1;

        for (int x = 0; x < 8; x++)
        {
            Piece piece = board[x, y];
            if (piece != null)
            {
                if (piece.Data.type == PieceType.Rook && piece.IsWhite == king.IsWhite && piece.HasMoved == false)
                {
                    if (x < king.CurrentPosition.x) leftRookX = x; // 킹보다 왼쪽에 있으면 퀸사이드 룩
                    else if (x > king.CurrentPosition.x) rightRookX = x; // 킹보다 오른쪽에 있으면 킹사이드 룩
                }
            }
        }

        // 3. 찾은 룩을 바탕으로 캐슬링 확인
        if (rightRookX != -1 && CanCastleVariant(board, king, rightRookX, true) == true)
        {
            moves.Add(new Vector2Int(6, y)); // 킹사이드 캐슬링의 킹 위치 추가
        }

        if (leftRookX != -1 && CanCastleVariant(board, king, leftRookX, false) == true)
        {
            moves.Add(new Vector2Int(2, y));
        }

        return moves;
    }

    // 캐슬링이 가능한지 확인하는 함수
    private static bool CanCastleVariant(Piece[,] board, Piece king, int rookX, bool isKingSide)
    {
        int y = king.CurrentPosition.y;
        int kingX = king.CurrentPosition.x;

        int finalKingX = isKingSide ? 6 : 2;
        int finalRookX = isKingSide ? 5 : 3;

        int minEmptyX = Mathf.Min(kingX, rookX, finalKingX, finalRookX);
        int maxEmptyX = Mathf.Max(kingX, rookX, finalKingX, finalRookX);

        // 1. 캐슬링을 하려는 킹과 룩 사이에 기물이 존재하는지 확인
        for (int x = minEmptyX; x <= maxEmptyX; x++)
        {
            if (x == kingX || x == rookX) continue; // 본인(킹, 룩)이 서있을 경우, 패스
            if (board[x, y] != null) return false; // 다른 기물이 막고 있을 경우, false 리턴
        }

        // 2. 킹이 현재 위치에서 최종 위치까지 적의 공격을 받는지 확인
        int step = (finalKingX > kingX) ? 1 : -1;
        for (int x = kingX + step; x != finalKingX + step; x += step)
        {
            if (IsKingInCheck(board, king.IsWhite, new Vector2Int(x, y)) == true) return false;
        }

        return true;
    }
}
