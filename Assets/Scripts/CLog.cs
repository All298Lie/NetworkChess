using System.Diagnostics;

public static class CLog // 개발 디버그용
{

    [Conditional("UNITY_EDITOR")]
    [Conditional("DEVELOPMENT_BUILD")]
    public static void Log(string message)
    {
        UnityEngine.Debug.Log(message);
    }

    [Conditional("UNITY_EDITOR")]
    [Conditional("DEVELOPMENT_BUILD")]
    public static void LogWarning(string message)
    {
        UnityEngine.Debug.LogWarning(message);
    }

    [Conditional("UNITY_EDITOR")]
    [Conditional("DEVELOPMENT_BUILD")]
    public static void LogError(string message)
    {
        UnityEngine.Debug.LogError(message);
    }
}
