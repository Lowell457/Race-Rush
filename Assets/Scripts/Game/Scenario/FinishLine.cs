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

    [Networked]
    public int playersFinished { get; set; }

    private string[] finishList;

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
        finishList = new string[playerAmmount];
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!HasStateAuthority)
            return;

        PlayerMovement plMov = collision.gameObject.GetComponent<PlayerMovement>();

        plMov.RPC_WinScreen();
        
        if (plMov != null)
        {
            plMov.PlayerStopForever();
        }

        playersFinished++;
        
        for (int i = 0; i > finishList.Length; i++)
        {
            if (finishList[i] == default)
            {
                finishList[i] = plMov.GetPlayerName();
                break;
            }
        }

        if (playersFinished >= playerAmmount)
            RPC_ShowEndResults(finishList);
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

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]    
    public void RPC_ShowEndResults(string[] positions)
    {
        Debug.Log("META");

        foreach (string position in positions)
        {
            Debug.Log(position);
        }

        wfopText.SetActive(false);
        PositionText.SetActive(true);

        int myPos = 0;
        for (int i = 0; i < positions.Length; i++)
        {
            if (positions[i].Equals(NetworkManager.Instance.LocalNickname))
            {
                myPos = i + 1;
            }
        }
        
        ShowPlayerPosition(myPos);
    }
}
