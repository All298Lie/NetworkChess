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

    [Header("알람 UI")]
    [SerializeField] private GameObject alertPopUpUIPrefab;
    private AlertPopUpUI alert;

    [Header("연결 UI")]
    [SerializeField] private GameObject connectPopUpUIPrefab;
    private ConnectUI connectUI;

    #region + 유니티 함수

    #region Start 함수
    void Start()
    {
        // 1. UI 초기화
        InitializeAlertPopUpUI();
        InitializeConnectPopUpUI();

        // 2. 버튼 연결
        this.connectBtn.onClick.AddListener(OnSendLoginRequest);
        this.exitBtn.onClick.AddListener(OnExitGame);
    }
    #endregion

    #region OnEnable 함수
    void OnEnable()
    {
        NetworkManager.OnLoginFailed += HandleLoginFailed;
    }
    #endregion

    #region OnDisable 함수
    void OnDisable()
    {
        NetworkManager.OnLoginFailed -= HandleLoginFailed;
    }
    #endregion

    #endregion

    #region + 초기화 관련 함수

    #region 알람 UI 초기화 함수
    private void InitializeAlertPopUpUI()
    {
        GameObject alertPopUpUI = Instantiate(this.alertPopUpUIPrefab, transform);
        alertPopUpUI.name = "AlertPopUpUI";

        this.alert = alertPopUpUI.GetComponent<AlertPopUpUI>();
    }
    #endregion

    #region 연결 UI 초기화 함수
    private void InitializeConnectPopUpUI()
    {
        GameObject connectPopUpUI = Instantiate(this.connectPopUpUIPrefab, transform);
        connectPopUpUI.name = "ConnectPopUpUI";

        this.connectUI = connectPopUpUI.GetComponent<ConnectUI>();
    }
    #endregion

    #endregion - 초기화 관련 함수

    #region + 버튼 함수

    #region 로그인 요청 함수
    private void OnSendLoginRequest()
    {
        CLog.Log("[버튼 클릭] 서버 접속(시도)");

        // 1. 닉네임 문자열 받아오기
        string inputNickname = input.text.Trim();

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

    #region 로그인 실패 핸들
    private void HandleLoginFailed(string errorMessage)
    {
        this.alert.ShowPopup("로그인 실패", errorMessage);
    }
    #endregion
}