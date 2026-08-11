using UnityEngine;

public class EnemyTankAttack : MonoBehaviour
{
    // ==================================================
    // References
    // ==================================================

    [Header("References")]

    public EnemyMovement movement;

    public EnemyTankTelegraph telegraph;


    // ==================================================
    // Attack
    // ==================================================

    [Header("Attack")]

    [Range(0.1f, 1f)]
    [Tooltip(
        "Slash Range 중 몇 % 거리까지 접근하면 공격을 시작할지. "
        + "0.67 = 약 2/3 지점"
    )]
    public float attackTriggerRangeRatio =
        0.67f;


    [Tooltip("실제 반원 참격 거리")]
    public float slashRange =
        1.60f;


    [Range(1f, 179f)]
    [Tooltip("90이면 좌우 합쳐서 180도")]
    public float slashHalfAngle =
        90f;


    [Tooltip("Tank 강공격 Damage")]
    public int damage =
        3;


    // ==================================================
    // Windup
    // ==================================================

    [Header("Windup")]

    public float windupDuration =
        0.55f;


    [Tooltip("마지막 방향 고정 시간")]
    public float aimLockDuration =
        0.12f;


    [Range(0f, 1f)]
    public float windupMoveMultiplier =
        0.12f;


    // ==================================================
    // Slash
    // ==================================================

    [Header("Slash")]

    public float slashDuration =
        0.20f;


    [Tooltip(
        "Slash 시작 후 실제 Damage가 발생하는 시점"
    )]
    public float impactTime =
        0.08f;


    // ==================================================
    // Slash Ink
    // ==================================================

    [Header("Slash Ink")]

    [Tooltip("참격이 바닥에 Enemy Ink를 생성할지")]
    public bool paintSlashInk =
        true;


    [Tooltip(
        "Tank 몸 바로 밑부터 칠하지 않도록 "
        + "시작하는 최소 거리"
    )]
    public float inkInnerRadius =
        0.25f;


    [Tooltip(
        "부채꼴을 안쪽→바깥쪽으로 몇 줄 칠할지"
    )]
    [Range(2, 6)]
    public int inkRadialLayers =
        4;


    [Tooltip(
        "몇 도마다 Ink 덩어리를 만들지. "
        + "작을수록 촘촘하지만 비용 증가"
    )]
    public float inkAngleStep =
        16f;


    [Tooltip("각 Ink 덩어리 크기")]
    public float inkBlobRadius =
        0.24f;


    [Tooltip("각 지점의 불규칙한 Splat 수")]
    public int inkSplatCount =
        2;


    [Tooltip(
        "너무 완벽한 부채꼴이 되지 않도록 "
        + "위치를 흔드는 정도"
    )]
    public float inkPositionJitter =
        0.08f;


    [Tooltip(
        "각도도 조금 흔들어서 붓 느낌을 만듦"
    )]
    public float inkAngleJitter =
        4f;


    // ==================================================
    // Recovery
    // ==================================================

    [Header("Recovery")]

    public float recoveryDuration =
        0.65f;


    [Range(0f, 1f)]
    public float recoveryMoveMultiplier =
        0.20f;


    // ==================================================
    // Obstacle
    // ==================================================

    [Header("Obstacle")]

    public LayerMask obstacleLayer;


    // ==================================================
    // State
    // ==================================================

    private enum TankState
    {
        Chase,
        Windup,
        Slash,
        Recovery
    }


    private TankState state =
        TankState.Chase;


    private Transform player;


    private float stateTimer =
        0f;


    private Vector2 attackDirection =
        Vector2.right;


    private bool aimLocked =
        false;


    private bool damageApplied =
        false;


    // ==========================================
    // Slash Ink Runtime
    // ==========================================

    private float lastPaintedSlashAngle;


    // ==================================================
    // Public
    // ==================================================

    public bool IsAttacking =>
        state != TankState.Chase;


    public bool IsSlashing =>
        state == TankState.Slash;


    public float AttackTriggerRange =>
        slashRange
        * attackTriggerRangeRatio;


    // ==================================================
    // Awake
    // ==================================================

    private void Awake()
    {
        if (movement == null)
        {
            movement =
                GetComponent<EnemyMovement>();
        }


        if (telegraph == null)
        {
            telegraph =
                GetComponent<EnemyTankTelegraph>();
        }
    }


    // ==================================================
    // Start
    // ==================================================

