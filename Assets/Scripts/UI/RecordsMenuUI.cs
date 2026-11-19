using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Pantalla RÉCORDS: selección de mapa (01..05) + top 5 del mapa elegido.
/// El botón RÉCORDS del menú principal tiene que llamar a Open().
/// </summary>
public class RecordsMenuUI : MonoBehaviour
{
    [Header("Paneles")]
    [SerializeField] GameObject mapSelectPanel;   // "SELECCIONÁ UN CIRCUITO"
    [SerializeField] GameObject boardPanel;       // "RÉCORDS — MAPA 0X"

    [Header("Selección de mapa")]
    [SerializeField] Button[] mapButtons = new Button[5];
    [SerializeField] Button closeButton;          // vuelve al menú principal

    [Header("Leaderboard")]
    [SerializeField] TMP_Text titleText;
    [SerializeField] TMP_Text[] rowTexts = new TMP_Text[5];
    [SerializeField] TMP_Text myBestText;
    [SerializeField] TMP_Text statusText;
    [SerializeField] Button backButton;           // vuelve a la selección de mapa

    [Header("Estilo")]
    [SerializeField] Color myRowColor = new Color(1f, 0.83f, 0f);

    int _currentMap = -1;

    void Awake()
    {
        for (int i = 0; i < mapButtons.Length; i++)
        {
            int idx = i; // captura para el lambda
            if (mapButtons[i] != null) mapButtons[i].onClick.AddListener(() => ShowBoard(idx));
        }
        if (backButton != null) backButton.onClick.AddListener(BackToMapSelect);
        if (closeButton != null) closeButton.onClick.AddListener(Close);
    }

    public void Open()
    {
        gameObject.SetActive(true);
        BackToMapSelect();
    }

    public void Close()
    {
        _currentMap = -1;
        gameObject.SetActive(false);
    }

    void BackToMapSelect()
    {
        _currentMap = -1;
        mapSelectPanel.SetActive(true);
        boardPanel.SetActive(false);
    }

    async void ShowBoard(int mapIndex)
    {
        _currentMap = mapIndex;
        mapSelectPanel.SetActive(false);
        boardPanel.SetActive(true);

        titleText.text = $"RÉCORDS — MAPA {mapIndex + 1:00}";
        statusText.text = "Cargando...";
        foreach (var r in rowTexts) r.text = "";
        myBestText.text = "";

        var lb = LeaderboardManager.Instance;
        if (lb == null)
        {
            statusText.text = "Falta el LeaderboardManager en la escena.";
            return;
        }

        var rows = await lb.GetTopAsync(mapIndex, rowTexts.Length);
        if (this == null || _currentMap != mapIndex) return; // cerraron o cambiaron de mapa mientras cargaba

        statusText.text = rows.Count == 0 ? "Todavía no hay tiempos. ¡Sé el primero!" : "";
        for (int i = 0; i < rowTexts.Length; i++)
        {
            if (i >= rows.Count) { rowTexts[i].text = $"{i + 1}.<pos=12%>—"; continue; }

            var r = rows[i];
            // <pos=x%> alinea las columnas: puesto | nombre | tiempo
            string line = $"{r.Rank}.<pos=12%>{r.Name}<pos=72%>{LeaderboardManager.FormatTime(r.TimeMs)}";
            rowTexts[i].text = r.IsMe ? $"<color=#{ColorUtility.ToHtmlStringRGB(myRowColor)}>{line}</color>" : line;
        }

        var mine = await lb.GetMyBestAsync(mapIndex);
        if (this == null || _currentMap != mapIndex) return;
        myBestText.text = mine.HasValue
            ? $"Tu récord: {LeaderboardManager.FormatTime(mine.Value.TimeMs)}  (puesto {mine.Value.Rank})"
            : "Todavía no corriste este mapa.";
    }
}