using UnityEngine;

[System.Serializable]
public struct PieceSpritesSet
{
    public Sprite pawn;
    public Sprite knight;
    public Sprite bishop;
    public Sprite rook;
    public Sprite queen;
    public Sprite king;
}

[CreateAssetMenu(fileName = "NewPieceTheme", menuName = "Chess/Theme/Piece", order = 2)]
public class PieceThemeData : ScriptableObject
{
    public string themeName;

    [Header("백 진영 기물 세트")]
    public PieceSpritesSet whiteSprites;

    [Header("흑 진영 기물 세트")]
    public PieceSpritesSet blackSprites;

    // 요구하는 기물의 스프라이트를 반환하는 함수
    public Sprite GetSprite(PieceType type, bool isWhite)
    {
        PieceSpritesSet target = (isWhite ? whiteSprites : blackSprites);

        switch (type)
        {
            case PieceType.Knight:
                return target.knight;

            case PieceType.Bishop:
                return target.bishop;

            case PieceType.Rook:
                return target.rook;

            case PieceType.Queen:
                return target.queen;

            case PieceType.King:
                return target.king;

            case PieceType.Pawn:
            default:
                return target.pawn;
        }
    }
}