    private void Start()
    {
        GameObject playerObject =
            GameObject.FindGameObjectWithTag(
                "Player"
            );


        if (playerObject != null)
        {
            player =
                playerObject.transform;
        }


        if (movement != null &&
            obstacleLayer.value == 0)
        {
            obstacleLayer =
                movement.obstacleLayer;
        }


        // ==========================================
        // 실제 판정 각도와
        // Telegraph 각도가 항상 같도록
        // 자동 동기화
        // ==========================================

        if (telegraph != null)
        {
            telegraph.halfAngle =
                slashHalfAngle;
        }
    }


    // ==================================================
    // Update
    // ==================================================

    private void Update()
    {
        if (player == null)
            return;


        switch (state)
        {
            case TankState.Chase:

                UpdateChase();

                break;


            case TankState.Windup:

                UpdateWindup();

                break;


            case TankState.Slash:

                UpdateSlash();

                break;


            case TankState.Recovery:

                UpdateRecovery();

                break;
        }
    }


    // ==================================================
    // Chase
    // ==================================================

    private void UpdateChase()
    {
        float distance =
            Vector2.Distance(
                transform.position,
                player.position
            );


        // ==========================================
        // 이전:
        // Slash 범위에 들어오자마자 공격
        //
        // 변경:
        // Slash 범위의 약 2/3까지
        // 접근해야 공격 시작
        // ==========================================

        float triggerRange =
            slashRange
            * attackTriggerRangeRatio;


        if (distance >
            triggerRange)
        {
            return;
        }


        // 공격 시작 전까지만
        // 시야 검사
        if (!HasLineOfSight())
        {
            return;
        }


        BeginWindup();
    }


    // ==================================================
    // Begin Windup
    // ==================================================

    private void BeginWindup()
    {
        state =
            TankState.Windup;


        stateTimer =
            0f;


        aimLocked =
            false;


        attackDirection =
            GetDirectionToPlayer();


        if (movement != null)
        {
            movement
                .SetAttackSpeedMultiplier(
                    windupMoveMultiplier
                );
        }


        if (telegraph != null)
        {
            // 현재 Inspector 값이 바뀌었어도
            // 실제 공격과 다시 동기화
            telegraph.halfAngle =
                slashHalfAngle;


            telegraph.BeginWarning();


            telegraph.UpdateWarning(
                transform.position,
                attackDirection,
                0f,
                false,
                slashRange
            );
        }
    }


    // ==================================================
    // Windup
    // ==================================================

    private void UpdateWindup()
    {
        stateTimer +=
            Time.deltaTime;


        float safeWindup =
            Mathf.Max(
                windupDuration,
                0.01f
            );


        float safeLockDuration =
            Mathf.Clamp(
                aimLockDuration,
                0f,
                safeWindup
            );


        float lockStartTime =
            safeWindup
            - safeLockDuration;


        // ==========================================
        // 중요 변경
        //
        // 공격이 시작된 이후에는
        // Range / Line Of Sight를
        // 다시 검사하지 않는다.
        //
        // 따라서 Telegraph가 한 번 보였다면
        // 반드시 Slash까지 실행됨.
        // ==========================================

        if (!aimLocked)
        {
            // ======================================
            // Lock 전까지는 Player를 계속 조준
            // ======================================

            attackDirection =
                GetDirectionToPlayer();


            // ======================================
            // Aim Lock
            // ======================================

            if (stateTimer >=
                lockStartTime)
            {
                aimLocked =
                    true;


                attackDirection =
                    GetDirectionToPlayer();


                if (movement != null)
                {
                    movement
                        .SetAttackSpeedMultiplier(
                            0f
                        );
                }
            }
        }


        // ==========================================
        // Telegraph
        // ==========================================

        float progress =
            Mathf.Clamp01(
                stateTimer
                / safeWindup
            );


        if (telegraph != null)
        {
            telegraph.UpdateWarning(
                transform.position,
                attackDirection,
                progress,
                aimLocked,
                slashRange
            );
        }


        // ==========================================
        // 공격 실행
        // ==========================================

        if (stateTimer >=
            safeWindup)
        {
            BeginSlash();
        }
    }


    // ==================================================
    // Begin Slash
    // ==================================================

    private void BeginSlash()
    {
        state =
            TankState.Slash;


        stateTimer =
            0f;


        damageApplied =
            false;


        // ==========================================
        // Ink도 왼쪽에서 오른쪽으로
        // Slash와 같이 시작
        // ==========================================

        lastPaintedSlashAngle =
            -slashHalfAngle;


        if (movement != null)
        {
            movement
                .SetAttackSpeedMultiplier(
                    0f
                );
        }


        if (telegraph != null)
        {
            telegraph.HideWarning();


            telegraph.PlaySlash(
                transform.position,
                attackDirection,
                slashRange,
                slashDuration
            );
        }
    }


