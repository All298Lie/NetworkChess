using Cysharp.Threading.Tasks;
using NetworkChess.Core;
using System;
using System.Threading;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameOverUIController : MonoBehaviour
{
    private AlertPopUpUI alert;

    [Header("게임종료 UI")]
    [SerializeField] private GameObject popUpContainer;

    [Header("내용")]
    [SerializeField] private TMP_Text resultTxt;
    [SerializeField] private TMP_Text reasonTxt;
    [SerializeField] private TMP_Text replayCodeTxt;

    private string replayCode;

    [Header("버튼")]
    [SerializeField] private Button returnLobbyBtn;
    [SerializeField] private Button replayBtn;
    private bool isProgress;

    private CancellationTokenSource timeoutCts;
    private const int TIMEOUT_SECONDS = 5;

    #region + 유니티 함수

    #region Start 함수
    void Awake()
    {
        isProgress = false;

        gameObject.SetActive(false);

        this.returnLobbyBtn.onClick.AddListener(OnReturnToLobby);
        this.replayBtn.onClick.AddListener(OnReplayClick);
    }
    #endregion

    #region OnDisable 함수
    void OnDisable()
    {
        if (this.timeoutCts != null)
        {
            this.timeoutCts.Cancel();
            
            this.timeoutCts = null;
        }
    }
    #endregion

    #endregion - 유니티 함수

    #region 게임UI매니저 주입 함수
    public void Setup(AlertPopUpUI alert)
    {
        this.alert = alert;
    }
    #endregion

    #region 게임 종료 시 작동되는 함수
    public void ShowGameOver(string winner, string reason, string replayCode)
    {
        // 1. UI 팝업
        gameObject.SetActive(true);

        // 2. 결과 표시
        if (winner == "$Draw")
        {
            this.resultTxt.text = "무승부";
        }
        else if (winner == NetworkManager.Instance.MyNickname)
        {
            this.resultTxt.text = "승리";
        }
        else
        {
            this.resultTxt.text = "패배";
        }

        // 3. 이유 표시
        this.reasonTxt.text = reason;

        // 4. 공유 코드 표시
        this.replayCode = replayCode;
        this.replayCodeTxt.text = $"공유 코드 : {replayCode}";
    }
    #endregion

    public void CloseGameOverUI()
    {
        gameObject.SetActive(false);
    }

    #region + 버튼 클릭 관련 함수

    #region 로비로 이동 버튼을 누를 때 작동하는 함수
    private void OnReturnToLobby()
    {
        // 1. 작업 중인지 확인
        if (this.isProgress == true) return;

        // 2. 버튼 비활성화
        this.isProgress = true;

        // 3. 방 나가기 요청
        C2S_RoomLeaveReq req = new C2S_RoomLeaveReq();

        NetworkManager.Instance.SendPacket(req).Forget();

        // 4. 타임아웃 감시 타이머 구동
        StartTimeoutTimer().Forget();
    }
    #endregion

    #region 게임 리뷰 버튼을 누를 때 작동하는 함수
    private void OnReplayClick()
    {
        // 1. 작업 중인지 확인
        if (this.isProgress == true) return;

        // 2. 버튼 비활성화
        this.isProgress = true;

        // 3. 리플레이 요청
        C2S_ReplayReq req = new C2S_ReplayReq();
        req.ReplayCode = this.replayCode;

        NetworkManager.Instance.SendPacket(req).Forget();

        // 4. 타임아웃 감시 타이머 구동
        StartTimeoutTimer().Forget();
    }
    #endregion

    #endregion - 버튼 클릭 관련 함수

    #region 타임아웃 감시 비동기 함수
    private async UniTaskVoid StartTimeoutTimer()
    {
        // 혹시 기존에 돌고 있던 타이머가 있다면 취소 후 새로 생성
        this.timeoutCts?.Cancel();
        this.timeoutCts = new CancellationTokenSource();

        try
        {
            // 설정한 시간만큼 비동기로 대기 (토큰을 연결하여 중도 취소 가능하게 만듦)
            await UniTask.Delay(TimeSpan.FromSeconds(TIMEOUT_SECONDS), cancellationToken: timeoutCts.Token);

            // 💡 만약 취소되지 않고 지정된 시간을 다 채웠다면 타임아웃 발생!
            Debug.LogWarning($"<color=red>[네트워크]</color> 서버 응답 시간 초과");

            this.alert.ShowPopup("오류", "서버 응답 시간이 초과되었습니다. 다시 시도해 주세요.");

            this.isProgress = false;
        }
        catch (OperationCanceledException)
        {
            // 정상적으로 서버 응답을 받아 중간에 취소된 경우
            Debug.Log($"<color=green>[네트워크]</color> 타임아웃 타이머 정상 안전 종료");
        }
    }
    #endregion

    #region 서버로부터 응답 패킷이 도착했을 때 호출되는 함수
    public void HandleRoomLeaveResponse(S2C_RoomLeaveRes res)
    {
        timeoutCts?.Cancel();

        if (res.IsSuccess == false)
        {
            // 서버가 명시적으로 실패 이유를 보내온 경우 (예: "방이 이미 파괴됨" 등)
            this.alert.ShowPopup("오류", res.Message);

            this.isProgress = false;
        }
    }
    #endregion
}
