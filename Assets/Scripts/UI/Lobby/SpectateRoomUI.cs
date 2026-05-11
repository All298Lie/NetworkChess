using Cysharp.Threading.Tasks;
using NetworkChess.Core;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SpectateRoomUI : MonoBehaviour
{
    [Header("게임 관전 UI")]
    [SerializeField] private GameObject popUpUI;
    [SerializeField] private TMP_InputField input;
    [SerializeField] private Button spectateRoomBtn;

    private AlertPopUpUI alertPopUpUI;

    void Start()
    {
        this.spectateRoomBtn.onClick.AddListener(OnSpectateRoom);

        this.popUpUI.SetActive(false);
    }

    public void Initialize(AlertPopUpUI alert)
    {
        this.alertPopUpUI = alert;
    }

    private void OnSpectateRoom()
    {
        // 1. 입력된 텍스트 가져오기
        string targetName = this.input.text.Trim();

        if (string.IsNullOrEmpty(targetName) == true)
        {
            CLog.LogWarning("[방 관전] 참가할 방의 대전자 닉네임을 입력해야 합니다.");

            this.alertPopUpUI.ShowPopup("게임 관전", "참가할 방의 게임 중인 유저 닉네임을 입력해야 합니다.");

            return;
        }

        // 3. 자체 검증에 통과했을 경우 패킷 생성 및 서버 전송
        CLog.Log($"[네트워크] '{targetName}'님의 방으로 참가 요청을 보냅니다.");

        C2S_RoomJoinReq req = new C2S_RoomJoinReq();
        req.TargetNickname = targetName;
        req.IsSpectator = true;

        if (NetworkManager.Instance != null)
        {
            NetworkManager.Instance.SendPacket(req).Forget();
        }
    }
}
