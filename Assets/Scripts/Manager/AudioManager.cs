using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class AudioManager : MonoBehaviour
{
    [Header("사운드 클립")]
    public AudioClip myMoveClip;
    public AudioClip opponentMoveClip;
    public AudioClip captureClip;

    private AudioSource sfxSource;

    private bool isCapturePlayedThisTurn = false;

    void Awake()
    {
        this.sfxSource = GetComponent<AudioSource>();    
    }

    void Start()
    {
        if (GameManager.Instance != null && GameManager.Instance.ActiveMode != null)
        {
            GameManager.Instance.OnPieceMoveSound += PlayPieceMoveSound;
        }
    }

    void OnDestroy()
    {
        if (GameManager.Instance != null && GameManager.Instance.ActiveMode != null)
        {
            GameManager.Instance.OnPieceMoveSound -= PlayPieceMoveSound;
        }
    }

    #region 기물이 이동했을 때의 효과음을 재생시켜주는 함수
    private void PlayPieceMoveSound(string SAN, bool isMyTurn)
    {
        if (SAN.Contains('x') == true || SAN.Contains('+') == true || SAN.Contains('#') == true)
        {
            this.sfxSource.PlayOneShot(this.captureClip);
            return;
        }

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
