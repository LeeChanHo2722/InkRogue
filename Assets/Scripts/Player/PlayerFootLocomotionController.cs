using UnityEngine;

public class PlayerFootLocomotionController : MonoBehaviour
{
    // ==================================================
    // References
    // ==================================================

    [Header("Controllers")]
    [SerializeField]
    private PlayerMovement playerMovement;

    [SerializeField]
    private PlayerVisualDirectionController directionController;


    [Header("Feet")]
    [SerializeField]
    private Transform leftFootAnchor;

    [SerializeField]
    private Transform rightFootAnchor;

    [SerializeField]
    private SpriteRenderer leftFootRenderer;

    [SerializeField]
    private SpriteRenderer rightFootRenderer;


    // ==================================================
    // Foot Sprites
    // ==================================================

    [Header("Foot Sprites")]
    [SerializeField]
    private Sprite footFront;

    [SerializeField]
    private Sprite footBack;

    [SerializeField]
    private Sprite footSide;


    // ==================================================
    // Walk
    // ==================================================

    [Header("Walk")]

    [Tooltip("Sprite Pixels Per Unit")]
    [SerializeField]
    private float pixelsPerUnit = 100f;


    [Tooltip("한쪽 발이 뒤에서 앞으로 이동하는 시간")]
    [SerializeField]
    private float stepDuration = 0.34f;


    [Tooltip("한 발의 뒤 → 앞 전체 보폭")]
    [SerializeField]
    private float stepLengthPixels = 18f;


    [Tooltip("앞으로 움직이는 발의 들림 높이")]
    [SerializeField]
    private float liftPixels = 3f;


    [Tooltip("Swing Foot이 몸 중심 쪽으로 들어오는 양")]
    [SerializeField]
    private float inwardPixels = 3f;


    // ==================================================
    // Body Facing Stance
    // ==================================================

    [Header("Body Facing Stance")]

    [Tooltip("SIDE에서 두 발의 좌우 차이")]
    [SerializeField]
    private float sideHorizontalPixels = 3f;


    [Tooltip("SIDE에서 가까운 발 / 먼 발의 상하 차이")]
    [SerializeField]
    private float sideVerticalPixels = 10f;


    [Tooltip("SIDE에서 먼 발 크기")]
    [Range(0.7f, 1f)]
    [SerializeField]
    private float sideFarFootScale = 0.92f;


    [Tooltip("몸 방향이 바뀔 때 발 기본 자세 전환 속도")]
    [SerializeField]
    private float stanceSmoothTime = 0.12f;


    // ==================================================
    // Smooth
    // ==================================================

    [Header("Smooth")]

    [Tooltip("WASD 방향 변화에 보행 궤도가 따라가는 시간")]
    [SerializeField]
    private float moveDirectionSmoothTime = 0.08f;


    [Tooltip("걷기 시작 / 정지 블렌드 속도")]
    [SerializeField]
    private float movementBlendSpeed = 8f;


    [Tooltip("느린 상태의 최소 보행 애니메이션 속도")]
    [SerializeField]
    private float minimumAnimationSpeed = 0.35f;


    [Tooltip("최대 보행 애니메이션 속도")]
    [SerializeField]
    private float maximumAnimationSpeed = 1.5f;


    // ==================================================
    // Sorting
    // ==================================================

    [Header("Sorting")]

    [SerializeField]
    private int nearFootOrder = 90;


    [SerializeField]
    private int farFootOrder = 10;


    // ==================================================
    // Captured FRONT Base
    // ==================================================

    private Vector3 leftFrontBase;

    private Vector3 rightFrontBase;


    private Vector3 leftOriginalScale;

    private Vector3 rightOriginalScale;


    private Vector2 frontCenter;


    // ==================================================
    // Current Facing Stance
    // ==================================================

    private Vector3 currentLeftStance;

    private Vector3 currentRightStance;


    private Vector3 currentLeftScale;

    private Vector3 currentRightScale;


