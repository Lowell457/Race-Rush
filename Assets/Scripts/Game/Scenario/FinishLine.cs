using System.Collections.Generic;
using Fusion;
using TMPro;
using UnityEngine;

public class FinishLine : NetworkBehaviour
{
    public static FinishLine instance;

    [SerializeField] GameObject VictoryScreen;
    [SerializeField] GameObject wfopText;
    [SerializeField] GameObject PositionText;

    [Header("Cuenta regresiva final")]
    [SerializeField] float finishCountdownSeconds = 10f;
    [SerializeField] TextMeshProUGUI finishCountdownText;

    TextMeshProUGUI posTextTMP;

    [Networked] public int playersFinished { get; set; }
    [Networked] TickTimer FinishCountdown { get; set; }
    [Networked] NetworkBool RaceEnded { get; set; }

    readonly List<string> finishList = new();   // solo lo usa el host

    void Awake()
    {
        if (instance != null && instance != this) { Destroy(gameObject); return; }
        instance = this;

        posTextTMP = PositionText.GetComponent<TextMeshProUGUI>();
        if (finishCountdownText) finishCountdownText.gameObject.SetActive(false);
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (!HasStateAuthority || RaceEnded) return;

        var plMov = collision.GetComponent<PlayerMovement>();
        if (plMov == null || plMov.HasFinished) return;

        plMov.PlayerStopForever();
        plMov.RPC_WinScreen();

        playersFinished++;
        finishList.Add(plMov.GetPlayerName());

        // El primero en llegar arranca la cuenta regresiva
        if (playersFinished == 1)
            FinishCountdown = TickTimer.CreateFromSeconds(Runner, finishCountdownSeconds);

        if (playersFinished >= NetworkManager.Instance.GetActivePlayersAmount())
            EndRace();
    }

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority || RaceEnded) return;

        if (FinishCountdown.Expired(Runner))
            EndRace();
    }

    // Corre en host y clientes: muestra los segundos que quedan
    public override void Render()
    {
        if (!finishCountdownText) return;

        bool show = FinishCountdown.IsRunning && !RaceEnded;
        finishCountdownText.gameObject.SetActive(show);

        if (show)
        {
            float left = FinishCountdown.RemainingTime(Runner) ?? 0f;
            finishCountdownText.text = Mathf.CeilToInt(left).ToString();
        }
    }

    // Pública para llamarla también desde el TimeManager cuando se acaba el tiempo
    public void EndRace()
    {
        if (!HasStateAuthority || RaceEnded) return;
        RaceEnded = true;

        int finishedCount = finishList.Count;
        var results = new List<string>(finishList);

        // Los que no llegaron: se frenan y quedan al final como descalificados
        foreach (var p in FindObjectsByType<PlayerMovement>(FindObjectsSortMode.None))
        {
            if (p.HasFinished) continue;
            p.PlayerStopForever();
            results.Add(p.GetPlayerName());
        }

        NetworkManager.Instance.SaveLastRaceResults(results, finishedCount);
        RPC_ShowEndResults(results.ToArray(), finishedCount);
        Invoke(nameof(GoToLobby), 3f);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    void RPC_ShowEndResults(string[] positions, int finishedCount)
    {
        NetworkManager.Instance.SetLocalLastRace(NetworkManager.EncodeResults(positions, finishedCount)); // NUEVO

        VictoryScreen.SetActive(true);   // los descalificados nunca la habían abierto
        wfopText.SetActive(false);
        PositionText.SetActive(true);

        int myPos = System.Array.IndexOf(positions, NetworkManager.Instance.LocalNickname) + 1;

        if (myPos == 0 || myPos > finishedCount)
        {
            posTextTMP.text = "Disqualified";
            return;
        }

        ShowPlayerPosition(myPos, positions.Length);
    }

    void ShowPlayerPosition(int position, int totalPlayers)
    {
        if (totalPlayers == 1)
        {
            posTextTMP.text = "You Made it, maybe try getting some friends next";
            return;
        }

        posTextTMP.text = position switch
        {
            1 => "You came in First",
            2 => "You came in Second",
            3 => "You came in Third",
            4 => "You came in Last",
            _ => "You came in " + position
        };
    }

    public void TriggerWinScreen() => VictoryScreen.SetActive(true);

    void GoToLobby() => Runner.LoadScene("Lobby");
}