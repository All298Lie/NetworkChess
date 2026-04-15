using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameOverUIController : MonoBehaviour
{
    [Header("게임종료 UI")]
    [SerializeField] private GameObject popUpContainer;
    [SerializeField] private TextMeshProUGUI resultText;
    [SerializeField] private Button returnLobbyBtn;

    void Awake()
    {
        gameObject.SetActive(false);

        // 로비로 이동하기 버튼 클릭시 OnReturnToLobby() 함수 호출
        this.returnLobbyBtn.onClick.AddListener(OnReturnToLobby);
    }

    // 게임 종료 시 작동되는 함수
    public void ShowGameOver(string winnerName, string reason)
    {
        gameObject.SetActive(true);

        if (winnerName == "$Draw")
        {
            this.resultText.text = $"무승부\n <size=50%>({reason})</size>";
        }
        else
        {
            this.resultText.text = $"{winnerName} 승리 !\n <size=50%>({reason})</size>";
        }
    }

    // 로비로 이동 버튼을 누를 때 작동하는 함수
    private void OnReturnToLobby()
    {
        SceneManager.LoadScene("LobbyScene");
    }
}
