using Fusion;
using UnityEngine;

public class Barrier : NetworkBehaviour
{
    [SerializeField] private float speedMultiplierPercent;
    [SerializeField] private float boostDuration;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        PlayerMovement mov = collision.GetComponent<PlayerMovement>();

        if (mov != null)
        {
            mov.ChangeSpeedMultiplier(speedMultiplierPercent, boostDuration);

            Runner.Despawn(Object);
        }
    }
}
