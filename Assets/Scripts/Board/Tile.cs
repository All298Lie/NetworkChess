using TMPro;
using UnityEngine;

public class Tile : MonoBehaviour
{
    [Header("타일 하이라이트")]
    [SerializeField] private GameObject moveHighlight;
    [SerializeField] private GameObject captureHighlight;
    [SerializeField] private GameObject lastMoveHighlight;
    [SerializeField] private GameObject selectHighlight;

    private bool isShowLastMoveHighlight;

    [Header("좌표 설정")]
    [SerializeField] private TMP_Text rankInfo;
    [SerializeField] private TMP_Text fileInfo;
    private int rank = 0;
    private int file = 0;

    #region OnDestroy 함수
    void OnDestroy()
    {
        ThemeManager.OnBoardThemeChanged -= SetCoordinatesColor;
    }
    #endregion

    #region 보드매니저로 생성할 때 딱 한 번 호출하는 함수
    public void Setup(int x, int y)
    {
        // 1. 타일 이름 변경
        gameObject.name = $"Tile_{x}_{y}";

        // 2. 하이라이트 설정
        if (this.moveHighlight != null) this.moveHighlight.SetActive(false);

        if (this.captureHighlight != null) this.captureHighlight.SetActive(false);

        this.isShowLastMoveHighlight = false;

        // 3. 보드 좌표 설정
        this.file = x;
        this.rank = y;

        char fileChar = (char)('a' + x);
        string rankString = (y + 1).ToString();

        bool isBottomRow = GameData.IsWhite == true ? (y == 0) : (y == 7);
        bool isLeftColumn = GameData.IsWhite == true ? (x == 0) : (x == 7);

        this.fileInfo.text = (isBottomRow == true ? fileChar.ToString() : string.Empty);
        this.rankInfo.text = (isLeftColumn == true ? rankString : string.Empty);

        if (isBottomRow == true || isLeftColumn == true)
        {
            ThemeManager.OnBoardThemeChanged += SetCoordinatesColor;

            SetCoordinatesColor();
        }
    }
    #endregion

    #region 좌표 텍스트 색 설정
    private void SetCoordinatesColor()
    {
        // 1. 백색 타일인지 확인
        bool isLightSquare = ((this.rank % 2 == 0) ^ (this.file % 2 == 0));

        // 2. 타일 색에 따라 색 지정 (흑 타일 = 백색, 백 타일 = 흑색)
        Color textColor = isLightSquare ? ThemeManager.Instance.CurrentBoardTheme.blackColor : ThemeManager.Instance.CurrentBoardTheme.whiteColor;

        // 3. 표시되어있는 타일인지 확인 후, 색 지정
        if (this.rankInfo != null && string.IsNullOrEmpty(this.rankInfo.text) == false)
            this.rankInfo.color = textColor;

        if (this.fileInfo != null && string.IsNullOrEmpty(this.fileInfo.text) == false)
            this.fileInfo.color = textColor;
    }
    #endregion

    #region 이동/공격 하이라이트 설정
    public void SetMoveHighlight(bool show, bool isCapture)
    {
        // 1. 우선 하이라이트 끄기
        this.moveHighlight.SetActive(false);
        this.captureHighlight.SetActive(false);

        // 2. 하이라이트가 필요할 경우
        if (show == true)
        {
            if (isCapture == true) // 공격 위치일 경우
            {
                this.captureHighlight.SetActive(true);
            }
            else // 이동 위치일 경우
            {
                this.moveHighlight.SetActive(true);
            }
        }
    }
    #endregion

    #region 기물 이동 하이라이트 설정
    public void SetLastMoveHighlight(bool show)
    {
        this.lastMoveHighlight.SetActive(show);

        this.isShowLastMoveHighlight = show;
    }
    #endregion

    #region + 선택 하이라이트

    #region 선택 하이라이트 토글
    public void ToggleSelectHighlight()
    {
        // 1. 우선 현재 위치 하이라이트 끄기
        this.lastMoveHighlight.SetActive(false);

        // 2. 하이라이트가 켜져있을 경우 끄고, 꺼져있을 경우 키기
        this.selectHighlight.SetActive(selectHighlight.activeSelf ^ true);

        if (this.isShowLastMoveHighlight == true && this.selectHighlight.activeSelf == false)
        {
            this.lastMoveHighlight.SetActive(true);
        }
    }
    #endregion

    #region 선택 하이라이트 끄기
    public void HideSelectHighlight()
    {
        this.selectHighlight.SetActive(false);

        if (this.isShowLastMoveHighlight == true)
        {
            this.lastMoveHighlight.SetActive(true);
        }
    }
    #endregion

    #endregion - 선택 하이라이트
}
