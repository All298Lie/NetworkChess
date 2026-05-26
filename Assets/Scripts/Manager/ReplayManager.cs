using NetworkChess.Core;
using System;
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

    public event Action<string, bool> OnReplayPieceMoveSound;

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
    public void UpdateTimeLine(ChessMoveEntry entry, bool skipAnimation = false)
    {
        if (entry == null || string.IsNullOrEmpty(entry.FEN) == true) return;

        this.entries.Add(entry);

        if (this.currentViewerIndex == entries.Count - 2)
        {
            JumpToPly(entries.Count - 1, skipAnimation);
        }
    }
    #endregion

    #region 마지막 이동을 제거하는 함수
    public void PopLatestMove()
    {
        if (this.entries.Count > 1) // 최초 시작 FEN은 지우면 안 됨
        {
            this.entries.RemoveAt(this.entries.Count - 1);

            // 뷰어 인덱스 동기화
            if (this.currentViewerIndex >= this.entries.Count)
            {
                this.currentViewerIndex = this.entries.Count - 1;
            }
        }
    }
    #endregion

    #region + 버튼 함수

    #region UI 버튼 (기보 클릭)
    public void JumpToPly(int targetIndex, bool skipAnimation = false)
    {
        // 1. 유효한 인덱스인지 확인
        if (targetIndex < 0 || targetIndex >= this.entries.Count) return;

        this.currentViewerIndex = targetIndex;
        ChessMoveEntry entry = this.entries[this.currentViewerIndex];

        // 2. 하이라이트 초기화
        HighlightManager.Instance.HideMoveHighlights();

        // 3. 시작 보드로 이동할 경우 즉시 세팅
        if (this.currentViewerIndex == 0)
        {
            BoardManager.Instance.SyncVisualsWithFEN(entry.FEN);

            return;
        }

        // 4. 직전 상태로 보드 갱신
        int prevIndex = this.currentViewerIndex - 1;
        ChessMoveEntry prevEntry = this.entries[prevIndex];

        BoardManager.Instance.SyncVisualsWithFEN(prevEntry.FEN);

        // 5. 직전 상태에서 이동한 기물을 애니메이션으로 연출
        if (skipAnimation == false)
        {
            BoardManager.Instance.AnimatePieceMove(entry.StartPos, entry.EndPos, () =>
            {
                if (this.IsViewingLatest == true && GameData.IsReplay == false)
                {
                    BoardManager.Instance.SyncVisualsWithCore(GameManager.Instance.ActiveMode);
                }
            });
        }

        // 7. 하이라이트 작업
        HighlightManager.Instance.UpdateLastMoveHighlight(entry.StartPos, entry.EndPos);

        // 8. 리플레이/복기 사운드 재생
        if (this.IsViewingLatest == true && GameData.IsReplay == false) return;

        // 사운드 이벤트 직접 호출!
        if (GameManager.Instance != null)
        {
            OnReplayPieceMoveSound?.Invoke(entry.MoveNotation, true);
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
