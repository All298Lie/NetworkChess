using UnityEngine;

[CreateAssetMenu(fileName = "NewBoardTheme", menuName = "Chess/Theme/Board", order = 1)]
public class BoardThemeData : ScriptableObject
{
    public string themeName;

    [Header("보드판")]
    public Sprite boardSprite;
}
