using UnityEngine;

public class EnemySprinklerMovement : MonoBehaviour
{
    // ==================================================
    // Movement
    // ==================================================

    [Header("Movement")]

    public float moveSpeed = 1.8f;

    [Tooltip("Player와 유지하려는 거리")]
    public float preferredDistance = 4.5f;

    public float distanceTolerance = 0.8f;

    [Tooltip("적정 거리에서 옆으로 움직이는 정도")]
    [Range(0f, 1f)]
    public float strafeWeight = 0.8f;

    public float strafeSwitchInterval = 1.5f;


    // ==================================================
    // Player Ink Slow
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
    // Knockback yields AI control for a moment; see EnemyKnockbackReceiver.
    private EnemyKnockbackReceiver knockbackReceiver;


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

        if (knockbackReceiver == null)
        {
            knockbackReceiver =
                GetComponent<EnemyKnockbackReceiver>();
        }



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

        // Knockback owns velocity while it lasts.
        if (knockbackReceiver != null &&
            knockbackReceiver.IsKnockbackActive)
        {
            return;
        }

        if (rb == null ||
            player == null)
        {
            return;
        }


        // ==========================================
        // Spawn이 끝나기 전에는 정지
        // ==========================================

        if (spawnVisual != null &&
            !spawnVisual.IsSpawnFinished)
        {
            rb.linearVelocity =
                Vector2.zero;

            return;
        }


        UpdateStrafe();


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


        Vector2 sideDirection =
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
                + sideDirection * 0.25f;
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
                + sideDirection * 0.35f;
        }

        // ==========================================
        // 적정 거리
        // → Player 주변을 천천히 선회
        // ==========================================

        else
        {
            desiredDirection =
                sideDirection
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

    private void UpdateStrafe()
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
                strafeSwitchInterval * 0.75f,
                strafeSwitchInterval * 1.25f
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


        return
            strafeDirection > 0f
                ? left
                : right;
    }


    // ==================================================
    // Player Ink Slow
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
    // Disable
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