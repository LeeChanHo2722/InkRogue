using UnityEngine;

public class PlayerVisualMotionController : MonoBehaviour
{
    // ==================================================
    // References
    // ==================================================

    [Header("Direction")]

    [SerializeField]
    private PlayerVisualDirectionController directionController;


    [Header("Movement")]

    [SerializeField]
    private PlayerMovement playerMovement;


    [Header("Body Parts")]

    [SerializeField]
    private Transform head;


    [SerializeField]
    private Transform eyes;


    [SerializeField]
    private Transform torso;


    [SerializeField]
    private Transform scarfBand;


    [SerializeField]
    private Transform scarfTail;


    [SerializeField]
    private Transform strap;


    // ==================================================
    // Hands
    // 실제 Hand만 애니메이션.
    // Anchor는 Direction Controller가 관리.
    // ==================================================

    [Header("Animated Hands")]

    [SerializeField]
    private Transform leftHand;


    [SerializeField]
    private Transform rightHand;


    // ==================================================
    // Idle
    // ==================================================

    [Header("Idle")]

    [Tooltip("Sprite Pixels Per Unit")]
    [SerializeField]
    private float pixelsPerUnit =
        100f;


    [Tooltip("Idle 상하 이동량")]
    [SerializeField]
    private int idleBobPixels =
        8;


    [Tooltip("Idle 한 사이클 속도")]
    [SerializeField]
    private float idleSpeed =
        0.5f;


    // ==================================================
    // Idle Follow Delay
    // ==================================================

    [Header("Idle Follow Delay")]

    [SerializeField]
    private float torsoDelay =
        0f;


    [SerializeField]
    private float scarfBandDelay =
        0.18f;


    [SerializeField]
    private float strapDelay =
        0.28f;


    [SerializeField]
    private float headDelay =
        0.42f;


    [SerializeField]
    private float eyesDelay =
        0.42f;


    [SerializeField]
    private float handsDelay =
        0.62f;


    [SerializeField]
    private float scarfTailDelay =
        0.82f;


    // ==================================================
    // Move
    // ==================================================

    [Header("Move Upper Body")]

    [Tooltip("초당 상체 보행 반응 사이클")]
    [SerializeField]
    private float moveCyclesPerSecond =
        1.15f;


    [Tooltip("이동 중 몸통 상하 움직임")]
    [SerializeField]
    private int moveBodyBobPixels =
        3;


    [Tooltip("손의 추가 움직임")]
    [SerializeField]
    private int moveHandSwingPixels =
        3;


    [Tooltip("느린 이동에서도 최소 애니메이션 속도")]
    [SerializeField]
    private float minimumMoveAnimationSpeed =
        0.35f;


    [Tooltip("최대 애니메이션 속도 배율")]
    [SerializeField]
    private float maximumMoveAnimationSpeed =
        1.5f;


    // ==================================================
    // Move Follow Delay
    // ==================================================

    [Header("Move Follow Delay")]

    [SerializeField]
    private float moveScarfBandDelay =
        0.08f;


    [SerializeField]
    private float moveStrapDelay =
        0.13f;


    [SerializeField]
    private float moveHeadDelay =
        0.20f;


    [SerializeField]
    private float moveHandsDelay =
        0.27f;


    [SerializeField]
    private float moveScarfTailDelay =
        0.40f;


    // ==================================================
    // Fixed Base Pose
    // ==================================================

    private Vector3 headBase;

    private Vector3 eyesBase;

    private Vector3 torsoBase;

    private Vector3 scarfBandBase;


    private Vector3 leftHandBase;

    private Vector3 rightHandBase;


    // ==================================================
    // Direction Dependent Base
    // ==================================================

    private Vector3 scarfTailBase;

    private Vector3 strapBase;


    // ==================================================
    // Runtime State
    // ==================================================

    private PlayerVisualDirectionController.FacingDirection
        lastFacing;


    private bool initialized;

    private bool wasMoving;


