using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BossAttackController : MonoBehaviour
{
    // ==================================================
    // References
    // ==================================================

    [Header("References")]

    public GameObject projectilePrefab;

    public GameObject bombDropPrefab;

    public Transform firePoint;

    public SpriteRenderer referenceRenderer;


    // ==================================================
    // Attack Cycle
    // ==================================================

    [Header("Attack Cycle")]

    public float phase1AttackGap = 1.25f;

    public float phase2AttackGap = 1.00f;

    public float phase3AttackGap = 0.75f;


    // ==================================================
    // Bomb
    // ==================================================

    [Header("Bomb")]

    public float bombWarningDuration = 0.85f;

    public float bombDamage = 2f;

    public float bombDamageRadius = 1.25f;

    public float bombInkRadius = 1.80f;

    public int bombInkSplatCount = 24;


    [Tooltip("Player로부터 최소 랜덤 거리")]
    public float bombMinDistance = 0.45f;

    [Tooltip("Player로부터 최대 랜덤 거리")]
    public float bombMaxDistance = 2.4f;

    public float bombSpawnStagger = 0.12f;

    [Tooltip("Bomb끼리 너무 겹치지 않도록 하는 거리")]
    public float bombMinSeparation = 0.85f;

    public LayerMask obstacleLayer;


    // ==================================================
    // Shoot
    // ==================================================

    [Header("Shoot")]

    [Tooltip("실제 연사 전에 조준선만 보여주는 시간")]
    public float shootWarmup = 0.35f;


    [Tooltip("Player를 따라가며 연사하는 시간")]
    public float trackingShootDuration = 1.0f;


    [Tooltip(
        "Player 추적 종료 후 "
        + "마지막 회전 방향으로 계속 쏘는 시간"
    )]
    public float sweepShootDuration = 2.0f;


    public float shootFireRate = 4.5f;

    public float projectileSpeed = 8.5f;

    public float projectileDamage = 1f;


    [Tooltip(
        "Phase 2의 가운데 탄 기준 좌우 Spread"
    )]
    public float phase2SpreadAngle = 15f;


    [Tooltip(
        "추적 종료 후 선풍기 회전속도"
    )]
    public float sweepDegreesPerSecond = 85f;


    // ==================================================
    // Aim Line
    // ==================================================

    [Header("Aim Telegraph")]

    public float aimLineLength = 7f;

    public float aimLineWidth = 0.045f;

    public int aimSortingOffset = 4;


    // ==================================================
    // Runtime
    // ==================================================

    private BossHealth bossHealth;

    private Transform player;

    private LineRenderer aimLine;


    private bool combatActive = false;

    private Coroutine combatRoutine;


    // ==================================================
    // Awake
    // ==================================================

    private void Awake()
    {
        bossHealth =
            GetComponent<BossHealth>();


        GameObject playerObject =
            GameObject.FindGameObjectWithTag(
                "Player"
            );


        if (playerObject != null)
        {
            player =
                playerObject.transform;
        }


        if (firePoint == null)
        {
            firePoint =
                transform;
        }


        if (referenceRenderer == null)
        {
            referenceRenderer =
                GetComponentInChildren<
                    SpriteRenderer
                >();
        }


        CreateAimLine();


        if (bossHealth != null)
        {
            bossHealth.BossDied +=
                OnBossDied;
        }
    }


    // ==================================================
    // Combat
    // ==================================================

    public void BeginCombat()
    {
        if (combatActive)
            return;


        combatActive =
            true;


        combatRoutine =
            StartCoroutine(
                CombatRoutine()
            );
    }


    public void StopCombat()
    {
        combatActive =
            false;


        if (combatRoutine != null)
        {
            StopCoroutine(
                combatRoutine
            );


            combatRoutine =
                null;
        }


        StopAllCoroutines();


        HideAimLine();
    }


    // ==================================================
    // Main Pattern
    // ==================================================

    private IEnumerator CombatRoutine()
    {
        bool nextBomb =
            true;


        while (combatActive &&
               bossHealth != null &&
               !bossHealth.IsDead)
        {
            int phase =
                bossHealth.CurrentPhase;


            if (nextBomb)
            {
                yield return StartCoroutine(
                    BombAttackRoutine(
                        phase
                    )
                );
            }
            else
            {
                yield return StartCoroutine(
                    ShootAttackRoutine(
                        phase
                    )
                );
            }


            nextBomb =
                !nextBomb;


            float gap =
                GetAttackGap(
                    bossHealth.CurrentPhase
                );


            float timer =
                0f;


            while (timer < gap &&
                   combatActive)
            {
                timer +=
                    Time.deltaTime;


                yield return null;
            }
        }
    }


    // ==================================================
    // Bomb
    // ==================================================

    private IEnumerator BombAttackRoutine(
        int phase)
    {
        int bombCount =
            GetBombCount(
                phase
            );


        List<Vector2> targets =
            new List<Vector2>();


        for (int i = 0;
             i < bombCount;
             i++)
        {
            if (!combatActive)
                yield break;


            Vector2 target =
                FindBombTarget(
                    targets
                );


            targets.Add(
                target
            );


            SpawnBomb(
                target
            );


            if (bombSpawnStagger > 0f)
            {
                yield return
                    new WaitForSeconds(
                        bombSpawnStagger
                    );
            }
        }
    }


    private int GetBombCount(
        int phase)
    {
        switch (phase)
        {
            case 1:
                return 1;


            case 2:
                return 2;


            default:
                return 5;
        }
    }


    private Vector2 FindBombTarget(
        List<Vector2> previousTargets)
    {
        if (player == null)
        {
            return transform.position;
        }


        Vector2 playerPosition =
            player.position;


        for (int attempt = 0;
             attempt < 12;
             attempt++)
        {
            Vector2 direction =
                Random.insideUnitCircle;


            if (direction.sqrMagnitude <
                0.001f)
            {
                direction =
                    Vector2.right;
            }


            direction.Normalize();


            float distance =
                Random.Range(
                    bombMinDistance,
                    bombMaxDistance
                );


            Vector2 candidate =
                playerPosition
                + direction
                * distance;


            // ======================================
            // Wall 위에는 떨어지지 않게
            // ======================================

            bool obstacle =
                Physics2D.OverlapCircle(
                    candidate,
                    0.25f,
                    obstacleLayer
                ) != null;


            if (obstacle)
                continue;


            // ======================================
            // Bomb 중첩 감소
            // ======================================

            bool tooClose =
                false;


            foreach (
                Vector2 previous
                in previousTargets)
            {
                if (Vector2.Distance(
                        candidate,
                        previous)
                    <
                    bombMinSeparation)
                {
                    tooClose =
                        true;


                    break;
                }
            }


            if (!tooClose)
            {
                return candidate;
            }
        }


        // 찾지 못했을 경우
        return
            playerPosition
            + Random.insideUnitCircle
            * bombMaxDistance;
    }


    private void SpawnBomb(
        Vector2 target)
    {
        if (bombDropPrefab == null)
            return;


        GameObject bombObject =
            Instantiate(
                bombDropPrefab,
                target,
                Quaternion.identity
            );


        BossBombDrop bomb =
            bombObject.GetComponent<
                BossBombDrop
            >();


        if (bomb == null)
            return;


        bomb.Initialize(
            target,
            bombWarningDuration,
            bombDamage,
            bombDamageRadius,
            bombInkRadius,
            bombInkSplatCount
        );
    }


    // ==================================================
    // Shoot
    // ==================================================

    private IEnumerator ShootAttackRoutine(
        int phase)
    {
        if (player == null)
            yield break;


        float currentAngle =
            GetAngleToPlayer();


        float previousPlayerAngle =
            currentAngle;


        float lastTurnDirection =
            Random.value < 0.5f
                ? -1f
                : 1f;


        // ==========================================
        // 1. Warmup
        // ==========================================

        float timer =
            0f;


        while (timer <
               shootWarmup)
        {
            if (!combatActive)
                yield break;


            timer +=
                Time.deltaTime;


            currentAngle =
                GetAngleToPlayer();


            DrawAimLine(
                currentAngle,
                0.55f
            );


            yield return null;
        }


        // ==========================================
        // 2. Player 추적 + 1초 연사
        // ==========================================

        timer =
            0f;


        float shotTimer =
            0f;


        while (timer <
               trackingShootDuration)
        {
            if (!combatActive)
                yield break;


            float deltaTime =
                Time.deltaTime;


            timer +=
                deltaTime;


            float newPlayerAngle =
                GetAngleToPlayer();


            float deltaAngle =
                Mathf.DeltaAngle(
                    previousPlayerAngle,
                    newPlayerAngle
                );


            // Player를 따라 조준축이 어느 방향으로
            // 회전했는지 기억
            if (Mathf.Abs(
                    deltaAngle)
                > 0.20f)
            {
                lastTurnDirection =
                    Mathf.Sign(
                        deltaAngle
                    );
            }


            currentAngle =
                newPlayerAngle;


            previousPlayerAngle =
                newPlayerAngle;


            DrawAimLine(
                currentAngle,
                0.75f
            );


            shotTimer -=
                deltaTime;


            if (shotTimer <= 0f)
            {
                FireVolley(
                    currentAngle,
                    phase
                );


                shotTimer +=
                    1f
                    / Mathf.Max(
                        shootFireRate,
                        0.1f
                    );
            }


            yield return null;
        }


        // ==========================================
        // 3. Player 추적 종료
        //
        // 마지막으로 회전하던 방향으로
        // 2초 동안 선풍기처럼 계속 회전
        // ==========================================

        timer =
            0f;


        shotTimer =
            0f;


        while (timer <
               sweepShootDuration)
        {
            if (!combatActive)
                yield break;


            float deltaTime =
                Time.deltaTime;


            timer +=
                deltaTime;


            currentAngle +=
                lastTurnDirection
                * sweepDegreesPerSecond
                * deltaTime;


            DrawAimLine(
                currentAngle,
                0.60f
            );


            shotTimer -=
                deltaTime;


            if (shotTimer <= 0f)
            {
                FireVolley(
                    currentAngle,
                    phase
                );


                shotTimer +=
                    1f
                    / Mathf.Max(
                        shootFireRate,
                        0.1f
                    );
            }


            yield return null;
        }


        HideAimLine();
    }


    // ==================================================
    // Fire Volley
    // ==================================================

    private void FireVolley(
        float baseAngle,
        int phase)
    {
        // ==========================================
        // Phase 1
        // 단일 방향
        // ==========================================

        if (phase == 1)
        {
            FireProjectile(
                baseAngle
            );


            return;
        }


        // ==========================================
        // Phase 2
        // 3-way Fan
        // ==========================================

        if (phase == 2)
        {
            FireProjectile(
                baseAngle
                - phase2SpreadAngle
            );


            FireProjectile(
                baseAngle
            );


            FireProjectile(
                baseAngle
                + phase2SpreadAngle
            );


            return;
        }


        // ==========================================
        // Phase 3
        // 8방향 Radial
        //
        // 이 전체 8방향 축 자체가
        // 선풍기처럼 회전한다.
        // ==========================================

        const int directions =
            8;


        float angleStep =
            360f
            / directions;


        for (int i = 0;
             i < directions;
             i++)
        {
            FireProjectile(
                baseAngle
                + angleStep * i
            );
        }
    }


    // ==================================================
    // Projectile
    // ==================================================

    private void FireProjectile(
        float angleDegrees)
    {
        if (projectilePrefab == null)
            return;


        float radians =
            angleDegrees
            * Mathf.Deg2Rad;


        Vector2 direction =
            new Vector2(
                Mathf.Cos(radians),
                Mathf.Sin(radians)
            );


        GameObject projectileObject =
            Instantiate(
                projectilePrefab,
                firePoint.position,
                Quaternion.Euler(
                    0f,
                    0f,
                    angleDegrees
                )
            );


        BossProjectile projectile =
            projectileObject.GetComponent<
                BossProjectile
            >();


        if (projectile != null)
        {
            projectile.Initialize(
                direction,
                projectileSpeed,
                projectileDamage
            );
        }
    }


    // ==================================================
    // Aim
    // ==================================================

    private float GetAngleToPlayer()
    {
        if (player == null)
            return 0f;


        Vector2 difference =
            (Vector2)player.position
            - (Vector2)firePoint.position;


        return
            Mathf.Atan2(
                difference.y,
                difference.x
            )
            * Mathf.Rad2Deg;
    }


    // ==================================================
    // Aim Line
    // ==================================================

    private void CreateAimLine()
    {
        GameObject lineObject =
            new GameObject(
                "Runtime_BossAimLine"
            );


        lineObject.transform.SetParent(
            transform,
            false
        );


        aimLine =
            lineObject.AddComponent<
                LineRenderer
            >();


        aimLine.useWorldSpace =
            true;


        aimLine.positionCount =
            2;


        aimLine.startWidth =
            aimLineWidth;


        aimLine.endWidth =
            aimLineWidth;


        aimLine.enabled =
            false;


        if (referenceRenderer != null)
        {
            aimLine.sharedMaterial =
                referenceRenderer.sharedMaterial;


            aimLine.sortingLayerID =
                referenceRenderer.sortingLayerID;


            aimLine.sortingOrder =
                referenceRenderer.sortingOrder
                + aimSortingOffset;
        }
    }


    private void DrawAimLine(
        float angleDegrees,
        float alpha)
    {
        if (aimLine == null)
            return;


        aimLine.enabled =
            true;


        float radians =
            angleDegrees
            * Mathf.Deg2Rad;


        Vector2 direction =
            new Vector2(
                Mathf.Cos(radians),
                Mathf.Sin(radians)
            );


        Vector2 start =
            firePoint.position;


        Vector2 end =
            start
            + direction
            * aimLineLength;


        Color color =
            GetAttackColor();


        color.a =
            alpha;


        aimLine.startColor =
            color;


        aimLine.endColor =
            color;


        aimLine.SetPosition(
            0,
            start
        );


        aimLine.SetPosition(
            1,
            end
        );
    }


    private void HideAimLine()
    {
        if (aimLine != null)
        {
            aimLine.enabled =
                false;
        }
    }


    // ==================================================
    // Attack Gap
    // ==================================================

    private float GetAttackGap(
        int phase)
    {
        switch (phase)
        {
            case 1:
                return phase1AttackGap;


            case 2:
                return phase2AttackGap;


            default:
                return phase3AttackGap;
        }
    }


    // ==================================================
    // Color
    // ==================================================

    private Color GetAttackColor()
    {
        if (InkMap.Instance != null)
        {
            Color color =
                InkMap.Instance.enemyInkColor;


            color =
                Color.Lerp(
                    color,
                    Color.black,
                    0.25f
                );


            color.a =
                1f;


            return color;
        }


        return Color.red;
    }


    // ==================================================
    // Boss Death
    // ==================================================

    private void OnBossDied()
    {
        StopCombat();
    }


    private void OnDestroy()
    {
        if (bossHealth != null)
        {
            bossHealth.BossDied -=
                OnBossDied;
        }
    }
}