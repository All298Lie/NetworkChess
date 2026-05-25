using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerInfoUI : MonoBehaviour
{
    [SerializeField] private TMP_Text nickname;
    [SerializeField] private Image icon;

    private bool isWhite;

    void OnEnable()
    {
        ThemeManager.OnPieceThemeChanged += UpdateIcon;
    }

    void OnDisable()
    {
        ThemeManager.OnPieceThemeChanged -= UpdateIcon;
    }

    public void Setup(string nickname, bool isWhite)
    {
        this.nickname.text = nickname;

        this.isWhite = isWhite;

        UpdateIcon();
    }

    public void UpdateIcon()
    {
        if (isWhite == true) this.icon.sprite = ThemeManager.Instance.CurrentPieceTheme.whiteSprites.pawn;
        else this.icon.sprite = ThemeManager.Instance.CurrentPieceTheme.blackSprites.pawn;
    }
}
