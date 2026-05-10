using System;
using UnityEngine;
using UnityEngine.UI;

public abstract class RoomPopUpBase : MonoBehaviour
{
    [Header("뒤로가기 UI")]
    [SerializeField] protected Button backButton;

    public void InitializeBase(Action onBackClick)
    {
        if (this.backButton != null)
        {
            this.backButton.onClick.AddListener(() => onBackClick?.Invoke());
        }
    }
}
