using System;
using UnityEngine;
using UnityEngine.UI;

public class GameStartUI : MonoBehaviour
{
    [Header("게임시작 UI")]
    [SerializeField] private GameObject popUpUI;
    [SerializeField] private Button createRoomBtn;
    [SerializeField] private Button joinRoomBtn;

    #region Start 함수
    void Start()
    {
        this.popUpUI.SetActive(false);
    }
    #endregion

    #region 초기화
    public void Initialize(Action onCreateRoomClick, Action onJoinRoomClick)
    {
        this.createRoomBtn.onClick.AddListener(() => onCreateRoomClick?.Invoke());
        this.joinRoomBtn.onClick.AddListener(() => onJoinRoomClick?.Invoke());
    }
    #endregion
}
