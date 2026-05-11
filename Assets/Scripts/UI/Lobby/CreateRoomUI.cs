using Cysharp.Threading.Tasks;
using NetworkChess.Core;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 게임모드 옵션에 사용할 구조체
[Serializable]
public struct GameModeOption
{
    public string displayName;
    public GameMode modeType;
}

public class CreateRoomUI : RoomPopUpBase
{
    [Header("새 게임 UI")]
    [SerializeField] private GameObject popUpUI;
    [SerializeField] private TMP_Dropdown dropdown;
    [SerializeField] private Button createRoomBtn;

    [Header("드롭박스 설정")]
    [SerializeField] private List<GameModeOption> modeOptions;
    [SerializeField] private GameMode currentSelectedMode;

    void Start()
    {
        createRoomBtn.onClick.AddListener(OnCreateRoom);

        popUpUI.SetActive(false);
    }

    public void Initialize(Action onBackButtonClick)
    {
        base.InitializeBase(onBackButtonClick);

        SetupDropdown();
    }

    #region 드롭다운 초기화 및 이벤트 연결 함수
    private void SetupDropdown()
    {
        if (dropdown == null || modeOptions.Count == 0) return;

        // 1. 드롭다운 초기화
        this.dropdown.ClearOptions();


        // 2. 드롭다운 목록 추가
        List<string> options = new List<string>();
        foreach (var option in this.modeOptions)
        {
            options.Add(option.displayName);
        }

        this.dropdown.AddOptions(options);

        // 3. 초기값 세팅
        this.currentSelectedMode = modeOptions[0].modeType;

        // 4. 드롭다운 값이 바뀔때마다 이벤트가 실행되도록 연결
        this.dropdown.onValueChanged.AddListener(OnDropdownValueChanged);
    }
    #endregion

    #region 드롭다운 값이 바뀔 때 실행되는 함수
    private void OnDropdownValueChanged(int index)
    {
        this.currentSelectedMode = this.modeOptions[index].modeType;
        CLog.Log($"[UI] 게임 모드 변경 : {this.currentSelectedMode}");
    }
    #endregion

    private void OnCreateRoom()
    {
        CLog.Log($"[네트워크] 게임모드를 '{this.currentSelectedMode}'으로 하여 방 생성 시도합니다.");

        // 1. 패킷 전송
        C2S_RoomCreateReq req = new C2S_RoomCreateReq();

        req.GameMode = this.currentSelectedMode;

        if (NetworkManager.Instance != null)
        {
            NetworkManager.Instance.SendPacket(req).Forget();
        }
    }
}
