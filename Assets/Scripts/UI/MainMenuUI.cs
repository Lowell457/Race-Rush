using System.Collections.Generic;
using Fusion;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MainMenuUI : MonoBehaviour
{
    [Header("Paneles")]
    [SerializeField] GameObject panelPrincipal;
    [SerializeField] GameObject panelBuscar;

    [Header("Panel principal")]
    [SerializeField] Button btnCrear;
    [SerializeField] Button btnBuscar;
    [SerializeField] Button btnSalir;
    [SerializeField] Button btnReintentar;
    [SerializeField] TMP_Text txtEstado;
    [SerializeField] TMP_Text txtAviso;

    [Header("Panel buscar")]
    [SerializeField] Transform content;
    [SerializeField] SessionRowUI rowPrefab;
    [SerializeField] GameObject txtSinPartidas;
    [SerializeField] Button btnVolver;

    [SerializeField] TMP_InputField inputNombre;

    bool busy;

    NetworkManager NM => NetworkManager.Instance;

    void Start()
    {
        NM.StatusChanged += OnStatus;
        NM.SessionListUpdated += OnSessions;

        btnCrear.onClick.AddListener(OnCrear);
        btnBuscar.onClick.AddListener(() => ShowPanel(panelBuscar));
        btnVolver.onClick.AddListener(() => ShowPanel(panelPrincipal));
        btnSalir.onClick.AddListener(OnSalir);
        btnReintentar.onClick.AddListener(() => _ = Connect());

        ShowPanel(panelPrincipal);
        ShowAviso(NM.PendingMessage);   // ej: "El host cerró la partida"
        NM.PendingMessage = null;

        ClearList();
        txtSinPartidas.SetActive(true);

        if (NM.Runner == null) _ = Connect();
        else SetInteractable(true);

        inputNombre.text = PlayerPrefs.GetString("nick", "");
        inputNombre.onValueChanged.AddListener(_ => SetInteractable(!busy && NM.Runner != null));
    }

    void OnDestroy()
    {
        if (NM == null) return;
        NM.StatusChanged -= OnStatus;
        NM.SessionListUpdated -= OnSessions;
    }

    // ---------- Conexión ----------

    async System.Threading.Tasks.Task Connect()
    {
        busy = true;
        SetInteractable(false);
        btnReintentar.gameObject.SetActive(false);

        bool ok = await NM.JoinLobby();

        busy = false;
        SetInteractable(ok);
        btnReintentar.gameObject.SetActive(!ok);
    }

    async void OnCrear()
    {
        if (busy || NM.Runner == null) return;
        if (string.IsNullOrWhiteSpace(inputNombre.text)) return;

        SaveNickname();
        busy = true;
        SetInteractable(false);

        bool ok = await NM.CreateSession();
        // Si sale bien, Fusion carga la escena Lobby y esta UI se destruye.
        if (!ok)
        {
            busy = false;
            ShowAviso("No se pudo crear la partida.");
            await Connect(); // el runner murió: volvemos al lobby de Photon
        }
    }
    async void OnUnirse(SessionInfo info)
    {
        if (busy || NM.Runner == null) return;
        if (string.IsNullOrWhiteSpace(inputNombre.text)) return;

        SaveNickname();
        busy = true;
        SetInteractable(false);

        bool ok = await NM.JoinSession(info);
        if (!ok)
        {
            busy = false;
            ShowAviso($"No se pudo entrar a {info.Name} (¿llena o cerrada?).");
            ShowPanel(panelPrincipal);
            await Connect();
        }
    }

    void OnSalir()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // ---------- Lista de sesiones ----------

    void OnSessions(List<SessionInfo> list)
    {
        ClearList();
        int shown = 0;

        foreach (var s in list)
        {
            if (!s.IsOpen || !s.IsVisible || s.PlayerCount >= s.MaxPlayers) continue;
            var row = Instantiate(rowPrefab, content);
            row.Setup(s, OnUnirse);
            shown++;
        }

        txtSinPartidas.SetActive(shown == 0);
    }

    void ClearList()
    {
        foreach (Transform t in content) Destroy(t.gameObject);
    }

    // ---------- Helpers de UI ----------

    void OnStatus(string s) { if (txtEstado) txtEstado.text = s; }

    void ShowAviso(string msg)
    {
        if (!txtAviso) return;
        txtAviso.gameObject.SetActive(!string.IsNullOrEmpty(msg));
        txtAviso.text = msg;
    }

    void ShowPanel(GameObject panel)
    {
        panelPrincipal.SetActive(panel == panelPrincipal);
        panelBuscar.SetActive(panel == panelBuscar);
    }

    void SetInteractable(bool on)
    {
        bool hasName = inputNombre != null && !string.IsNullOrWhiteSpace(inputNombre.text);
        btnCrear.interactable = on && hasName;
        btnBuscar.interactable = on && hasName;
        inputNombre.interactable = on;
        foreach (var b in content.GetComponentsInChildren<Button>()) b.interactable = on && hasName;
    }

    void SaveNickname()
    {
        NM.LocalNickname = inputNombre.text.Trim();
        PlayerPrefs.SetString("nick", NM.LocalNickname);
    }
}