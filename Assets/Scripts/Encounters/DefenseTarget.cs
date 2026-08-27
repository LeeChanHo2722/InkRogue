using System;
using UnityEngine;

// The thing a Defense Floor protects. It owns its own health, and it is
// never destroyed as a GameObject so the same Floor can be retried with
// ResetForFloor(). It is a movable object: attacks push it and the Player
// can shove it, so keeping it in a good spot is the Defense gameplay.
[RequireComponent(typeof(Rigidbody2D))]
public class DefenseTarget : MonoBehaviour,
    IEncounterDamageTarget,
    IKnockbackReceiver
{
    [Header("Health")]

    [Min(1f)]
    [SerializeField]
    private float maxHealth = 100f;

    [Header("Knockback")]

    [Min(0f)]
    [SerializeField]
    private float knockbackMultiplier = 1f;

    [Tooltip("Hard cap so stacked explosions cannot fling the Target.")]
    [Min(0.1f)]
    [SerializeField]
    private float maxSpeed = 8f;

    private Rigidbody2D body;

    private Vector2 initialPosition;

    private float currentHealth;

    private bool isDestroyed;

    public event Action Destroyed;

    public float MaxHealth => maxHealth;

    public float CurrentHealth => currentHealth;

    public bool IsDestroyed => isDestroyed;

    public Transform TargetTransform => transform;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        initialPosition = transform.position;

        ResetForFloor();
    }

    private void FixedUpdate()
    {
        if (body == null)
        {
            return;
        }

        Vector2 velocity = body.linearVelocity;

        if (velocity.sqrMagnitude > maxSpeed * maxSpeed)
        {
            body.linearVelocity =
                velocity.normalized * maxSpeed;
        }
    }

    // Restores health AND physics state, so a retry never starts from
    // wherever the previous attempt left the Target.
    public void ResetForFloor()
    {
        currentHealth = maxHealth;
        isDestroyed = false;

        if (body != null)
        {
            body.linearVelocity = Vector2.zero;
            body.angularVelocity = 0f;
            body.position = initialPosition;
        }

        transform.position = initialPosition;
    }

    public void TakeDamage(
        float damage,
        Vector2 hitSourcePosition)
    {
        if (isDestroyed)
        {
            return;
        }

        if (damage <= 0f)
        {
            return;
        }

        currentHealth = Mathf.Max(
            0f,
            currentHealth - damage);

        if (currentHealth > 0f)
        {
            return;
        }

        isDestroyed = true;
        Destroyed?.Invoke();
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
            (Vector2)transform.position - sourcePosition;

        if (direction.sqrMagnitude <= 0.0001f)
        {
            direction = Vector2.up;
        }

        body.AddForce(
            direction.normalized
                * force
                * knockbackMultiplier,
            ForceMode2D.Impulse);
    }
}
