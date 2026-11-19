using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Leaderboards;
using Unity.Services.Leaderboards.Exceptions;
using UnityEngine;

/// <summary>
/// Singleton que habla con UGS: inicializa, loguea, envía tiempos y trae el top.
/// Ponelo en un GameObject de la escena del menú (sobrevive entre escenas).
/// </summary>
public class LeaderboardManager : MonoBehaviour
{
    public static LeaderboardManager Instance { get; private set; }

    // IDs tal cual los creaste en el Dashboard de UGS (orden ascendente, "keep best").
    static readonly string[] MapLeaderboardIds = { "map_01", "map_02", "map_03", "map_04", "map_05" };

    // Tiempo mínimo razonable por mapa (ms). Si llega algo menor, es bug o trampa: no se envía.
    [SerializeField] int[] minPlausibleMs = { 3000, 3000, 3000, 3000, 3000 };

    public bool IsReady { get; private set; }

    // Envío pendiente si falló la red, para el botón "Reintentar".
    int _pendingMap = -1;
    int _pendingMs;
    public bool HasPendingSubmit => _pendingMap >= 0;

    public event Action<string> OnError;          // la UI muestra el mensaje
    public event Action<int, int> OnSubmitted;     // (mapa, ms) enviado OK

    // Una sola inicialización compartida: si alguien pide datos mientras se está
    // inicializando, espera ESA misma tarea en vez de arrancar otra en paralelo.
    Task<bool> _initTask;

    // Bug conocido de Leaderboards 2.3.x en Unity 6: LeaderboardsService.Instance puede tirar
    // "not initialized" aunque Core esté OK. Se pide el servicio al registro de Core y,
    // si no está, se cae al acceso estático.
    ILeaderboardsService _lb;
    ILeaderboardsService LB => _lb ??= ResolveLeaderboards();

    static ILeaderboardsService ResolveLeaderboards()
    {
        try
        {
            var s = UnityServices.Instance.GetLeaderboardsService();
            if (s != null) return s;
        }
        catch (Exception e) { Debug.LogWarning($"[Leaderboards] Registro de Core falló, uso el estático: {e.Message}"); }
        return LeaderboardsService.Instance;
    }

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // Inicializar en Start (no en Awake): también reportado como parte del workaround del bug.
    void Start()
    {
        if (Instance == this) _ = EnsureReadyAsync();
    }

    public Task<bool> EnsureReadyAsync()
    {
        if (IsReady) return Task.FromResult(true);
        if (_initTask == null || (_initTask.IsCompleted && !_initTask.Result))
            _initTask = InitInternalAsync(); // primera vez, o reintento si la anterior falló
        return _initTask;
    }

    async Task<bool> InitInternalAsync()
    {
        try
        {
            if (UnityServices.State != ServicesInitializationState.Initialized)
                await UnityServices.InitializeAsync();

            if (!AuthenticationService.Instance.IsSignedIn)
                await AuthenticationService.Instance.SignInAnonymouslyAsync();

            // Diagnóstico: Core y Auth OK no garantizan que Leaderboards se haya registrado.
            try { _lb = ResolveLeaderboards(); }
            catch (Exception e)
            {
                Debug.LogError($"[Leaderboards] Core={UnityServices.State}, SignedIn={AuthenticationService.Instance.IsSignedIn}, " +
                               $"pero el servicio de Leaderboards NO está registrado: {e.Message}");
                return false;
            }

            IsReady = true;
            Debug.Log($"[Leaderboards] Listo. PlayerId: {AuthenticationService.Instance.PlayerId}");
            return true;
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[Leaderboards] Init falló: {e.Message}");
            OnError?.Invoke("No se pudo conectar con el servidor de récords.");
            return false;
        }
    }

    /// <summary>Nombre visible en la tabla. Sin espacios; UGS le agrega "#1234".</summary>
    public async Task SetPlayerNameAsync(string name)
    {
        if (!await EnsureReadyAsync()) return;
        try { await AuthenticationService.Instance.UpdatePlayerNameAsync(name.Replace(" ", "_")); }
        catch (Exception e) { Debug.LogWarning($"[Leaderboards] Nombre: {e.Message}"); }
    }

