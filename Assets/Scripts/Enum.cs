// 기물 종류를 나타내는 열거형
public enum PieceType
{
    Pawn, // 폰
    Rook, // 룩
    Knight, // 나이트
    Bishop, // 비숍
    Queen, // 퀸
    King // 킹
}

// 게임모드 종류를 나타내는 열거형
public enum GameMode
{ 
    Standard, // 스탠다드 체스
    ForOfWar,
    FischerRoandom
}

public enum InputState
{
    None, // 없음
    Dragging, // 드래그 상태
    Selected // 선택 상태
}