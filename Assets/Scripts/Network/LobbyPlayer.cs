using System;
using Fusion;
using System.Collections.Generic;
using static Fusion.NetworkBehaviour;
using static Unity.Collections.Unicode;

public class LobbyPlayer : NetworkBehaviour
{
    public static readonly List<LobbyPlayer> All = new();
    public static event Action ListChanged;

    [Networked] public NetworkString<_16> Nickname { get; set; }
    [Networked] public NetworkBool IsReady { get; set; }

    ChangeDetector _changes;

    public override void Spawned()
    {
        _changes = GetChangeDetector(ChangeDetector.Source.SimulationState);
        All.Add(this);
        if (Object.HasInputAuthority)
        {
            var nick = NetworkManager.Instance.LocalNickname;
            if (string.IsNullOrWhiteSpace(nick)) nick = $"Jugador {Runner.LocalPlayer.PlayerId}";
            RPC_SetNickname(nick);
        }
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        All.Remove(this);
        ListChanged?.Invoke();
    }

    public override void Render()
    {
        foreach (var _ in _changes.DetectChanges(this)) { ListChanged?.Invoke(); break; }
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    void RPC_SetNickname(string nick) => Nickname = nick;

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void RPC_ToggleReady() => IsReady = !IsReady;
}