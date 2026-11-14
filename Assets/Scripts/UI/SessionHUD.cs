using System.Linq;
using Fusion;
using TMPro;
using UnityEngine;

public class SessionHUD : MonoBehaviour
{
    [SerializeField] TMP_Text roomNameText;
    [SerializeField] TMP_Text playerCountText;

    string lastName;
    int lastCount = -1;

    void Update()
    {
        var nm = NetworkManager.Instance;
        if (nm == null) return;

        var runner = nm.Runner;
        if (runner == null || !runner.IsRunning) return;

        var info = runner.SessionInfo;
        if (info == null || !info.IsValid) return;

        // Nombre de la sala (solo se actualiza si cambia)
        if (info.Name != lastName)
        {
            lastName = info.Name;
            roomNameText.text = $"Sala: {info.Name}";
        }

        // Cantidad de jugadores
        int count = runner.ActivePlayers.Count();
        if (count != lastCount)
        {
            lastCount = count;
            int max = info.MaxPlayers > 0 ? info.MaxPlayers : NetworkManager.MaxPlayers;
            playerCountText.text = $"Jugadores: {count}/{max}";
        }
    }
}