    private float idleElapsed;

    private float moveElapsed;


    // ==================================================
    // Awake
    // ==================================================

    private void Awake()
    {
        AutoFindReferences();


        CaptureFixedBasePose();


        initialized =
            true;
    }


    // ==================================================
    // On Enable
    // ==================================================

    private void OnEnable()
    {
        if (!initialized)
        {
            return;
        }


        // ==========================================
        // 사망/부활 후 누적 위치 제거
        // ==========================================

        RestoreFixedBasePose();


        idleElapsed =
            0f;


        moveElapsed =
            0f;


        wasMoving =
            false;


        // ==========================================
        // 현재 Direction Pose 재적용
        // ==========================================

        if (directionController != null)
        {
            directionController
                .ForceApply(
                    directionController.CurrentFacing
                );


            lastFacing =
                directionController.CurrentFacing;


            CaptureDirectionalBasePose();
        }
    }


    // ==================================================
    // Late Update
    // ==================================================

    private void LateUpdate()
    {
        if (directionController == null)
        {
            return;
        }


        // ==========================================
        // Direction Changed
        // ==========================================

        if (directionController.CurrentFacing !=
            lastFacing)
        {
            lastFacing =
                directionController.CurrentFacing;


            CaptureDirectionalBasePose();
        }


        bool isMoving =
            playerMovement != null &&
            playerMovement.IsMoving;


        // ==========================================
        // MOVE
        // ==========================================

        if (isMoving)
        {
            if (!wasMoving)
            {
                moveElapsed =
                    0f;
            }


            float speedMultiplier =
                playerMovement != null
                ? playerMovement.MoveSpeedRatio
                : 1f;


            speedMultiplier =
                Mathf.Clamp(
                    speedMultiplier,
                    minimumMoveAnimationSpeed,
                    maximumMoveAnimationSpeed
                );


            moveElapsed +=
                Time.deltaTime
                * speedMultiplier;


            ApplyMoveMotion();
        }

        // ==========================================
        // IDLE
        // ==========================================

        else
        {
            if (wasMoving)
            {
                idleElapsed =
                    0f;
            }


            idleElapsed +=
                Time.deltaTime;


            ApplyIdleMotion();
        }


        wasMoving =
            isMoving;
    }


    // ==================================================
    // Idle
    // ==================================================

    private void ApplyIdleMotion()
    {
        float torsoBob =
            GetIdleBob(
                torsoDelay
            );


        float bandBob =
            GetIdleBob(
                scarfBandDelay
            );


        float strapBob =
            GetIdleBob(
                strapDelay
            );


        float headBob =
            GetIdleBob(
                headDelay
            );


        float eyesBob =
            GetIdleBob(
                eyesDelay
            );


        float handsBob =
            GetIdleBob(
                handsDelay
            );


        float tailBob =
            GetIdleBob(
                scarfTailDelay
            );


        // ==========================================
        // Torso
        // ==========================================

        SetPosition(
            torso,
            torsoBase
            + Vector3.up
            * torsoBob
        );


        // ==========================================
        // Scarf Band
        // ==========================================

        SetPosition(
            scarfBand,
            scarfBandBase
            + Vector3.up
            * bandBob
        );


        // ==========================================
        // Strap
        // ==========================================

        SetPosition(
            strap,
            strapBase
            + Vector3.up
            * strapBob
        );


        // ==========================================
        // Head / Eyes
        // ==========================================

        SetPosition(
            head,
            headBase
            + Vector3.up
            * headBob
        );


        SetPosition(
            eyes,
            eyesBase
            + Vector3.up
            * eyesBob
        );


        // ==========================================
        // Hands
        // ==========================================

        SetPosition(
            leftHand,
            leftHandBase
            + Vector3.up
            * handsBob
        );


        SetPosition(
            rightHand,
            rightHandBase
            + Vector3.up
            * handsBob
        );


        // ==========================================
        // Tail
        // ==========================================

        SetPosition(
            scarfTail,
            scarfTailBase
            + Vector3.up
            * tailBob
        );
    }


