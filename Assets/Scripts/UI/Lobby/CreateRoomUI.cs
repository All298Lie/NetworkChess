using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CreateRoomUI : RoomPopUpBase
{
    [Header("새 게임 UI")]
    [SerializeField] private GameObject popUpUI;
    [SerializeField] private TMP_Dropdown dropdown;
    [SerializeField] private Button createRoomBtn;

    void Start()
    {
        this.popUpUI.SetActive(false);
    }
}
