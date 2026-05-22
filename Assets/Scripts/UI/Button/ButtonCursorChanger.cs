using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ButtonCursorChanger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    private bool isEntered = false;
    private Selectable selectable;

    void Awake()
    {
        selectable = GetComponent<Selectable>();
    }

    private bool IsInteractable => (selectable == null || selectable.interactable);

    public void OnPointerEnter(PointerEventData eventData)
    {
        this.isEntered = true;

        if (this.IsInteractable == false) return;

        if (CursorManager.Instance != null)
        {
            CursorManager.Instance.SetHoverCursor();
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {   
        this.isEntered = false;

        if (CursorManager.Instance != null)
        {
            CursorManager.Instance.SetDefaultCursor();
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (this.IsInteractable == false) return;

        if (CursorManager.Instance != null)
        {
            if (this.isEntered == true)
            {
                CursorManager.Instance.SetHoverCursor();
            }
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (this.IsInteractable == false) return;

        if (CursorManager.Instance != null)
        {
            CursorManager.Instance.SetGrabCursor();
        }
    }
}