    /// <summary>mapIndex 0..4. Llamar UNA vez por carrera, solo para el jugador local.</summary>
    public async Task SubmitTimeAsync(int mapIndex, int timeMs)
    {
        if (mapIndex < 0 || mapIndex >= MapLeaderboardIds.Length) return;
        Debug.Log($"[Leaderboards] Enviando {FormatTime(timeMs)} ({timeMs} ms) a {MapLeaderboardIds[mapIndex]}");
        if (timeMs < minPlausibleMs[mapIndex])
        {
            Debug.LogWarning($"[Leaderboards] Tiempo descartado: {timeMs} ms es menor al mínimo de {minPlausibleMs[mapIndex]} ms (ajustalo en el Inspector)");
            return;
        }

        _pendingMap = mapIndex;
        _pendingMs = timeMs;

        try
        {
            if (!await EnsureReadyAsync()) throw new Exception("Servicios no inicializados");
            // "Keep best" en el Dashboard hace que solo se guarde si mejora el récord.
            var entry = await LB.AddPlayerScoreAsync(MapLeaderboardIds[mapIndex], timeMs);
            Debug.Log($"[Leaderboards] Guardado OK. Tu mejor marca: {FormatTime((int)entry.Score)} (puesto {entry.Rank + 1})");
            _pendingMap = -1;
            OnSubmitted?.Invoke(mapIndex, timeMs);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[Leaderboards] Envío falló: {e.Message} | Core={UnityServices.State} IsReady={IsReady} " +
                             $"Instancia={GetInstanceID()} Escena={gameObject.scene.name}");
            OnError?.Invoke("No se pudo guardar tu tiempo. Probá de nuevo.");
        }
    }

    public Task RetryPendingAsync() =>
        HasPendingSubmit ? SubmitTimeAsync(_pendingMap, _pendingMs) : Task.CompletedTask;

    public struct Row { public int Rank; public string Name; public int TimeMs; public bool IsMe; }

    /// <summary>Top N de un mapa (por defecto 5).</summary>
    public async Task<List<Row>> GetTopAsync(int mapIndex, int limit = 5)
    {
        var rows = new List<Row>();
        try
        {
            if (!await EnsureReadyAsync()) return rows;
            var page = await LB.GetScoresAsync(
                MapLeaderboardIds[mapIndex], new GetScoresOptions { Limit = limit });

            string myId = AuthenticationService.Instance.PlayerId;
            foreach (var e in page.Results)
                rows.Add(new Row
                {
                    Rank = e.Rank + 1, // UGS arranca en 0
                    Name = StripSuffix(e.PlayerName),
                    TimeMs = (int)e.Score,
                    IsMe = e.PlayerId == myId
                });
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[Leaderboards] Lectura falló: {e.Message}");
            OnError?.Invoke("No se pudieron cargar los récords.");
        }
        return rows;
    }

    /// <summary>Récord personal en un mapa, o null si nunca lo corrió.</summary>
    public async Task<Row?> GetMyBestAsync(int mapIndex)
    {
        try
        {
            if (!await EnsureReadyAsync()) return null;
            var e = await LB.GetPlayerScoreAsync(MapLeaderboardIds[mapIndex]);
            return new Row { Rank = e.Rank + 1, Name = StripSuffix(e.PlayerName), TimeMs = (int)e.Score, IsMe = true };
        }
        catch (LeaderboardsException e) when (e.Reason == LeaderboardsExceptionReason.EntryNotFound)
        {
            return null;
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[Leaderboards] Mi récord falló: {e.Message}");
            return null;
        }
    }

    static string StripSuffix(string n)
    {
        if (string.IsNullOrEmpty(n)) return "???";
        int i = n.LastIndexOf('#');
        return i > 0 ? n.Substring(0, i) : n;
    }

    /// <summary>48320 -> "00:48.32"</summary>
    public static string FormatTime(int ms)
    {
        var t = TimeSpan.FromMilliseconds(ms);
        return $"{(int)t.TotalMinutes:00}:{t.Seconds:00}.{t.Milliseconds / 10:00}";
    }
}