using UnityEngine;

public abstract class GameModeBase : MonoBehaviour
{
    protected bool isWhiteTurn;

    // 게임 시작할때 작동하는 함수
    public virtual void StartGame() { }

    // 2. 보드 매니저에서 받은 기물 이동 리퀘스트 관련 처리를 하는 함수
    public abstract void HandlePieceMoveRequest(Piece piece, Vector2Int targetPos);

    // 승리/종료 판정을 내리는 함수
    protected abstract void CheckWinCondition();

}
