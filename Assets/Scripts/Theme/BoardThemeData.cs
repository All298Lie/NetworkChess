using UnityEngine;

[CreateAssetMenu(fileName = "NewBoardTheme", menuName = "Chess/Theme/Board", order = 1)]
public class BoardThemeData : ScriptableObject
{
    public string themeName;

    [Header("보드판")]
    public Sprite boardSprite;

    public Color32 blackColor;
    public Color32 whiteColor;
}
