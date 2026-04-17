using UnityEngine;

public class Tile : MonoBehaviour
{
    [Header("타일 하이라이트")]
    [SerializeField] private GameObject moveHighlight;
    [SerializeField] private GameObject captureHighlight;
    [SerializeField] private GameObject lastMoveHighlight;
    [SerializeField] private GameObject selectHighlight;

    private bool isShowLastMoveHighlight;

    // 보드매니저로 생성할 때 딱 한 번 호출하는 함수
    public void Setup(int x, int y)
    {
        gameObject.name = $"Tile_{x}_{y}";

        if (moveHighlight != null)
        {
            moveHighlight.SetActive(false);
        }

        if (captureHighlight != null)
        {
            captureHighlight.SetActive(false);
        }

        isShowLastMoveHighlight = false;
    }

    // 하이라이트 지정
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

    public void SetLastMoveHighlight(bool show)
    {
        this.lastMoveHighlight.SetActive(show);

        this.isShowLastMoveHighlight = show;
    }

    public void ToggleSelectHighlight()
    {
        // 1. 우선 현재 위치 하이라이트 끄기
        this.lastMoveHighlight.SetActive(false);

        // 2. 하이라이트가 켜져있을 경우 끄고, 꺼져있을 경우 키기
        selectHighlight.SetActive(selectHighlight.activeSelf ^ true);

        if (this.isShowLastMoveHighlight == true && selectHighlight.activeSelf == false)
        {
            this.lastMoveHighlight.SetActive(true);
        }
    }

    public void HideSelectHighlight()
    {
        selectHighlight.SetActive(false);

        if (this.isShowLastMoveHighlight == true)
        {
            this.lastMoveHighlight.SetActive(true);
        }
    }
}
