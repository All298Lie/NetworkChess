using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LobbyUIManager : MonoBehaviour
{
    [Header("버튼 UI")]
    [SerializeField] private Button gameStartBtn; // 게임시작
    [SerializeField] private Button spectateBtn; // 관전하기
    [SerializeField] private Button replayBtn; // 기보 복기
    [SerializeField] private Button settingsBtn; // 환경설정(아이콘 형태)
    [SerializeField] private Button disconnectBtn; // 접속종료

    [Header("환경설정 UI")]
    [SerializeField] private GameObject optionUIPrefab;
    private PopUpUI optionUI;

    [Header("유저 닉네임 UI")]
    [SerializeField] private GameObject playerUI;
    [SerializeField] private TMP_Text nicknameText;

    #region Start 함수
    void Start()
    {
        // 환경설정 UI 초기화
        InitializeOptionUI();

        // 로비 버튼에 이벤트 연결
        this.gameStartBtn.onClick.AddListener(OnStartGame);
        this.settingsBtn.onClick.AddListener(OnSettings);
        this.disconnectBtn.onClick.AddListener(OnDisConnectServer);

        // 플레이어 UI 초기화
        InitializePlayerUI();
    }
    #endregion

    #region + 초기화 함수

    #region OptionUI 초기화 함수
    private void InitializeOptionUI()
    {
        // 1. 프리팹을 통한 생성
        GameObject optionUI = Instantiate(optionUIPrefab, transform);
        optionUI.name = "OptionUI";

        // 2. 환경설정 UI 변수에 담기
        this.optionUI = optionUI.GetComponent<PopUpUI>();
    }
    #endregion

    #region PlayerUI 초기화 함수
    private void InitializePlayerUI()
    {
        if (NetworkManager.Instance != null) // 네트워크 연결이 되어있을 경우, 닉네임 표기
        {
            this.nicknameText.text = NetworkManager.Instance.MyNickname;
        }
        else // 연결 안 되어있을 경우, UI 닫기
        {
            playerUI.SetActive(false);
        }
    }
    #endregion

    #endregion - 초기화 함수

    #region + 버튼 함수

    #region 게임시작 버튼을 눌렀을 때 작동하는 함수
    private void OnStartGame()
    {
        CLog.Log("[버튼 클릭] 게임 시작");

        // 방 생성 참가 UI 추가 필요

        SceneManager.LoadScene("GameScene");
    }
    #endregion

    #region 환경설정 버튼을 눌렀을 때 작동하는 함수
    private void OnSettings()
    {
        CLog.Log("[버튼 클릭] 환경설정");
        this.optionUI.OpenPopUpUI();
    }
    #endregion

    #region 기보 복기 버튼을 눌렀을 때 작동하는 함수
    private void OnReplay()
    {
        CLog.Log("[버튼 클릭] 기보 복기");

        // TODO : 기보 복기 구현
    }
    #endregion

    #region 접속종료 버튼을 눌렀을 때 작동하는 함수
    private void OnDisConnectServer()
    {
        CLog.Log("[버튼 클릭] 접속 종료");

        // 1. 로그인을 관리하고 있던 NetworkManager 삭제
        if (NetworkManager.Instance != null)
        {
            Destroy(NetworkManager.Instance.gameObject);
        }

        // 2. 타이틀 씬으로 이동
        SceneManager.LoadScene("TitleScene");
    }
    #endregion

    #endregion - 버튼 함수
}
