using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    // ==================================================
    // Movement
    // ==================================================

    [Header("Movement")]

    public float moveSpeed = 5f;


    // ==================================================
    // Ink
    // ==================================================

    [Header("Ink")]

    public float enemyInkSlowMultiplier = 0.6f;

    public float diveMoveMultiplier = 1.6f;

    public float inkSampleRadius = 0.22f;


    // ==================================================
    // Shooting
    // ==================================================

    [Header("Shooting")]

    [Tooltip(
        "Shooter 사용 중 이동속도 배율"
    )]
    public float firingMoveMultiplier = 0.35f;


    // ==================================================
    // Emergency
    // ==================================================

    [Header("Emergency")]

    [Tooltip(
        "Shield 파괴 상태에서 모든 이동에 적용"
    )]
    public float emergencyMoveMultiplier = 0.55f;


    // ==================================================
    // Internal
    // ==================================================

    private Rigidbody2D rb;

    private Vector2 moveInput;


    private ShooterWeaponBehaviour
        shooterWeaponBehaviour;


    private PlayerDive playerDive;

    private PlayerShield playerShield;


    // ==================================================
    // Visual Motion Info
    // ==================================================

    public bool IsMoving
    {
        get
        {
            return
                moveInput.sqrMagnitude >
                0.001f;
        }
    }


    public float CurrentMoveSpeed
    {
        get
        {
            if (rb == null)
            {
                return 0f;
            }


            return
                rb.linearVelocity.magnitude;
        }
    }


    public float MoveSpeedRatio
    {
        get
        {
            if (moveSpeed <= 0.0001f)
            {
                return 0f;
            }


            return
                CurrentMoveSpeed
                /
                moveSpeed;
        }
    }


    public Vector2 MoveDirection
    {
        get
        {
            // 실제 Rigidbody 이동 방향 우선
            if (rb != null &&
                rb.linearVelocity.sqrMagnitude >
                0.001f)
            {
                return
                    rb.linearVelocity.normalized;
            }


            // 아직 Physics Step이 오지 않았다면
            // 현재 입력 방향 사용
            if (moveInput.sqrMagnitude >
                0.001f)
            {
                return
                    moveInput.normalized;
            }


            return Vector2.zero;
        }
    }


    // ==================================================
    // Awake
    // ==================================================

    private void Awake()
    {
        rb =
            GetComponent<Rigidbody2D>();


        shooterWeaponBehaviour =
            GetComponentInChildren<
                ShooterWeaponBehaviour
            >(
                true
            );


        playerDive =
            GetComponent<
                PlayerDive
            >();


        playerShield =
            GetComponent<
                PlayerShield
            >();
    }


    // ==================================================
    // Update
    // ==================================================

    private void Update()
    {
        moveInput =
            Vector2.zero;


        if (Keyboard.current == null)
        {
            return;
        }


        if (Keyboard.current
            .wKey
            .isPressed)
        {
            moveInput.y +=
                1f;
        }


        if (Keyboard.current
            .sKey
            .isPressed)
        {
            moveInput.y -=
                1f;
        }


        if (Keyboard.current
            .aKey
            .isPressed)
        {
            moveInput.x -=
                1f;
        }


        if (Keyboard.current
            .dKey
            .isPressed)
        {
            moveInput.x +=
                1f;
        }


        moveInput =
            moveInput.normalized;
    }


    // ==================================================
    // Fixed Update
    // ==================================================

    private void FixedUpdate()
    {
        float currentSpeed =
            moveSpeed;


        InkTeam currentInk =
            InkTeam.Neutral;


        if (InkMap.Instance != null)
        {
            currentInk =
                InkMap.Instance
                    .GetDominantInkTeam(
                        rb.position,
                        inkSampleRadius
                    );
        }


        // ==========================================
        // Form / Ground
        // ==========================================

        if (playerDive != null &&
            playerDive.IsDiving)
        {
            currentSpeed *=
                diveMoveMultiplier;
        }
        else
        {
            if (currentInk ==
                InkTeam.Enemy)
            {
                currentSpeed *=
                    enemyInkSlowMultiplier;
            }
        }


        // ==========================================
        // Shooter 사용 중 이동 감속
        //
        // 어느 Slot에 Shooter가 있든
        // 실제 Shooter가 발사 중이면 적용.
        // ==========================================

        if (shooterWeaponBehaviour != null &&
            shooterWeaponBehaviour.IsUsing)
        {
            currentSpeed *=
                firingMoveMultiplier;
        }


        // ==========================================
        // Emergency
        // ==========================================

        if (playerShield != null &&
            playerShield.IsEmergency)
        {
            currentSpeed *=
                emergencyMoveMultiplier;
        }


        // ==========================================
        // Movement
        // ==========================================

        rb.linearVelocity =
            moveInput
            *
            currentSpeed;
    }
}