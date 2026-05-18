using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ButtonTextColor : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    [Header("버튼")]
    public Button button { get; private set; }
    public TMP_Text btnTxt { get; private set; }

    [Header("색코드")]
    [SerializeField] private Color normalColor = new Color32(226, 232, 240, 255);
    [SerializeField] private Color hoverColor = new Color32(253, 230, 138, 255);
    [SerializeField] private Color pressedColor = new Color32(217, 119, 6, 255);
    [SerializeField] private Color disabledColor = new Color32(71, 85, 105, 255);
    [SerializeField] private Color selectedColor = new Color32(233, 196, 106, 255);

    private bool isSelected = false;
    private bool isEntered = false;

    void Awake()
    {
        this.button = GetComponent<Button>();
        this.btnTxt = transform.GetChild(0).GetComponent<TMP_Text>();
    }

    #region + 마우스 이벤트 자동 핸들러

    #region 마우스가 들어왔을 때 작동하는 이벤트
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (this.button.interactable == false || this.isSelected == true) return;

        this.isEntered = true;

        this.btnTxt.color = this.hoverColor;
    }
    #endregion

    #region 마우스가 나갔을 때 작동하는 이벤트
    public void OnPointerExit(PointerEventData eventData)
    {
        if (this.button.interactable == false) return;

        this.isEntered = false;

        UpdateTextColor();
    }
    #endregion

    #region 마우스로 클릭했을 때 작동하는 이벤트
    public void OnPointerDown(PointerEventData eventData)
    {
        if (this.button.interactable == false) return;

        this.btnTxt.color = this.pressedColor;
    }
    #endregion

    #region 마우스로 클릭을 뗐을 때 작동하는 이벤트
    public void OnPointerUp(PointerEventData eventData)
    {
        if (this.button.interactable == false) return;

        if (this.isSelected == false && this.isEntered == true)
        {
            this.btnTxt.color = this.hoverColor;
        }
    }
    #endregion

    #endregion - 마우스 이벤트 자동 핸들러

    #region 현재 상태에 맞게 색상을 업데이트 해주는 함수
    private void UpdateTextColor()
    {
        if (this.button.interactable == false)
        {
            this.btnTxt.color = this.disabledColor;
        }
        else if (this.isSelected == true)
        {
             this.btnTxt.color = this.selectedColor;
        }
        else
        {
            this.btnTxt.color = this.normalColor;
        }
    }
    #endregion

    #region 선택 상태를 켜고 끄는 함수
    public void SetSelected(bool selected)
    {
        this.isSelected = selected;
        UpdateTextColor();
    }
    #endregion

    #region 버튼 활성화/비활성화를 제어하는 함수
    public void SetInteractable(bool interactable)
    {
        this.button.interactable = interactable;
        UpdateTextColor();
    }
    #endregion
}
