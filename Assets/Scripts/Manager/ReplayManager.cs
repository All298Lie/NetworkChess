using Cysharp.Threading.Tasks;
using NetworkChess.Core;
using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;

public class ReplayManager : MonoBehaviour
{
    public static ReplayManager Instance { get; private set; }

    [Header("버튼")]
    [SerializeField] private Button firstBtn;
    [SerializeField] private Button previousBtn;
    [SerializeField] private Button nextBtn;
    [SerializeField] private Button lastBtn;

    private List<ChessMoveEntry> entries = new List<ChessMoveEntry>();
    private int currentViewerIndex = 0;

    public bool IsViewingLatest => this.currentViewerIndex == this.entries.Count - 1;

    public int LatestIndex => this.entries.Count - 1;

    public event Action<string, bool> OnReplayPieceMoveSound;

    private CancellationTokenSource rewindCancelTokenSource;
    private bool isRewind = false;

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
        this.firstBtn.onClick.AddListener(OnClickFirstMove);
        this.previousBtn.onClick.AddListener(OnClickPreviousMove);
        this.nextBtn.onClick.AddListener(OnClickNextMove);
        this.lastBtn.onClick.AddListener(OnClickLastMove);    
    }
    #endregion

    #region OnDestroy 함수
    void OnDestroy()
    {
        if (this.rewindCancelTokenSource != null)
        {
            this.rewindCancelTokenSource.Cancel();
            this.rewindCancelTokenSource.Dispose();

            this.rewindCancelTokenSource = null;
        }
    }
    #endregion

    #region 리플레이 진입 시 최초 1회 실행할 보드 되감기 연출 비동기 함수
    public async UniTaskVoid AnimateRewindEffectAsync()
    {
        // 1. 기존에 연출이 돌고있었다면 취소 처리
        this.rewindCancelTokenSource?.Cancel();
        this.rewindCancelTokenSource?.Dispose();
        this.rewindCancelTokenSource = new CancellationTokenSource();
        CancellationToken token = this.rewindCancelTokenSource.Token;

        // 2. 현재 저장된 기보 확인
        if (this.entries == null || this.entries.Count <= 1) return;

        // 3. 버튼 상호작용 잠금
        this.isRewind = true;

        float originMoveDuration = BoardManager.Instance.moveDuration;

        // 4. 되감기 연출 시작
        try
        {
            int totalPlies = this.entries.Count - 1;

            // 속도 조절용 변수
            int maxDelayMs = 80;
            int minDelayMs = 10;

            for (int index = totalPlies; index >= 0; index--)
            {
                if (token.IsCancellationRequested == true) return; // 연출이 취소 되었을 경우 리턴

                // 사인 함수를 사용해 시작과 끝이 가장 느렸다가 중간 지점에 올 수록 빨라지도록 하여 애니메이션 속도 조절
                float progress = 1f - ((float)index / (totalPlies - 1)); // 작업 진행도 : 0.0 ~ 1.0
                float curveWeight = Mathf.Sin(progress * Mathf.PI); // 사인 곡선을 이용한 가중치
                int currentDelayMs = Mathf.RoundToInt(Mathf.Lerp(maxDelayMs, minDelayMs, curveWeight)); // 시간 계산

                BoardManager.Instance.moveDuration = currentDelayMs / 1000f; // 초 -> 밀리초 계산 (UniTask는 밀리초단위, DOTween은 초단위라 보정 필요)

                this.ExecuteJumpToPly(index);

                await UniTask.Delay(currentDelayMs, cancellationToken: token);
            }
        }
        finally
        {
            BoardManager.Instance.moveDuration = originMoveDuration;

            // 버튼 상호작용 잠금 해제
            this.isRewind = false;
        }
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
            this.currentViewerIndex = this.entries.Count - 1;

            this.isRewind = false;
            this.rewindCancelTokenSource?.Cancel();
        }
    }
    #endregion

    #region 해당 인덱스의 기보 화면으로 이동하는 함수
    private void ExecuteJumpToPly(int targetIndex, bool skipAnimation = false)
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

        if (skipAnimation == true)
        {
            if (this.IsViewingLatest == true && GameData.IsReplay == false)
            {
                BoardManager.Instance.SyncVisualsWithCore(GameManager.Instance.ActiveMode);
            }
            else
            {
                BoardManager.Instance.SyncVisualsWithFEN(entry.FEN);
            }

            HighlightManager.Instance.UpdateLastMoveHighlight(entry.StartPos, entry.EndPos);

            return;
        }

        // 4. 직전 상태로 보드 갱신
        int prevIndex = this.currentViewerIndex - 1;
        ChessMoveEntry prevEntry = this.entries[prevIndex];

        BoardManager.Instance.SyncVisualsWithFEN(prevEntry.FEN);

        // 5. 직전 상태에서 이동한 기물을 애니메이션으로 연출
        BoardManager.Instance.AnimatePieceMove(entry.StartPos, entry.EndPos, () =>
        {
            if (this.IsViewingLatest == true && GameData.IsReplay == false)
            {
                BoardManager.Instance.SyncVisualsWithCore(GameManager.Instance.ActiveMode);
            }
            else
            {
                BoardManager.Instance.SyncVisualsWithFEN(entry.FEN);
            }
        });

        // 6. 하이라이트 작업
        HighlightManager.Instance.UpdateLastMoveHighlight(entry.StartPos, entry.EndPos);

        // 7. 리플레이/복기 사운드 재생
        if (this.IsViewingLatest == true && GameData.IsReplay == false) return;

        OnReplayPieceMoveSound?.Invoke(entry.MoveNotation, true);
    }
    #endregion

    #region + 버튼 함수

    #region UI 버튼 (기보 클릭)
    public void JumpToPly(int targetIndex, bool skipAnimation = false)
    {
        // 1. 연출 중인지 확인
        if (this.isRewind == true) return;

        // 2. 로직 호출
        ExecuteJumpToPly(targetIndex, skipAnimation);
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
