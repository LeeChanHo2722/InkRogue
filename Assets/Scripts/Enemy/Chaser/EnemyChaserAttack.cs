using UnityEngine;

public class EnemyChaserAttack : MonoBehaviour
{
    // ==================================================
    // References
    // ==================================================

    [Header("References")]

    public EnemyMovement movement;

    public EnemyContactDamage contactDamage;

    public EnemyChaserTelegraph telegraph;


    // ==================================================
    // Attack
    // ==================================================

    [Header("Attack")]

    [Tooltip("이 거리 안으로 들어오면 공격 준비 시작")]
    public float attackTriggerRange =
        1.8f;


    [Tooltip("준비 중 이 거리보다 멀어지면 공격 취소")]
    public float cancelRange =
        2.6f;


    // ==================================================
    // Windup
    // ==================================================

    [Header("Windup")]

    public float windupDuration =
        0.32f;


    [Tooltip("마지막 방향 고정 시간")]
    public float aimLockDuration =
        0.08f;


    [Range(0f, 1f)]
    public float windupMoveMultiplier =
        0.25f;


    // ==================================================
    // Dash
    // ==================================================

    [Header("Dash")]

    public float dashSpeed =
        7f;


    public float dashDuration =
        0.18f;


    // ==================================================
    // Recovery
    // ==================================================

    [Header("Recovery")]

    public float recoveryDuration =
        0.45f;


    [Range(0f, 1f)]
    public float recoveryMoveMultiplier =
        0.35f;


    // ==================================================
    // Obstacle
    // ==================================================

    [Header("Obstacle")]

    public LayerMask obstacleLayer;


    // ==================================================
    // State
    // ==================================================

    private enum ChaserState
    {
        Chase,
        Windup,
        Dash,
        Recovery
    }


    private ChaserState state =
        ChaserState.Chase;


    private Rigidbody2D rb;

    private Transform player;


    private float stateTimer =
        0f;


    private Vector2 dashDirection =
        Vector2.right;


    private bool aimLocked =
        false;


    // ==================================================
    // Public
    // ==================================================

    public bool IsAttacking =>
        state != ChaserState.Chase;


    public bool IsDashing =>
        state == ChaserState.Dash;


    // ==================================================
    // Awake
    // ==================================================

    private void Awake()
    {
        rb =
            GetComponent<Rigidbody2D>();


        if (movement == null)
        {
            movement =
                GetComponent<EnemyMovement>();
        }


        if (contactDamage == null)
        {
            contactDamage =
                GetComponent<EnemyContactDamage>();
        }


        if (telegraph == null)
        {
            telegraph =
                GetComponent<EnemyChaserTelegraph>();
        }
    }


    // ==================================================
    // Start
    // ==================================================

