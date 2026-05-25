using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.EventSystems;

public class CursorManager : MonoBehaviour
{
    public static CursorManager Instance { get; private set; }

    [Header("커서 이미지")]
    [SerializeField] private Texture2D defaultCursor;
    [SerializeField] private Texture2D hoverCursor;
    [SerializeField] private Texture2D grabCursor;

    [Header("핫스팟 좌표")]
    [SerializeField] private Vector2 hotSpot = new Vector2(28.0f, 16.0f);

    #region Awake 함수
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(transform.root.gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    #endregion

    #region Start 함수
    void Start()
    {
        SetDefaultCursor();
    }
    #endregion

    #region + 커서 지정 함수

    public void SetDefaultCursor() => Cursor.SetCursor(this.defaultCursor, this.hotSpot, CursorMode.ForceSoftware);
    public void SetHoverCursor() => Cursor.SetCursor(this.hoverCursor, this.hotSpot, CursorMode.ForceSoftware);
    public void SetGrabCursor() => Cursor.SetCursor(this.grabCursor, this.hotSpot, CursorMode.ForceSoftware);

    #endregion
}
