using TMPro;
using UnityEngine;
using Photon;
using Fusion;

public class ChatManager : NetworkBehaviour
{
    [SerializeField] GameObject textHolder;
    TextMeshProUGUI text;
    RectTransform textRect;
    [SerializeField] TextMeshProUGUI inputFieldText;

    [SerializeField] float growthSize;


    private void Awake()
    {
        text = textHolder.GetComponent<TextMeshProUGUI>();
        textRect = textHolder.GetComponent<RectTransform>();
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.All)]
    public void RPC_playerChat(string message)
    {

        if (message.Length > 0)
        {
            text.text += NetworkManager.Instance.LocalNickname + ": " + message + "\n";

            Vector3 temp = textRect.sizeDelta;
            temp.y = textRect.sizeDelta.y + growthSize;
            textRect.sizeDelta = temp;
            /*Vector3 temp2 = textRect.sizeDelta;
            temp2 = textRect.position;
            temp2.y = textRect.sizeDelta.y / 2;
            textRect.position = temp2;*/
        }


    }
    public void ClearInputField()
    {
        inputFieldText.text = string.Empty;
    }

}
