using UnityEngine;

// Makes a regular Enemy pushable through the shared knockback contract.
// Movement scripts keep full control except during the short knockback
// window, which they skip by checking IsKnockbackActive.
[RequireComponent(typeof(Rigidbody2D))]
public class EnemyKnockbackReceiver
    : MonoBehaviour,
      IKnockbackReceiver
{
    [Header("Knockback")]

    [Tooltip("How long the AI yields control after being hit.")]
    [Min(0.01f)]
    [SerializeField]
    private float knockbackDuration = 0.18f;


    [Tooltip("Hard cap so a heavy hit cannot fling an enemy across "
        + "the Map.")]
    [Min(0.1f)]
    [SerializeField]
    private float maxKnockbackSpeed = 18f;


    private Rigidbody2D body;

    private float knockbackTimer;


    // Read by every Enemy movement script before it writes velocity.
    public bool IsKnockbackActive =>
        knockbackTimer > 0f;


    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
    }


    public void ApplyKnockback(
        Vector2 sourcePosition,
        float force)
    {
        if (body == null || force <= 0f)
        {
            return;
        }


        Vector2 direction =
            (Vector2)transform.position
            - sourcePosition;


        if (direction.sqrMagnitude <= 0.0001f)
        {
            direction = Vector2.up;
        }


        // Velocity is set directly rather than added: the AI overwrites
        // velocity every FixedUpdate, so an impulse would be erased the
        // moment the window ends anyway.
        body.linearVelocity =
            Vector2.ClampMagnitude(
                direction.normalized * force,
                maxKnockbackSpeed
            );


        knockbackTimer = knockbackDuration;
    }


    private void FixedUpdate()
    {
        if (knockbackTimer <= 0f)
        {
            return;
        }


        knockbackTimer -= Time.fixedDeltaTime;


        if (knockbackTimer > 0f)
        {
            return;
        }


        knockbackTimer = 0f;


        // Hand a clean slate back to the AI.
        if (body != null)
        {
            body.linearVelocity = Vector2.zero;
        }
    }


    private void OnDisable()
    {
        knockbackTimer = 0f;
    }
}