    // ==================================================
    // Runtime
    // ==================================================

    private Vector2 smoothMoveDirection =
        Vector2.down;


    private Vector2 moveDirectionVelocity;


    // 0 ~ 1
    private float stepProgress = 0.5f;


    private bool leftFootIsSwing = true;


    private float moveBlend;


    private bool wasMoving;


    private bool initialized;


    // ==================================================
    // Awake
    // ==================================================

    private void Awake()
    {
        AutoFindReferences();

        CaptureFrontBase();

        initialized = true;
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


        ResetFeet();
    }


    // ==================================================
    // Late Update
    // ==================================================

    private void LateUpdate()
    {
        if (playerMovement == null ||
            directionController == null)
        {
            return;
        }


        bool isMoving =
            playerMovement.IsMoving;


        // ==========================================
        // 1. 몸이 바라보는 방향에 따른
        //    발의 기본 배치
        // ==========================================

        UpdateFacingStance();


        // ==========================================
        // 2. 실제 WASD 이동 방향
        // ==========================================

        UpdateMoveDirection();


        // ==========================================
        // 3. 이동 시작 / 정지 Blend
        // ==========================================

        float targetBlend =
            isMoving
            ? 1f
            : 0f;


        moveBlend =
            Mathf.MoveTowards(
                moveBlend,
                targetBlend,
                movementBlendSpeed
                * Time.deltaTime
            );


        // ==========================================
        // 이동 시작
        // ==========================================

        if (isMoving &&
            !wasMoving)
        {
            // 발을 극단적인 앞/뒤 위치에서
            // 시작하지 않도록 함.
            stepProgress = 0f;
        }


        // ==========================================
        // 4. Walk Phase
        //
        // 몸 방향이 바뀌어도 절대로
        // 초기화하지 않는다.
        // ==========================================

        if (isMoving)
        {
            float speedMultiplier =
                Mathf.Clamp(
                    playerMovement.MoveSpeedRatio,
                    minimumAnimationSpeed,
                    maximumAnimationSpeed
                );


            float actualStepDuration =
                stepDuration
                / speedMultiplier;


            stepProgress +=
                Time.deltaTime
                / actualStepDuration;


            // ======================================
            // 한 발의 Step 완료
            // ======================================

            if (stepProgress >= 1f)
            {
                stepProgress -= 1f;


                leftFootIsSwing =
                    !leftFootIsSwing;
            }
        }


        // ==========================================
        // 5. 몸 방향에 맞는 Sprite
        // ==========================================

        ApplyFacingSprites();


        // ==========================================
        // 6. Walk 적용
        // ==========================================

        ApplyWalk();


        wasMoving =
            isMoving;
    }


    // ==================================================
    // Facing Stance
    // ==================================================

