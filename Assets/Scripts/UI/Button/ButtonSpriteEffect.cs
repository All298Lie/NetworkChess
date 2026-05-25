using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ButtonSpriteEffect : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    [Header("버튼")]
    public Button Button { get; private set; }
    public RectTransform RectTrans { get; private set;}
    public Image BtnImage { get; private set; }

    [Header("애니메이션")]
    [SerializeField] private float hoverMoveY = 4f;
    [SerializeField] private float pressMoveY = -2f;
    [SerializeField] private Color pressedColor = new Color32(148, 163, 184, 255);

    private Vector2 originPos;
    private Color originColor;

    private bool isEntered = false;

    #region Awake 함수
    void Awake()
    {
        this.Button = GetComponent<Button>();
        this.BtnImage = transform.GetChild(0).GetComponent<Image>();
        this.RectTrans = this.BtnImage.GetComponent<RectTransform>();

        this.originPos = this.RectTrans.anchoredPosition;
        this.originColor = this.BtnImage.color;
    }
    #endregion

    #region OnDisable 함수
    void OnDisable()
    {
        if (this.RectTrans != null) this.RectTrans.anchoredPosition = this.originPos;
        if (this.BtnImage != null) this.BtnImage.color = this.originColor;
        this.isEntered = false;
    }
    #endregion

    #region + 마우스 이벤트 자동 핸들러

    #region 마우스가 들어왔을 때 작동하는 이벤트
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (this.RectTrans == null) return;

        this.isEntered = true;
        this.RectTrans.anchoredPosition = this.originPos + new Vector2(0, this.hoverMoveY);
    }
    #endregion

    #region 마우스가 나갔을 때 작동하는 이벤트
    public void OnPointerExit(PointerEventData eventData)
    {
        if (this.RectTrans == null) return;

        this.isEntered = false;
        this.RectTrans.anchoredPosition = this.originPos;
        if (this.BtnImage != null) this.BtnImage.color = this.originColor;
    }
    #endregion

    #region 마우스 클릭했을 때 작동하는 이벤트
    public void OnPointerDown(PointerEventData eventData)
    {
        if (this.RectTrans == null) return;

        this.RectTrans.anchoredPosition = this.RectTrans.anchoredPosition + new Vector2(0, this.pressMoveY);
        if (this.BtnImage != null) this.BtnImage.color = this.pressedColor;
    }
    #endregion

    #region 마우스 클릭을 뗐을 때 작동하는 이벤트
    public void OnPointerUp(PointerEventData eventData)
    {
        if (this.RectTrans == null) return;

        if (this.isEntered == true)
        {
            this.RectTrans.anchoredPosition = this.originPos + new Vector2(0, this.hoverMoveY);
        }
        else
        {
            this.RectTrans.anchoredPosition = this.originPos;
        }

        if (this.BtnImage != null) this.BtnImage.color = this.originColor;
    }
    #endregion

    #endregion - 마우스 이벤트 자동 핸들러
}
