using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class GameModeDescription : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("버튼")]
    [SerializeField] private Button infoBtn;

    [SerializeField] private GameObject panel;
    [SerializeField] private TMP_Text title;
    [SerializeField] private TMP_Text description;

    #region Start 함수
    void Start()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameStart += Initialize;
        }

        this.panel.SetActive(false);
    }
    #endregion

    #region OnDestroy 함수
    void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameStart -= Initialize;
        }
    }
    #endregion

    private void Initialize()
    {
        if (GameManager.Instance != null)
        {
            this.title.text = GameManager.Instance.ActiveMode.ModeName;
            this.description.text = GameManager.Instance.ActiveMode.ModeDescription;
        }
    }

    #region + 마우스 이벤트 핸들러

    #region 마우스가 들어왔을 때 작동하는 이벤트
    public void OnPointerEnter(PointerEventData eventData)
    {
        this.panel.SetActive(true);
    }
    #endregion

    #region 마우스가 나갔을 때 작동하는 이벤트
    public void OnPointerExit(PointerEventData eventData)
    {
        this.panel.SetActive(false);
    }
    #endregion

    #endregion - 마우스 이벤트 핸들러
}
