using System;

namespace NetworkChess.Core
{
    public struct BoardPos : IEquatable<BoardPos>
    {
        public int x;
        public int y;

        public BoardPos(int x, int y)
        {
            this.x = x;
            this.y = y;
        }

        // 더하기 빼기 연산
        public static BoardPos operator +(BoardPos a, BoardPos b) => new BoardPos(a.x + b.x, a.y + b.y);
        public static BoardPos operator -(BoardPos a, BoardPos b) => new BoardPos(a.x - b.x, a.y - b.y);

        // 비교 연산
        public static bool operator ==(BoardPos a, BoardPos b) => (a.x == b.x) && (a.y == b.y);
        public static bool operator !=(BoardPos a, BoardPos b) => (a == b) == false;

        // Dictionary의 Key나 List.Contains 등에 필요한 필수 메서드
        public override bool Equals(object obj) => obj is BoardPos pos && Equals((BoardPos)obj);
        public bool Equals(BoardPos other) => (x == other.x) && (y == other.y);
        public override int GetHashCode() => HashCode.Combine(x, y);

        // 디버깅 편의를 위한 ToString 재정의
        public override string ToString() => $"{x}, {y}";
    }
}