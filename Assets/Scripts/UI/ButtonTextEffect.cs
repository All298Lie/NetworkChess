using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ButtonTextEffect : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    [Header("버튼")]
    public Button Button { get; private set; }
    public TMP_Text BtnTxt { get; private set; }

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
        this.Button = GetComponent<Button>();
        this.BtnTxt = transform.GetChild(0).GetComponent<TMP_Text>();
    }

    #region + 마우스 이벤트 자동 핸들러

    #region 마우스가 들어왔을 때 작동하는 이벤트
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (this.Button.interactable == false || this.isSelected == true) return;

        this.isEntered = true;

        this.BtnTxt.color = this.hoverColor;
    }
    #endregion

    #region 마우스가 나갔을 때 작동하는 이벤트
    public void OnPointerExit(PointerEventData eventData)
    {
        if (this.Button.interactable == false) return;

        this.isEntered = false;

        UpdateTextColor();
    }
    #endregion

    #region 마우스로 클릭했을 때 작동하는 이벤트
    public void OnPointerDown(PointerEventData eventData)
    {
        if (this.Button.interactable == false) return;

        this.BtnTxt.color = this.pressedColor;
    }
    #endregion

    #region 마우스로 클릭을 뗐을 때 작동하는 이벤트
    public void OnPointerUp(PointerEventData eventData)
    {
        if (this.Button.interactable == false) return;

        if (this.isSelected == false && this.isEntered == true)
        {
            this.BtnTxt.color = this.hoverColor;
        }
    }
    #endregion

    #endregion - 마우스 이벤트 자동 핸들러

    #region 현재 상태에 맞게 색상을 업데이트 해주는 함수
    private void UpdateTextColor()
    {
        if (this.Button.interactable == false)
        {
            this.BtnTxt.color = this.disabledColor;
        }
        else if (this.isSelected == true)
        {
             this.BtnTxt.color = this.selectedColor;
        }
        else
        {
            this.BtnTxt.color = this.normalColor;
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
        this.Button.interactable = interactable;
        UpdateTextColor();
    }
    #endregion
}
