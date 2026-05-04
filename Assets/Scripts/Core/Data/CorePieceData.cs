using System.Collections.Generic;

namespace NetworkChess.Core
{
    public class CorePieceData
    {
        public PieceType type;

        public List<BoardPos> moveOffsets;
        public List<BoardPos> attackOffsets;
        public List<BoardPos> slideDirections;

        public CorePieceData(PieceType type, List<BoardPos> moveOffsets, List<BoardPos> attackOffsets, List<BoardPos> slideDirections)
        {
            this.type = type;
            this.moveOffsets = moveOffsets;
            this.attackOffsets = attackOffsets;
            this.slideDirections = slideDirections;
        }
    }
}
