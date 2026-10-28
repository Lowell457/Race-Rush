using UnityEngine;

public class Spikes : MonoBehaviour
{
    [SerializeField] private float stunDuration;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        PlayerMovement mov = collision.GetComponent<PlayerMovement>();

        if (mov != null)
        {
            mov.PushBack(stunDuration);
        }
    }
}
