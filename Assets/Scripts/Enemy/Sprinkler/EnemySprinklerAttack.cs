using System.Collections;
using UnityEngine;

public class EnemySprinklerAttack : MonoBehaviour
{
    // ==================================================
    // References
    // ==================================================

    [Header("References")]

    public EnemySprinklerMovement movement;

    public EnemySprinklerTelegraph telegraph;


    // ==================================================
    // Attack
    // ==================================================

    [Header("Attack")]

    [Tooltip("Ink 분사 사이의 시간")]
    public float attackCooldown = 2.8f;

    [Tooltip("분사 전 경고 시간")]
    public float windupDuration = 0.65f;

    [Tooltip("경고 중 이동속도")]
    public float windupMoveMultiplier = 0.20f;


    // ==================================================
    // Spray
    // ==================================================

    [Header("Spray")]

    [Tooltip("한 번에 뿌리는 방향 수")]
    public int sprayDirections = 6;

    [Tooltip("최대 분사 거리")]
    public float sprayDistance = 2.8f;

    [Tooltip("각 방향의 실제 Ink를 순차적으로 그리는 시간")]
    public float sprayDuration = 0.42f;

    [Tooltip("Ink가 찍히는 간격")]
    public float paintSpacing = 0.16f;

    [Tooltip("중앙 Ink 크기")]
    public float centerInkRadius = 0.45f;

    [Tooltip("분사 선 Ink 크기")]
    public float sprayInkRadius = 0.25f;

    public int splatCount = 2;

    [Tooltip("매 공격마다 전체 방향을 약간 회전")]
    public float randomRotationRange = 30f;


    // ==================================================
    // Wall
    // ==================================================

    [Header("Wall")]

    public LayerMask obstacleLayer;


    // ==================================================
    // Runtime
    // ==================================================

    private EnemySpawnVisual spawnVisual;

    private float cooldownTimer;

    private bool attacking = false;


    // ==================================================
    // Awake
    // ==================================================

    private void Awake()
    {
        if (movement == null)
        {
            movement =
                GetComponent<EnemySprinklerMovement>();
        }


        if (telegraph == null)
        {
            telegraph =
                GetComponent<EnemySprinklerTelegraph>();
        }


        spawnVisual =
            GetComponentInParent<EnemySpawnVisual>();
    }


    // ==================================================
    // Start
    // ==================================================

    private void Start()
    {
        cooldownTimer =
            Random.Range(
                0.8f,
                1.5f
            );
    }


    // ==================================================
    // Update
    // ==================================================

    private void Update()
    {
        if (attacking)
            return;


        if (spawnVisual != null &&
            !spawnVisual.IsSpawnFinished)
        {
            return;
        }


        cooldownTimer -=
            Time.deltaTime;


        if (cooldownTimer <= 0f)
        {
            StartCoroutine(
                AttackRoutine()
            );
        }
    }


    // ==================================================
    // Attack
    // ==================================================

    private IEnumerator AttackRoutine()
    {
        attacking =
            true;


        // ==========================================
        // Windup 중 이동 감소
        // ==========================================

        if (movement != null)
        {
            movement
                .SetAttackSpeedMultiplier(
                    windupMoveMultiplier
                );
        }


        float timer =
            0f;


        float safeWindup =
            Mathf.Max(
                windupDuration,
                0.01f
            );


        // ==========================================
        // 1. Warning
        // ==========================================

        while (timer <
               safeWindup)
        {
            timer +=
                Time.deltaTime;


            float progress =
                Mathf.Clamp01(
                    timer
                    / safeWindup
                );


            if (telegraph != null)
            {
                telegraph.Show(
                    progress
                );
            }


            yield return null;
        }


        if (telegraph != null)
        {
            telegraph.Hide();
        }


        // ==========================================
        // 2. 중심부 Ink
        // ==========================================

        if (InkMap.Instance != null)
        {
            InkMap.Instance.PaintExplosion(
                transform.position,
                centerInkRadius,
                InkTeam.Enemy,
                8
            );
        }


        // ==========================================
        // 3. 방사형 Spray
        // ==========================================

        yield return StartCoroutine(
            SprayRoutine()
        );


        // ==========================================
        // 4. 이동 복구
        // ==========================================

        if (movement != null)
        {
            movement
                .SetAttackSpeedMultiplier(
                    1f
                );
        }


        cooldownTimer =
            attackCooldown;


        attacking =
            false;
    }


