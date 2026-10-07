using Fusion;
using System;
using UnityEngine;

public class SessionRowUI : MonoBehaviour
{
    [SerializeField] TMPro.TMP_Text label;
    [SerializeField] UnityEngine.UI.Button button;

    public void Setup(SessionInfo info, Action<SessionInfo> onClick)
    {
        label.text = $"{info.Name}   {info.PlayerCount}/{info.MaxPlayers}";
        button.onClick.AddListener(() => onClick(info));
    }
}