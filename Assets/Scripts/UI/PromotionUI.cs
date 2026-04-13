using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class PromotionUI : MonoBehaviour
{
    public static PromotionUI Instance { get; private set; }

    [Header("UI 오브젝트")]
    [SerializeField] private GameObject promotionPanel; // 취소 버튼 판정용 패널
    [SerializeField] private GameObject promotionSelects; // 프로모션 기물 선택 버튼 부모 오브젝트

    [Header("UI Rect")]
    [SerializeField] private RectTransform promotionUIRect; // 프로모션UI Rect
    [SerializeField] private RectTransform containerRect; // 기물 선택 버튼 부모 Rect

    private RectTransform[] pieceButtonsRects;

    private Vector2 cachedTileSize;

    private bool isSizeCached;

    [Header("UI 컴포넌트")]
    [SerializeField] private VerticalLayoutGroup containerLayoutGroup; // 기물 선택 버튼 부모의 정렬 그룹

    [Header("UI 기물 이미지")]
    [SerializeField] private Image selectQueen;
    [SerializeField] private Image selectRook;
    [SerializeField] private Image selectBishop;
    [SerializeField] private Image selectKnight;

    [HideInInspector] public bool IsWhite;

    private UniTaskCompletionSource<PieceType?> promotionTcs;


    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Debug.LogWarning("이미 프로모션 UI가 존재합니다.");
            Destroy(gameObject);
        }

        this.pieceButtonsRects = new RectTransform[4];
        for (int i = 0; i < 4; i++)
        {
            this.pieceButtonsRects[i] = this.containerRect.GetChild(i).GetComponent<RectTransform>();
        }

        this.isSizeCached = false;

        this.promotionPanel.SetActive(false);
        this.promotionSelects.SetActive(false);
    }

    void OnEnable()
    {
        if (ThemeManager.Instance != null) // 테마 매니저가 존재할 경우, 이벤트 구독
        {
            ThemeManager.OnPieceThemeChanged += RefreshImage;
        }
    }

    void OnDisable()
    {
        if (ThemeManager.Instance != null) // 테마 매니저가 존재할 경우, 이벤트 구독 해제
        {
            ThemeManager.OnPieceThemeChanged -= RefreshImage;
        }
    }

    void Update()
    {
        if (this.promotionPanel.activeSelf == true && promotionSelects.activeSelf == true)
        {
            if (Mouse.current.rightButton.wasPressedThisFrame == true)
            {
                OnCancelPromotion();
            }
        }
    }

    // 프로모션 버튼의 기물 스프라이트를 새로고침 해주는 함수
    private void RefreshImage()
    {
        if (ThemeManager.Instance == null) return;

        PieceThemeData activeTheme = ThemeManager.Instance.CurrentPieceTheme;

        if (this.selectQueen != null)
        {
            this.selectQueen.sprite = activeTheme.GetSprite(PieceType.Queen, this.IsWhite);
        }

        if (this.selectRook != null)
        {
            this.selectRook.sprite = activeTheme.GetSprite(PieceType.Rook, this.IsWhite);
        }

        if (this.selectBishop != null)
        {
            this.selectBishop.sprite = activeTheme.GetSprite(PieceType.Bishop, this.IsWhite);
        }

        if (this.selectKnight != null)
        {
            this.selectKnight.sprite = activeTheme.GetSprite(PieceType.Knight, this.IsWhite);
        }
    }

    // UI와 보드 동기화 작업을 하는 ㅎ마수
    private void SyncTransformWithBoard(Vector2Int targetPos, bool isTopRank)
    {
        // 1. 버튼 정렬
        ReverseButtonsOrder(isTopRank);

        // 2. 위치 동기화
        Vector3 worldPos = BoardManager.Instance.GetWorldPosition(targetPos.x, targetPos.y);
        Vector2 screenPos = Camera.main.WorldToScreenPoint(worldPos);

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(this.promotionUIRect, screenPos, null, out Vector2 localPoint) == true)
        {
            if (isTopRank == true)
            {
                localPoint.y = localPoint.y + (this.cachedTileSize.y / 2.0f);
            }
            else
            {
                localPoint.y = localPoint.y - (this.cachedTileSize.y / 2.0f);
            }

                this.containerRect.anchoredPosition = localPoint;
        }

        // 3. 크기 동기화
        this.containerRect.sizeDelta = new Vector2(this.cachedTileSize.x, this.containerRect.sizeDelta.y);

        foreach (RectTransform btnRect in this.pieceButtonsRects)
        {
            btnRect.sizeDelta = new Vector2(btnRect.sizeDelta.x, this.cachedTileSize.y);
        }
    }

    // 버튼을 프로모션하는 위치에 맞게 정렬해주는 함수
    private void ReverseButtonsOrder(bool isTopRank)
    {
        if (isTopRank == true)
        {
            this.containerRect.pivot = new Vector2(0.5f, 1.0f);
            this.containerLayoutGroup.childAlignment = TextAnchor.UpperCenter;

            this.selectQueen.transform.parent.SetSiblingIndex(0);
            this.selectRook.transform.parent.SetSiblingIndex(1);
            this.selectBishop.transform.parent.SetSiblingIndex(2);
            this.selectKnight.transform.parent.SetSiblingIndex(3);
        }
        else
        {
            this.containerRect.pivot = new Vector2(0.5f, 0.0f);
            this.containerLayoutGroup.childAlignment = TextAnchor.LowerCenter;

            this.selectQueen.transform.parent.SetSiblingIndex(3);
            this.selectRook.transform.parent.SetSiblingIndex(2);
            this.selectBishop.transform.parent.SetSiblingIndex(1);
            this.selectKnight.transform.parent.SetSiblingIndex(0);
        }
    }

    // 보드판의 타일크기와 프로모션 UI 크기를 계산해주는 함수
    private void CacheTileSize()
    {
        if (this.isSizeCached == true) return;

        // 1. 월드 기준 거리 계산
        Vector3 world00 = BoardManager.Instance.GetWorldPosition(0, 0);
        Vector3 world11 = BoardManager.Instance.GetWorldPosition(1, 1);

        // 2. 카메라 기준 거리 계산
        Vector2 screen00 = Camera.main.WorldToScreenPoint(world00);
        Vector2 screen11 = Camera.main.WorldToScreenPoint(world11);

        RectTransformUtility.ScreenPointToLocalPointInRectangle(this.promotionUIRect, screen00, null, out Vector2 local00);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(this.promotionUIRect, screen11, null, out Vector2 local11);

        // 3. 거리 계산
        float localTileWidth = Mathf.Abs(local11.x - local00.x);
        float localTileHeight = Mathf.Abs(local11.y - local00.y);

        this.cachedTileSize = new Vector2 (localTileWidth, localTileHeight);
        this.isSizeCached = true;
    }

    // 폰이 프로모션할 기물의 형태를 띄워주는 비동기 함수
    public async UniTask<PieceType?> SelectPieceAsync(Vector2Int targetPos, bool isTopRank)
    {
        RefreshImage(); // 진영과 테마에 맞게 기물 스프라이트 갱신

        // 버튼 활성화
        this.promotionPanel.SetActive(true);
        this.promotionSelects.SetActive(true);

        CacheTileSize();

        SyncTransformWithBoard(targetPos, isTopRank);

        // 버튼을 누를 때까지 대기
        this.promotionTcs = new UniTaskCompletionSource<PieceType?>();
        PieceType? selectedType = await this.promotionTcs.Task;

        // 버튼 비활성화
        this.promotionPanel.SetActive(false);
        this.promotionSelects.SetActive(false);

        return selectedType;
    }

    public bool IsActive()
    {
        if (this.promotionPanel == null || this.promotionSelects == null) return false;

        if (this.promotionPanel.activeSelf == false || this.promotionSelects.activeSelf == false) return false;
        
        return true;
    }

    // 프로모션 버튼 외의 화면을 눌렀을 때 작동하는 함수
    public void OnCancelPromotion()
    {
        this.promotionTcs?.TrySetResult(null);
    }

    // 퀸 프로모션 버튼을 눌렀을 때 작동하는 함수
    public void OnSelectQueen()
    {
        this.promotionTcs?.TrySetResult(PieceType.Queen);
    }

    // 룩 프로모션 버튼을 눌렀을 때 작동하는 함수
    public void OnSelectRook()
    {
        this.promotionTcs?.TrySetResult(PieceType.Rook);
    }

    // 비숍 프로모션 버튼을 눌렀을 때 작동하는 함수
    public void OnSelectBishop()
    {
        this.promotionTcs?.TrySetResult(PieceType.Bishop);
    }

    // 나이트 프로모션 버튼을 눌렀을 때 작동하는 함수
    public void OnSelectKnight()
    {
        this.promotionTcs?.TrySetResult(PieceType.Knight);
    }
}