    // ==================================================
    // Move Upper Body
    // ==================================================

    private void ApplyMoveMotion()
    {
        // ==========================================
        // Torso
        //
        // 발은 별도 Locomotion Controller가 처리.
        // 상체는 보행에 맞춰 묵직하게 반응.
        // ==========================================

        float torsoBob =
            GetMoveBob(
                0f
            );


        SetPosition(
            torso,
            torsoBase
            + Vector3.up
            * torsoBob
        );


        // ==========================================
        // Scarf Band
        // ==========================================

        float bandBob =
            GetMoveBob(
                moveScarfBandDelay
            );


        SetPosition(
            scarfBand,
            scarfBandBase
            + Vector3.up
            * bandBob
        );


        // ==========================================
        // Strap
        // ==========================================

        float strapBob =
            GetMoveBob(
                moveStrapDelay
            );


        SetPosition(
            strap,
            strapBase
            + Vector3.up
            * strapBob
        );


        // ==========================================
        // Head + Eyes
        // ==========================================

        float headBob =
            GetMoveBob(
                moveHeadDelay
            );


        SetPosition(
            head,
            headBase
            + Vector3.up
            * headBob
        );


        SetPosition(
            eyes,
            eyesBase
            + Vector3.up
            * headBob
        );


        // ==========================================
        // Hands
        // ==========================================

        float handBodyBob =
            GetMoveBob(
                moveHandsDelay
            );


        float handPhase =
            GetMovePhase(
                moveHandsDelay
            );


        float handWave =
            Mathf.Sin(
                handPhase
            );


        float leftSwing =
            QuantizePixels(
                Mathf.Max(
                    0f,
                    -handWave
                )
                * moveHandSwingPixels
            );


        float rightSwing =
            QuantizePixels(
                Mathf.Max(
                    0f,
                    handWave
                )
                * moveHandSwingPixels
            );


        SetPosition(
            leftHand,
            leftHandBase
            + Vector3.up
            * (
                handBodyBob
                + leftSwing
            )
        );


        SetPosition(
            rightHand,
            rightHandBase
            + Vector3.up
            * (
                handBodyBob
                + rightSwing
            )
        );


        // ==========================================
        // Scarf Tail
        // ==========================================

        float tailBob =
            GetMoveBob(
                moveScarfTailDelay
            );


        SetPosition(
            scarfTail,
            scarfTailBase
            + Vector3.up
            * tailBob
        );
    }


    // ==================================================
    // Idle Bob
    // ==================================================

    private float GetIdleBob(
        float delaySeconds
    )
    {
        float t =
            idleElapsed
            - delaySeconds;


        if (t <= 0f)
        {
            return 0f;
        }


        // ==========================================
        // 0 → 1 → 0
        // ==========================================

        float wave =
            (
                1f -
                Mathf.Cos(
                    t
                    * idleSpeed
                    * Mathf.PI
                    * 2f
                )
            )
            * 0.5f;


        return QuantizePixels(
            wave
            * idleBobPixels
        );
    }


    // ==================================================
    // Move Bob
    // ==================================================

    private float GetMoveBob(
        float delaySeconds
    )
    {
        float phase =
            GetMovePhase(
                delaySeconds
            );


        float wave =
            Mathf.Abs(
                Mathf.Sin(
                    phase
                )
            );


        return QuantizePixels(
            wave
            * moveBodyBobPixels
        );
    }


    // ==================================================
    // Move Phase
    // ==================================================

    private float GetMovePhase(
        float delaySeconds
    )
    {
        float t =
            Mathf.Max(
                0f,
                moveElapsed
                - delaySeconds
            );


        return t
            * moveCyclesPerSecond
            * Mathf.PI
            * 2f;
    }


    // ==================================================
    // Pixel Quantization
    // ==================================================

