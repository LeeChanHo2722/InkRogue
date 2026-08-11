using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;


    [Header("Ink")]
    public float enemyInkSlowMultiplier = 0.6f;

    public float diveMoveMultiplier = 1.6f;

    public float inkSampleRadius = 0.22f;


    [Header("Shooting")]
    public float firingMoveMultiplier = 0.35f;


    [Header("Emergency")]
    [Tooltip("Shield 파괴 상태에서 모든 이동에 적용")]
    public float emergencyMoveMultiplier = 0.55f;


    private Rigidbody2D rb;

    private Vector2 moveInput;


    private PlayerShoot playerShoot;
    private PlayerDive playerDive;
    private PlayerShield playerShield;


    private void Awake()
    {
        rb =
            GetComponent<Rigidbody2D>();


        playerShoot =
            GetComponentInChildren
            <PlayerShoot>();


        playerDive =
            GetComponent<PlayerDive>();


        playerShield =
            GetComponent<PlayerShield>();
    }


    private void Update()
    {
        moveInput =
            Vector2.zero;


        if (Keyboard.current == null)
            return;


        if (Keyboard.current.wKey.isPressed)
            moveInput.y += 1;

        if (Keyboard.current.sKey.isPressed)
            moveInput.y -= 1;

        if (Keyboard.current.aKey.isPressed)
            moveInput.x -= 1;

        if (Keyboard.current.dKey.isPressed)
            moveInput.x += 1;


        moveInput =
            moveInput.normalized;
    }


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
        // Action
        // ==========================================

        if (playerShoot != null &&
            playerShoot.IsFiring)
        {
            currentSpeed *=
                firingMoveMultiplier;
        }


        // ==========================================
        // Emergency
        // 가장 마지막에 전체 속도에 적용
        // ==========================================

        if (playerShield != null &&
            playerShield.IsEmergency)
        {
            currentSpeed *=
                emergencyMoveMultiplier;
        }


        rb.linearVelocity =
            moveInput
            * currentSpeed;
    }
}