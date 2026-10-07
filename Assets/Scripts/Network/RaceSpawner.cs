using System.Collections.Generic;
using Fusion;
using UnityEngine;

public class RaceSpawner : NetworkBehaviour, IPlayerJoined, IPlayerLeft
{
    [SerializeField] NetworkObject playerPrefab;
    [SerializeField] Transform[] spawnPoints;

    readonly Dictionary<PlayerRef, NetworkObject> _players = new();

    public override void Spawned()
    {
        if (!HasStateAuthority) return;
        foreach (var p in Runner.ActivePlayers) SpawnFor(p);
    }

    public void PlayerJoined(PlayerRef p)
    {
        if (HasStateAuthority) SpawnFor(p);
    }

    public void PlayerLeft(PlayerRef p)
    {
        if (!HasStateAuthority) return;
        if (_players.Remove(p, out var obj)) Runner.Despawn(obj);   // GDD 9: desaparece al desconectarse
    }

    void SpawnFor(PlayerRef p)
    {
        if (_players.ContainsKey(p)) return;

        var point = spawnPoints[_players.Count % spawnPoints.Length];
        var obj = Runner.Spawn(playerPrefab, point.position, Quaternion.identity, inputAuthority: p);
        Runner.SetPlayerObject(p, obj);
        _players[p] = obj;
    }
}