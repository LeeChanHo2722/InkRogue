using System.Collections.Generic;
using UnityEngine;

public class SplashBomb : MonoBehaviour
{
    // ==================================================
    // Ballistic Physics
    // ==================================================

    [Header("Ballistic Physics")]

    public float gravity = 18f;


    [Range(20f, 70f)]
    public float launchAngle = 45f;


    [Tooltip("높이에 따라 폭탄 Sprite가 커지는 정도")]
    public float visualScalePerHeight =
        0.10f;


    private Vector3 visualBaseScale;


    // ==================================================
    // Wall Bounce
    // ==================================================

    [Header("Wall Bounce")]

    [Range(0f, 1f)]
    [Tooltip("벽 충돌 후 유지되는 수평 속도 비율")]
    public float wallBounceRetention =
        0.72f;


    [Tooltip("이 속도보다 느려지면 수평 이동 정지")]
    public float minimumGroundSpeed =
        0.5f;


    // ==================================================
    // Landing
    // ==================================================

    [Header("Landing")]

    [Tooltip("착지 후 폭발까지 대기 시간")]
    public float groundFuseTime =
        0.55f;


    // ==================================================
    // Explosion
    // ==================================================

    [Header("Explosion")]

    public float innerDamageRadius =
        0.75f;


    public float outerDamageRadius =
        1.6f;


    public float innerDamage =
        12f;


    public float outerDamage =
        6f;


    // ==================================================
    // Ink
    // ==================================================

    [Header("Ink")]

    public float inkRadius =
        2.2f;


    public int inkSplatCount =
        32;


    // ==================================================
    // Collision
    // ==================================================

    [Header("Collision")]

    public LayerMask obstacleLayer;


    // ==================================================
    // Visual
    // ==================================================

    [Header("Visual")]

    public Transform visual;


    // ==================================================
    // Runtime Damage
    //
    // 어느 Slot에서 던졌는지에 따른
    // Damage 배율.
    //
    // Right = 1.0
    // Left  = 0.8
    // ==================================================

    private float damageMultiplier =
        1f;


    // ==================================================
    // Physics Runtime
    // ==================================================

    private Rigidbody2D rb;


    // 실제 바닥 XY 방향 속도
    private Vector2 groundVelocity;


    // 가상의 높이
    private float verticalHeight;


    // 가상의 수직 속도
    private float verticalVelocity;


    private bool isFlying =
        false;


    private bool hasLanded =
        false;


    private bool hasExploded =
        false;


    private float groundFuseTimer =
        0f;


    // ==================================================
    // Awake
    // ==================================================

    private void Awake()
    {
        rb =
            GetComponent<Rigidbody2D>();


        if (visual != null)
        {
            visualBaseScale =
                visual.localScale;
        }
    }


    // ==================================================
    // 발사 속도 계산
    //
    // Preview와 실제 폭탄이
    // 완전히 동일한 계산 사용
    // ==================================================

    public void CalculateLaunchVelocity(
        float targetRange,
        out float horizontalSpeed,
        out float verticalSpeed
    )
    {
        targetRange =
            Mathf.Max(
                targetRange,
                0.01f
            );


        float angleRadians =
            launchAngle
            *
            Mathf.Deg2Rad;


        float sinDoubleAngle =
            Mathf.Sin(
                2f
                *
                angleRadians
            );


        sinDoubleAngle =
            Mathf.Max(
                sinDoubleAngle,
                0.01f
            );


        float launchSpeed =
            Mathf.Sqrt(
                targetRange
                *
                gravity
                /
                sinDoubleAngle
            );


        horizontalSpeed =
            launchSpeed
            *
            Mathf.Cos(
                angleRadians
            );


        verticalSpeed =
            launchSpeed
            *
            Mathf.Sin(
                angleRadians
            );
    }


    // ==================================================
    // Launch
    //
    // 신규 Weapon Slot 버전
    // ==================================================

