using NetworkChess.Core;
using NUnit.Framework;
using System.Collections.Generic;

public static class GameData
{
    public static bool IsSpectator { get; set; } = false;
    public static bool IsReplay { get; set; } = false;

    public static string ReplayCode { get; set; } = string.Empty;

    public static List<string> FENHistory { get; set; } = new List<string>();

    public static bool IsWhite { get; set; }
    public static GameMode CurrentMode { get; set; }
    public static string StartingFEN { get; set; } = string.Empty;
    public static string OpponentNickname { get; set; } = string.Empty;

    #region 설정 초기화 함수
    public static void Clear()
    {
        IsSpectator = false;
        IsReplay = false;

        ReplayCode = string.Empty;

        FENHistory = new List<string>();

        IsWhite = false;
        CurrentMode = GameMode.Standard;
        StartingFEN = string.Empty;
        OpponentNickname = string.Empty;
    }
    #endregion
}
