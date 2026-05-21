using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameHistoryItemUI : MonoBehaviour
{
    [Header("텍스트 UI")]
    [SerializeField] private TMP_Text fullMoveNumber;
    [SerializeField] private TMP_Text whiteMove;
    [SerializeField] private TMP_Text blackMove;

    [Header("버튼")]
    [SerializeField] private Button whiteMoveBtn;
    [SerializeField] private Button blackMoveBtn;

    private string whiteFEN;
    private string blackFEN;

    #region 백 이동을 표기하는 함수
    public void SetWhiteMove(int fullMoveNumber, string whiteMove, string whiteFEN)
    {
        this.fullMoveNumber.text = $"{fullMoveNumber}.";

        this.whiteMove.text = whiteMove;
        this.whiteFEN = whiteFEN;

        this.whiteMoveBtn.onClick.RemoveAllListeners();
        this.whiteMoveBtn.onClick.AddListener(() => OnMoveClick(this.whiteFEN));

        this.blackMove.text = "";
        this.blackMoveBtn.interactable = false;
    }
    #endregion

    #region 흑 이동을 표기하는 함수
    public void UpdateBlackMove(string blackMove, string blackFEN)
    {
        this.blackMove.text = blackMove;
        this.blackFEN = blackFEN;

        this.blackMoveBtn.onClick.RemoveAllListeners();
        this.blackMoveBtn.onClick.AddListener(() => OnMoveClick(this.blackFEN));

        blackMoveBtn.interactable = true;
    }
    #endregion

    #region 버튼 클릭시 작동하는 함수
    private void OnMoveClick(string targetFEN)
    {
        // 리플레이 시스템을 blackFEN으로 호출하여 보드 갱신
    }
    #endregion
}
