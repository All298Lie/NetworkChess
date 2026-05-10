using Cysharp.Threading.Tasks;
using NetworkChess.Core;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class JoinRoomUI : RoomPopUpBase
{
    [Header("새 게임 UI")]
    [SerializeField] private GameObject popUpUI;
    [SerializeField] private TMP_InputField input;
    [SerializeField] private Button joinRoomBtn;

    private AlertPopUpUI alertPopUpUI;

    void Start()
    {
        this.joinRoomBtn.onClick.AddListener(OnJoinRoom);

        this.popUpUI.SetActive(false);
    }

    public void Initialize(AlertPopUpUI alert, Action onBackButtonClick)
    {
        base.InitializeBase(onBackButtonClick);

        this.alertPopUpUI = alert;
    }

    private void OnJoinRoom()
    {
        // 1. 입력된 텍스트 가져오기
        string targetName = this.input.text.Trim();

        if (string.IsNullOrEmpty(targetName) == true)
        {
            CLog.LogWarning("[방 참가] 참가할 방장 닉네임을 입력해야 합니다.");

            this.alertPopUpUI.ShowPopup("참가할 방의 유저 닉네임을 입력해야 합니다.");

            return;
        }

        // 3. 자체 검증에 통과했을 경우 패킷 생성 및 서버 전송
        CLog.Log($"[네트워크] '{targetName}'님의 방으로 참가 요청을 보냅니다.");

        C2S_RoomJoinReq req = new C2S_RoomJoinReq();
        req.TargetNickname = targetName;
        req.IsSpectator = false;

        if (NetworkManager.Instance != null)
        {
            NetworkManager.Instance.SendPacket(req).Forget();
        }
    }
}
