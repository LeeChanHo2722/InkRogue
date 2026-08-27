using UnityEngine;

public class EnemyShooterMovement : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 1.6f;

    public float preferredDistance = 5f;
    public float distanceTolerance = 0.8f;

    public float inkSlowMultiplier = 0.5f;


    [Header("Ink")]
    public float inkSampleRadius = 0.22f;


    [Header("Obstacle Avoidance")]
    public LayerMask obstacleLayer;

    public float detectionDistance = 1.2f;
    public float bodyRadius = 0.4f;
    public float wallPush = 0.35f;
    public float sideLockTime = 0.4f;


    private Rigidbody2D rb;

    private Transform player;


    private int avoidanceSide = 1;

    private float sideLockTimer = 0f;


    // ==================================================
    // Attack Movement
    // ==================================================

    private float attackSpeedMultiplier =
        1f;


    public float AttackSpeedMultiplier =>
        attackSpeedMultiplier;


    // ==================================================
    // Awake
    // ==================================================

    private void Awake()
    {
        rb =
            GetComponent<Rigidbody2D>();


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
    // External Attack Speed
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
    // Fixed Update
    // ==================================================

    private void FixedUpdate()
    {
        if (player == null)
        {
            rb.linearVelocity =
                Vector2.zero;

            return;
        }


        Vector2 toPlayer =
            (Vector2)player.position
            - rb.position;


        float distance =
            toPlayer.magnitude;


        Vector2 desiredDirection =
            Vector2.zero;


        // ==========================================
        // Player와 거리 유지
        // ==========================================

        if (distance >
            preferredDistance
            + distanceTolerance)
        {
            desiredDirection =
                toPlayer.normalized;
        }
        else if (distance <
                 preferredDistance
                 - distanceTolerance)
        {
            desiredDirection =
                -toPlayer.normalized;
        }


        Vector2 moveDirection =
            GetMoveDirection(
                desiredDirection
            );


        // ==========================================
        // 기본 속도
        // +
        // 공격 준비 상태 속도
        // ==========================================

        float currentSpeed =
            moveSpeed
            * attackSpeedMultiplier;


        // ==========================================
        // Player Ink 위에서는 추가 감속
        // ==========================================

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
                currentSpeed *=
                    inkSlowMultiplier;
            }
        }


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
            float distanceA =
                Vector2.Distance(
                    rb.position
                    + tangentA,

                    player.position
                );


            float distanceB =
                Vector2.Distance(
                    rb.position
                    + tangentB,

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


        return (
            tangent
            + hit.normal
            * wallPush
        ).normalized;
    }


    // ==================================================
    // Safety
    // ==================================================

    private void OnDisable()
    {
        attackSpeedMultiplier =
            1f;


        if (rb != null)
        {
            rb.linearVelocity =
                Vector2.zero;
        }
    }
}