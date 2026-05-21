using System;
using TMPro;
using UnityEngine;

public class PlayerInfoUI : MonoBehaviour
{
    [SerializeField] private TMP_Text nickname;
    [SerializeField] private TMP_Text timer;

    private float time;
    private bool isStarted = false;

    void Update()
    {
        if (this.isStarted == false) return;

        if (this.time > 0.0f)
        {
            this.time = Mathf.Max(0.0f, this.time - Time.deltaTime);

            SetTime();
        }
    }

    private void SetTime()
    {
        TimeSpan timeSpan = TimeSpan.FromSeconds(this.time);

        if (this.time > 10.0f)
        {
            this.timer.text = timeSpan.ToString(@"mm\:ss");
        }
        else
        {
            this.timer.text = timeSpan.ToString(@"ss\.ff");
        }
    }

    public void Setup(string nickname, int ms)
    {
        this.nickname.text = nickname;

        this.time = ms / 1000f;

        SetTime();
    }

    public void SetTimer(bool isStarted)
    {
        this.isStarted = isStarted;
    }
}
