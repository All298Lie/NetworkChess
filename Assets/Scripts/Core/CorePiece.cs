namespace NetworkChess.Core
{
    public class CorePiece
    {
        public CorePieceData Data { get; private set; }

        public BoardPos CurrentPosition;

        public bool IsWhite;
        public bool HasMoved;

        public CorePiece(CorePieceData data)
        {
            this.Data = data;
        }

        public void UpdateData(CorePieceData data)
        {
            this.Data = data;
        }
    }
}