    // ==================================================
    // Spray Routine
    // ==================================================

    private IEnumerator SprayRoutine()
    {
        int safeDirections =
            Mathf.Max(
                1,
                sprayDirections
            );


        float baseAngle =
            Random.Range(
                -randomRotationRange,
                randomRotationRange
            );


        float angleStep =
            360f
            / safeDirections;


        float timer =
            0f;


        float safeDuration =
            Mathf.Max(
                sprayDuration,
                0.01f
            );


        // ==========================================
        // 각 방향별 최대 거리 계산
        // ==========================================

        Vector2[] directions =
            new Vector2[
                safeDirections
            ];


        float[] distances =
            new float[
                safeDirections
            ];


        for (int i = 0;
             i < safeDirections;
             i++)
        {
            float angle =
                (
                    baseAngle
                    + angleStep * i
                )
                * Mathf.Deg2Rad;


            Vector2 direction =
                new Vector2(
                    Mathf.Cos(angle),
                    Mathf.Sin(angle)
                );


            directions[i] =
                direction;


            distances[i] =
                GetWallLimitedDistance(
                    direction
                );
        }


        float previousProgress =
            0f;


        // ==========================================
        // 시간에 따라 중심 → 바깥으로 Ink 진행
        // ==========================================

        while (timer <
               safeDuration)
        {
            timer +=
                Time.deltaTime;


            float progress =
                Mathf.Clamp01(
                    timer
                    / safeDuration
                );


            for (int i = 0;
                 i < safeDirections;
                 i++)
            {
                PaintSection(
                    directions[i],
                    distances[i],
                    previousProgress,
                    progress
                );
            }


            previousProgress =
                progress;


            yield return null;
        }


        // 마지막 프레임 보정
        for (int i = 0;
             i < safeDirections;
             i++)
        {
            PaintSection(
                directions[i],
                distances[i],
                previousProgress,
                1f
            );
        }
    }


    // ==================================================
    // Paint Section
    // ==================================================

    private void PaintSection(
        Vector2 direction,
        float maxDistance,
        float fromProgress,
        float toProgress)
    {
        if (InkMap.Instance == null)
            return;


        float startDistance =
            maxDistance
            * fromProgress;


        float endDistance =
            maxDistance
            * toProgress;


        float sectionLength =
            endDistance
            - startDistance;


        if (sectionLength <= 0f)
            return;


        float safeSpacing =
            Mathf.Max(
                paintSpacing,
                0.03f
            );


        int sampleCount =
            Mathf.Max(
                1,
                Mathf.CeilToInt(
                    sectionLength
                    / safeSpacing
                )
            );


        Vector2 center =
            transform.position;


        for (int i = 0;
             i <= sampleCount;
             i++)
        {
            float t =
                (float)i
                / sampleCount;


            float distance =
                Mathf.Lerp(
                    startDistance,
                    endDistance,
                    t
                );


            Vector2 position =
                center
                + direction
                * distance;


            float jitter =
                Random.Range(
                    -0.08f,
                    0.08f
                );


            Vector2 perpendicular =
                new Vector2(
                    -direction.y,
                    direction.x
                );


            position +=
                perpendicular
                * jitter;


            float radius =
                sprayInkRadius
                * Random.Range(
                    0.80f,
                    1.20f
                );


            InkMap.Instance
                .PaintExplosion(
                    position,
                    radius,
                    InkTeam.Enemy,
                    splatCount
                );
        }
    }


    // ==================================================
    // Wall Limit
    // ==================================================

    private float GetWallLimitedDistance(
        Vector2 direction)
    {
        RaycastHit2D hit =
            Physics2D.Raycast(
                transform.position,
                direction,
                sprayDistance,
                obstacleLayer
            );


        if (hit.collider != null)
        {
            return
                Mathf.Max(
                    0f,
                    hit.distance
                    - 0.10f
                );
        }


        return
            sprayDistance;
    }


    // ==================================================
    // Disable
    // ==================================================

    private void OnDisable()
    {
        StopAllCoroutines();


        attacking =
            false;


        if (telegraph != null)
        {
            telegraph.Hide();
        }


        if (movement != null)
        {
            movement
                .SetAttackSpeedMultiplier(
                    1f
                );
        }
    }
}