    public void Launch(
        Vector2 direction,
        float targetRange,
        float slotDamageMultiplier
    )
    {
        damageMultiplier =
            Mathf.Max(
                0f,
                slotDamageMultiplier
            );


        LaunchInternal(
            direction,
            targetRange
        );
    }


    // ==================================================
    // Legacy Launch
    //
    // 다른 기존 코드가 아직
    // Launch(direction, range)를 호출하더라도
    // 깨지지 않도록 유지.
    // ==================================================

    public void Launch(
        Vector2 direction,
        float targetRange
    )
    {
        damageMultiplier =
            1f;


        LaunchInternal(
            direction,
            targetRange
        );
    }


    // ==================================================
    // Launch Internal
    // ==================================================

    private void LaunchInternal(
        Vector2 direction,
        float targetRange
    )
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
            *
            horizontalSpeed;


        verticalVelocity =
            startVerticalSpeed;


        verticalHeight =
            0.01f;


        isFlying =
            true;


        hasLanded =
            false;


        hasExploded =
            false;


        groundFuseTimer =
            0f;


        if (rb != null)
        {
            rb.linearVelocity =
                groundVelocity;
        }
    }


    // ==================================================
    // Fixed Update
    // ==================================================

    private void FixedUpdate()
    {
        if (!isFlying)
        {
            return;
        }


        // ==========================================
        // 실제 Rigidbody는 바닥 XY 이동
        // ==========================================

        if (rb != null)
        {
            rb.linearVelocity =
                groundVelocity;
        }


        // ==========================================
        // 가상 높이 중력
        // ==========================================

        verticalVelocity -=
            gravity
            *
            Time.fixedDeltaTime;


        verticalHeight +=
            verticalVelocity
            *
            Time.fixedDeltaTime;


        // ==========================================
        // Visual Height
        // ==========================================

        if (visual != null)
        {
            float height =
                Mathf.Max(
                    0f,
                    verticalHeight
                );


            float scaleMultiplier =
                1f
                +
                height
                *
                visualScalePerHeight;


            visual.localPosition =
                Vector3.zero;


            visual.localScale =
                visualBaseScale
                *
                scaleMultiplier;
        }


        // ==========================================
        // Ground Landing
        // ==========================================

        if (verticalHeight <= 0f &&
            verticalVelocity < 0f)
        {
            Land();
        }
    }


    // ==================================================
    // Update
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
    // Wall Bounce
    // ==================================================

    private void OnCollisionEnter2D(
        Collision2D collision
    )
    {
        if (!isFlying ||
            hasExploded)
        {
            return;
        }


        int collisionLayer =
            collision.gameObject.layer;


        bool isObstacle =
            (
                obstacleLayer.value
                &
                (1 << collisionLayer)
            )
            != 0;


        if (!isObstacle)
        {
            return;
        }


        if (collision.contactCount <= 0)
        {
            return;
        }


        Vector2 wallNormal =
            collision
                .GetContact(0)
                .normal;


        groundVelocity =
            Vector2.Reflect(
                groundVelocity,
                wallNormal
            );


        groundVelocity *=
            wallBounceRetention;


        if (groundVelocity.magnitude <
            minimumGroundSpeed)
        {
            groundVelocity =
                Vector2.zero;
        }


        if (rb != null)
        {
            rb.linearVelocity =
                groundVelocity;
        }
    }


    // ==================================================
    // Landing
    // ==================================================

    private void Land()
    {
        if (hasLanded)
        {
            return;
        }


        isFlying =
            false;


        hasLanded =
            true;


        verticalHeight =
            0f;


        verticalVelocity =
            0f;


        groundVelocity =
            Vector2.zero;


        if (rb != null)
        {
            rb.linearVelocity =
                Vector2.zero;
        }


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
        if (hasExploded)
        {
            return;
        }


        hasExploded =
            true;


        // ==========================================
        // VFX
        // ==========================================

        CreateExplosionVFX();


        // ==========================================
        // SFX
        // ==========================================

        if (GameAudioManager.Instance != null)
        {
            GameAudioManager.Instance
                .PlaySplashBomb();
        }


        // ==========================================
        // Normal Enemy Damage
        // ==========================================

        DamageEnemies();


        // ==========================================
        // Boss Damage
        // ==========================================

        DamageBosses();


        // ==========================================
        // Ink Explosion
        // ==========================================

        if (InkMap.Instance != null)
        {
            InkMap.Instance.PaintExplosion(
                transform.position,
                inkRadius,
                InkTeam.Player,
                inkSplatCount
            );
        }


        // 모든 폭발 처리가 끝난 후 삭제
        Destroy(
            gameObject
        );
    }


    // ==================================================
    // Enemy Damage
    // ==================================================

    private void DamageEnemies()
    {
        Collider2D[] hits =
            Physics2D.OverlapCircleAll(
                transform.position,
                outerDamageRadius
            );


        HashSet<EnemyHealth>
            damagedEnemies =
            new HashSet<EnemyHealth>();


        foreach (Collider2D hit in hits)
        {
            EnemyHealth enemy =
                hit.GetComponent<
                    EnemyHealth
                >();


            if (enemy == null)
            {
                enemy =
                    hit.GetComponentInParent<
                        EnemyHealth
                    >();
            }


            if (enemy == null)
            {
                continue;
            }


            // Collider 중복 Damage 방지
            if (!damagedEnemies.Add(
                    enemy
                ))
            {
                continue;
            }


            Vector2 bombPosition =
                transform.position;


            Vector2 enemyPosition =
                enemy.transform.position;


            Vector2 difference =
                enemyPosition
                -
                bombPosition;


            float distance =
                difference.magnitude;


            // ==========================================
            // Wall Blocking
            // ==========================================

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


            float baseDamage =
                distance <=
                    innerDamageRadius
                    ? innerDamage
                    : outerDamage;


            float finalDamage =
                CalculateFinalDamage(
                    baseDamage
                );


            enemy.TakeDamage(
                finalDamage
            );
        }
    }


    // ==================================================
    // Boss Damage
    // ==================================================

    private void DamageBosses()
    {
        Collider2D[] bossHitColliders =
            Physics2D.OverlapCircleAll(
                transform.position,
                outerDamageRadius
            );


        HashSet<BossHealth>
            damagedBosses =
            new HashSet<BossHealth>();


        foreach (
            Collider2D hit
            in bossHitColliders
        )
        {
            BossHealth boss =
                hit.GetComponentInParent<
                    BossHealth
                >();


            if (boss == null)
            {
                continue;
            }


            if (!damagedBosses.Add(
                    boss
                ))
            {
                continue;
            }


            Vector2 explosionPosition =
                transform.position;


            Vector2 bossPosition =
                boss.transform.position;


            float distance =
                Vector2.Distance(
                    explosionPosition,
                    bossPosition
                );


            if (distance >
                outerDamageRadius)
            {
                continue;
            }


            // ==========================================
            // Wall Blocking
            // ==========================================

            Vector2 toBoss =
                bossPosition
                -
                explosionPosition;


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


            float baseDamage =
                distance <=
                    innerDamageRadius
                    ? innerDamage
                    : outerDamage;


            float finalDamage =
                CalculateFinalDamage(
                    baseDamage
                );


            boss.TakeDamage(
                finalDamage
            );
        }
    }


    // ==================================================
    // Damage Calculation
    // ==================================================

    private float CalculateFinalDamage(
    float baseDamage
)
    {
        return Mathf.Max(
            0f,
            baseDamage
            *
            damageMultiplier
        );
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


        effect.Initialize(
            outerDamageRadius,
            material
        );
    }


    // ==================================================
    // Editor
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