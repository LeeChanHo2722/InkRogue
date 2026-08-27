using UnityEngine;

public class Bullet : MonoBehaviour
{
    // ==================================================
    // Bullet
    // ==================================================

    public float speed = 12f;

    public float lifeTime = 3f;

    // int ¡æ float
    public float damage = 1f;


    // ==================================================
    // Ink
    // ==================================================

    [Header("Ink")]

    public float impactInkRadius = 0.55f;


    // ==================================================
    // Knockback
    // ==================================================

    [Header("Knockback")]

    [Min(0f)]
    public float knockbackForce = 2f;


    // ==================================================
    // Runtime
    // ==================================================

    private Rigidbody2D rb;

    private Vector2 previousPosition;

    private bool isDestroyed = false;


    // ==================================================
    // Awake
    // ==================================================

    private void Awake()
    {
        rb =
            GetComponent<Rigidbody2D>();
    }


    // ==================================================
    // Start
    // ==================================================

    private void Start()
    {
        previousPosition =
            transform.position;


        rb.linearVelocity =
            transform.right
            * speed;


        Invoke(
            nameof(Expire),
            lifeTime
        );
    }


    // ==================================================
    // Update
    // ==================================================

    private void Update()
    {
        if (isDestroyed)
        {
            return;
        }


        Vector2 currentPosition =
            transform.position;


        if (InkMap.Instance != null)
        {
            InkMap.Instance.PaintTrail(
                previousPosition,
                currentPosition,
                InkTeam.Player
            );
        }


        previousPosition =
            currentPosition;
    }


    // ==================================================
    // Collision
    // ==================================================

    private void OnTriggerEnter2D(
        Collider2D other
    )
    {
        if (isDestroyed)
        {
            return;
        }


        // ==========================================
        // Boss
        // ==========================================

        BossHealth boss =
            other.GetComponentInParent<
                BossHealth
            >();


        if (boss != null)
        {
            boss.TakeDamage(
                damage
            );


            FinishBullet();


            return;
        }


        // ==========================================
        // Normal Enemy
        // ==========================================

        EnemyHealth enemy =
            other.GetComponent<
                EnemyHealth
            >();


        if (enemy != null)
        {
            enemy.TakeDamage(
                damage
            );


            FinishBullet();


            return;
        }


        // ==========================================
        // Push-only target (Defense Target)
        //
        // Player fire never damages it, it only shoves it.
        // ==========================================

        if (KnockbackUtility.TryApply(
                other,
                transform.position,
                knockbackForce))
        {
            FinishBullet();


            return;
        }


        // ==========================================
        // Wall
        // ==========================================

        if (other.CompareTag(
                "Obstacle"))
        {
            FinishBullet();
        }
    }


    // ==================================================
    // Expire
    // ==================================================

    private void Expire()
    {
        if (!isDestroyed)
        {
            FinishBullet();
        }
    }


    // ==================================================
    // Finish
    // ==================================================

    private void FinishBullet()
    {
        if (isDestroyed)
        {
            return;
        }


        isDestroyed =
            true;


        if (InkMap.Instance != null)
        {
            // ¸¶Áö¸· ÀÌµ¿ ±¸°£
            InkMap.Instance.PaintTrail(
                previousPosition,
                transform.position,
                InkTeam.Player
            );


            // ÂøÅº Ink
            InkMap.Instance.PaintCircle(
                transform.position,
                impactInkRadius,
                InkTeam.Player
            );
        }


        Destroy(
            gameObject
        );
    }
}