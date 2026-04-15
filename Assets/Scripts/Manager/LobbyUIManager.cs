using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LobbyUIManager : MonoBehaviour
{
    [Header("메인 로비 UI")]
    [SerializeField] private Button gameStartBtn;
    [SerializeField] private Button settingsBtn;

    [Header("패널")]
    [SerializeField] private GameObject settingsPanel;

    void Start()
    {
        this.settingsPanel.SetActive(false); // 시작시 설정창 닫기
    }

    // 게임시작 버튼을 눌렀을 때 작동하는 함수
    public void OnStartGameClicked()
    {
        Debug.Log("게임 씬으로 이동합니다.");
        SceneManager.LoadScene("GameScene");
    }

    // 환경설정 버튼을 눌렀을 때 작동하는 함수
    public void OnSettingsClicked()
    {
        Debug.Log("환경설정 패널을 활성화합니다.");
        this.settingsPanel.SetActive(true);
    }

    // 환경설정 닫기 버튼을 눌렀을 때 작동하는 함수
    public void OnCloseSettingsClicked()
    {
        Debug.Log("환경설정 패널을 닫습니다.");
        this.settingsPanel.SetActive(false);
    }
}
