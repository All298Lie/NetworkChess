using System;
using System.Collections.Generic;

namespace NetworkChess.Core
{
    public abstract class GameModeBase
    {
        public event Action<string, string> OnGameOverEvent; // 게임 종료 이벤트
        public event Action<CorePiece> OnPieceCapturedEvent; // 기물 잡기 이벤트
        public event Action<CorePiece, PieceType> OnPawnPromotedEvent; // 폰 프로모션 이벤트
        public event Action<CorePiece, BoardPos> OnPieceMovedEvent; // 기물 이동 이벤트

        public virtual BoardPos? CurrentEnPassantPos { get; protected set; } = null;

        public Dictionary<CorePiece, List<BoardPos>> LegalMovesCache { get; protected set; }

        public bool IsWhiteTurn { get; protected set; }

        // 게임 시작할때 작동하는 함수
        public virtual void StartGame() { }

        // 2. 보드 매니저에서 받은 기물 이동 리퀘스트 관련 처리를 하는 함수
        public virtual bool HandlePieceMoveRequest(CorePiece piece, BoardPos targetPos, PieceType? promotionType)
        {
            return true;
        }

        // 승리/종료 판정을 내리는 함수
        protected abstract void CheckWinCondition();

        // 게임 종료 시 결과화 함꼐 UI를 띄우도록 방송하는 함수
        protected void GameOver(string winnerName, string reason)
        {
            OnGameOverEvent?.Invoke(winnerName, reason);
        }

        // 기물이 잡혔을 때 파괴 처리하도록 방송하는 함수
        protected void CapturePiece(CorePiece capturedPiece)
        {
            OnPieceCapturedEvent?.Invoke(capturedPiece);
        }

        // 폰이 프로모션했을 때 스프라이트 등을 바꾸도록 방송하는 함수
        protected void InvokePawnPromotedEvent(CorePiece pawn, PieceType newType)
        {
            OnPawnPromotedEvent?.Invoke(pawn, newType);
        }

        // 기물이 이동했을 때 클라이언트 뷰 이동을 지시하는 함수
        protected void InvokePieceMovedEvent(CorePiece piece, BoardPos newPos)
        {
            OnPieceMovedEvent?.Invoke(piece, newPos);
        }
    }
}