    private void Start()
    {
        GameObject playerObject =
            EncounterTarget.ResolveGameObject();


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
            case ChaserState.Chase:
                UpdateChase();
                break;


            case ChaserState.Windup:
                UpdateWindup();
                break;


            case ChaserState.Recovery:
                UpdateRecovery();
                break;
        }
    }


    // ==================================================
    // Fixed Update
    // ==================================================

    private void FixedUpdate()
    {
        if (state ==
            ChaserState.Dash)
        {
            UpdateDashPhysics();
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


        if (distance >
            attackTriggerRange)
        {
            return;
        }


        if (!HasLineOfSight())
        {
            return;
        }


        BeginWindup();
    }


    // ==================================================
    // Windup
    // ==================================================

    private void BeginWindup()
    {
        state =
            ChaserState.Windup;


        stateTimer =
            0f;


        aimLocked =
            false;


        dashDirection =
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
            telegraph
                .BeginTelegraph();


            telegraph
                .UpdateTelegraph(
                    transform.position,
                    dashDirection,
                    0f,
                    false
                );
        }
    }


    // ==================================================
    // Update Windup
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
        // 아직 방향 Lock 전
        // ==========================================

        if (!aimLocked)
        {
            float distance =
                Vector2.Distance(
                    transform.position,
                    player.position
                );


            // 너무 멀어지거나 벽 뒤로 숨으면 취소
            if (distance >
                cancelRange ||
                !HasLineOfSight())
            {
                CancelWindup();

                return;
            }


            // Player를 계속 추적
            dashDirection =
                GetDirectionToPlayer();


            // ======================================
            // 방향 Lock
            // ======================================

            if (stateTimer >=
                lockStartTime)
            {
                aimLocked =
                    true;


                dashDirection =
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
        // Visual
        // ==========================================

        float progress =
            Mathf.Clamp01(
                stateTimer
                / safeWindup
            );


        if (telegraph != null)
        {
            telegraph
                .UpdateTelegraph(
                    transform.position,
                    dashDirection,
                    progress,
                    aimLocked
                );
        }


        // ==========================================
        // Dash Start
        // ==========================================

        if (stateTimer >=
            safeWindup)
        {
            BeginDash();
        }
    }


    // ==================================================
    // Cancel
    // ==================================================

    private void CancelWindup()
    {
        state =
            ChaserState.Chase;


        stateTimer =
            0f;


        aimLocked =
            false;


        if (movement != null)
        {
            movement
                .SetAttackSpeedMultiplier(
                    1f
                );
        }


        if (telegraph != null)
        {
            telegraph
                .HideTelegraph();
        }
    }


    // ==================================================
    // Dash Start
    // ==================================================

    private void BeginDash()
    {
        state =
            ChaserState.Dash;


        stateTimer =
            0f;


        if (telegraph != null)
        {
            telegraph
                .HideTelegraph();


            telegraph
                .PlayDashFlash(
                    transform.position
                );
        }


        // 일반 추적 Movement가
        // Rigidbody를 덮어쓰지 못하게 함
        if (movement != null)
        {
            movement
                .SetMovementOverride(
                    true
                );


            movement
                .SetAttackSpeedMultiplier(
                    1f
                );
        }


        // Dash 중에만 Damage 허용
        if (contactDamage != null)
        {
            contactDamage
                .BeginAttackWindow();
        }
    }


    // ==================================================
    // Dash Physics
    // ==================================================

    private void UpdateDashPhysics()
    {
        stateTimer +=
            Time.fixedDeltaTime;


        float speedMultiplier =
            1f;


        // ==========================================
        // Player Ink 바닥
        // +
        // 총에 맞은 Suppression
        //
        // 둘 다 Dash에도 적용
        // ==========================================

        if (movement != null)
        {
            speedMultiplier =
                movement
                    .GetEnvironmentSpeedMultiplier();
        }


        rb.linearVelocity =
            dashDirection
            * dashSpeed
            * speedMultiplier;


        if (stateTimer >=
            dashDuration)
        {
            EndDash();
        }
    }


    // ==================================================
    // Dash End
    // ==================================================

    private void EndDash()
    {
        if (state !=
            ChaserState.Dash)
        {
            return;
        }


        state =
            ChaserState.Recovery;


        stateTimer =
            0f;


        rb.linearVelocity =
            Vector2.zero;


        if (contactDamage != null)
        {
            contactDamage
                .EndAttackWindow();
        }


        if (movement != null)
        {
            movement
                .SetMovementOverride(
                    false
                );


            movement
                .SetAttackSpeedMultiplier(
                    recoveryMoveMultiplier
                );
        }
    }


    // ==================================================
    // Recovery
    // ==================================================

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
            ChaserState.Chase;


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
    // Direction
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
    // Line Of Sight
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
    // Collision
    // ==================================================

    private void OnCollisionEnter2D(
        Collision2D collision)
    {
        if (state !=
            ChaserState.Dash)
        {
            return;
        }


        // Dash 중 벽 충돌
        // → 즉시 Dash 종료
        int collisionLayer =
            collision.gameObject.layer;


        bool hitObstacle =
            (
                obstacleLayer.value
                & (1 << collisionLayer)
            ) != 0;


        if (hitObstacle)
        {
            EndDash();
        }
    }


    // ==================================================
    // Safety
    // ==================================================

    private void OnDisable()
    {
        if (contactDamage != null)
        {
            contactDamage
                .EndAttackWindow();
        }


        if (movement != null)
        {
            movement
                .SetMovementOverride(
                    false
                );


            movement
                .SetAttackSpeedMultiplier(
                    1f
                );
        }


        if (telegraph != null)
        {
            telegraph
                .HideTelegraph();
        }


        if (rb != null)
        {
            rb.linearVelocity =
                Vector2.zero;
        }
    }
}