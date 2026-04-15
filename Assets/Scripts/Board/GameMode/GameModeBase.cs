using System.Collections.Generic;
using UnityEngine;

public abstract class GameModeBase : MonoBehaviour
{
    public Dictionary<Piece, List<Vector2Int>> LegalMovesCache { get; protected set; }

    public bool IsWhiteTurn { get; protected set; }

    // 게임 시작할때 작동하는 함수
    public virtual void StartGame() { }

    // 2. 보드 매니저에서 받은 기물 이동 리퀘스트 관련 처리를 하는 함수
    public abstract void HandlePieceMoveRequest(Piece piece, Vector2Int targetPos);

    // 승리/종료 판정을 내리는 함수
    protected abstract void CheckWinCondition();

    // 게임 종료 시 결과와 함께 UI를 띄우는 함수
    protected void GameOver(string winnerName, string reason)
    {
        GameManager.Instance.GameOverUI.ShowGameOver(winnerName, reason);   
    }
}
