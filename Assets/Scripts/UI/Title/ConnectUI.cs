using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ConnectUI : AnimateLoadUI
{
    [Header("버튼")]
    [SerializeField] private Button retryBtn;
    [SerializeField] private TMP_Text retryBtnTxt;
    [SerializeField] private Button exitBtn;

    [Header("내용")]
    [SerializeField] private TMP_Text message;

    [Header("팝업 UI")]
    [SerializeField] private GameObject popUpUI;

    [Header("네트워크")]
    [SerializeField] private const int MAX_RETRY_COUNT = 3;
    [SerializeField] private const int RETRY_DELAY_MS = 5 * 1_000;

    private bool isConnecting = false;

    #region + 유니티 함수

    #region Awake 함수
    protected override void Awake()
    {
        if (NetworkManager.Instance == null)
        {
            Destroy(this.gameObject);

            return;
        }

        base.Awake();
    }
    #endregion

    #region Start 함수
    void Start()
    {
        // 1. 버튼 연결
        this.retryBtn.onClick.AddListener(OnRetryButtonClick);
        this.exitBtn.onClick.AddListener(OnExitButtonClick);

        // 2. 연결 시도
        TryConnectToServer().Forget();
    }
    #endregion

    #endregion - 유니티 함수

    #region 서버와 연결을 시도하는 함수
    private async UniTaskVoid TryConnectToServer()
    {
        // 1. NetworkManager가 존재하는지 확인
        if (NetworkManager.Instance == null) return;
        if (this.isConnecting == true) return;

        this.isConnecting = true;

        // 팝업 활성화
        this.popUpUI.SetActive(true);
        SetActiveRetryButton(false);
        this.message.text = "서버와 연결 중입니다...";
        ExecuteAnimateTitleText();

        // 2. 연결 시도
        bool isSuccess = await NetworkManager.Instance.ConnectAsync();

        // 3. 결과에 따른 작업 진행
        if (isSuccess == false) // 연결 실패일 경우
        {
            ConnectWithRetryAsync().Forget();
        }
        else
        {
            CancelAnimateTitleText();

            this.popUpUI.SetActive(false);
            this.isConnecting = false;
        }
    }
    #endregion

    #region 서버와 연결을 재시도하는 함수
    private async UniTaskVoid ConnectWithRetryAsync()
    {
        // 1. 재시도 버튼 비활성화
        SetActiveRetryButton(false);

        // 2. 연결 재시도
        for (int tryCount = 1; tryCount <= MAX_RETRY_COUNT; tryCount++)
        {
            this.message.text = $"서버와 연결 중입니다... ({tryCount}/{MAX_RETRY_COUNT})";

            bool isSuccess = await NetworkManager.Instance.ConnectAsync();

            if (isSuccess == true) // 연결에 성공한 경우
            {
                this.popUpUI.SetActive(false);
                this.isConnecting = false;

                CancelAnimateTitleText();

                return;
            }

            if (tryCount < MAX_RETRY_COUNT) await UniTask.Delay(RETRY_DELAY_MS);
        }

        // 3. 모든 연결 재시도에서 실패했을 경우
        CancelAnimateTitleText();

        this.isConnecting = false;
        this.message.text = "서버와 연결할 수 없습니다.\n인터넷 상태를 확인해 주세요.";

        // 4. 재시도 버튼 활성화
        SetActiveRetryButton(true);
    }
    #endregion

    #region 재시작 버튼 활성화 상태 설정
    private void SetActiveRetryButton(bool isActive)
    {
        // 버튼 설정
        retryBtn.interactable = isActive;

        if (isActive == true)
        {
            // 활성화 색상으로 변경
            retryBtnTxt.color = new Color(226f / 255f, 232f / 255f, 240f / 255f); // HEX #E2E8F0
        }
        else
        {
            // 비활성화 색상으로 변경
            retryBtnTxt.color = new Color(71f / 255f, 85f / 255f, 105f / 255f); // HEX #475569
        }
    }
    #endregion

    #region 재시작 버튼 클릭 시 작동되는 함수
    private void OnRetryButtonClick()
    {
        if (this.isConnecting == true) return;

        CLog.Log("[버튼 클릭] 연결 재시도");

        this.isConnecting = true;
        ExecuteAnimateTitleText();
        ConnectWithRetryAsync().Forget();
    }
    #endregion

    #region 종료 버튼 클릭 시 작동되는 함수
    private void OnExitButtonClick()
    {
        CLog.Log("[버튼 클릭] 게임 종료");

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif

    }
    #endregion
}