    // ==================================================
    // Update Slash
    // ==================================================

    private void UpdateSlash()
    {
        stateTimer +=
            Time.deltaTime;


        float safeSlashDuration =
            Mathf.Max(
                slashDuration,
                0.01f
            );


        float slashProgress =
            Mathf.Clamp01(
                stateTimer
                / safeSlashDuration
            );


        // ==========================================
        // 참격 VFX와 동일한 진행 곡선
        // ==========================================

        float easedProgress =
            EaseOutCubic(
                slashProgress
            );


        float currentSlashAngle =
            Mathf.Lerp(
                -slashHalfAngle,
                slashHalfAngle,
                easedProgress
            );


        // ==========================================
        // 호쿠사이 느낌:
        //
        // 검이 지나간 구간만
        // 순서대로 Ink Paint
        // ==========================================

        if (paintSlashInk)
        {
            PaintSlashInkBetween(
                lastPaintedSlashAngle,
                currentSlashAngle
            );
        }


        lastPaintedSlashAngle =
            currentSlashAngle;


        // ==========================================
        // Damage는 기존처럼 한 번만
        // ==========================================

        if (!damageApplied &&
            stateTimer >= impactTime)
        {
            damageApplied =
                true;


            ApplySlashDamage();
        }


        if (stateTimer >=
            safeSlashDuration)
        {
            // 마지막 끝부분까지
            // Paint가 누락되지 않게 보정
            if (paintSlashInk &&
                lastPaintedSlashAngle <
                slashHalfAngle)
            {
                PaintSlashInkBetween(
                    lastPaintedSlashAngle,
                    slashHalfAngle
                );


                lastPaintedSlashAngle =
                    slashHalfAngle;
            }


            BeginRecovery();
        }
    }


    // ==================================================
    // Slash Damage
    // ==================================================

    private void ApplySlashDamage()
    {
        if (player == null)
            return;


        Vector2 origin =
            transform.position;


        Vector2 toPlayer =
            (Vector2)player.position
            - origin;


        float distance =
            toPlayer.magnitude;


        // ==========================================
        // Range
        // ==========================================

        if (distance >
            slashRange)
        {
            return;
        }


        if (distance >
            0.001f)
        {
            // ======================================
            // 180도 반원 Angle 판정
            // ======================================

            float angle =
                Vector2.Angle(
                    attackDirection,
                    toPlayer.normalized
                );


            if (angle >
                slashHalfAngle)
            {
                return;
            }


            // ======================================
            // 벽 뒤 Player는 Damage 없음
            // ======================================

            RaycastHit2D wallHit =
                Physics2D.Raycast(
                    origin,
                    toPlayer.normalized,
                    distance,
                    obstacleLayer
                );


            if (wallHit.collider != null)
            {
                return;
            }
        }


        PlayerShield playerShield =
            player.GetComponent<PlayerShield>();


        if (playerShield == null)
            return;


        playerShield.TakeDamage(
            damage,
            transform.position
        );
    }


    // ==================================================
    // Slash Ink
    // ==================================================

    private void PaintSlashInkBetween(
        float fromAngle,
        float toAngle)
    {
        if (InkMap.Instance == null)
            return;


        if (toAngle <=
            fromAngle)
        {
            return;
        }


        float safeAngleStep =
            Mathf.Max(
                3f,
                inkAngleStep
            );


        float angle =
            fromAngle;


        // ==========================================
        // 프레임 사이에 각도가 크게 넘어가더라도
        // 중간 부분이 비지 않도록
        // 여러 각도로 나누어 Paint
        // ==========================================

        while (angle <
               toAngle)
        {
            angle =
                Mathf.Min(
                    angle
                    + safeAngleStep,
                    toAngle
                );


            PaintInkSlice(
                angle
            );
        }
    }


    // ==================================================
    // One Brush Slice
    // ==================================================

