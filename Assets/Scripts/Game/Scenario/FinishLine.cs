using UnityEngine;
using Fusion;
using System.Linq;
using TMPro;
public class FinishLine : NetworkBehaviour
{
    public static FinishLine instance;

    [SerializeField] GameObject VictoryScreen;
    [SerializeField] GameObject wfopText;
    [SerializeField] GameObject PositionText;
    TextMeshProUGUI posTextTMP;

    private int playerAmmount;

    int playersFinished;

    private void Awake()
    {
        if (instance != null)
        {
            Destroy(gameObject);
        }
        else
        {
            instance = this;
        }

        UpdatePlayerAmmount();
        posTextTMP = PositionText.GetComponent<TextMeshProUGUI>();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        PlayerMovement plMov = collision.gameObject.GetComponent<PlayerMovement>();

        plMov.RPC_WinScreen();
        
        if (plMov != null)
        {
            plMov.PlayerStopForever();
        }

        playersFinished++;
    }

    public void ShowPlayerPosition(int position)
    {

        UpdatePlayerAmmount();

        posTextTMP.text = "You came in " + position;

        switch (position) 
        {
            case 1:
                if (playerAmmount == 1) { posTextTMP.text = "You Made it, maybe try getting some friends next"; }
                posTextTMP.text = "You came in First";
                break;
            default:
                posTextTMP.text = "You came in " + position;
                break;
        }
    }

    private void UpdatePlayerAmmount() 
    {
        playerAmmount = NetworkManager.Instance.GetActivePlayersAmount();
    }

    public void TriggerWinScreen()
    {
        VictoryScreen.SetActive(true);
    }

    public void ShowEndResults()
    {
        if (playersFinished >= playerAmmount)
        {
            wfopText.SetActive(false);
            PositionText.SetActive(true);
            ShowPlayerPosition(1); //Aca poner la referencia a la posicion del player

            Debug.Log(playersFinished);
        }
    }
}
