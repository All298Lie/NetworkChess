using UnityEngine;

public abstract class GameModeBase : MonoBehaviour
{
    protected bool isWhiteTurn;

    public virtual void StartGame() { }

    public abstract void HandlePieceMoveRequest(Piece piece, Vector2Int targetPos);

    protected abstract void CheckWinCondition();

}
