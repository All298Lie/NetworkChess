using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("게임 세팅")]
    [SerializeField] private Transform canvas;
    [SerializeField] private GameMode currentMode = GameMode.Standard;

    [Header("프리팹")]
    [SerializeField] private GameObject gameOverUIPrefab;

    public GameOverUIController GameOverUI { get; private set; }
    public GameModeBase ActiveMode { get; private set; }

    public bool IsGameEnd { get; private set; }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Debug.LogWarning("게임 매니저가 이미 존재합니다.");
            Destroy(gameObject);
        }
    }

    void Start()
    {
        GameObject gameOverUI = Instantiate(gameOverUIPrefab, canvas);
        gameOverUI.name = "GameOverUI";

        this.GameOverUI = gameOverUI.GetComponent<GameOverUIController>();
    }

    public void RegisterModeManager(GameModeBase modeManager)
    {
        this.ActiveMode = modeManager;
        Debug.Log($"현재 활성화된 체스 모드: {currentMode}");

        this.ActiveMode.StartGame();
    }

    public void OnClickExitButton()
    {
        SceneManager.LoadScene("LobbyScene");
    }
}
