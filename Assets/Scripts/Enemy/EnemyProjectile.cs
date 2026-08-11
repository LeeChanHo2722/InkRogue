using UnityEngine;

public class EnemyProjectile : MonoBehaviour
{
    public float speed = 6f;
    public int damage = 1;
    public float lifeTime = 4f;


    [Header("Ink")]
    public float impactInkRadius = 0.55f;


    private Rigidbody2D rb;

    private Vector2 previousPosition;

    private bool isDestroyed = false;


    private void Awake()
    {
        rb =
            GetComponent<Rigidbody2D>();
    }


    private void Start()
    {
        previousPosition =
            transform.position;


        Destroy(
            gameObject,
            lifeTime
        );
    }


    public void SetDirection(
        Vector2 direction)
    {
        rb.linearVelocity =
            direction.normalized
            * speed;
    }


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
                InkTeam.Enemy
            );
        }


        previousPosition =
            currentPosition;
    }


    private void OnTriggerEnter2D(
        Collider2D other)
    {
        if (isDestroyed)
            return;


        PlayerShield playerShield =
            other.GetComponent<PlayerShield>();

        if (playerShield != null)
        {
            playerShield.TakeDamage(
                damage,
                transform.position
            );

            FinishProjectile();

            return;
        }


        if (other.CompareTag(
                "Obstacle"))
        {
            FinishProjectile();
        }
    }


    private void FinishProjectile()
    {
        if (isDestroyed)
            return;


        isDestroyed =
            true;


        if (InkMap.Instance != null)
        {
            InkMap.Instance.PaintTrail(
                previousPosition,
                transform.position,
                InkTeam.Enemy
            );


            InkMap.Instance.PaintCircle(
                transform.position,
                impactInkRadius,
                InkTeam.Enemy
            );
        }


        Destroy(gameObject);
    }
}