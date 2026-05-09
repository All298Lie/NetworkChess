using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LoginFailUI : MonoBehaviour
{
    [SerializeField] private GameObject popupUI;
    [SerializeField] private TMP_Text messageText;

    #region + 유니티 함수

    #region OnEnable 함수
    void OnEnable()
    {
        NetworkManager.OnLoginFailed += ShowPopup;
    }
    #endregion

    #region OnDisable 함수
    void OnDisable()
    {
        NetworkManager.OnLoginFailed -= ShowPopup;
    }
    #endregion

    #endregion - 유니티 함수

    #region 로그인 실패 UI 출력
    private void ShowPopup(string errorMessage)
    {
        // 1. 로그인 실패 사유 출력
        this.messageText.text = errorMessage;

        // 2. 팝업UI 띄우기
        this.popupUI.SetActive(true);
    }
    #endregion
}
