using UnityEngine;

public class Bullet : MonoBehaviour
{
    // ==================================================
    // Bullet
    // ==================================================

    public float speed = 12f;

    public float lifeTime = 3f;

    public int damage = 1;


    // ==================================================
    // Ink
    // ==================================================

    [Header("Ink")]

    public float impactInkRadius = 0.55f;


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
            return;


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
        Collider2D other)
    {
        if (isDestroyed)
            return;


        // ==========================================
        // Boss
        //
        // GetComponentInParent를 사용하므로
        // 나중에 Boss Collider를
        // 자식 Object로 분리해도 작동한다.
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
            return;


        isDestroyed =
            true;


        if (InkMap.Instance != null)
        {
            // 마지막 이동 구간
            InkMap.Instance.PaintTrail(
                previousPosition,
                transform.position,
                InkTeam.Player
            );


            // 착탄 Ink
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