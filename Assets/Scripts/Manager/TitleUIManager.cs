using Cysharp.Threading.Tasks;
using NetworkChess.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TitleUIManager: MonoBehaviour
{
    [Header("버튼 UI")]
    [SerializeField] private TMP_InputField input;
    [SerializeField] private Button connectBtn;
    [SerializeField] private Button exitBtn;

    [SerializeField] private GameObject loginFailUIPrefab;

    #region Start 함수
    void Start()
    {
        // 1. 로그인 실패 UI 초기화
        InitializeLoginFailUI();

        // 2. 버튼 연결
        connectBtn.onClick.AddListener(OnSendLoginRequest);
        exitBtn.onClick.AddListener(OnExitGame);
    }
    #endregion

    #region 로그인 실패 UI 초기화 함수
    private void InitializeLoginFailUI()
    {
        GameObject loginFailUI = Instantiate(loginFailUIPrefab, transform);
        loginFailUI.name = "LoginFailUI";
    }
    #endregion

    #region + 버튼 함수

    #region 로그인 요청 함수
    private void OnSendLoginRequest()
    {
        CLog.Log("[버튼 클릭] 서버 접속(시도)");

        // 1. 닉네임 문자열 받아오기
        string inputNickname = input.text;

        // 2. 패킷으로 저장
        C2S_LoginReq req = new C2S_LoginReq();
        req.Nickname = inputNickname;

        // 3. 서버에 로그인 요청
        NetworkManager.Instance.SendPacket(req).Forget();
    }
    #endregion

    #region 게임 종료 함수
    private void OnExitGame()
    {
        CLog.Log("[버튼 클릭] 게임 종료");

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
    #endregion

    #endregion - 버튼 함수
}