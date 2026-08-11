using System.Collections.Generic;
using UnityEngine;

public class SplashBomb : MonoBehaviour
{
    [Header("Ballistic Physics")]
    public float gravity = 18f;

    [Range(20f, 70f)]
    public float launchAngle = 45f;

    [Tooltip("높이에 따라 폭탄 Sprite가 커지는 정도")]
    public float visualScalePerHeight = 0.10f;

    private Vector3 visualBaseScale;


    [Header("Wall Bounce")]
    [Range(0f, 1f)]
    [Tooltip("벽 충돌 후 유지되는 수평 속도 비율")]
    public float wallBounceRetention = 0.72f;

    [Tooltip("이 속도보다 느려지면 수평 이동 정지")]
    public float minimumGroundSpeed = 0.5f;


    [Header("Landing")]
    [Tooltip("착지 후 폭발까지 대기 시간")]
    public float groundFuseTime = 0.55f;


    [Header("Explosion")]
    public float innerDamageRadius = 0.75f;
    public float outerDamageRadius = 1.6f;

    public int innerDamage = 12;
    public int outerDamage = 6;


    [Header("Ink")]
    public float inkRadius = 2.2f;
    public int inkSplatCount = 32;


    [Header("Collision")]
    public LayerMask obstacleLayer;


    [Header("Visual")]
    public Transform visual;


    private Rigidbody2D rb;

    // 실제 바닥 XY 방향의 속도
    private Vector2 groundVelocity;

    // 가상의 높이
    private float verticalHeight;

    // 가상의 수직 속도
    private float verticalVelocity;

    private bool isFlying = false;
    private bool hasLanded = false;
    private bool hasExploded = false;

