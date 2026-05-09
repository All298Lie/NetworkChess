using UnityEngine;
using UnityEngine.UI;

public class PopUpUI : MonoBehaviour
{
    [SerializeField] private GameObject popUpUI;

    [SerializeField] private Button background;
    [SerializeField] private Button closeButton;

    #region Start 함수
    void Start()
    {
        // 1. 버튼 연결(연결되어있는 것만 추가)
        background?.onClick.AddListener(ClosePopUpUI);
        closeButton?.onClick.AddListener(ClosePopUpUI);

        // 2. 팝업 UI 기본적으로 끄도록 설정
        ClosePopUpUI();
    }
    #endregion

    #region 열기 버튼을 눌렀을 때 팝업UI를 띄우는 함수
    public void OpenPopUpUI()
    {
        CLog.Log("[버튼 클릭] 팝업 열기");
        this.popUpUI.SetActive(true);
    }
    #endregion

    #region 닫기 버튼을 눌렀을 때 팝업UI를 닫는 함수
    private void ClosePopUpUI()
    {
        CLog.Log("[버튼 클릭] 팝업 닫기");
        this.popUpUI.SetActive(false);
    }
    #endregion
}