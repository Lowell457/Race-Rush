using Fusion;
using UnityEngine;

public class PlayerChatController : NetworkBehaviour
{
    public override void Spawned()
    {
        base.Spawned();

        ChatManager.instance.OnMessageSend += RPC_SendMessage;
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    public void RPC_SendMessage(string playerName, string message)
    {
        if (message.Length >= 20)
            return;

        ChatManager.instance.PlayerChat(playerName, message);
    }
}
