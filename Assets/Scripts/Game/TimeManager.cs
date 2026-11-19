using Fusion;
using TMPro;
using UnityEngine;

/// <summary>
/// Cuenta regresiva + tiempo límite, sincronizados por red con TickTimer.
/// Lo decide todo la State Authority (host); los demás solo lo dibujan en Render().
/// También guarda StartTick para calcular el tiempo de cada jugador para el leaderboard.
/// </summary>
public class TimeManager : NetworkBehaviour
{
    public static TimeManager instance;

    [Header("Mapa")]
    [Tooltip("0 a 4 — mismo orden que los leaderboards map_01..map_05")]
    [SerializeField] private int mapIndex;
    public int MapIndex => mapIndex;

    [Header("Start Countdown")]
    [SerializeField] private TextMeshProUGUI countDownText;
    [SerializeField] private float countDownTime = 3f;
    [SerializeField] private float maxSize;
    [SerializeField] private float minSize;
    [SerializeField] private GameObject panel;
    [SerializeField] private float goDisplayTime = 1f;

    [Header("Time")]
    [SerializeField] private float maxTime;
    [SerializeField] private TextMeshProUGUI timerText;

    // ── Estado de red (lo escribe solo la State Authority) ──
    [Networked] private TickTimer CountDown { get; set; }
    [Networked] private TickTimer RaceLimit { get; set; }
    [Networked] public int StartTick { get; private set; }   // 0 = no arrancó
    [Networked] public NetworkBool RaceOver { get; private set; }

    public bool RaceStarted => StartTick > 0;

    // ── Estado local, solo visual ──
    private bool startInvokedLocally;
    private float goHideAt;

    public override void Spawned()
    {
        if (instance != null && instance != this)
        {
            if (HasStateAuthority) Runner.Despawn(Object);
            return;
        }
        instance = this;

        panel.SetActive(true);
        countDownText.gameObject.SetActive(true);
        timerText.gameObject.SetActive(false);

        if (HasStateAuthority)
            CountDown = TickTimer.CreateFromSeconds(Runner, countDownTime);
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        if (instance == this) instance = null;
    }

    // Lógica: solo la autoridad. Se corre por tick, no por frame.
    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority) return;

        if (!RaceStarted && CountDown.Expired(Runner))
        {
            StartTick = Runner.Tick.Raw;
            RaceLimit = TickTimer.CreateFromSeconds(Runner, maxTime);
        }

        if (RaceStarted && !RaceOver && RaceLimit.Expired(Runner))
        {
            RaceOver = true;
            FinishLine.instance.EndRace(2); // fin por tiempo — lo decide UNA sola máquina
        }
    }

    // Visual: todos los clientes, una vez por frame.
    public override void Render()
    {
        if (!RaceStarted)
        {
            DrawCountdown();
            return;
        }

        if (!startInvokedLocally)
        {
            startInvokedLocally = true;
            panel.SetActive(false);
            countDownText.text = "GO";
            countDownText.color = Color.green;
            countDownText.fontSize = maxSize;
            goHideAt = Time.time + goDisplayTime;
            timerText.gameObject.SetActive(true);
            NetworkManager.Instance.StartRace?.Invoke();
        }

        if (goHideAt > 0f && Time.time >= goHideAt)
        {
            countDownText.gameObject.SetActive(false);
            goHideAt = 0f;
        }

        float remaining = RaceOver ? 0f : (RaceLimit.RemainingTime(Runner) ?? 0f);
        timerText.text = remaining <= 0f ? "0" : (Mathf.Floor(remaining * 10f) / 10f).ToString("0.0");
    }

    private void DrawCountdown()
    {
        float realTime = CountDown.RemainingTime(Runner) ?? countDownTime; // null = todavía no llegó el estado
        int seconds = (int)realTime;

        countDownText.text = (seconds + 1).ToString();
        countDownText.fontSize = Mathf.Lerp(minSize, maxSize, realTime - seconds);
        countDownText.color = seconds == 1 ? Color.yellow
                            : seconds == 0 ? Color.green
                            : Color.red;
    }

    /// <summary>Ticks desde la largada → milisegundos. Mismo resultado en todas las máquinas.</summary>
    public int TicksToMs(int finishTick) =>
        Mathf.RoundToInt((finishTick - StartTick) * Runner.DeltaTime * 1000f);
}