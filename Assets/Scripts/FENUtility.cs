using System.Text;
using UnityEngine;

public static class FENUtility
{
    public static string GeneratingStateString(Piece[,] board, bool isWhiteTurn)
    {
        StringBuilder fen = new StringBuilder();

        // 1. 포지션의 기물 배치 기록
        for (int y = 7; y >= 0; y--)
        {
            int emptyCount = 0;
            for (int x = 0; x < 8; x++)
            {
                Piece piece = board[x, y];
                if (piece == null)
                {
                    emptyCount = emptyCount + 1;
                }
                else
                {
                    if (emptyCount > 0)
                    {
                        fen.Append(emptyCount);
                        emptyCount = 0;
                    }
                    fen.Append(GetPieceChar(piece));
                }
            }

            if (emptyCount > 0) fen.Append(emptyCount);
            if (y > 0) fen.Append('/'); // 보드 줄바꿈
        }

        // 2. 차례
        fen.Append(isWhiteTurn == true ? " w " : " b ");

        // 3. 캐슬링 가능여부
        StringBuilder castling = new StringBuilder("");
        castling.Append(GetCastlingRight(board, true, true)); // 백 킹사이드 캐슬링
        castling.Append(GetCastlingRight(board, true, false)); // 백 퀸사이드 캐슬링
        castling.Append(GetCastlingRight(board, false, true)); // 흑 킹사이드 캐슬링
        castling.Append(GetCastlingRight(board, false, false)); // 흑 퀸사이드 캐슬링

        if (castling.Length == 0) castling.Append("-");
        fen.Append(castling).Append(" ");

        // 4. 앙파상 가능한 칸
        if (BoardManager.Instance.EnPassant.HasValue == true)
        {
            Vector2Int ep = BoardManager.Instance.EnPassant.Value;

            char file = (char)(ep.x + 'a'); // 0~7 -> a->h
            int rank = ep.y + 1; // 0~7 -> 1~8

            fen.Append($"{file}{rank}");
        }
        else
        {
            fen.Append("-");
        }

        return fen.ToString();
    }

    // 기물에 따라 알파벳 할당 (백은 대문자, 흑은 소문자)
    private static char GetPieceChar(Piece piece)
    {
        char c;

        switch (piece.Data.type)
        {
            case PieceType.Knight:
                c = 'N';
                break;

            case PieceType.Bishop:
                c = 'B';
                break;


            case PieceType.Rook:
                c = 'R';
                break;


            case PieceType.Queen:
                c = 'Q';
                break;


            case PieceType.King:
                c = 'K';
                break;


            case PieceType.Pawn:
            default:
                c = 'P';
                break;
        }

        return piece.IsWhite ? c : char.ToLower(c);
    }

    // 두 진영의 캐슬링 가능 여부를 확인 후 FEN 형식으로 반환하는 함수
    private static string GetCastlingRight(Piece [,] board, bool isWhite, bool isKingSide)
    {
        Vector2Int kingPos = BoardManager.Instance.GetKingPosition(isWhite);
        Piece king = BoardManager.Instance.Board[kingPos.x, kingPos.y];

        // 1. 킹이 존재하지 않거나 킹이 움직였을 경우
        if (king == null || king.HasMoved == true) return "";

        // 2. 룩 위치 확인
        Piece targetRook = null;
        if (isKingSide == true)
        {
            for (int x = kingPos.x + 1; x < 8; x++)
            {
                Piece p = board[x, kingPos.y];
                if (p != null && p.Data.type == PieceType.Rook && p.IsWhite == isWhite)
                {
                    targetRook = p;
                    break;
                }
            }
        }
        else
        {
            for (int x = 0; x < kingPos.x; x++)
            {
                Piece p = board[x, kingPos.y];
                if (p != null && p.Data.type == PieceType.Rook && p.IsWhite == isWhite)
                {
                    targetRook = p;
                    break;
                }
            }
        }

        // 3. 룩이 움직이지 않았을 경우 캐슬링 위치 및 진영에 따라 문자 반환
        if (targetRook != null && targetRook.HasMoved == false)
        {
            string symbol = isKingSide ? "K" : "Q";

            return isWhite ? symbol : symbol.ToLower();
        }

        return "";
    }
}