    private float groundFuseTimer = 0f;


    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        if (visual != null)
        {
            visualBaseScale =
                visual.localScale;
        }
    }


    // ==================================================
    // 발사 속도 계산
    // Preview와 실제 폭탄이 동일한 계산을 사용
    // ==================================================

    public void CalculateLaunchVelocity(
        float targetRange,
        out float horizontalSpeed,
        out float verticalSpeed)
    {
        targetRange =
            Mathf.Max(targetRange, 0.01f);

        float angleRadians =
            launchAngle * Mathf.Deg2Rad;

        float sinDoubleAngle =
            Mathf.Sin(
                2f * angleRadians
            );

        sinDoubleAngle =
            Mathf.Max(
                sinDoubleAngle,
                0.01f
            );

        float launchSpeed =
            Mathf.Sqrt(
                targetRange
                * gravity
                / sinDoubleAngle
            );

        horizontalSpeed =
            launchSpeed
            * Mathf.Cos(angleRadians);

        verticalSpeed =
            launchSpeed
            * Mathf.Sin(angleRadians);
    }


    // ==================================================
    // Launch
    // ==================================================

    public void Launch(
        Vector2 direction,
        float targetRange)
    {
        if (direction.sqrMagnitude <
            0.001f)
        {
            direction =
                Vector2.right;
        }

        direction.Normalize();


        CalculateLaunchVelocity(
            targetRange,
            out float horizontalSpeed,
            out float startVerticalSpeed
        );


        groundVelocity =
            direction
            * horizontalSpeed;

        verticalVelocity =
            startVerticalSpeed;

        verticalHeight =
            0.01f;


        isFlying = true;
        hasLanded = false;
        hasExploded = false;

        groundFuseTimer =
            0f;


        rb.linearVelocity =
            groundVelocity;
    }


    // ==================================================
    // 비행
    // ==================================================

    private void FixedUpdate()
    {
        if (!isFlying)
            return;


        // 실제 Rigidbody는
        // 바닥의 XY 방향으로 이동
        rb.linearVelocity =
            groundVelocity;


        // 가상 높이에는 중력 적용
        verticalVelocity -=
            gravity
            * Time.fixedDeltaTime;


        verticalHeight +=
            verticalVelocity
            * Time.fixedDeltaTime;


        if (visual != null)
        {
            float height =
                Mathf.Max(
                    0f,
                    verticalHeight
                );

            float scaleMultiplier =
                1f +
                height * visualScalePerHeight;

            visual.localPosition =
                Vector3.zero;

            visual.localScale =
                visualBaseScale
                * scaleMultiplier;
        }


        // 다시 지면 높이에 도달하면 착지
        if (verticalHeight <= 0f &&
            verticalVelocity < 0f)
        {
            Land();
        }
    }


    // ==================================================
    // 착지 후 Fuse
    // ==================================================

    private void Update()
    {
        if (!hasLanded ||
            hasExploded)
        {
            return;
        }


        groundFuseTimer +=
            Time.deltaTime;


        if (groundFuseTimer >=
            groundFuseTime)
        {
            Explode();
        }
    }


    // ==================================================
    // 벽 반사
    // ==================================================

    private void OnCollisionEnter2D(
        Collision2D collision)
    {
        if (!isFlying ||
            hasExploded)
        {
            return;
        }


        // Obstacle Layer인지 확인
        int collisionLayer =
            collision.gameObject.layer;

        bool isObstacle =
            (
                obstacleLayer.value
                & (1 << collisionLayer)
            ) != 0;


        if (!isObstacle)
            return;


        if (collision.contactCount <= 0)
            return;


        Vector2 wallNormal =
            collision
                .GetContact(0)
                .normal;


        // 벽의 법선 방향을 기준으로 반사
        groundVelocity =
            Vector2.Reflect(
                groundVelocity,
                wallNormal
            );


        // 충돌하면서 에너지 일부 손실
        groundVelocity *=
            wallBounceRetention;


        if (groundVelocity.magnitude <
            minimumGroundSpeed)
        {
            groundVelocity =
                Vector2.zero;
        }


        rb.linearVelocity =
            groundVelocity;
    }


    // ==================================================
    // Landing
    // ==================================================

    private void Land()
    {
        if (hasLanded)
            return;


        isFlying = false;
        hasLanded = true;

        verticalHeight =
            0f;

        verticalVelocity =
            0f;

        groundVelocity =
            Vector2.zero;


        rb.linearVelocity =
            Vector2.zero;


        if (visual != null)
        {
            visual.localPosition =
                Vector3.zero;

            visual.localScale =
                visualBaseScale;
        }
    }


    // ==================================================
    // Explosion
    // ==================================================

    private void Explode()
    {

        CreateExplosionVFX();

        if (hasExploded)
            return;


        hasExploded = true;
        if (GameAudioManager.Instance != null)
        {
            GameAudioManager.Instance
                .PlaySplashBomb();
        }



        Collider2D[] hits =
            Physics2D.OverlapCircleAll(
                transform.position,
                outerDamageRadius
            );


        HashSet<EnemyHealth> damagedEnemies =
            new HashSet<EnemyHealth>();


        foreach (Collider2D hit in hits)
        {
            EnemyHealth enemy =
                hit.GetComponent<EnemyHealth>();


            if (enemy == null)
            {
                enemy =
                    hit.GetComponentInParent
                    <EnemyHealth>();
            }


            if (enemy == null)
                continue;


            // Collider가 여러 개인 Enemy의
            // 중복 Damage 방지
            if (!damagedEnemies.Add(enemy))
                continue;


            Vector2 bombPosition =
                transform.position;

            Vector2 enemyPosition =
                enemy.transform.position;

            Vector2 difference =
                enemyPosition
                - bombPosition;

            float distance =
                difference.magnitude;


            // 벽 뒤 적에게 폭발 피해가
            // 들어가는 것 방지
            if (distance > 0.001f)
            {
                RaycastHit2D wallHit =
                    Physics2D.Raycast(
                        bombPosition,
                        difference.normalized,
                        distance,
                        obstacleLayer
                    );


                if (wallHit.collider != null)
                {
                    continue;
                }
            }


            if (distance <=
                innerDamageRadius)
            {
                enemy.TakeDamage(
                    innerDamage
                );
            }
            else
            {
                enemy.TakeDamage(
                    outerDamage
                );
            }
        }


        // Ink 폭발
        if (InkMap.Instance != null)
        {
            InkMap.Instance.PaintExplosion(
                transform.position,
                inkRadius,
                InkTeam.Player,
                inkSplatCount
            );
        }


        Destroy(gameObject);
        // ==================================================
        // Boss Damage
        // ==================================================

        Collider2D[] bossHitColliders =
            Physics2D.OverlapCircleAll(
                transform.position,
                outerDamageRadius
            );


        HashSet<BossHealth> damagedBosses =
            new HashSet<BossHealth>();


        foreach (Collider2D hit
                 in bossHitColliders)
        {
            BossHealth boss =
                hit.GetComponentInParent<BossHealth>();


            if (boss == null)
                continue;


            // Boss가 Collider를 여러 개 가지고 있어도
            // 폭탄 1개당 한 번만 피해 적용
            if (damagedBosses.Contains(boss))
                continue;


            damagedBosses.Add(
                boss
            );


            Vector2 explosionPosition =
                transform.position;


            Vector2 bossPosition =
                boss.transform.position;


            float distance =
                Vector2.Distance(
                    explosionPosition,
                    bossPosition
                );


            // ==========================================
            // 폭발 범위 밖
            // ==========================================

            if (distance >
                outerDamageRadius)
            {
                continue;
            }


            // ==========================================
            // 벽 차단
            // ==========================================

            Vector2 toBoss =
                bossPosition
                - explosionPosition;


            if (distance > 0.001f)
            {
                RaycastHit2D wallHit =
                    Physics2D.Raycast(
                        explosionPosition,
                        toBoss.normalized,
                        distance,
                        obstacleLayer
                    );


                if (wallHit.collider != null)
                {
                    continue;
                }
            }


            // ==========================================
            // Inner / Outer Damage
            // ==========================================

            int bossDamage =
                distance <= innerDamageRadius
                    ? innerDamage
                    : outerDamage;


            boss.TakeDamage(
                bossDamage
            );
        }
    }

    // ==================================================
    // Explosion VFX
    // ==================================================

    private void CreateExplosionVFX()
    {
        GameObject effectObject =
            new GameObject(
                "Runtime_SplashBombExplosionVFX"
            );


        effectObject.transform.position =
            transform.position;


        SplashBombExplosionVFX effect =
            effectObject.AddComponent<
                SplashBombExplosionVFX
            >();


        SpriteRenderer renderer =
            GetComponentInChildren<
                SpriteRenderer
            >();


        Material material =
            renderer != null
                ? renderer.sharedMaterial
                : null;


        // 시각적 외곽 크기는
        // 실제 Outer Damage Radius 기준
        effect.Initialize(
            outerDamageRadius,
            material
        );
    }


    // ==================================================
    // Editor 확인용
    // ==================================================

    private void OnDrawGizmosSelected()
    {
        Gizmos.DrawWireSphere(
            transform.position,
            innerDamageRadius
        );

        Gizmos.DrawWireSphere(
            transform.position,
            outerDamageRadius
        );
    }
}