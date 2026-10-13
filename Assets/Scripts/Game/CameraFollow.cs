using Unity.VisualScripting;
using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    private Transform targetFollow;
    private Rigidbody2D targetRb;
    [SerializeField] private float speed;
    [SerializeField] private float rbVelocityModifier;

    private void Start()
    {
        targetFollow = NetworkManager.Instance.selfTransform;
    }

    private void Update()
    {
        if (targetFollow != null)
        {
            Vector2 futureTargetPos = targetRb.linearVelocity * rbVelocityModifier * Time.deltaTime;
            Vector2 pos = Vector2.Lerp(transform.position, targetFollow.position, speed * Time.deltaTime) + futureTargetPos;
            transform.position = pos;
        }
        else GetInfo();
    }

    private void GetInfo()
    {
        targetFollow = NetworkManager.Instance.selfTransform;
        targetRb = NetworkManager.Instance.selfRb;
    }
}
