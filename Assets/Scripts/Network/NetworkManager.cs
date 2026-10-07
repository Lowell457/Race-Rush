using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Fusion;
using Fusion.Sockets;
using UnityEngine;

public class NetworkManager : MonoBehaviour, INetworkRunnerCallbacks
{
    public static NetworkManager Instance { get; private set; }
    public NetworkRunner Runner { get; private set; }

    public string PendingMessage { get; set; }
    public string LocalNickname { get; set; }

    public event Action<List<SessionInfo>> SessionListUpdated;
    public event Action<string> StatusChanged;

    [SerializeField] int lobbySceneIndex = 1;
    [SerializeField] int menuSceneIndex = 0;
    public const int MaxPlayers = 4;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void SetStatus(string s) { Debug.Log(s); StatusChanged?.Invoke(s); }

    NetworkRunner CreateRunner()
    {
        var go = new GameObject("NetworkRunner");
        go.transform.SetParent(transform);
        var runner = go.AddComponent<NetworkRunner>();
        runner.ProvideInput = true;
        go.AddComponent<NetworkSceneManagerDefault>();
        runner.AddCallbacks(this);
        return runner;
    }

    void KillRunner()
    {
        if (Runner != null) { Destroy(Runner.gameObject); Runner = null; }
    }

    public async Task<bool> JoinLobby()
    {
        if (Runner == null) Runner = CreateRunner();
        SetStatus("Conectando...");
        var r = await Runner.JoinSessionLobby(SessionLobby.ClientServer);
        if (r.Ok) { SetStatus("Conectado. Buscando partidas..."); return true; }
        SetStatus($"Error al conectar: {r.ShutdownReason}");
        KillRunner();
        return false;
    }

    public async Task<bool> CreateSession()
    {
        SetStatus("Creando partida...");
        var r = await Runner.StartGame(new StartGameArgs
        {
            GameMode = GameMode.Host,
            SessionName = $"Carrera-{UnityEngine.Random.Range(1000, 9999)}",
            PlayerCount = MaxPlayers,
            Scene = SceneRef.FromIndex(lobbySceneIndex),
            SceneManager = Runner.GetComponent<NetworkSceneManagerDefault>()
        });
        if (r.Ok) return true;
        SetStatus($"Error al crear: {r.ShutdownReason}");
        KillRunner();
        return false;
    }

    public async Task<bool> JoinSession(SessionInfo info)
    {
        SetStatus($"Entrando a {info.Name}...");
        var r = await Runner.StartGame(new StartGameArgs
        {
            GameMode = GameMode.Client,
            SessionName = info.Name,
            SceneManager = Runner.GetComponent<NetworkSceneManagerDefault>()
        });
        if (r.Ok) return true;
        SetStatus($"No se pudo entrar: {r.ShutdownReason}");
        KillRunner();
        return false;
    }

    public void Leave() { if (Runner != null) Runner.Shutdown(); }

    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList)
    => SessionListUpdated?.Invoke(sessionList);

    public void OnShutdown(NetworkRunner runner, ShutdownReason reason)
    {
        if (Runner == runner) Runner = null;
        if (runner != null) Destroy(runner.gameObject);

        if (reason != ShutdownReason.Ok)
            PendingMessage = $"La sesión terminó: {reason}";

        SetStatus("Desconectado.");

        var active = UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex;
        if (active != menuSceneIndex)
            UnityEngine.SceneManagement.SceneManager.LoadScene(menuSceneIndex);
    }

    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
        => SetStatus($"Se perdió la conexión con el host ({reason})");

    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player)
    {
    }

    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player)
    {
    }

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
    }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
    }

    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token)
    {
    }

    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason)
    {
    }

    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message)
    {
    }

    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data)
    {
    }

    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress)
    {
    }

    public void OnInput(NetworkRunner runner, NetworkInput input)
    {
    }

    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input)
    {
    }

    public void OnConnectedToServer(NetworkRunner runner)
    {
    }

    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data)
    {
    }

    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken)
    {
    }

    public void OnSceneLoadDone(NetworkRunner runner)
    {
    }

    public void OnSceneLoadStart(NetworkRunner runner)
    {
    }
}