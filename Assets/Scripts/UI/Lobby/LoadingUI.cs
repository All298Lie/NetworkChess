using Cysharp.Threading.Tasks;
using NetworkChess.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LoadingUI : AnimateLoadUI
{
    [Header("취소 버튼")]
    [SerializeField] private Button background;
    [SerializeField] private Button cancelBtn;
    
    [Header("내용 텍스트")]
    [SerializeField] private TMP_Text message;

    [Header("팝업 UI")]
    [SerializeField] private GameObject popUpUI;

    #region + 유니티 함수

    #region Start 함수
    void Start()
    {
        background.onClick.AddListener(OnCancelButtonClick);
        cancelBtn.onClick.AddListener(OnCancelButtonClick);
    }
    #endregion

    #endregion - 유니티 함수

    #region 메세지 설정 후 UI를 팝업시켜주는 함수
    public void ShowWaiting(string message, bool canCancel = false)
    {
        // 1. 버튼 상태 변경
        this.background.enabled = canCancel;
        this.cancelBtn.gameObject.SetActive(canCancel);

        // 2. 메세지 수정
        this.message.text = message;

        // 3. 애니메이션 실행
        ExecuteAnimateTitleText();

        // 4. UI 팝업
        this.popUpUI.SetActive(true);
    }
    #endregion

    #region 취소 버튼 클릭 시 작동되는 함수
    private void OnCancelButtonClick()
    {
        if (NetworkManager.Instance == null) return;

        C2S_RoomLeaveReq req = new C2S_RoomLeaveReq();

        NetworkManager.Instance.SendPacket(req).Forget();
    }
    #endregion
}