    private float QuantizePixels(
        float pixelAmount
    )
    {
        if (pixelsPerUnit <= 0f)
        {
            return 0f;
        }


        float pixels =
            Mathf.Round(
                pixelAmount
            );


        return pixels
            / pixelsPerUnit;
    }


    // ==================================================
    // Capture Fixed Base
    // ==================================================

    private void CaptureFixedBasePose()
    {
        headBase =
            GetPosition(
                head
            );


        eyesBase =
            GetPosition(
                eyes
            );


        torsoBase =
            GetPosition(
                torso
            );


        scarfBandBase =
            GetPosition(
                scarfBand
            );


        leftHandBase =
            GetPosition(
                leftHand
            );


        rightHandBase =
            GetPosition(
                rightHand
            );
    }


    // ==================================================
    // Capture Direction Dependent Base
    // ==================================================

    private void CaptureDirectionalBasePose()
    {
        scarfTailBase =
            GetPosition(
                scarfTail
            );


        strapBase =
            GetPosition(
                strap
            );
    }


    // ==================================================
    // Restore Fixed Base
    // ==================================================

    private void RestoreFixedBasePose()
    {
        SetPosition(
            head,
            headBase
        );


        SetPosition(
            eyes,
            eyesBase
        );


        SetPosition(
            torso,
            torsoBase
        );


        SetPosition(
            scarfBand,
            scarfBandBase
        );


        SetPosition(
            leftHand,
            leftHandBase
        );


        SetPosition(
            rightHand,
            rightHandBase
        );
    }


    // ==================================================
    // Auto Find
    // ==================================================

    [ContextMenu(
        "AUTO FIND - Motion References"
    )]
    private void AutoFindReferences()
    {
        // ==========================================
        // Direction
        // ==========================================

        if (directionController == null)
        {
            directionController =
                GetComponent<
                    PlayerVisualDirectionController
                >();
        }


        // ==========================================
        // Movement
        // ==========================================

        if (playerMovement == null)
        {
            playerMovement =
                GetComponentInParent<
                    PlayerMovement
                >();
        }


        // ==========================================
        // Visual Offset
        // ==========================================

        Transform visualOffset =
            transform.Find(
                "VisualOffset"
            );


        if (visualOffset == null)
        {
            Debug.LogError(
                "[PlayerVisualMotion] VisualOffset을 찾을 수 없습니다.",
                this
            );

            return;
        }


        // ==========================================
        // Body
        // ==========================================

        head =
            visualOffset.Find(
                "Head"
            );


        eyes =
            visualOffset.Find(
                "Eyes"
            );


        torso =
            visualOffset.Find(
                "Torso"
            );


        scarfBand =
            visualOffset.Find(
                "ScarfBand"
            );


        scarfTail =
            visualOffset.Find(
                "ScarfTail"
            );


        strap =
            visualOffset.Find(
                "Strap"
            );


        // ==========================================
        // Hands
        // ==========================================

        leftHand =
            visualOffset.Find(
                "Hands/LeftHandAnchor/LeftHand"
            );


        rightHand =
            visualOffset.Find(
                "Hands/RightHandAnchor/RightHand"
            );


        Debug.Log(
            "[PlayerVisualMotion] References 자동 연결 완료.",
            this
        );
    }


    // ==================================================
    // Helpers
    // ==================================================

    private Vector3 GetPosition(
        Transform target
    )
    {
        if (target == null)
        {
            return Vector3.zero;
        }


        return target.localPosition;
    }


    private void SetPosition(
        Transform target,
        Vector3 position
    )
    {
        if (target == null)
        {
            return;
        }


        target.localPosition =
            position;
    }


    // ==================================================
    // Disable / Death
    // ==================================================

    private void OnDisable()
    {
        if (!initialized)
        {
            return;
        }


        RestoreFixedBasePose();


        SetPosition(
            scarfTail,
            scarfTailBase
        );


        SetPosition(
            strap,
            strapBase
        );
    }
}