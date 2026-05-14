using Cysharp.Threading.Tasks;
using NetworkChess.Core;
using System;
using System.Threading;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LoadingUI : MonoBehaviour
{
    [SerializeField] private Button background;
    [SerializeField] private Button cancelBtn;
    [SerializeField] private GameObject cancelButton;

    [SerializeField] private TMP_Text title;
    [SerializeField] private TMP_Text message;
    private string originTitleText;

    [SerializeField] private GameObject popUpUI;

    private CancellationTokenSource cts;

    #region + 유니티 함수

    #region Start 함수
    void Start()
    {
        this.originTitleText = this.title.text;

        background.onClick.AddListener(OnCancelButtonClick);
        cancelBtn.onClick.AddListener(OnCancelButtonClick);
    }
    #endregion

    #region OnDisable 함수
    void OnDisable()
    {
        if (cts != null)
        {
            cts.Cancel();
            cts.Dispose();
            cts = null;
        }
    }
    #endregion

    #endregion - 유니티 함수

    #region 메세지 설정 후 UI를 팝업시켜주는 함수
    public void ShowWaiting(string message, bool canCancel = false)
    {
        // 1. 버튼 상태 변경
        this.background.enabled = canCancel;
        this.cancelButton.SetActive(canCancel);

        // 2. 메세지 수정
        this.message.text = message;

        // 3. 애니메이션 초기화
        if (this.cts != null)
        {
            this.cts.Cancel();
            this.cts.Dispose();
        }

        this.cts = new CancellationTokenSource();

        // 4. UniTask 애니메이션 실행
        AnimateTitleTextAsync(this.cts.Token).Forget();

        // 3. UI 팝업
        this.popUpUI.SetActive(true);
    }
    #endregion

    #region 비동기 타이틀 로딩 연출 함수
    private async UniTaskVoid AnimateTitleTextAsync(CancellationToken token)
    {
        int dotIndex = 0;
        string[] dots = { "", ".", "..", "..."};

        try
        {
            while (token.IsCancellationRequested == false)
            {
                this.title.text = originTitleText + dots[dotIndex];
                dotIndex = (dotIndex + 1) % dots.Length;

                await UniTask.Delay(500, cancellationToken: token);
            }
        }
        catch (OperationCanceledException)
        {
            CLog.Log("[UI] 로딩 종료");
        }
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
