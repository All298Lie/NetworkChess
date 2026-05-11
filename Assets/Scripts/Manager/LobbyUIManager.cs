using NetworkChess.Core;
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

    [Header("게임시작 UI")]
    [SerializeField] private GameObject gameStartUIPrefab;
    private PopUpUI gameStartUI;

    [Header("방 생성 UI")]
    [SerializeField] private GameObject createRoomUIPrefab;
    private PopUpUI createRoomUI;

    [Header("방 참가 UI")]
    [SerializeField] private GameObject joinRoomUIPrefab;
    private PopUpUI joinRoomUI;

    [Header("게임 관전 UI")]
    [SerializeField] private GameObject spectateRoomUIPrefab;
    private PopUpUI spectateRoomUI;

    [Header("유저 닉네임 UI")]
    [SerializeField] private GameObject playerUI;
    [SerializeField] private TMP_Text nicknameText;

    [Header("알림 UI")]
    [SerializeField] private GameObject alertPopUpUIPrefab;
    private PopUpUI alertPopUpUI;
    private AlertPopUpUI alert;

    [Header("로딩 UI")]
    [SerializeField] private GameObject loadingPopUpUIPrefab;
    private PopUpUI loadingUI;
    private LoadingUI loading;

    [Header("매칭 UI")]
    [SerializeField] private GameObject matchRoomUIPrefab;
    private PopUpUI matchRoomUI;
    private AlertPopUpUI match;

    #region + 유니티 함수

    #region Start 함수
    void Start()
    {
        // 환경설정 UI 초기화
        InitializeOptionUI();

        // 로비 버튼에 이벤트 연결
        this.gameStartBtn.onClick.AddListener(OnStartGame);
        this.spectateBtn.onClick.AddListener(OnSpectateGame);
        this.settingsBtn.onClick.AddListener(OnSettings);
        this.disconnectBtn.onClick.AddListener(OnDisConnectServer);

        // 알람 UI 초기화
        InitializeAlertPopUpUI();
        InitializeMatchRoomUI();

        // 플레이어 UI 초기화
        InitializePlayerUI();

        // 게임시작 UI, 방 생성 UI, 방 참가 UI 초기화
        InitializeGameStartUI();
        InitializeCreateRoomUI();
        InitializeJoinRoomUI();

        InitializeSpectateRoomUI();

        // 로딩 UI 초기화
        InitializeLoadingPopUpUI();

        // 알람 UI와 매치 성공 UI가 화면 최상단에 위치해야하므로 위치 조정
        this.alertPopUpUI.transform.SetAsLastSibling();
        this.matchRoomUI.transform.SetAsLastSibling();
    }
    #endregion

    #region OnEnable 함수
    void OnEnable()
    {
        NetworkManager.OnRoomFailed += HandleRoomJoinFailed;
        NetworkManager.OnRoomCreateSuccess += HandleRoomCreateSuccess;
        NetworkManager.OnRoomJoinSuccess += HandleRoomJoinSuccess;

        NetworkManager.OnRoomLeave += HandleRoomLeave;

        NetworkManager.OnMatchStarted += HandleRoomMatch;
    }
    #endregion

    #region OnDisable 함수
    void OnDisable()
    {
        NetworkManager.OnRoomFailed -= HandleRoomJoinFailed;
        NetworkManager.OnRoomCreateSuccess -= HandleRoomCreateSuccess;
        NetworkManager.OnRoomJoinSuccess -= HandleRoomJoinSuccess;

        NetworkManager.OnRoomLeave -= HandleRoomLeave;

        NetworkManager.OnMatchStarted -= HandleRoomMatch;
    }
    #endregion

    #endregion - 유니티 함수

    #region + 초기화 함수

    #region OptionUI 초기화 함수
    private void InitializeOptionUI()
    {
        // 1. 프리팹을 통한 생성
        GameObject optionUI = Instantiate(this.optionUIPrefab, transform);
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
            this.playerUI.SetActive(false);
        }
    }
    #endregion

    #region GameStartUI 초기화 함수
    private void InitializeGameStartUI()
    {
        // 1. 프리팹을 통한 생성
        GameObject gameStartUI = Instantiate(this.gameStartUIPrefab, transform);
        gameStartUI.name = "GameStartUI";

        // 2. 게임시작 UI 변수에 담기
        this.gameStartUI = gameStartUI.GetComponent<PopUpUI>();

        // 3. 게임 시작 UI 초기화

        GameStartUI ui = gameStartUI.GetComponent<GameStartUI>();
        ui.Initialize(this.OpenCreateRoomUI, this.OpenJoinRoomUI);
    }
    #endregion

    #region CreateRoomUI 초기화 함수
    private void InitializeCreateRoomUI()
    {
        // 1. 프리팹을 통한 생성
        GameObject createRoomUI = Instantiate(this.createRoomUIPrefab, transform);
        createRoomUI.name = "CreateRoomUI";

        // 2. 방 생성 UI 변수에 담기
        this.createRoomUI = createRoomUI.GetComponent<PopUpUI>();

        // 3. 방 생성 UI 초기화
        CreateRoomUI ui = createRoomUI.GetComponent<CreateRoomUI>();
        ui.Initialize(this.OpenGameStartUI);
    }
    #endregion

    #region JoinRoomUI 초기화 함수
    private void InitializeJoinRoomUI()
    {
        // 1. 프리팹을 통한 생성
        GameObject joinRoomUI = Instantiate(this.joinRoomUIPrefab, transform);
        joinRoomUI.name = "JoinRoomUI";

        // 2. 방 참가 UI 변수에 담기
        this.joinRoomUI = joinRoomUI.GetComponent<PopUpUI>();

        // 3. 방 참가 UI 초기화
        JoinRoomUI ui = joinRoomUI.GetComponent<JoinRoomUI>();
        ui.Initialize(this.alert, this.OpenGameStartUI);
    }
    #endregion

    #region SpectateRoomUI 초기화 함수
    private void InitializeSpectateRoomUI()
    {
        // 1. 프리팹을 통한 생성
        GameObject spectateRoomUI = Instantiate(this.spectateRoomUIPrefab, transform);
        spectateRoomUI.name = "SpectateRoomUI";

        // 2. 게임 관전 UI 변수에 담기
        this.spectateRoomUI = spectateRoomUI.GetComponent<PopUpUI>();

        // 3. 게임 관전 UI 초기화
        SpectateRoomUI ui = spectateRoomUI.GetComponent<SpectateRoomUI>();
        ui.Initialize(this.alert);
    }
    #endregion

    #region AlertPopUpUI 초기화 함수
    private void InitializeAlertPopUpUI()
    {
        // 1. 프리팹을 통한 생성
        GameObject alertPopUpUI = Instantiate(this.alertPopUpUIPrefab, transform);
        alertPopUpUI.name = "AlertPopUpUI";

        // 2. 알림 팝업 UI 변수에 담기
        this.alertPopUpUI = alertPopUpUI.GetComponent<PopUpUI>();
        this.alert = alertPopUpUI.GetComponent<AlertPopUpUI>();
    }
    #endregion

    #region LoadingPopUpUI 초기화 함수
    private void InitializeLoadingPopUpUI()
    {
        // 1. 프리팹을 통한 생성
        GameObject loadingPopUpUI = Instantiate(this.loadingPopUpUIPrefab, transform);
        loadingPopUpUI.name = "LoadingPopUpUI";

        // 2. 로딩 팝업 UI 변수에 담기
        this.loadingUI = loadingPopUpUI.GetComponent<PopUpUI>();
        this.loading = loadingPopUpUI.GetComponent<LoadingUI>();
    }
    #endregion

    #region MatchRoomUI 초기화 함수
    private void InitializeMatchRoomUI()
    {
        // 1. 프리팹을 통한 생성
        GameObject matchRoomUI = Instantiate(this.matchRoomUIPrefab, transform);
        matchRoomUI.name = "MatchRoomUI";

        // 2. 매치 성공 UI 변수에 담기
        this.matchRoomUI = matchRoomUI.GetComponent<PopUpUI>();
        this.match = matchRoomUI.GetComponent<AlertPopUpUI>();
    }
    #endregion

    #endregion - 초기화 함수

    #region + 버튼 함수

    #region 게임시작 버튼을 눌렀을 때 작동하는 함수
    private void OnStartGame()
    {
        CLog.Log("[버튼 클릭] 게임 시작");
        this.gameStartUI.OpenPopUpUI();

        // SceneManager.LoadScene("GameScene");
    }
    #endregion

    #region 게임관전 버튼을 눌렀을 때 작동하는 함수
    private void OnSpectateGame()
    {
        CLog.Log("[버튼 클릭] 게임 관전");
        this.spectateRoomUI.OpenPopUpUI();
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

    #region 방 생성 버튼을 눌렀을 때 작동하는 함수(게임시작 UI 전용)
    private void OpenCreateRoomUI()
    {
        CLog.Log("[버튼 클릭] 방 생성");

        this.gameStartUI.ClosePopUpUI();
        this.createRoomUI.OpenPopUpUI();
    }
    #endregion

    #region 방 참가 버튼을 눌렀을 때 작동하는 함수(게임시작 UI 전용)
    private void OpenJoinRoomUI()
    {
        CLog.Log("[버튼 클릭] 방 참가");

        this.gameStartUI.ClosePopUpUI();
        this.joinRoomUI.OpenPopUpUI();
    }
    #endregion

    #region 뒤로가기 버튼을 눌렀을 때 작동하는 함수(방 참가/생성 UI 전용)
    private void OpenGameStartUI()
    {
        CLog.Log("[버튼 클릭] 뒤로가기");

        this.createRoomUI.ClosePopUpUI();
        this.joinRoomUI.ClosePopUpUI();

        this.gameStartUI.OpenPopUpUI();
    }
    #endregion

    #endregion - 버튼 함수

    #region 이벤트 처리 핸들러

    #region 방 참가 실패 핸들러
    private void HandleRoomJoinFailed(string errorMessage)
    {
        if (alert != null)
        {
            alert.ShowPopup("방 참가", errorMessage);
        }
    }
    #endregion

    #region 방 참가 성공 핸들러
    private void HandleRoomJoinSuccess()
    {
        // 1. 팝업 UI 닫기
        joinRoomUI.ClosePopUpUI();

        // 2. 로딩 UI 띄우기(취소 기능 X)
        loading.ShowWaiting("방에 입장했습니다. 게임 시작을 준비합니다...");
    }
    #endregion

    #region 방 생성 성공 핸들러
    private void HandleRoomCreateSuccess()
    {
        // 1. 팝업 UI 닫기
        createRoomUI.ClosePopUpUI();

        // 2. 로딩 UI 띄우기(취소 기능 O)
        loading.ShowWaiting("상대방을 기다리는 중입니다...", true);
    }
    #endregion

    #region 방 매칭 완료 핸들러
    private void HandleRoomMatch(bool isWhite, GameMode gameMode, string startingFEN)
    {
        // 1. 로딩 UI 닫기
        loadingUI.ClosePopUpUI();

        // 2. 매칭 성공 알람 출력
        string teamText = isWhite ? "백(White)" : "흑(Black)";
        match.ShowPopup("매치 성공", $"당신은 {teamText} 진영입니다.\n곧 게임이 시작됩니다.");

        // 3. 인게임 데이터 준비

    }
    #endregion

    #region 방 나가기 핸들러
    private void HandleRoomLeave()
    {
        // 1. 로딩 UI 닫기
        loadingUI.ClosePopUpUI();
    }
    #endregion

    #endregion
}
