using TMPro;
using UnityEngine;

public class AlertPopUpUI : MonoBehaviour
{
    [SerializeField] private GameObject popupUI;
    [SerializeField] private TMP_Text messageText;

    #region 로그인 실패 UI 출력
    public void ShowPopup(string errorMessage)
    {
        // 1. 로그인 실패 사유 출력
        this.messageText.text = errorMessage;

        // 2. 팝업UI 띄우기
        this.popupUI.SetActive(true);
    }
    #endregion
}