    private void UpdateFacingStance()
    {
        if (pixelsPerUnit <= 0f)
        {
            return;
        }


        float pixel =
            1f / pixelsPerUnit;


        Vector3 targetLeft =
            leftFrontBase;


        Vector3 targetRight =
            rightFrontBase;


        Vector3 targetLeftScale =
            leftOriginalScale;


        Vector3 targetRightScale =
            rightOriginalScale;


        PlayerVisualDirectionController
            .FacingDirection facing =
            directionController.CurrentFacing;


        // ==========================================
        // FRONT
        //
        // 중요:
        //
        // 캐릭터가 카메라를 바라보면
        // 해부학적 LEFT는 화면 오른쪽,
        // 해부학적 RIGHT는 화면 왼쪽이다.
        //
        // 따라서 FRONT에서만
        // Left / Right의 기본 위치를 교환한다.
        // ==========================================

        if (facing ==
            PlayerVisualDirectionController
                .FacingDirection.Front)
        {
            targetLeft =
                rightFrontBase;


            targetRight =
                leftFrontBase;


            targetLeftScale =
                rightOriginalScale;


            targetRightScale =
                leftOriginalScale;
        }


        // ==========================================
        // BACK
        //
        // 캐릭터 Left = 화면 Left
        // 캐릭터 Right = 화면 Right
        // ==========================================

        else if (facing ==
            PlayerVisualDirectionController
                .FacingDirection.Back)
        {
            targetLeft =
                leftFrontBase;


            targetRight =
                rightFrontBase;


            targetLeftScale =
                leftOriginalScale;


            targetRightScale =
                rightOriginalScale;
        }


        // ==========================================
        // LEFT SIDE
        //
        // LeftFoot = 가까운 발
        // RightFoot = 먼 발
        //
        // SIDE는 좌우가 아니라
        // 위 / 아래로 가까이 배치.
        // ==========================================

        else if (facing ==
            PlayerVisualDirectionController
                .FacingDirection.Left)
        {
            targetLeft =
                new Vector3(
                    frontCenter.x
                    - sideHorizontalPixels
                    * pixel,

                    frontCenter.y
                    - sideVerticalPixels
                    * pixel,

                    leftFrontBase.z
                );


            targetRight =
                new Vector3(
                    frontCenter.x
                    + sideHorizontalPixels
                    * pixel,

                    frontCenter.y
                    + sideVerticalPixels
                    * pixel,

                    rightFrontBase.z
                );


            targetLeftScale =
                leftOriginalScale;


            targetRightScale =
                rightOriginalScale
                * sideFarFootScale;
        }


        // ==========================================
        // RIGHT SIDE
        //
        // RightFoot = 가까운 발
        // LeftFoot = 먼 발
        // ==========================================

        else
        {
            targetLeft =
                new Vector3(
                    frontCenter.x
                    - sideHorizontalPixels
                    * pixel,

                    frontCenter.y
                    + sideVerticalPixels
                    * pixel,

                    leftFrontBase.z
                );


            targetRight =
                new Vector3(
                    frontCenter.x
                    + sideHorizontalPixels
                    * pixel,

                    frontCenter.y
                    - sideVerticalPixels
                    * pixel,

                    rightFrontBase.z
                );


            targetLeftScale =
                leftOriginalScale
                * sideFarFootScale;


            targetRightScale =
                rightOriginalScale;
        }


        // ==========================================
        // Smooth Stance Transition
        // ==========================================

        float smooth =
            1f
            - Mathf.Exp(
                -Time.deltaTime
                / Mathf.Max(
                    0.001f,
                    stanceSmoothTime
                )
            );


        currentLeftStance =
            Vector3.Lerp(
                currentLeftStance,
                targetLeft,
                smooth
            );


        currentRightStance =
            Vector3.Lerp(
                currentRightStance,
                targetRight,
                smooth
            );


        currentLeftScale =
            Vector3.Lerp(
                currentLeftScale,
                targetLeftScale,
                smooth
            );


        currentRightScale =
            Vector3.Lerp(
                currentRightScale,
                targetRightScale,
                smooth
            );


        if (leftFootAnchor != null)
        {
            leftFootAnchor.localScale =
                currentLeftScale;
        }


        if (rightFootAnchor != null)
        {
            rightFootAnchor.localScale =
                currentRightScale;
        }
    }


    // ==================================================
    // Move Direction
    // ==================================================

    private void UpdateMoveDirection()
    {
        Vector2 targetDirection =
            playerMovement.MoveDirection;


        if (targetDirection.sqrMagnitude <
            0.001f)
        {
            return;
        }


        // ==========================================
        // 중요:
        //
        // FRONT라고 해서 X축을 뒤집지 않는다.
        //
        // 이동 방향은 항상 실제 WASD의
        // World 방향 그대로 사용한다.
        //
        // FRONT의 좌우 차이는
        // UpdateFacingStance에서
        // 발의 '정체성'으로 해결한다.
        // ==========================================

        smoothMoveDirection =
            Vector2.SmoothDamp(
                smoothMoveDirection,
                targetDirection.normalized,
                ref moveDirectionVelocity,
                moveDirectionSmoothTime
            );


        if (smoothMoveDirection.sqrMagnitude >
            0.001f)
        {
            smoothMoveDirection.Normalize();
        }
    }


