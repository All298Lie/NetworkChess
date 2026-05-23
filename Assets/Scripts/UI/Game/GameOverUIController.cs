using Cysharp.Threading.Tasks;
using NetworkChess.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameOverUIController : MonoBehaviour
{
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

    #region Start 함수
    void Awake()
    {
        gameObject.SetActive(false);

        this.returnLobbyBtn.onClick.AddListener(OnReturnToLobby);
        this.replayBtn.onClick.AddListener(OnReplayClick);
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
        this.returnLobbyBtn.onClick.RemoveAllListeners();
        this.replayBtn.onClick.RemoveAllListeners();

        C2S_RoomLeaveReq req = new C2S_RoomLeaveReq();
        NetworkManager.Instance.SendPacket(req).Forget();
    }
    #endregion

    #region 게임 리뷰 버튼을 누를 때 작동하는 함수
    private void OnReplayClick()
    {
        this.returnLobbyBtn.onClick.RemoveAllListeners();
        this.replayBtn.onClick.RemoveAllListeners();

        C2S_ReplayReq req = new C2S_ReplayReq();
        req.ReplayCode = this.replayCode;

        NetworkManager.Instance.SendPacket(req).Forget();
    }
    #endregion

    #endregion - 버튼 클릭 관련 함수
}
