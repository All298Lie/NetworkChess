using System;
using System.Collections.Generic;

namespace NetworkChess.Core
{
    public class StandardChessMode : GameModeBase
    {
        private Dictionary<string, int> stateHistory;

        private string currentTurnFEN;
        private int halfMoveClock = 0;
        private bool isProcessingMove;

        private CorePiece[,] board;
        private Dictionary<PieceType, CorePieceData> pieceDataDic;

        public void Initialize(CorePiece[, ] coreBoard, Dictionary<PieceType, CorePieceData> dataDic)
        {
            this.board = coreBoard;
            this.pieceDataDic = dataDic;

            this.LegalMovesCache = new Dictionary<CorePiece, List<BoardPos>>();
            this.stateHistory = new Dictionary<string, int>();

            this.isProcessingMove = false;
        }

        // 게임 시작 시 실행되는 함수
        public override void StartGame()
        {
            this.IsWhiteTurn = true;
            this.CurrentEnPassantPos = null;
            CalculateLegalMovesForTurn();
        }

        // 기물 이동 리퀘스트 관련 처리를 하는 함수
        public override bool HandlePieceMoveRequest(CorePiece piece, BoardPos targetPos, PieceType? promotionType)
        {
            if (this.isProcessingMove == true) return false;

            // 잘못된 기물 이동 방식일 경우, 취소 후 리턴
            if (piece.IsWhite != this.IsWhiteTurn || this.LegalMovesCache.ContainsKey(piece) == false || this.LegalMovesCache[piece].Contains(targetPos) == false)
            {
                return false;
            }

            // 프로모션 검증
            bool isPromotionCondition = (piece.Data.type == PieceType.Pawn) && (targetPos.y == (piece.IsWhite ? 7 : 0));

            if (isPromotionCondition == true && promotionType == null) return false;
            if (isPromotionCondition == false && promotionType != null) return false;

            // 정상적인 기물 이동 방식일 경우, 기물 이동 처리
            this.isProcessingMove = true;
            bool moveSuccess = FinalizeMove(piece, targetPos, promotionType);

            return moveSuccess;
        }

        // 승리/종료 판정을 내리는 함수
        protected override void CheckWinCondition()
        {
            // 50수 규칙
            if (halfMoveClock >= 100)
            {
                GameOver("$Draw", "50수 규칙");

                return;
            }

            // 3회 동형 상황일 경우
            if (this.stateHistory.ContainsKey(this.currentTurnFEN) == true && this.stateHistory[this.currentTurnFEN] >= 3)
            {
                GameOver("$Draw", "3회 동형 반복");

                return;
            }

            // 스테일메이트 또는 체크메이트 상황일 경우
            if (this.LegalMovesCache.Count == 0)
            {
                BoardPos myKingPos = MoveValidator.FindKingPosition(this.board, this.IsWhiteTurn);
                bool inCheck = MoveValidator.IsKingInCheck(this.board, this.IsWhiteTurn, myKingPos, this.CurrentEnPassantPos);

                if (inCheck == true)
                {
                    GameOver($"{(this.IsWhiteTurn ? "Black" : "White")}", "체크메이트");

                    // TODO : 네트워크 통신을 통해 대전이 이뤄질 경우, 해당 유저의 닉네임을 UI에 띄우도록 수정
                }
                else
                {
                    GameOver("$Draw", "스테일메이트");
                }

                return;
            }

            // 기물 부족으로 인한 스테일메이트 (킹vs킹, 킹+나이트vs킹, 킹+비숍vs킹)
            if (IsInsufficientMaterial() == true)
            {
                GameOver("$Draw", "기물 부족");

                return;
            }

            // TODO : 기권, 시간패, 합의 추가
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
                    CorePiece piece = this.board[x, y];
                    if (piece != null && piece.IsWhite == this.IsWhiteTurn)
                    {
                        List<BoardPos> moves = MoveValidator.GetLegalMoves(this.board, piece, this.CurrentEnPassantPos);

                        if (moves.Count > 0)
                        {
                            this.LegalMovesCache.Add(piece, moves);
                        }
                    }
                }
            }