    // ==================================================
    // Walk
    // ==================================================

    private void ApplyWalk()
    {
        if (leftFootAnchor == null ||
            rightFootAnchor == null)
        {
            return;
        }


        if (pixelsPerUnit <= 0f)
        {
            return;
        }


        Vector2 forward =
            smoothMoveDirection;


        if (forward.sqrMagnitude <
            0.001f)
        {
            forward =
                Vector2.down;
        }


        forward.Normalize();


        float pixel =
            1f / pixelsPerUnit;


        float halfStep =
            stepLengthPixels
            * 0.5f
            * pixel;


        // ==========================================
        // Swing Foot
        //
        // 뒤 → 앞으로
        // SmoothStep으로 시작/착지를 부드럽게.
        // ==========================================

        float swingT =
            Mathf.SmoothStep(
                0f,
                1f,
                stepProgress
            );


        float swingDistance =
            Mathf.Lerp(
                -halfStep,
                halfStep,
                swingT
            );


        // ==========================================
        // Stance Foot
        //
        // 앞 → 뒤
        // 캐릭터가 앞으로 나가는 동안
        // 지면을 뒤로 미는 느낌.
        // ==========================================

        float stanceDistance =
            Mathf.Lerp(
                halfStep,
                -halfStep,
                stepProgress
            );


        // ==========================================
        // Lift Arc
        //
        // 시작 0
        // 중간 최대
        // 착지 0
        // ==========================================

        float lift01 =
            Mathf.Sin(
                stepProgress
                * Mathf.PI
            );


        float lift =
            lift01
            * liftPixels
            * pixel;


        // ==========================================
        // 현재 Facing Stance 중심
        // ==========================================

        Vector2 stanceCenter =
            (
                new Vector2(
                    currentLeftStance.x,
                    currentLeftStance.y
                )
                +
                new Vector2(
                    currentRightStance.x,
                    currentRightStance.y
                )
            )
            * 0.5f;


        Vector2 leftTowardCenter =
            GetDirectionToCenter(
                currentLeftStance,
                stanceCenter
            );


        Vector2 rightTowardCenter =
            GetDirectionToCenter(
                currentRightStance,
                stanceCenter
            );


        // ==========================================
        // 발이 들릴 때만
        // 중앙으로 살짝 들어옴.
        // ==========================================

        float inward =
            lift01
            * inwardPixels
            * pixel;


        // ==========================================
        // LEFT SWING
        // ==========================================

        if (leftFootIsSwing)
        {
            Vector2 leftOffset =
                forward
                * swingDistance
                +
                leftTowardCenter
                * inward
                +
                Vector2.up
                * lift;


            Vector2 rightOffset =
                forward
                * stanceDistance;


            ApplyFootPosition(
                leftFootAnchor,
                currentLeftStance,
                leftOffset
            );


            ApplyFootPosition(
                rightFootAnchor,
                currentRightStance,
                rightOffset
            );
        }

        // ==========================================
        // RIGHT SWING
        // ==========================================

        else
        {
            Vector2 leftOffset =
                forward
                * stanceDistance;


            Vector2 rightOffset =
                forward
                * swingDistance
                +
                rightTowardCenter
                * inward
                +
                Vector2.up
                * lift;


            ApplyFootPosition(
                leftFootAnchor,
                currentLeftStance,
                leftOffset
            );


            ApplyFootPosition(
                rightFootAnchor,
                currentRightStance,
                rightOffset
            );
        }


        ApplyFacingSorting();
    }


    // ==================================================
    // Apply Foot Position
    // ==================================================

    private void ApplyFootPosition(
        Transform foot,
        Vector3 stanceBase,
        Vector2 movementOffset
    )
    {
        if (foot == null)
        {
            return;
        }


        Vector3 finalPosition =
            stanceBase
            +
            new Vector3(
                movementOffset.x,
                movementOffset.y,
                0f
            )
            * moveBlend;


        foot.localPosition =
            finalPosition;
    }


