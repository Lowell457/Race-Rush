using Fusion;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class MovingPlatform : NetworkBehaviour
{
    [SerializeField] Vector2 offset = new Vector2(5f, 0f);
    [SerializeField] float travelTime = 2f;
    [SerializeField] float waitTime = 0.5f;
    [Range(0f, 1f)][SerializeField] float phase = 0f;
    [SerializeField] bool smooth = true;

    Rigidbody2D rb;
    Vector2 startPos;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        startPos = rb.position;
    }

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority) return;
        rb.MovePosition(Evaluate(SimTime));
    }

    // Se calcula al vuelo para el tick actual: no depende del orden en que Fusion ejecute los FixedUpdateNetwork
    public Vector2 Velocity
    {
        get
        {
            if (Runner == null) return Vector2.zero;
            float t = SimTime;
            return (Evaluate(t) - Evaluate(t - Runner.DeltaTime)) / Runner.DeltaTime;
        }
    }

    float SimTime => (int)Runner.Tick * Runner.DeltaTime;

    Vector2 Evaluate(float time)
    {
        float cycle = 2f * (travelTime + waitTime);
        float t = Mathf.Repeat(time + phase * cycle, cycle);

        float k;
        if (t < travelTime) k = t / travelTime;
        else if (t < travelTime + waitTime) k = 1f;
        else if (t < 2f * travelTime + waitTime) k = 1f - (t - travelTime - waitTime) / travelTime;
        else k = 0f;

        if (smooth) k = Mathf.SmoothStep(0f, 1f, k);
        return startPos + offset * k;
    }

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        Vector2 a = Application.isPlaying ? startPos : (Vector2)transform.position;
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(a, a + offset);
        Gizmos.DrawWireSphere(a + offset, 0.2f);
    }
#endif
}