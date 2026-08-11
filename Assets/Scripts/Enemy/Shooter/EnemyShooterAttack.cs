using UnityEngine;

public class EnemyShooterAttack : MonoBehaviour
{
    // ==================================================
    // References
    // ==================================================

    [Header("References")]

    public GameObject projectilePrefab;

    public EnemyShooterTelegraph telegraph;

    public EnemyShooterMovement shooterMovement;


    [Tooltip(
        "비워두면 Shooter 중심에서 발사"
    )]
    public Transform fireOrigin;


    // ==================================================
    // Attack
    // ==================================================

    [Header("Attack")]

    [Tooltip("초당 발사 횟수")]
    public float fireRate = 0.8f;

    public float attackRange = 8f;

    public LayerMask obstacleLayer;


    // ==================================================
    // Telegraph
    // ==================================================

    [Header("Attack Telegraph")]

    [Tooltip(
        "조준 시작부터 발사까지 전체 시간"
    )]
    public float telegraphDuration =
        0.45f;


    [Tooltip(
        "발사 직전 조준이 고정되는 시간"
    )]
    public float aimLockDuration =
        0.10f;


    [Range(0f, 1f)]
    [Tooltip(
        "Telegraph 중 이동속도"
    )]
    public float chargingMoveMultiplier =
        0.45f;


    [Range(0f, 1f)]
    [Tooltip(
        "Aim Lock 중 이동속도"
    )]
    public float lockedMoveMultiplier =
        0f;


    [Tooltip(
        "전조가 취소된 뒤 재시도까지 시간"
    )]
    public float cancelRetryDelay =
        0.15f;


    // ==================================================
    // State
    // ==================================================

    private enum AttackState
    {
        Ready,
        Charging
    }


    private AttackState attackState =
        AttackState.Ready;


    private Transform player;


    private float nextAttackStartTime =
        0f;


    private float chargeTimer =
        0f;


    private Vector2 aimDirection;
    private Vector2 aimTargetPosition;


    private bool aimLocked =
        false;


    // ==================================================
    // Public
    // ==================================================

    public bool IsCharging =>
        attackState ==
        AttackState.Charging;


    public bool IsAimLocked =>
        aimLocked;


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


        if (telegraph == null)
        {
            telegraph =
                GetComponent<EnemyShooterTelegraph>();
        }


