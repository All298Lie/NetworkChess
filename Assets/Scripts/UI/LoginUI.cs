using Cysharp.Threading.Tasks;
using NetworkChess.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LoginUI : MonoBehaviour
{
    [SerializeField] private TMP_InputField input;
    [SerializeField] private Button loginButton;    

    #region Start 함수
    void Start()
    {
        loginButton.onClick.AddListener(SendLoginRequest);
    }
    #endregion

    #region 로그인 요청 함수
    private void SendLoginRequest()
    {
        // 1. 닉네임 문자열 받아오기
        string inputNickname = input.text;

        // 2. 패킷으로 저장
        C2S_LoginReq req = new C2S_LoginReq();
        req.Nickname = inputNickname;

        // 3. 서버에 로그인 요청
        NetworkManager.Instance.SendPacket(req).Forget();
    }
    #endregion
}