    // ==================================================
    // Facing Sprites
    // ==================================================

    private void ApplyFacingSprites()
    {
        if (leftFootRenderer == null ||
            rightFootRenderer == null)
        {
            return;
        }


        PlayerVisualDirectionController
            .FacingDirection facing =
            directionController.CurrentFacing;


        // ==========================================
        // FRONT
        // ==========================================

        if (facing ==
            PlayerVisualDirectionController
                .FacingDirection.Front)
        {
            leftFootRenderer.sprite =
                footFront;


            rightFootRenderer.sprite =
                footFront;


            leftFootRenderer.flipX =
                false;


            rightFootRenderer.flipX =
                false;
        }


        // ==========================================
        // BACK
        // ==========================================

        else if (facing ==
            PlayerVisualDirectionController
                .FacingDirection.Back)
        {
            leftFootRenderer.sprite =
                footBack;


            rightFootRenderer.sprite =
                footBack;


            leftFootRenderer.flipX =
                false;


            rightFootRenderer.flipX =
                false;
        }


        // ==========================================
        // LEFT SIDE
        // ==========================================

        else if (facing ==
            PlayerVisualDirectionController
                .FacingDirection.Left)
        {
            leftFootRenderer.sprite =
                footSide;


            rightFootRenderer.sprite =
                footSide;


            leftFootRenderer.flipX =
                false;


            rightFootRenderer.flipX =
                false;
        }


        // ==========================================
        // RIGHT SIDE
        // ==========================================

        else
        {
            leftFootRenderer.sprite =
                footSide;


            rightFootRenderer.sprite =
                footSide;


            leftFootRenderer.flipX =
                true;


            rightFootRenderer.flipX =
                true;
        }
    }


    // ==================================================
    // Facing Sorting
    // ==================================================

    private void ApplyFacingSorting()
    {
        if (leftFootRenderer == null ||
            rightFootRenderer == null)
        {
            return;
        }


        PlayerVisualDirectionController
            .FacingDirection facing =
            directionController.CurrentFacing;


        // ==========================================
        // LEFT SIDE
        //
        // LeftFoot = 가까운 발
        // ==========================================

        if (facing ==
            PlayerVisualDirectionController
                .FacingDirection.Left)
        {
            leftFootRenderer.sortingOrder =
                nearFootOrder;


            rightFootRenderer.sortingOrder =
                farFootOrder;
        }


        // ==========================================
        // RIGHT SIDE
        //
        // RightFoot = 가까운 발
        // ==========================================

        else if (facing ==
            PlayerVisualDirectionController
                .FacingDirection.Right)
        {
            leftFootRenderer.sortingOrder =
                farFootOrder;


            rightFootRenderer.sortingOrder =
                nearFootOrder;
        }


        // ==========================================
        // FRONT / BACK
        //
        // Swing Foot가 위로 지나간다.
        // ==========================================

        else
        {
            if (leftFootIsSwing)
            {
                leftFootRenderer.sortingOrder =
                    nearFootOrder;


                rightFootRenderer.sortingOrder =
                    farFootOrder;
            }
            else
            {
                leftFootRenderer.sortingOrder =
                    farFootOrder;


                rightFootRenderer.sortingOrder =
                    nearFootOrder;
            }
        }
    }


    // ==================================================
    // Direction To Center
    // ==================================================

    private Vector2 GetDirectionToCenter(
        Vector3 footPosition,
        Vector2 center
    )
    {
        Vector2 direction =
            center
            -
            new Vector2(
                footPosition.x,
                footPosition.y
            );


        if (direction.sqrMagnitude <
            0.001f)
        {
            return Vector2.zero;
        }


        return direction.normalized;
    }


    // ==================================================
    // Capture FRONT Base
    // ==================================================

