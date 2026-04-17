using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SettingsUIController : MonoBehaviour
{
    [Header("비디오 UI")]
    [SerializeField] private TMP_Dropdown resDropdown;
    [SerializeField] private Toggle fullscreenToggle;

    [Header("테마 설정")]
    [SerializeField] private TMP_Dropdown boardThemeDropdown;
    [SerializeField] private TMP_Dropdown pieceThemeDropdown;

    void Start()
    {
        // 해상도 설정 초기화

        this.resDropdown.ClearOptions();
        List<Resolution> resolutions = GlobalSettingsManager.Instance.AvilableResolutions;
        List<string> options = new List<string>();

        foreach (Resolution res in resolutions)
        {
            options.Add($"{res.width} x {res.height}");
        }
        this.resDropdown.AddOptions(options);

        // 1. 기존 설정값으로 UI 초기화

        // 해상도
        this.resDropdown.value = PlayerPrefs.GetInt("ResIndex", 0);
        this.fullscreenToggle.isOn = PlayerPrefs.GetInt("Fullscreen", 1) == 1;

        // 테마
        this.boardThemeDropdown.value = PlayerPrefs.GetInt("BoardTheme", 0);
        this.pieceThemeDropdown.value = PlayerPrefs.GetInt("PieceTheme", 0);

        // 2. 이벤트 연결 (값이 바뀔 때마다 실시간 적용)

        // 해상도 설정
        this.resDropdown.onValueChanged.AddListener(_ => ApplyVideoSettings());
        this.fullscreenToggle.onValueChanged.AddListener(_ => ApplyVideoSettings());

        // 테마 설정
        this.boardThemeDropdown.onValueChanged.AddListener(_ => ApplyThemeSettings());
        this.pieceThemeDropdown.onValueChanged.AddListener(_ => ApplyThemeSettings());
    }

    // 해상도 설정을 적용하는 함수
    private void ApplyVideoSettings()
    {
        GlobalSettingsManager.Instance.ApplyResolution(this.resDropdown.value, this.fullscreenToggle.isOn);
    }

    // 테마 설정을 적용하는 함수
    private void ApplyThemeSettings()
    {
        GlobalSettingsManager.Instance.ApplyTheme(this.boardThemeDropdown.value, this.pieceThemeDropdown.value);
    }
}
