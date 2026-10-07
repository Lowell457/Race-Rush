using Fusion;
using System.Collections.Generic;
using UnityEngine;
using static Unity.Collections.Unicode;

public class LobbyManager : NetworkBehaviour, IPlayerJoined, IPlayerLeft
{
    [SerializeField] NetworkObject lobbyPlayerPrefab;
    readonly Dictionary<PlayerRef, NetworkObject> _players = new();

    public override void Spawned()
    {
        if (!HasStateAuthority) return;
        foreach (var p in Runner.ActivePlayers) SpawnFor(p); // el host ya estaba antes de cargar la escena
    }

    public void PlayerJoined(PlayerRef p) { if (HasStateAuthority) SpawnFor(p); }

    public void PlayerLeft(PlayerRef p)
    {
        if (!HasStateAuthority) return;
        if (_players.Remove(p, out var obj)) Runner.Despawn(obj);
    }

    void SpawnFor(PlayerRef p)
    {
        if (_players.ContainsKey(p)) return;
        var obj = Runner.Spawn(lobbyPlayerPrefab, inputAuthority: p);
        Runner.SetPlayerObject(p, obj);
        _players[p] = obj;
    }
}