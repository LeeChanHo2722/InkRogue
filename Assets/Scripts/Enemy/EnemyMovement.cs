using UnityEngine;

public class EnemyMovement : MonoBehaviour
{
    // ==================================================
    // Movement
    // ==================================================

    [Header("Movement")]
    public float moveSpeed = 2f;

    public float inkSlowMultiplier = 0.5f;


    // ==================================================
    // Ink
    // ==================================================

    [Header("Ink")]
    public float inkSampleRadius = 0.25f;


    // ==================================================
    // Obstacle Avoidance
    // ==================================================

    [Header("Obstacle Avoidance")]
    public LayerMask obstacleLayer;

    public float detectionDistance = 1.2f;
    public float bodyRadius = 0.6f;
    public float wallPush = 0.4f;
    public float sideLockTime = 0.4f;


    // ==================================================
    // Runtime
    // ==================================================

    private Rigidbody2D rb;
    // Knockback yields AI control for a moment; see EnemyKnockbackReceiver.
    private EnemyKnockbackReceiver knockbackReceiver;

    private Transform player;

    private int avoidanceSide = 1;
    private float sideLockTimer = 0f;


    // 공격 상태에서 사용하는 속도 배율
    private float attackSpeedMultiplier = 1f;


    // Player Ink 공격에 맞았을 때 사용하는 배율
    private float suppressionSpeedMultiplier = 1f;


    // Dash처럼 다른 Script가 Rigidbody를 직접 제어할 때
    private bool movementOverride = false;


    // ==================================================
    // Public
    // ==================================================

    public float AttackSpeedMultiplier =>
        attackSpeedMultiplier;


    public float SuppressionSpeedMultiplier =>
        suppressionSpeedMultiplier;


    public bool IsMovementOverridden =>
        movementOverride;


    // ==================================================
    // Awake
    // ==================================================

    private void Awake()
    {
        rb =
            GetComponent<Rigidbody2D>();

        if (knockbackReceiver == null)
        {
            knockbackReceiver =
                GetComponent<EnemyKnockbackReceiver>();
        }



        avoidanceSide =
            Random.value < 0.5f
                ? 1
                : -1;
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
    }


    // ==================================================
    // Fixed Update
    // ==================================================

    private void FixedUpdate()
    {

        // Knockback owns velocity while it lasts.
        if (knockbackReceiver != null &&
            knockbackReceiver.IsKnockbackActive)
        {
            return;
        }

        if (rb == null)
            return;


        // Chaser Dash 등 다른 스크립트가
        // Rigidbody를 직접 제어하는 중
        if (movementOverride)
        {
            return;
        }


        if (player == null)
        {
            rb.linearVelocity =
                Vector2.zero;

            return;
        }


        Vector2 desiredDirection =
            ((Vector2)player.position
            - rb.position)
            .normalized;


        Vector2 moveDirection =
            GetMoveDirection(
                desiredDirection
            );


        // ==========================================
        // 모든 이동 배율 결합
        // ==========================================

        float currentSpeed =
            moveSpeed
            * attackSpeedMultiplier
            * GetEnvironmentSpeedMultiplier();


        rb.linearVelocity =
            moveDirection
            * currentSpeed;


        if (sideLockTimer > 0f)
        {
            sideLockTimer -=
                Time.fixedDeltaTime;
        }
    }


    // ==================================================
    // Attack Multiplier
    // ==================================================

    public void SetAttackSpeedMultiplier(
        float multiplier)
    {
        attackSpeedMultiplier =
            Mathf.Clamp01(
                multiplier
            );
    }


    // ==================================================
    // Suppression Multiplier
    // ==================================================

    public void SetSuppressionSpeedMultiplier(
        float multiplier)
    {
        suppressionSpeedMultiplier =
            Mathf.Clamp(
                multiplier,
                0.05f,
                1f
            );
    }


    // ==================================================
    // Movement Override
    // ==================================================

    public void SetMovementOverride(
        bool value)
    {
        movementOverride =
            value;


        if (value &&
            rb != null)
        {
            rb.linearVelocity =
                Vector2.zero;
        }
    }


    // ==================================================
    // Environment Slow
    // ==================================================

    public float GetEnvironmentSpeedMultiplier()
    {
        float multiplier =
            suppressionSpeedMultiplier;


        // Player Ink 위에 있으면 추가 감속
        if (InkMap.Instance != null)
        {
            InkTeam currentInk =
                InkMap.Instance
                    .GetDominantInkTeam(
                        rb.position,
                        inkSampleRadius
                    );


            if (currentInk ==
                InkTeam.Player)
            {
                multiplier *=
                    inkSlowMultiplier;
            }
        }


        return multiplier;
    }


    // ==================================================
    // Obstacle Avoidance
    // ==================================================

    private Vector2 GetMoveDirection(
        Vector2 desiredDirection)
    {
        if (desiredDirection ==
            Vector2.zero)
        {
            return Vector2.zero;
        }


        RaycastHit2D hit =
            Physics2D.CircleCast(
                rb.position,
                bodyRadius,
                desiredDirection,
                detectionDistance,
                obstacleLayer
            );


        if (hit.collider == null)
        {
            return desiredDirection;
        }


        Vector2 tangentA =
            new Vector2(
                -hit.normal.y,
                hit.normal.x
            );


        Vector2 tangentB =
            -tangentA;


        if (sideLockTimer <= 0f)
        {
            Vector2 position =
                rb.position;


            float distanceA =
                Vector2.Distance(
                    position + tangentA,
                    player.position
                );


            float distanceB =
                Vector2.Distance(
                    position + tangentB,
                    player.position
                );


            if (distanceA <
                distanceB)
            {
                avoidanceSide =
                    1;
            }
            else if (distanceB <
                     distanceA)
            {
                avoidanceSide =
                    -1;
            }


            sideLockTimer =
                sideLockTime;
        }


        Vector2 tangent =
            avoidanceSide == 1
                ? tangentA
                : tangentB;


        Vector2 moveDirection =
            tangent
            + hit.normal
            * wallPush;


        return moveDirection.normalized;
    }


    // ==================================================
    // Safety
    // ==================================================

    private void OnDisable()
    {
        attackSpeedMultiplier =
            1f;


        suppressionSpeedMultiplier =
            1f;


        movementOverride =
            false;


        if (rb != null)
        {
            rb.linearVelocity =
                Vector2.zero;
        }
    }
}