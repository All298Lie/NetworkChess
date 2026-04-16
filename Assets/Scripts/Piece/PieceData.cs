using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Piece", menuName = "Chess/Piece", order = 1)]
public class PieceData : ScriptableObject
{
    [Header("이동/공격 관련")]
    public List<Vector2Int> moveOffsets; // 이동 오프셋
    public List<Vector2Int> attackOffsets; // 공격 오프셋(이동 오프셋과 공격 오프셋이 다른 폰 때문)
    public List<Vector2Int> slideDirections; // 슬라이드 방향 (룩, 비숍, 퀸 같이 막힐때까지 쭉 나아가는 기물을 위한 변수)

    [Header("기타 속성")]
    public int meterialValue; // 기물 점수

    [Header("열거형")]
    public PieceType type; // 기물 종류
}