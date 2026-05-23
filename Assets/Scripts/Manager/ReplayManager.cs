using NetworkChess.Core;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ReplayManager : MonoBehaviour
{
    public static ReplayManager Instance { get; private set; }

    [SerializeField] private Button firstBtn;
    [SerializeField] private Button previousBtn;
    [SerializeField] private Button nextBtn;
    [SerializeField] private Button lastBtn;

    private List<ChessMoveEntry> entries = new List<ChessMoveEntry>();
    private int currentViewerIndex = 0;

    public bool IsViewingLatest => this.currentViewerIndex == this.entries.Count - 1;

    public int LatestIndex => this.entries.Count - 1;

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

    #region Start 함수
    void Start()
    {
        firstBtn.onClick.AddListener(OnClickFirstMove);    
        previousBtn.onClick.AddListener(OnClickPreviousMove);    
        nextBtn.onClick.AddListener(OnClickNextMove);    
        lastBtn.onClick.AddListener(OnClickLastMove);    
    }
    #endregion

    #region 패킷에서 기록을 몽땅 받아올때 사용하는 함수
    public void SetupTimeline(List<ChessMoveEntry> entries)
    {
        this.entries = entries;
        this.currentViewerIndex = this.entries.Count - 1; // 최신 수로 세팅
    }
    #endregion

    #region 기록을 업데이트 하는 함수
    public void UpdateTimeLine(ChessMoveEntry entry)
    {
        if (entry == null || string.IsNullOrEmpty(entry.FEN) == true) return;

        this.entries.Add(entry);

        if (this.currentViewerIndex == entries.Count - 2)
        {
            JumpToPly(entries.Count - 1);
        }
    }
    #endregion

    #region + 버튼 함수

    #region UI 버튼 (기보 클릭)
    public void JumpToPly(int targetIndex)
    {
        if (targetIndex < 0 || targetIndex >= this.entries.Count) return;

        this.currentViewerIndex = targetIndex;
        ChessMoveEntry entry = this.entries[this.currentViewerIndex];

        // 1. 기물 렌더링 덮어씌우기 작업
        if (this.IsViewingLatest == true)
        {
            BoardManager.Instance.SyncVisualsWithCore(GameManager.Instance.ActiveMode);
        }
        else
        {
            BoardManager.Instance.SyncVisualsWithFEN(entry.FEN);
        }

        // 2. 하이라이트 작업
        if (MoveValidator.IsOnBoard(entry.StartPos) == true && MoveValidator.IsOnBoard(entry.EndPos) == true)
        {
            HighlightManager.Instance.UpdateLastMoveHighlight(entry.StartPos, entry.EndPos);
        }
        else
        {
            HighlightManager.Instance.HideMoveHighlights();
        }
    }
    #endregion

    #region 화살표 버튼 (처음 수)
    public void OnClickFirstMove() => JumpToPly(0);
    #endregion

    #region 화살표 버튼 (이전 수)
    public void OnClickPreviousMove() => JumpToPly(this.currentViewerIndex - 1);
    #endregion

    #region 화살표 버튼 (다음 수)
    public void OnClickNextMove() => JumpToPly(this.currentViewerIndex + 1);
    #endregion

    #region 화살표 버튼 (마지막 수)
    public void OnClickLastMove() => JumpToPly(this.entries.Count - 1);
    #endregion

    #endregion - 버튼 함수
}
