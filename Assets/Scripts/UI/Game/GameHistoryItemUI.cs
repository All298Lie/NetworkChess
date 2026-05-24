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

    private int whitePlayIndex;
    private int blackPlayIndex;

    #region 백 이동을 표기하는 함수
    public void SetWhiteMove(int fullMoveNumber, string whiteMove, int playIndex)
    {
        this.fullMoveNumber.text = $"{fullMoveNumber}.";

        this.whiteMove.text = whiteMove;
        this.whitePlayIndex = playIndex;

        this.whiteMoveBtn.onClick.RemoveAllListeners();
        this.whiteMoveBtn.onClick.AddListener(() => OnMoveClick(this.whitePlayIndex));

        this.blackMove.text = "";
        this.blackMoveBtn.interactable = false;
    }
    #endregion

    #region 흑 이동을 표기하는 함수
    public void UpdateBlackMove(string blackMove, int playIndex)
    {
        this.blackMove.text = blackMove;
        this.blackPlayIndex = playIndex;

        this.blackMoveBtn.onClick.RemoveAllListeners();
        this.blackMoveBtn.onClick.AddListener(() => OnMoveClick(this.blackPlayIndex));

        this.blackMoveBtn.interactable = true;
    }
    #endregion

    #region 흑 이동을 삭제하는 함수
    public bool RemoveBlackMove()
    {
        if (this.blackMoveBtn.interactable == false) return false;

        this.blackMoveBtn.onClick.RemoveAllListeners();

        this.blackMove.text = "";
        this.blackMoveBtn.interactable = false;

        return true;
    }
    #endregion

    #region 버튼 클릭시 작동하는 함수
    private void OnMoveClick(int playIndex)
    {
        ReplayManager.Instance.JumpToPly(playIndex);
    }
    #endregion
}