            // 현재까지의 게임 진행 상황을 FEN 형식으로 생성해 저장
            this.currentTurnFEN = FENUtility.GeneratingStateString(this.board, this.IsWhiteTurn, this.CurrentEnPassantPos);

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
        private bool FinalizeMove(CorePiece piece, BoardPos targetPos, PieceType? promotionType)
        {
            CorePiece targetPiece = this.board[targetPos.x, targetPos.y];
            
            BoardPos originalPos = piece.CurrentPosition;
            this.board[originalPos.x, originalPos.y] = null;
            this.board[targetPos.x, targetPos.y] = piece;

            piece.CurrentPosition = targetPos;

            // 1. 캐슬링 처리
            if (piece.Data.type == PieceType.King && Math.Abs(targetPos.x - originalPos.x) > 1)
            {
                HandleCastling(piece, targetPos.x > originalPos.x);
            }

            // 2. 앙파상 처리
            CorePiece enPassantTarget = HandleEnPassant(piece, originalPos, targetPos);

            // 3. 승급 처리
            if (piece.Data.type == PieceType.Pawn && (targetPos.y == 7 || targetPos.y == 0))
            {
                PromotePawnCore(piece, promotionType.Value);
            }

            // 4. 50수 규칙과 3회 동형 관련 계산
            bool isCapture = (targetPiece != null);
            bool isPawnMove = (piece.Data.type == PieceType.Pawn);

            if (isCapture == true || isPawnMove == true) // 기물을 먹거나 폰을 움직였을 경우
            {
                this.halfMoveClock = 0;

                this.stateHistory.Clear();
            }
            else
            {
                this.halfMoveClock = this.halfMoveClock + 1;
            }

            // 5. 롤백 때 파괴되지 않도록 기물 지연 파괴 작업
            if (targetPiece != null) CapturePiece(targetPiece);
            if (enPassantTarget != null) CapturePiece(enPassantTarget);

            // 6. 턴 넘기기
            piece.HasMoved = true;
            this.IsWhiteTurn = !IsWhiteTurn;

            CalculateLegalMovesForTurn();

            this.isProcessingMove = false;

            return true;
        }

        // 캐슬링 관련 처리를 하는 함수
        private void HandleCastling(CorePiece king, bool isKingSide)
        {
            int y = king.CurrentPosition.y;

            // 룩의 출발점, 도착점 x값 찾기
            int oldRookX = isKingSide ? 7 : 0;
            int newRookX = isKingSide ? 5 : 3;

            CorePiece rook = this.board[oldRookX, y];

            if (rook != null && rook.Data.type == PieceType.Rook)
            {
                this.board[oldRookX, y] = null;
                this.board[newRookX, y] = rook;
                rook.CurrentPosition = new BoardPos(newRookX, y);
                rook.HasMoved = true;

                InvokePieceMovedEvent(rook, rook.CurrentPosition);
            }
        }

        // 앙파상 관련 처리를 하는 함수
        private CorePiece HandleEnPassant(CorePiece piece, BoardPos originalPos, BoardPos targetPos)
        {
            BoardPos? previousEnPassantTarget = this.CurrentEnPassantPos;

            this.CurrentEnPassantPos = null;

            if (piece.Data.type != PieceType.Pawn) return null;

            if (originalPos.x != targetPos.x && previousEnPassantTarget == targetPos) // 내가 대각선으로 이동했는데 도착한 칸에 아무도 없을 경우, 앙파상으로 처리한 상대 폰 삭제
            {
                int direction = piece.IsWhite ? 1 : -1;

                BoardPos enemyPawnPos = new BoardPos(targetPos.x, targetPos.y - direction);
                CorePiece enemyPawn = this.board[enemyPawnPos.x, enemyPawnPos.y];

                if (enemyPawn != null)
                {
                    this.board[enemyPawnPos.x, enemyPawnPos.y] = null;

                    return enemyPawn;
                }
            }
            else if (Math.Abs(targetPos.y - originalPos.y) == 2) // 폰이 2칸 이동했을 경우, 건너 뛴 위치를 앙파상 위치로 설정
            {
                int direction = piece.IsWhite ? 1 : -1;

                this.CurrentEnPassantPos = new BoardPos(originalPos.x, originalPos.y + direction);
            }

            return null;
        }

        // 프로모션 데이터 교체 함수
        private void PromotePawnCore(CorePiece pawn, PieceType newType)
        {
            if (this.pieceDataDic.TryGetValue(newType, out CorePieceData newData) == true)
            {
                pawn.UpdateData(newData);
                InvokePawnPromotedEvent(pawn, newType);
            }
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
                    CorePiece logicPiece = this.board[x, y];

                    // 1. 킹이거나 기물이 존재하지 않을 경우, 다음 보드판 확인
                    if (logicPiece == null || logicPiece.Data.type == PieceType.King) continue;

                    // 2. 마이너 기물 외에 다른 기물이 존재할 경우, 기물 부족이 아니므로 false 리턴
                    if (logicPiece.Data.type == PieceType.Pawn || logicPiece.Data.type == PieceType.Rook || logicPiece.Data.type == PieceType.Queen) return false;

                    // 3. 마이너 기물이 존재할 경우, 카운트 증가
                    if (logicPiece.Data.type == PieceType.Knight || logicPiece.Data.type == PieceType.Bishop)
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
}
