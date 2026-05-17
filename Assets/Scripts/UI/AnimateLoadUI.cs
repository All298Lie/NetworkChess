using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using TMPro;
using UnityEngine;

public class AnimateLoadUI : MonoBehaviour
{
    [Header("타이틀 애니메이션")]
    [SerializeField] protected TMP_Text title;
    private string originTitleText;

    private CancellationTokenSource cts;

    #region + 유니티 함수

    #region Awake 함수
    protected virtual void Awake()
    {
        this.originTitleText = this.title.text;
    }
    #endregion

    #region OnDisable 함수
    protected virtual void OnDisable()
    {
        CancelAnimateTitleText();
    }
    #endregion

    #endregion

    #region 타이틀 애니메이션 실행 함수
    protected void ExecuteAnimateTitleText()
    {
        // 1. 애니메이션 초기화
        CancelAnimateTitleText();

        this.cts = new CancellationTokenSource();

        // 2. UniTask 애니메이션 실행
        AnimateTitleTextAsync(this.cts.Token).Forget();
    }
    #endregion

    #region 비동기 타이틀 로딩 연출 함수
    private async UniTaskVoid AnimateTitleTextAsync(CancellationToken token)
    {
        int dotIndex = 0;
        string[] dots = { "", ".", "..", "..." };

        try
        {
            while (token.IsCancellationRequested == false)
            {
                this.title.text = originTitleText + dots[dotIndex];
                dotIndex = (dotIndex + 1) % dots.Length;

                await UniTask.Delay(500, cancellationToken: token);
            }
        }
        catch (OperationCanceledException)
        {
            CLog.Log("[UI] 로딩 종료");
        }
    }
    #endregion

    #region 로딩 연출 취소 함수
    protected void CancelAnimateTitleText()
    {
        if (this.cts != null)
        {
            this.cts.Cancel();
            this.cts.Dispose();

            this.cts = null;
        }

    }
    #endregion
}
