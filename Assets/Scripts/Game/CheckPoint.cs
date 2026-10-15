using UnityEngine;

public class CheckPoint : MonoBehaviour
{
    [SerializeField] private LayerMask playerLayer;
    [SerializeField] private int priority;


    private void OnTriggerEnter2D(Collider2D collision)
    {
        if ((playerLayer.value & (1 << collision.gameObject.layer)) != 0)
        {
            PlayerMovement player = collision.gameObject.GetComponent<PlayerMovement>();

            if (player != null)
                player.SaveCheckPoint(transform, priority);
        }
    }
}
