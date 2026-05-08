using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LoginFailUI : MonoBehaviour
{
    [SerializeField] private GameObject popupUI;
    [SerializeField] private TMP_Text messageText;
    [SerializeField] private Button closeButton;
    [SerializeField] private Button panelButton;

    #region + 유니티 함수

    #region Start 함수
    void Start()
    {
        closeButton.onClick.AddListener(() => popupUI.SetActive(false));
        panelButton.onClick.AddListener(() => popupUI.SetActive(false));

        popupUI.SetActive(false);
    }
    #endregion

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

    #endregion

    #region 로그인 실패 UI 출력
    private void ShowPopup(string errorMessage)
    {
        // 1. 로그인 실패 사유 출력
        messageText.text = errorMessage;

        // 2. 팝업UI 띄우기
        popupUI.SetActive(true);
    }
    #endregion
}
