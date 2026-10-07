using System.Linq;
using Fusion;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LobbyUI : MonoBehaviour
{
    [Header("Lista")]
    [SerializeField] Transform listaJugadores;   // con Vertical Layout Group
    [SerializeField] TMP_Text rowPrefab;          // prefab de una fila de texto

    [Header("Info")]
    [SerializeField] TMP_Text txtSesion;
    [SerializeField] TMP_Text txtContador;
    [SerializeField] TMP_Text txtEstado;
    [SerializeField] TMP_Text txtAviso;

    [Header("Botones")]
    [SerializeField] Button btnListo;
    [SerializeField] TMP_Text txtBtnListo;
    [SerializeField] Button btnIniciar;
    [SerializeField] Button btnSalir;

    [Header("Config")]
    [SerializeField] int raceSceneIndex = 2;
    [SerializeField] bool requireAllReady = true;
    [SerializeField] float avisoDuracion = 3f;

    int prevCount = -1;
    float avisoHasta;

    NetworkRunner Runner => NetworkManager.Instance != null ? NetworkManager.Instance.Runner : null;

    void OnEnable() => LobbyPlayer.ListChanged += Refresh;
    void OnDisable() => LobbyPlayer.ListChanged -= Refresh;

    void Start()
    {
        btnListo.onClick.AddListener(OnListo);
        btnIniciar.onClick.AddListener(OnIniciar);
        btnSalir.onClick.AddListener(() => NetworkManager.Instance.Leave());
        txtAviso.gameObject.SetActive(false);
        Refresh();
    }

    void Update()
    {
        if (txtAviso.gameObject.activeSelf && Time.time > avisoHasta)
            txtAviso.gameObject.SetActive(false);
    }

    void Refresh()
    {
        var runner = Runner;
        if (runner == null) return;

        var players = LobbyPlayer.All
            .Where(p => p != null && p.Object != null && p.Object.IsValid)
            .OrderBy(p => p.Object.InputAuthority.PlayerId)
            .ToList();

        int count = players.Count;
        int max = NetworkManager.MaxPlayers;

        // Aviso de desconexión (GDD sección 9)
        if (prevCount > 0 && count < prevCount) ShowAviso("Un jugador abandonó la sala.");
        else if (prevCount > 0 && count > prevCount) ShowAviso("Se unió un jugador.");
        prevCount = count;

        // Filas: siempre 4, con huecos vacíos para mostrar la capacidad
        foreach (Transform t in listaJugadores) Destroy(t.gameObject);
        for (int i = 0; i < max; i++)
        {
            var row = Instantiate(rowPrefab, listaJugadores);
            if (i < count)
            {
                var p = players[i];
                string nick = string.IsNullOrEmpty(p.Nickname.ToString()) ? "Conectando..." : p.Nickname.ToString();
                string estado = p.IsReady ? "LISTO" : "ESPERANDO";
                string vos = p.Object.HasInputAuthority ? "  (vos)" : "";
                string host = p.Object.InputAuthority == runner.LocalPlayer && runner.IsServer ? "  [HOST]" : "";
                row.text = $"● {nick}   {estado}{vos}{host}";
            }
            else
            {
                row.text = "○ — vacío —";
                row.alpha = 0.4f;
            }
        }

        // Info de sesión y contador
        txtSesion.text = runner.SessionInfo.IsValid ? runner.SessionInfo.Name : "";
        txtContador.text = $"{count} / {max} JUGADORES";

        // Botón Listo (mi propio jugador)
        var local = players.Find(p => p.Object.HasInputAuthority);
        btnListo.interactable = local != null;
        txtBtnListo.text = local != null && local.IsReady ? "CANCELAR" : "LISTO";

        // Botón Iniciar: solo host, sala llena (y todos listos si se exige)
        //bool full = count > 1;
        bool full = true;
        bool allReady = players.All(p => p.IsReady);
        bool canStart = full && (!requireAllReady || allReady);

        btnIniciar.gameObject.SetActive(runner.IsServer);
        btnIniciar.interactable = canStart;

        // Estado general
        if (!full)
            txtEstado.text = $"Esperando jugadores ({count}/{max})...";
        else if (requireAllReady && !allReady)
            txtEstado.text = "Sala llena. Esperando que todos estén listos...";
        else
            txtEstado.text = runner.IsServer ? "¡Todo listo! Podés iniciar la carrera." : "Esperando que el host inicie...";
    }

    void OnListo()
    {
        var local = LobbyPlayer.All.Find(p => p != null && p.Object != null && p.Object.HasInputAuthority);
        if (local != null) local.RPC_ToggleReady();
    }

    void OnIniciar()
    {
        var runner = Runner;
        if (runner == null || !runner.IsServer) return;

        if (raceSceneIndex >= UnityEngine.SceneManagement.SceneManager.sceneCountInBuildSettings)
        {
            ShowAviso("Todavía no existe la escena de carrera en Build Settings.");
            return;
        }

        runner.SessionInfo.IsOpen = false;   // desaparece de la lista de búsqueda
        runner.LoadScene(SceneRef.FromIndex(raceSceneIndex));
    }

    void ShowAviso(string msg)
    {
        txtAviso.text = msg;
        txtAviso.gameObject.SetActive(true);
        avisoHasta = Time.time + avisoDuracion;
    }
}