    private void PaintInkSlice(
        float angle)
    {
        if (InkMap.Instance == null)
            return;


        int layers =
            Mathf.Max(
                2,
                inkRadialLayers
            );


        // ==========================================
        // 약간 각도를 흔들어
        // 완벽한 기계식 반원을 방지
        // ==========================================

        float jitteredAngle =
            angle
            + Random.Range(
                -inkAngleJitter,
                inkAngleJitter
            );


        Vector2 direction =
            RotateVector(
                attackDirection,
                jitteredAngle
            );


        // ==========================================
        // 벽이 있다면
        // Ink도 벽을 뚫지 않게 최대 거리 제한
        // ==========================================

        float maxPaintDistance =
            GetInkPaintDistance(
                direction
            );


        if (maxPaintDistance <=
            inkInnerRadius)
        {
            return;
        }


        // ==========================================
        // 안쪽 → 바깥쪽으로 여러 덩어리
        //
        // 이것들이 이어져 한 번의
        // 넓은 붓자국처럼 보임
        // ==========================================

        for (int i = 0;
             i < layers;
             i++)
        {
            float ratio =
                layers <= 1
                    ? 1f
                    : (float)i
                    / (layers - 1);


            float distance =
                Mathf.Lerp(
                    inkInnerRadius,
                    maxPaintDistance,
                    ratio
                );


            Vector2 paintPosition =
                (Vector2)transform.position
                + direction
                * distance;


            // ======================================
            // 위치도 살짝 불규칙하게
            // ======================================

            paintPosition +=
                Random.insideUnitCircle
                * inkPositionJitter;


            float radius =
                inkBlobRadius
                * Random.Range(
                    0.82f,
                    1.18f
                );


            InkMap.Instance.PaintExplosion(
                paintPosition,
                radius,
                InkTeam.Enemy,
                inkSplatCount
            );
        }
    }


    // ==================================================
    // Ink Wall Limit
    // ==================================================

    private float GetInkPaintDistance(
        Vector2 direction)
    {
        RaycastHit2D hit =
            Physics2D.Raycast(
                transform.position,
                direction,
                slashRange,
                obstacleLayer
            );


        if (hit.collider == null)
        {
            return slashRange;
        }


        // Ink 덩어리 자체의 크기 때문에
        // 벽 너머로 살짝 삐져나가지 않게 여유
        return Mathf.Max(
            0f,
            hit.distance
            - inkBlobRadius * 0.5f
        );
    }


    // ==================================================
    // Recovery
    // ==================================================

    private void BeginRecovery()
    {
        state =
            TankState.Recovery;


        stateTimer =
            0f;


        aimLocked =
            false;


        if (movement != null)
        {
            movement
                .SetAttackSpeedMultiplier(
                    recoveryMoveMultiplier
                );
        }
    }


    private void UpdateRecovery()
    {
        stateTimer +=
            Time.deltaTime;


        if (stateTimer <
            recoveryDuration)
        {
            return;
        }


        state =
            TankState.Chase;


        stateTimer =
            0f;


        if (movement != null)
        {
            movement
                .SetAttackSpeedMultiplier(
                    1f
                );
        }
    }


    // ==================================================
    // Player Direction
    // ==================================================

    private Vector2 GetDirectionToPlayer()
    {
        Vector2 difference =
            (Vector2)player.position
            - (Vector2)transform.position;


        if (difference.sqrMagnitude <
            0.0001f)
        {
            return Vector2.right;
        }


        return difference.normalized;
    }


    // ==================================================
    // Initial LOS
    // ==================================================

    private bool HasLineOfSight()
    {
        if (player == null)
            return false;


        Vector2 difference =
            (Vector2)player.position
            - (Vector2)transform.position;


        float distance =
            difference.magnitude;


        if (distance <=
            0.001f)
        {
            return true;
        }


        RaycastHit2D hit =
            Physics2D.Raycast(
                transform.position,
                difference.normalized,
                distance,
                obstacleLayer
            );


        return hit.collider ==
            null;
    }


    // ==================================================
    // Rotate Vector
    // ==================================================

    private Vector2 RotateVector(
        Vector2 vector,
        float degrees)
    {
        float radians =
            degrees
            * Mathf.Deg2Rad;


        float cos =
            Mathf.Cos(
                radians
            );


        float sin =
            Mathf.Sin(
                radians
            );


        return new Vector2(
            vector.x * cos
            - vector.y * sin,

            vector.x * sin
            + vector.y * cos
        );
    }


    // ==================================================
    // Ease
    // ==================================================

    private float EaseOutCubic(
        float t)
    {
        float inverse =
            1f - t;


        return
            1f
            - inverse
            * inverse
            * inverse;
    }


    // ==================================================
    // Safety
    // ==================================================

    private void OnDisable()
    {
        if (movement != null)
        {
            movement
                .SetAttackSpeedMultiplier(
                    1f
                );
        }


        if (telegraph != null)
        {
            telegraph.HideWarning();
        }


        state =
            TankState.Chase;


        aimLocked =
            false;


        damageApplied =
            false;
    }
}