        if (shooterMovement == null)
        {
            shooterMovement =
                GetComponent<EnemyShooterMovement>();
        }
    }


    // ==================================================
    // Update
    // ==================================================

    private void Update()
    {
        if (player == null)
            return;


        if (fireRate <= 0f)
            return;


        // ==========================================
        // 현재 Charging 중
        // ==========================================

        if (attackState ==
            AttackState.Charging)
        {
            UpdateCharging();

            return;
        }


        // ==========================================
        // Cooldown
        // ==========================================

        if (Time.time <
            nextAttackStartTime)
        {
            return;
        }


        // ==========================================
        // 공격 시작 가능?
        // ==========================================

        if (!CanStartAttack())
            return;


        BeginAttack();
    }


    // ==================================================
    // Can Start
    // ==================================================

    private bool CanStartAttack()
    {
        float distance =
            Vector2.Distance(
                transform.position,
                player.position
            );


        if (distance >
            attackRange)
        {
            return false;
        }


        if (!HasLineOfSight())
        {
            return false;
        }


        return true;
    }


    // ==================================================
    // Begin Attack
    // ==================================================

    private void BeginAttack()
    {
        attackState =
            AttackState.Charging;


        chargeTimer =
            0f;


        aimLocked =
            false;


        aimTargetPosition =
            player.position;


        aimDirection =
            (
                aimTargetPosition
                - GetFirePosition()
            ).normalized;


        if (shooterMovement != null)
        {
            shooterMovement
                .SetAttackSpeedMultiplier(
                    chargingMoveMultiplier
                );
        }


        if (telegraph != null)
        {
            telegraph.BeginTelegraph();


            telegraph.UpdateTelegraph(
                GetFirePosition(),
                aimTargetPosition,
                0f,
                false,
                obstacleLayer
            );
        }
    }


    // ==================================================
    // Charging
    // ==================================================

    private void UpdateCharging()
    {
        if (player == null)
        {
            CancelAttack();

            return;
        }


        chargeTimer +=
            Time.deltaTime;


        float safeDuration =
            Mathf.Max(
                telegraphDuration,
                0.01f
            );


        float safeLockDuration =
            Mathf.Clamp(
                aimLockDuration,
                0f,
                safeDuration
            );


        float lockStartTime =
            safeDuration
            - safeLockDuration;


        // ==========================================
        // 아직 Lock 전이라면
        //
        // Player가 사거리/시야에서 벗어날 경우
        // 공격 취소
        // ==========================================

        if (!aimLocked)
        {
            float distance =
                Vector2.Distance(
                    transform.position,
                    player.position
                );


            if (distance >
                attackRange ||
                !HasLineOfSight())
            {
                CancelAttack();

                return;
            }


            // Player를 계속 추적 조준
            aimTargetPosition =
                player.position;


            aimDirection =
                (
                    aimTargetPosition
                    - GetFirePosition()
                ).normalized;


            // ======================================
            // 발사 직전 Aim Lock
            // ======================================

            if (chargeTimer >=
                lockStartTime)
            {
                aimLocked =
                    true;


                // ==========================================
                // 이 순간 Player 위치 고정
                // ==========================================

                aimTargetPosition =
                    player.position;


                Vector2 lockedDifference =
                    aimTargetPosition
                    - GetFirePosition();


                if (lockedDifference.sqrMagnitude >
                    0.0001f)
                {
                    aimDirection =
                        lockedDifference.normalized;
                }


                if (shooterMovement != null)
                {
                    shooterMovement
                        .SetAttackSpeedMultiplier(
                            lockedMoveMultiplier
                        );
                }
            }
        }


        // ==========================================
        // Visual
        // ==========================================

        float progress =
            Mathf.Clamp01(
                chargeTimer
                / safeDuration
            );


        if (telegraph != null)
        {
            telegraph.UpdateTelegraph(
                GetFirePosition(),
                aimTargetPosition,
                progress,
                aimLocked,
                obstacleLayer
            );
        }


        // ==========================================
        // Fire
        // ==========================================

        if (chargeTimer >=
            safeDuration)
        {
            Fire();
        }
    }


    // ==================================================
    // Fire
    // ==================================================

    private void Fire()
    {
        Vector2 firePosition =
            GetFirePosition();


        // ==========================================
        // Projectile
        // ==========================================

        if (projectilePrefab != null)
        {
            GameObject projectileObject =
                Instantiate(
                    projectilePrefab,
                    firePosition,
                    Quaternion.identity
                );


            EnemyProjectile projectile =
                projectileObject
                    .GetComponent<EnemyProjectile>();


            if (projectile != null)
            {
                projectile.SetDirection(
                    aimDirection
                );
            }
        }


        // ==========================================
        // VFX
        // ==========================================

        if (telegraph != null)
        {
            telegraph.HideTelegraph();


            telegraph.PlayFireFlash(
                firePosition
            );
        }


        // ==========================================
        // Movement 복구
        // ==========================================

        if (shooterMovement != null)
        {
            shooterMovement
                .SetAttackSpeedMultiplier(
                    1f
                );
        }


        attackState =
            AttackState.Ready;


        aimLocked =
            false;


        chargeTimer =
            0f;


        // ==========================================
        // 기존 fireRate 유지
        //
        // Telegraph 시간이 추가되어도
        // 실제 발사 간격은 기존 값과
        // 최대한 동일하게 유지.
        // ==========================================

        float shotInterval =
            1f / fireRate;


        float recoveryTime =
            Mathf.Max(
                0f,
                shotInterval
                - Mathf.Max(
                    telegraphDuration,
                    0f
                )
            );


        nextAttackStartTime =
            Time.time
            + recoveryTime;
    }


    // ==================================================
    // Cancel
    // ==================================================

    private void CancelAttack()
    {
        attackState =
            AttackState.Ready;


        chargeTimer =
            0f;


        aimLocked =
            false;


        if (telegraph != null)
        {
            telegraph.HideTelegraph();
        }


        if (shooterMovement != null)
        {
            shooterMovement
                .SetAttackSpeedMultiplier(
                    1f
                );
        }


        nextAttackStartTime =
            Time.time
            + cancelRetryDelay;
    }


    // ==================================================
    // Direction
    // ==================================================

    private Vector2 GetDirectionToPlayer()
    {
        Vector2 origin =
            GetFirePosition();


        Vector2 direction =
            (Vector2)player.position
            - origin;


        if (direction.sqrMagnitude <
            0.0001f)
        {
            return Vector2.right;
        }


        return direction.normalized;
    }


    // ==================================================
    // Fire Position
    // ==================================================

    private Vector2 GetFirePosition()
    {
        if (fireOrigin != null)
        {
            return fireOrigin.position;
        }


        return transform.position;
    }


    // ==================================================
    // Line Of Sight
    // ==================================================

    private bool HasLineOfSight()
    {
        if (player == null)
            return false;


        Vector2 origin =
            GetFirePosition();


        Vector2 difference =
            (Vector2)player.position
            - origin;


        float distance =
            difference.magnitude;


        if (distance <=
            0.001f)
        {
            return true;
        }


        Vector2 direction =
            difference.normalized;


        RaycastHit2D hit =
            Physics2D.Raycast(
                origin,
                direction,
                distance,
                obstacleLayer
            );


        return hit.collider ==
            null;
    }


    // ==================================================
    // Safety
    // ==================================================

    private void OnDisable()
    {
        if (telegraph != null)
        {
            telegraph.HideTelegraph();
        }


        if (shooterMovement != null)
        {
            shooterMovement
                .SetAttackSpeedMultiplier(
                    1f
                );
        }


        attackState =
            AttackState.Ready;


        aimLocked =
            false;
    }
}