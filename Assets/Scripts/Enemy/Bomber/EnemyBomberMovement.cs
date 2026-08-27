using UnityEngine;

public class EnemyBomberMovement : MonoBehaviour
{
    // ==================================================
    // Movement
    // ==================================================

    [Header("Movement")]

    public float moveSpeed = 2.1f;

    [Tooltip("Player와 유지하려는 거리")]
    public float preferredDistance = 5.0f;

    public float distanceTolerance = 0.65f;

    [Tooltip("적이 옆으로 움직이는 정도")]
    [Range(0f, 1f)]
    public float strafeWeight = 0.75f;

    public float strafeSwitchInterval = 1.2f;


    // ==================================================
    // Ink
    // ==================================================

    [Header("Player Ink Slow")]

    public float inkSampleRadius = 0.22f;

    public float playerInkSlowMultiplier = 0.5f;


    // ==================================================
    // Obstacle
    // ==================================================

    [Header("Obstacle")]

    public LayerMask obstacleLayer;

    public float obstacleCheckDistance = 0.9f;

    public float bodyRadius = 0.32f;


    // ==================================================
    // Runtime
    // ==================================================

    private Rigidbody2D rb;

    private Transform player;

    private EnemySpawnVisual spawnVisual;

    private float attackSpeedMultiplier = 1f;

    private float strafeDirection = 1f;

    private float nextStrafeSwitchTime;


    // ==================================================
    // Awake
    // ==================================================

    private void Awake()
    {
        rb =
            GetComponent<Rigidbody2D>();


        spawnVisual =
            GetComponentInParent<EnemySpawnVisual>();
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


        strafeDirection =
            Random.value < 0.5f
                ? -1f
                : 1f;


        nextStrafeSwitchTime =
            Time.time
            + strafeSwitchInterval;
    }


    // ==================================================
    // Fixed Update
    // ==================================================

    private void FixedUpdate()
    {
        if (rb == null ||
            player == null)
        {
            return;
        }


        // Enemy Spawn 연출 중에는 이동하지 않음
        if (spawnVisual != null &&
            !spawnVisual.IsSpawnFinished)
        {
            rb.linearVelocity =
                Vector2.zero;

            return;
        }


        UpdateStrafeDirection();


        Vector2 position =
            rb.position;


        Vector2 toPlayer =
            (Vector2)player.position
            - position;


        float distance =
            toPlayer.magnitude;


        if (distance <= 0.001f)
        {
            rb.linearVelocity =
                Vector2.zero;

            return;
        }


        Vector2 direction =
            toPlayer.normalized;


        Vector2 perpendicular =
            new Vector2(
                -direction.y,
                direction.x
            )
            * strafeDirection;


        Vector2 desiredDirection;


        // ==========================================
        // 너무 멀면 접근
        // ==========================================

        if (distance >
            preferredDistance
            + distanceTolerance)
        {
            desiredDirection =
                direction
                + perpendicular
                * strafeWeight
                * 0.35f;
        }

        // ==========================================
        // 너무 가까우면 후퇴
        // ==========================================

        else if (distance <
                 preferredDistance
                 - distanceTolerance)
        {
            desiredDirection =
                -direction
                + perpendicular
                * strafeWeight
                * 0.45f;
        }

        // ==========================================
        // 적정 거리면 옆으로 이동
        // ==========================================

        else
        {
            desiredDirection =
                perpendicular
                * strafeWeight;
        }


        if (desiredDirection.sqrMagnitude >
            0.001f)
        {
            desiredDirection.Normalize();
        }


        desiredDirection =
            AvoidObstacle(
                desiredDirection
            );


        float environmentMultiplier =
            GetEnvironmentSpeedMultiplier();


        float finalSpeed =
            moveSpeed
            * attackSpeedMultiplier
            * environmentMultiplier;


        rb.linearVelocity =
            desiredDirection
            * finalSpeed;
    }


    // ==================================================
    // Strafe
    // ==================================================

    private void UpdateStrafeDirection()
    {
        if (Time.time <
            nextStrafeSwitchTime)
        {
            return;
        }


        strafeDirection *=
            -1f;


        nextStrafeSwitchTime =
            Time.time
            + Random.Range(
                strafeSwitchInterval * 0.7f,
                strafeSwitchInterval * 1.3f
            );
    }


    // ==================================================
    // Obstacle
    // ==================================================

    private Vector2 AvoidObstacle(
        Vector2 desiredDirection)
    {
        if (desiredDirection.sqrMagnitude <
            0.001f)
        {
            return Vector2.zero;
        }


        RaycastHit2D hit =
            Physics2D.CircleCast(
                transform.position,
                bodyRadius,
                desiredDirection,
                obstacleCheckDistance,
                obstacleLayer
            );


        if (hit.collider == null)
        {
            return desiredDirection;
        }


        // 막혔으면 좌/우 중 열린 쪽으로 우회
        Vector2 left =
            new Vector2(
                -desiredDirection.y,
                desiredDirection.x
            );


        Vector2 right =
            -left;


        bool leftBlocked =
            Physics2D.CircleCast(
                transform.position,
                bodyRadius,
                left,
                obstacleCheckDistance,
                obstacleLayer
            ).collider != null;


        bool rightBlocked =
            Physics2D.CircleCast(
                transform.position,
                bodyRadius,
                right,
                obstacleCheckDistance,
                obstacleLayer
            ).collider != null;


        if (!leftBlocked &&
            rightBlocked)
        {
            return left;
        }


        if (!rightBlocked &&
            leftBlocked)
        {
            return right;
        }


        // 둘 다 비슷하면 기존 Strafe 방향
        return
            strafeDirection > 0f
                ? left
                : right;
    }


    // ==================================================
    // Ink Slow
    // ==================================================

    private float GetEnvironmentSpeedMultiplier()
    {
        if (InkMap.Instance == null)
        {
            return 1f;
        }


        InkTeam team =
            InkMap.Instance
                .GetDominantInkTeam(
                    transform.position,
                    inkSampleRadius
                );


        if (team ==
            InkTeam.Player)
        {
            return
                playerInkSlowMultiplier;
        }


        return 1f;
    }


    // ==================================================
    // Attack Movement
    // ==================================================

    public void SetAttackSpeedMultiplier(
        float multiplier)
    {
        attackSpeedMultiplier =
            Mathf.Max(
                0f,
                multiplier
            );
    }


    // ==================================================
    // Stop
    // ==================================================

    private void OnDisable()
    {
        if (rb != null)
        {
            rb.linearVelocity =
                Vector2.zero;
        }
    }
}