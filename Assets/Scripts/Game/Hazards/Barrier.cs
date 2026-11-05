using Fusion;
using UnityEngine;

public class Barrier : NetworkBehaviour
{
    [SerializeField] private float speedMultiplierPercent;
    [SerializeField] private float boostDuration;
    [SerializeField] private float respawnTime;
    //private Timer timer;

    [SerializeField] private BoxCollider2D col;
    [SerializeField] private SpriteRenderer sprite;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!HasStateAuthority)
            return;

        PlayerMovement mov = collision.GetComponent<PlayerMovement>();

        if (mov != null)
        {
            mov.ChangeSpeedMultiplier(speedMultiplierPercent, boostDuration);

            //Runner.Despawn(Object);
            col.enabled = false;
            sprite.enabled = false;

            Invoke("ResetBarrier", respawnTime);
        }
    }

    private void ResetBarrier()
    {
        if (!HasStateAuthority)
            return;

        col.enabled = true;
        sprite.enabled = true;
    }
}
