using UnityEngine;

public class BossProjectile : MonoBehaviour
{
    // ==================================================
    // Projectile
    // ==================================================

    [Header("Projectile")]

    public float lifeTime = 4f;

    public float impactInkRadius = 0.32f;


    // ==================================================
    // Runtime
    // ==================================================

    private Rigidbody2D rb;

    private float damage = 1f;

    private bool finished = false;


    // ==================================================
    // Awake
    // ==================================================

    private void Awake()
    {
        rb =
            GetComponent<Rigidbody2D>();
    }


    // ==================================================
    // Initialize
    // ==================================================

    public void Initialize(
        Vector2 direction,
        float speed,
        float projectileDamage)
    {
        damage =
            projectileDamage;


        if (rb != null)
        {
            rb.linearVelocity =
                direction.normalized
                * speed;
        }


        Destroy(
            gameObject,
            lifeTime
        );
    }


    // ==================================================
    // Collision
    // ==================================================

    private void OnTriggerEnter2D(
        Collider2D other)
    {
        if (finished)
            return;


        // ==========================================
        // Player
        // ==========================================

        PlayerShield playerShield =
            other.GetComponentInParent<
                PlayerShield
            >();


        if (playerShield != null)
        {
            playerShield.TakeDamage(
                damage,
                transform.position
            );


            Finish(
                true
            );


            return;
        }


        // ==========================================
        // Wall
        // ==========================================

        if (other.CompareTag(
                "Obstacle"))
        {
            Finish(
                true
            );
        }
    }


    // ==================================================
    // Finish
    // ==================================================

    private void Finish(
        bool paintInk)
    {
        if (finished)
            return;


        finished =
            true;


        if (paintInk &&
            InkMap.Instance != null)
        {
            InkMap.Instance.PaintCircle(
                transform.position,
                impactInkRadius,
                InkTeam.Enemy
            );
        }


        Destroy(
            gameObject
        );
    }
}