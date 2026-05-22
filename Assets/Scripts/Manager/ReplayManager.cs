using System.Collections.Generic;
using UnityEngine;

public class ReplayManager : MonoBehaviour
{
    public static ReplayManager Instance { get; private set; }

    private List<string> FENTimeline = new List<string>();
    private int currentViewerIndex = 0;

    #region Awake 함수
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }
    #endregion

    #region 패킷에서 기록을 몽땅 받아올때 사용하는 함수
    public void SetupTimeline(List<string> history)
    {
        this.FENTimeline = history;
        this.currentViewerIndex = this.FENTimeline.Count - 1; // 최신 수로 세팅
    }
    #endregion

    #region 기록을 업데이트 하는 함수
    public void UpdateTimeLine(string history)
    {
        this.FENTimeline.Add(history);

        if (this.currentViewerIndex == FENTimeline.Count - 2)
        {
            JumpToPly(FENTimeline.Count - 1);
        }
    }
    #endregion

    #region + 버튼 함수

    #region UI 버튼 (기보 클릭)
    public void JumpToPly(int targetIndex)
    {
        if (targetIndex < 0 || targetIndex >= this.FENTimeline.Count) return;

        this.currentViewerIndex = targetIndex;
        string targetFEN = this.FENTimeline[this.currentViewerIndex];

        // TODO: 보드 매니저에게 targetFen을 주면서 기물 렌더링 덮어씌우기
    }
    #endregion

    #region 화살표 버튼 (처음 수)
    public void OnClickFirstMove()
    {
        JumpToPly(0);
    }
    #endregion

    #region 화살표 버튼 (이전 수)
    public void OnClickPreviousMove()
    {
        JumpToPly(this.currentViewerIndex - 1);
    }
    #endregion

    #region 화살표 버튼 (다음 수)
    public void OnClickNextMove()
    {
        JumpToPly(this.currentViewerIndex + 1);
    }
    #endregion

    #region 화살표 버튼 (마지막 수)
    public void OnClickLastMove()
    {
        JumpToPly(this.FENTimeline.Count - 1);
    }
    #endregion

    #endregion - 버튼 함수
}
