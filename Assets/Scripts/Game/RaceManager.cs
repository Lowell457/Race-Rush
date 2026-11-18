using Fusion;
using UnityEngine;

// 1) RaceManager: uno por escena de mapa, como NetworkObject en la escena.
//    El que tiene State Authority (host, o master client en Shared) marca la largada.
public class RaceManager : NetworkBehaviour
{
    public static RaceManager Instance { get; private set; }

    [Tooltip("0 a 4 — tiene que coincidir con el orden de leaderboards en LeaderboardManager")]
    [SerializeField] int mapIndex;
    public int MapIndex => mapIndex;

    // Tick de red en el que arrancó la carrera. 0 = todavía no arrancó.
    [Networked] public int StartTick { get; set; }

    public bool RaceStarted => StartTick > 0;

    public override void Spawned() => Instance = this;
    public override void Despawned(NetworkRunner runner, bool hasState) { if (Instance == this) Instance = null; }

    /// <summary>Llamalo cuando termina la cuenta regresiva (solo tiene efecto en la autoridad).</summary>
    public void StartRace()
    {
        if (!HasStateAuthority || RaceStarted) return;
        StartTick = Runner.Tick.Raw;
    }

    /// <summary>Convierte ticks a ms usando el tick rate de la simulación (igual para todos).</summary>
    public int TicksToMs(int finishTick) =>
        Mathf.RoundToInt((finishTick - StartTick) * Runner.DeltaTime * 1000f);
}