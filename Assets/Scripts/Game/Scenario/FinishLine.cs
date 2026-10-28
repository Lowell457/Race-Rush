using UnityEngine;
using Fusion;
using System.Linq;
using TMPro;
public class FinishLine : MonoBehaviour
{

    [SerializeField] GameObject VictoryScreen;
    [SerializeField] GameObject wfopText;
    [SerializeField] GameObject PositionText;
    TextMeshProUGUI posTextTMP;

    private int playerAmmount;
    

    int playersFinished;

    private void Awake()
    {
        UpdatePlayerAmmount();
        posTextTMP = PositionText.GetComponent<TextMeshProUGUI>();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        collision.gameObject.GetComponent<PlayerMovement>().enabled = !collision.gameObject.GetComponent<PlayerMovement>().enabled;

        if (collision.transform == NetworkManager.Instance.selfTransform)
        {
            VictoryScreen.SetActive(true);
        }

        playersFinished++;

        if (playersFinished >= playerAmmount) 
        {
            wfopText.SetActive(false);
            PositionText.SetActive(true);
            ShowPlayerPosition(1); //Aca poner la referencia a la posicion del player
        }

        Debug.Log(playersFinished);
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



}
