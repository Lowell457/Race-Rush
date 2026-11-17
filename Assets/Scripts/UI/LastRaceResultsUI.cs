using System.Text;
using TMPro;
using UnityEngine;

public class LastRaceResultsUI : MonoBehaviour
{
    [SerializeField] TMP_Text resultsText;

    string lastRaw = "\0";   // valor imposible para forzar el primer refresh

    void Update()
    {
        var nm = NetworkManager.Instance;
        string raw = nm != null ? nm.GetLastRaceRaw() : null;

        if (raw == lastRaw) return;   // solo redibuja si cambió
        lastRaw = raw;
        resultsText.text = Format(raw);
    }

    static string Format(string raw)
    {
        if (string.IsNullOrEmpty(raw)) return "No previous record";

        var sb = new StringBuilder("Last race results\n");
        int pos = 1;

        foreach (var entry in raw.Split('|'))
        {
            if (entry.StartsWith("!"))
                sb.AppendLine($"DQ  {entry.Substring(1)}");
            else
                sb.AppendLine($"{pos++}.  {entry}");
        }
        return sb.ToString();
    }
}