using System;
using UnityEngine;
using UnityEngine.UI;

public class GameStartUI : MonoBehaviour
{
    [Header("게임시작 UI")]
    [SerializeField] private GameObject popUpUI;
    [SerializeField] private Button createRoomBtn;
    [SerializeField] private Button joinRoomBtn;

    void Start()
    {
        this.popUpUI.SetActive(false);
    }

    public void Initialize(Action onCreateRoomClick, Action onJoinRoomClick)
    {
        this.createRoomBtn.onClick.AddListener(() => onCreateRoomClick?.Invoke());
        this.joinRoomBtn.onClick.AddListener(() => onJoinRoomClick?.Invoke());
    }
}
