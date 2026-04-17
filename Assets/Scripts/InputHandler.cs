using UnityEngine;
using UnityEngine.InputSystem;

public class InputHandler : MonoBehaviour
{
    [Header("Input Actions")]
    [SerializeField] private InputAction leftClickAction;
    [SerializeField] private InputAction rightClickAction;
    [SerializeField] private InputAction pointerPositionAction;

    private bool isDragging;
    private bool isRightClickConsumed;

    void Start()
    {
        isDragging = false;
    }

    void OnEnable()
    {
        // 1. 액션 활성화
        leftClickAction.Enable();
        rightClickAction.Enable();
        pointerPositionAction.Enable();

        // 2. 버튼이 눌렸을 때 함수가 실행되도록 이벤트 구독
        leftClickAction.started += OnLeftClickStarted;
        leftClickAction.canceled += OnLeftClickCanceled;

        rightClickAction.started += OnRightClickStarted;
        rightClickAction.canceled += OnRightClickCanceled;
    }

    void Update()
    {
        DragPiece();
    }

    void OnDisable()
    {
        // 1. 버튼이 눌렸을 때 함수가 실행되지 않도록 이벤트 구독해제
        leftClickAction.started -= OnLeftClickStarted;
        leftClickAction.canceled -= OnLeftClickCanceled;

        rightClickAction.started -= OnRightClickStarted;
        rightClickAction.canceled -= OnRightClickCanceled;

        // 2. 액션 비활성화
        leftClickAction.Disable();
        rightClickAction.Disable();
        pointerPositionAction.Disable();
    }

    // 잡고 있는 기물을 마우스 위치로 이동시키는 함수
    private void DragPiece()
    {
        // 1. 예외 처리
        if (GameManager.Instance.IsGameEnd == true) return;
        if (PromotionUIController.Instance != null && PromotionUIController.Instance.IsActive() == true) return;

        // 2. 기물을 잡고 있는 상태일 경우, 프레임마다 마우스 위치에 따라 기물 위치 이동
        if (isDragging == true)
        {
            Vector2 screenPos = pointerPositionAction.ReadValue<Vector2>();
            BoardManager.Instance.OnDragPiece(screenPos);
        }
    }

    // 좌클릭 시작 시 호출되는 이벤트 함수
    private void OnLeftClickStarted(InputAction.CallbackContext context)
    {
        // 1. 예외 처리
        if (GameManager.Instance.IsGameEnd == true) return;
        if (PromotionUIController.Instance != null && PromotionUIController.Instance.IsActive() == true) return;

        Vector2 screenPos = pointerPositionAction.ReadValue<Vector2>();

        HighlightManager.Instance.OnLeftClickStarted(screenPos);

        if (BoardManager.Instance.OnLeftClickStarted(screenPos) == true)
        {
            this.isDragging = true;
        }
    }

    // 좌클릭 취소 시 호출되는 이벤트 함수
    private void OnLeftClickCanceled(InputAction.CallbackContext context)
    {
        // 1. 예외 처리
        if (GameManager.Instance.IsGameEnd == true) return;
        if (PromotionUIController.Instance != null && PromotionUIController.Instance.IsActive() == true) return;

        Vector2 screenPos = pointerPositionAction.ReadValue<Vector2>();

        BoardManager.Instance.OnLeftClickCanceled(screenPos);

        this.isDragging = false;
    }

    // 우클릭 시작 시 호출되는 이벤트 함수
    private void OnRightClickStarted(InputAction.CallbackContext context)
    {
        // 1. 예외 처리
        if (GameManager.Instance.IsGameEnd == true) return;
        if (PromotionUIController.Instance != null && PromotionUIController.Instance.IsActive() == true) return;

        Vector2 screenPos = pointerPositionAction.ReadValue<Vector2>();

        this.isRightClickConsumed = BoardManager.Instance.OnRightClickStarted();
        if (this.isRightClickConsumed == false) // 우클릭이 기물 취소에 이용되었을 경우, 하이라이트 표시 준비를 하지 않음
        {
            HighlightManager.Instance.OnRightClickStarted(screenPos);
        }
    }

    // 우클릭 취소 시 호출되는 이벤트 함수
    private void OnRightClickCanceled(InputAction.CallbackContext context)
    {
        // 1. 예외 처리
        if (GameManager.Instance.IsGameEnd == true) return;
        if (PromotionUIController.Instance != null && PromotionUIController.Instance.IsActive() == true) return;

        if (this.isRightClickConsumed == true) // 이번 우클릭이 기물 취소용이었을 경우, 하이라이트와 어노테이션에 사용하지 않음
        {
            this.isRightClickConsumed = false;
            return;
        }

        Vector2 screenPos = pointerPositionAction.ReadValue<Vector2>();

        HighlightManager.Instance.OnRightClickCanceled(screenPos);
    }
}
