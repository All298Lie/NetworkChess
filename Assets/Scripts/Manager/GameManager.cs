using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("게임 세팅")]
    [SerializeField] private GameMode currentMode = GameMode.Standard;

    public GameModeBase ActiveMode { get; private set; }

    public bool IsGameEnd { get; private set; }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // 씬이 넘어가도 파괴되지 않도록 설정
        }
        else
        {
            Debug.LogWarning("게임 매니저가 이미 존재합니다.");
            Destroy(gameObject);
        }
    }

    public void RegisterModeManager(GameModeBase modeManager)
    {
        ActiveMode = modeManager;
        Debug.Log($"현재 활성화된 체스 모드: {currentMode}");

        ActiveMode.StartGame();
    }
}
