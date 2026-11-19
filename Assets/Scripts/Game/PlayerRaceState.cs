using Fusion;
using UnityEngine;

/// <summary>
/// Va en el prefab del jugador. Guarda el tick en que cruzó la meta y,
/// en el cliente dueño del personaje, envía el tiempo a UGS una sola vez.
/// </summary>
public class PlayerRaceState : NetworkBehaviour
{
    // Tick en que cruzó la meta. 0 = no terminó. Lo escribe SOLO la State Authority.
    [Networked] public int FinishTick { get; private set; }

    private bool submitted; // local: un solo envío por carrera

    public bool Finished => FinishTick > 0;

    /// <summary>Llamalo desde tu FinishLine cuando el jugador toca la meta.</summary>
    public void MarkFinished()
    {
        Debug.Log($"[Race] MarkFinished llamado. StateAuth={HasStateAuthority} YaTerminó={Finished}");
        if (!HasStateAuthority || Finished) return;

        var tm = TimeManager.instance;
        if (tm == null) { Debug.LogWarning("[Race] No hay TimeManager.instance"); return; }
        if (!tm.RaceStarted || tm.RaceOver)
        {
            Debug.LogWarning($"[Race] Llegada ignorada. RaceStarted={tm.RaceStarted} RaceOver={tm.RaceOver}");
            return;
        }

        FinishTick = Runner.Tick.Raw;
        Debug.Log($"[Race] FinishTick = {FinishTick}");
    }

    public override void Render()
    {
        // AddPlayerScoreAsync escribe para el jugador logueado en ESTE cliente,
        // así que cada uno envía el suyo.
        if (submitted || !Finished || !Object.HasInputAuthority) return;

        var tm = TimeManager.instance;
        if (tm == null) return;

        submitted = true;
        int ms = tm.TicksToMs(FinishTick);
        Debug.Log($"[Race] Terminaste en {LeaderboardManager.FormatTime(ms)}");

        if (LeaderboardManager.Instance == null)
        {
            Debug.LogWarning("[Race] No hay LeaderboardManager: ¿arrancaste desde la escena del menú?");
            return;
        }
        _ = LeaderboardManager.Instance.SubmitTimeAsync(tm.MapIndex, ms);
    }
}