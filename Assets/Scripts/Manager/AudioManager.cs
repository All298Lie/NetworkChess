using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class AudioManager : MonoBehaviour
{
    [Header("사운드 클립")]
    public AudioClip myMoveClip;
    public AudioClip opponentMoveClip;
    public AudioClip captureClip;

    private AudioSource sfxSource;

    void Awake()
    {
        this.sfxSource = GetComponent<AudioSource>();    
    }

    void Start()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnPieceMoveSound += PlayPieceMoveSound;
        }

        if (ReplayManager.Instance != null)
        {
            ReplayManager.Instance.OnReplayPieceMoveSound += PlayPieceMoveSound;
        }
    }

    void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnPieceMoveSound -= PlayPieceMoveSound;
        }

        if (ReplayManager.Instance != null)
        {
            ReplayManager.Instance.OnReplayPieceMoveSound -= PlayPieceMoveSound;
        }
    }

    #region 기물이 이동했을 때의 효과음을 재생시켜주는 함수
    private void PlayPieceMoveSound(string SAN, bool isMyTurn)
    {
        // 1. 기물이 잡히거나 체크, 체크메이트 상태 인지 확인
        if (SAN.Contains("x") == true || SAN.Contains("+") == true || SAN.Contains("#") == true)
        {
            this.sfxSource.PlayOneShot(this.captureClip);
            return;
        }

        // 2. 리플레이, 관전 중이거나 실시간 복기 중인지 확인
        bool isReviewing = (GameData.IsReplay == true) || (GameData.IsSpectator == true)  || (ReplayManager.Instance.IsViewingLatest == false);

        if (isReviewing == true)
        {
            this.sfxSource.PlayOneShot(this.opponentMoveClip);
            return;
        }

        // 3. 내 턴인지 상대 턴인지 확인
        if (isMyTurn == true)
        {
            this.sfxSource.PlayOneShot(this.myMoveClip);
        }
        else
        {
            this.sfxSource.PlayOneShot(this.opponentMoveClip);
        }
    }
    #endregion
}