    [ContextMenu(
        "CAPTURE - Current Feet As FRONT Base"
    )]
    private void CaptureFrontBase()
    {
        if (leftFootAnchor == null ||
            rightFootAnchor == null)
        {
            return;
        }


        leftFrontBase =
            leftFootAnchor.localPosition;


        rightFrontBase =
            rightFootAnchor.localPosition;


        leftOriginalScale =
            leftFootAnchor.localScale;


        rightOriginalScale =
            rightFootAnchor.localScale;


        frontCenter =
            (
                new Vector2(
                    leftFrontBase.x,
                    leftFrontBase.y
                )
                +
                new Vector2(
                    rightFrontBase.x,
                    rightFrontBase.y
                )
            )
            * 0.5f;


        // ==========================================
        // 현재 Stance도 FRONT 기준으로 초기화
        // ==========================================

        currentLeftStance =
            leftFrontBase;


        currentRightStance =
            rightFrontBase;


        currentLeftScale =
            leftOriginalScale;


        currentRightScale =
            rightOriginalScale;


#if UNITY_EDITOR

        UnityEditor.EditorUtility
            .SetDirty(
                this
            );

#endif
    }


    // ==================================================
    // Auto Find
    // ==================================================

    [ContextMenu(
        "AUTO FIND - Foot References"
    )]
    private void AutoFindReferences()
    {
        // ==========================================
        // Player Movement
        // ==========================================

        if (playerMovement == null)
        {
            playerMovement =
                GetComponentInParent<
                    PlayerMovement
                >();
        }


        // ==========================================
        // Direction Controller
        // ==========================================

        if (directionController == null)
        {
            directionController =
                GetComponent<
                    PlayerVisualDirectionController
                >();
        }


        // ==========================================
        // Locomotion Feet
        // ==========================================

        Transform feetRoot =
            transform.Find(
                "LocomotionFeet"
            );


        if (feetRoot == null)
        {
            Debug.LogError(
                "[FootLocomotion] LocomotionFeet을 찾을 수 없습니다.",
                this
            );

            return;
        }


        leftFootAnchor =
            feetRoot.Find(
                "LeftFootAnchor"
            );


        rightFootAnchor =
            feetRoot.Find(
                "RightFootAnchor"
            );


        // ==========================================
        // Left Foot Renderer
        // ==========================================

        if (leftFootAnchor != null)
        {
            Transform leftFoot =
                leftFootAnchor.Find(
                    "LeftFoot"
                );


            if (leftFoot != null)
            {
                leftFootRenderer =
                    leftFoot.GetComponent<
                        SpriteRenderer
                    >();
            }
        }


        // ==========================================
        // Right Foot Renderer
        // ==========================================

        if (rightFootAnchor != null)
        {
            Transform rightFoot =
                rightFootAnchor.Find(
                    "RightFoot"
                );


            if (rightFoot != null)
            {
                rightFootRenderer =
                    rightFoot.GetComponent<
                        SpriteRenderer
                    >();
            }
        }


#if UNITY_EDITOR

        UnityEditor.EditorUtility
            .SetDirty(
                this
            );

#endif


        Debug.Log(
            "[FootLocomotion] References 자동 연결 완료.",
            this
        );
    }


    // ==================================================
    // Reset Feet
    // ==================================================

    private void ResetFeet()
    {
        if (leftFootAnchor != null)
        {
            leftFootAnchor.localPosition =
                leftFrontBase;


            leftFootAnchor.localScale =
                leftOriginalScale;
        }


        if (rightFootAnchor != null)
        {
            rightFootAnchor.localPosition =
                rightFrontBase;


            rightFootAnchor.localScale =
                rightOriginalScale;
        }


        currentLeftStance =
            leftFrontBase;


        currentRightStance =
            rightFrontBase;


        currentLeftScale =
            leftOriginalScale;


        currentRightScale =
            rightOriginalScale;


        moveBlend =
            0f;


        stepProgress =
            0.5f;


        wasMoving =
            false;
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


        ResetFeet();
    }
}