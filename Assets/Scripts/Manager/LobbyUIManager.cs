using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LobbyUIManager : MonoBehaviour
{
    [Header("메인 로비 UI")]
    [SerializeField] private Button gameStartBtn;
    [SerializeField] private Button settingsBtn;
    [SerializeField] private Button exitBtn;

    [Header("패널")]
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private Button settingsPanelBtn;
    [SerializeField] private Button closeSettingsBtn;

    void Start()
    {
        this.settingsPanel.SetActive(false); // 시작시 설정창 닫기

        // 로비 버튼에 이벤트 연결
        this.gameStartBtn.onClick.AddListener(OnStartGame);
        this.settingsBtn.onClick.AddListener(OnSettings);
        this.exitBtn.onClick.AddListener(OnExitGame);

        // 환경설정 패널에 이벤트 연결
        this.settingsPanelBtn.onClick.AddListener(OnCloseSettings);
        this.closeSettingsBtn.onClick.AddListener(OnCloseSettings);
    }

    // 게임시작 버튼을 눌렀을 때 작동하는 함수
    private void OnStartGame()
    {
        Debug.Log("게임 씬으로 이동합니다.");
        SceneManager.LoadScene("GameScene");
    }

    // 환경설정 버튼을 눌렀을 때 작동하는 함수
    private void OnSettings()
    {
        Debug.Log("환경설정 패널을 활성화합니다.");
        this.settingsPanel.SetActive(true);
    }

    // 환경설정 닫기 버튼을 눌렀을 때 작동하는 함수
    private void OnCloseSettings()
    {
        Debug.Log("환경설정 패널을 닫습니다.");
        this.settingsPanel.SetActive(false);
    }

    // 게임종료 버튼을 눌렀을 때 작동하는 함수
    private void OnExitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
