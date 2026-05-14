using NetworkChess.Core;

public static class GameData
{
    public static bool IsWhite { get; set; }
    public static GameMode CurrentMode { get; set; }
    public static string StartingFEN { get; set; } = string.Empty;
    public static string OpponentNickname { get; set; } = string.Empty;

    #region 설정 초기화 함수
    public static void Clear()
    {
        IsWhite = false;
        StartingFEN = string.Empty;
        OpponentNickname = string.Empty;
    }
    #endregion
}
