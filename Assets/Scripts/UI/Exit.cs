
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class Exit : MonoBehaviour
{
    bool isQuiting;
    float timer;
    [SerializeField] float exitTime = 3f;
    float signalTimer;
    [SerializeField] GameObject exitWarning;
    TextMeshProUGUI exitingText;

    private void Awake()
    {
        exitingText = exitWarning.GetComponent<TextMeshProUGUI>();
        signalTimer = exitTime / 3;
    }
    void Update()
    {
        if (isQuiting)
        {
            timer += Time.deltaTime;
            exitWarning.SetActive(true);
            if (timer >= signalTimer)
            {
                exitingText.text = exitingText.text+".";
                signalTimer += exitTime / 3;
            }
        }
        else 
        {
            timer = 0;
            exitWarning.SetActive(false);
            exitingText.text = "Quiting";
            signalTimer = exitTime / 5;
        }
        
        if (timer >= exitTime)
        {
            NetworkManager.Instance.Leave();
        }
    }

    public void PlayerExitKey(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            isQuiting = true;
        }
        if (context.canceled)
        {
            isQuiting = false;
        }
    }
}
