
using UnityEngine;
using UnityEngine.InputSystem;

public class Exit : MonoBehaviour
{
    bool isQuiting;
    float timer;
    [SerializeField] float exitTime = 3f;
    void Update()
    {
        if (isQuiting)
        {
            timer += Time.deltaTime;
        }
        else { timer = 0; }
        if (timer > exitTime)
        {
            //salidar
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
