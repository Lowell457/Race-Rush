using UnityEngine;

public class KillBox : MonoBehaviour
{
    [SerializeField] private LayerMask playerLayer;
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if ((playerLayer.value & (1 << collision.gameObject.layer)) != 0)
        {
            PlayerMovement player = collision.gameObject.GetComponent<PlayerMovement>();

            if (player != null)
                player.KillPlayer();
        }
    }
}
