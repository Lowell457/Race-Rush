using TMPro;
using UnityEngine;
using Photon;
using Fusion;
using System;

public class ChatManager : MonoBehaviour
{
    [SerializeField] GameObject textHolder;
    TextMeshProUGUI text;
    RectTransform textRect;
    [SerializeField] TextMeshProUGUI inputFieldText;

    [SerializeField] float growthSize;

    public static ChatManager instance;
    public Action<string, string> OnMessageSend; 

    private void Awake()
    {
        if (instance != null)
        {
            Destroy(gameObject);
        }
        else
        {
            instance = this;
        }

        text = textHolder.GetComponent<TextMeshProUGUI>();
        textRect = textHolder.GetComponent<RectTransform>();

        text.text = NetworkManager.Instance.ChatLog;
    }

    public void PlayerChat(string playerName, string message)
    {
        if (message.Length > 0)
        {
            text.text += playerName + ": " + message + "\n";

            Vector3 temp = textRect.sizeDelta;
            temp.y = textRect.sizeDelta.y + growthSize;
            textRect.sizeDelta = temp;
            /*Vector3 temp2 = textRect.sizeDelta;
            temp2 = textRect.position;
            temp2.y = textRect.sizeDelta.y / 2;
            textRect.position = temp2;*/
        }

        NetworkManager.Instance.SendChatLogs(text.text);
    }

    public void SendChatMessage(string newMessage)
    {
        OnMessageSend?.Invoke(NetworkManager.Instance.LocalNickname, newMessage);
    }

    public void ClearInputField()
    {
        inputFieldText.text = string.Empty;
    }
}
