using UnityEngine;

public class PlayerHandAimController : MonoBehaviour
{
    // ==================================================
    // References
    // ==================================================

    [Header("References")]
    [SerializeField]
    private Transform aimPivot;

    [SerializeField]
    private PlayerVisualDirectionController directionController;

    [SerializeField]
    private Transform handsRoot;

    [SerializeField]
    private Transform leftHandAimAnchor;

    [SerializeField]
    private Transform rightHandAimAnchor;


    // ==================================================
    // Aim Position
    // ==================================================

    [Header("Aim Position")]
    [SerializeField]
    private float pixelsPerUnit = 100f;

    [SerializeField]
    private float forwardPixels = 11f;

    [SerializeField]
    private float sidePixels = 6f;


    // ==================================================
    // Normal Follow
    // ==================================================

    [Header("Normal Follow")]
    [SerializeField]
    private float normalFollowSmoothTime = 0.055f;


    // ==================================================
    // Arc
    // ==================================================

    [Header("Direction Change Arc")]
    [SerializeField]
    private float arcDuration = 0.26f;

    [Range(0.2f, 0.8f)]
    [SerializeField]
    private float retractStageRatio = 0.45f;

    [Range(0.3f, 1f)]
    [SerializeField]
    private float retractRadiusMultiplier = 0.70f;

    [SerializeField]
    private float minimumArcRadiusPixels = 9f;

    [SerializeField]
    private float arcLiftPixels = 2f;


    // ==================================================
    // Aim
    // ==================================================

    private Vector2 aimDirection =
        Vector2.right;


    public Vector2 AimDirection
    {
        get { return aimDirection; }
    }


    // ==================================================
    // IMPORTANT:
    // Logical Hand World Positions
    //
    // Visual Left / Right와 무관한
    // 캐릭터 본인 기준 실제 왼손 / 오른손 위치.
    // ==================================================

    private Vector3 logicalLeftWorldPosition;

    private Vector3 logicalRightWorldPosition;


    public Vector3 LogicalLeftHandWorldPosition
    {
        get { return logicalLeftWorldPosition; }
    }


    public Vector3 LogicalRightHandWorldPosition
    {
        get { return logicalRightWorldPosition; }
    }


    // ==================================================
    // Normal Follow Velocity
    // ==================================================

    private Vector3 logicalLeftVelocity;

    private Vector3 logicalRightVelocity;


    // ==================================================
    // Facing
    // ==================================================

    private PlayerVisualDirectionController
        .FacingDirection previousFacing;

    private bool facingInitialized;


    // ==================================================
    // Arc Runtime
    // ==================================================

    private bool arcActive;

    private float arcElapsed;

    private Vector3 leftArcStartRelative;

    private Vector3 rightArcStartRelative;


    private bool initialized;


    // ==================================================
    // Awake
    // ==================================================

    private void Awake()
    {
        AutoFindReferences();

        InitializeLogicalHands();

        initialized = true;
    }


    // ==================================================
    // Enable
    // ==================================================

    private void OnEnable()
    {
        if (!initialized)
        {
            return;
        }

        InitializeLogicalHands();
    }


    // ==================================================
    // Late Update
    // ==================================================

    private void LateUpdate()
    {
        if (aimPivot == null ||
            directionController == null ||
            handsRoot == null ||
            leftHandAimAnchor == null ||
            rightHandAimAnchor == null)
        {
            return;
        }


        if (pixelsPerUnit <= 0f)
        {
            return;
        }


        // ==================================================
        // 1. Aim Direction
        // ==================================================

        Vector2 targetAim =
            new Vector2(
                aimPivot.right.x,
                aimPivot.right.y
            );


        if (targetAim.sqrMagnitude <
            0.0001f)
        {
            return;
        }


        targetAim.Normalize();

        aimDirection =
            targetAim;


        // ==================================================
        // 2. Logical Offset
        // ==================================================

        float pixel =
            1f / pixelsPerUnit;


        Vector2 aimRight =
            new Vector2(
                aimDirection.y,
                -aimDirection.x
            );


        Vector2 logicalRightOffset =
            aimDirection
            * forwardPixels
            * pixel
            +
            aimRight
            * sidePixels
            * pixel;


        Vector2 logicalLeftOffset =
            aimDirection
            * forwardPixels
            * pixel
            -
            aimRight
            * sidePixels
            * pixel;


        // ==================================================
        // 3. 현재 Facing에서
        //    논리적 손의 목표 위치 계산
        // ==================================================

        Vector3 targetLogicalRight =
            GetLogicalRightTarget(
                logicalRightOffset
            );


        Vector3 targetLogicalLeft =
            GetLogicalLeftTarget(
                logicalLeftOffset
            );


        // ==================================================
        // 4. Facing Change 확인
        // ==================================================

        UpdateFacingTransition();


        // ==================================================
        // 5. Logical Hand Animation
        // ==================================================

        if (arcActive)
        {
            UpdateLogicalArc(
                targetLogicalLeft,
                targetLogicalRight
            );
        }
        else
        {
            UpdateLogicalNormalFollow(
                targetLogicalLeft,
                targetLogicalRight
            );
        }


        // ==================================================
        // 6. Logical Hands
        //    ↓
        //    Visual Hand Anchors
        //
        // 여기서만 Visual 매핑한다.
        // ==================================================

        ApplyLogicalHandsToVisuals();


        leftHandAimAnchor.localRotation =
            Quaternion.identity;

        rightHandAimAnchor.localRotation =
            Quaternion.identity;

        leftHandAimAnchor.localScale =
            Vector3.one;

        rightHandAimAnchor.localScale =
            Vector3.one;
    }


    // ==================================================
    // Logical Right Target
    // ==================================================

    private Vector3 GetLogicalRightTarget(
        Vector2 logicalOffset
    )
    {
        Transform visualAnchor =
            GetVisualAnchorForLogicalRight();


        if (visualAnchor == null ||
            visualAnchor.parent == null)
        {
            return logicalRightWorldPosition;
        }


        return visualAnchor.parent.position
            +
            new Vector3(
                logicalOffset.x,
                logicalOffset.y,
                0f
            );
    }


    // ==================================================
    // Logical Left Target
    // ==================================================

    private Vector3 GetLogicalLeftTarget(
        Vector2 logicalOffset
    )
    {
        Transform visualAnchor =
            GetVisualAnchorForLogicalLeft();


        if (visualAnchor == null ||
            visualAnchor.parent == null)
        {
            return logicalLeftWorldPosition;
        }


        return visualAnchor.parent.position
            +
            new Vector3(
                logicalOffset.x,
                logicalOffset.y,
                0f
            );
    }


    // ==================================================
    // Logical Right → Visual Anchor
    // ==================================================

    private Transform GetVisualAnchorForLogicalRight()
    {
        PlayerVisualDirectionController
            .FacingDirection facing =
            directionController.CurrentFacing;


        // FRONT:
        // 캐릭터 오른손 = 화면 Left Visual
        if (facing ==
            PlayerVisualDirectionController
                .FacingDirection.Front)
        {
            return leftHandAimAnchor;
        }


        // BACK / LEFT / RIGHT
        return rightHandAimAnchor;
    }


    // ==================================================
    // Logical Left → Visual Anchor
    // ==================================================

    private Transform GetVisualAnchorForLogicalLeft()
    {
        PlayerVisualDirectionController
            .FacingDirection facing =
            directionController.CurrentFacing;


        if (facing ==
            PlayerVisualDirectionController
                .FacingDirection.Front)
        {
            return rightHandAimAnchor;
        }


        return leftHandAimAnchor;
    }


    // ==================================================
    // Apply Logical → Visual
    // ==================================================

    private void ApplyLogicalHandsToVisuals()
    {
        PlayerVisualDirectionController
            .FacingDirection facing =
            directionController.CurrentFacing;


        // ==================================================
        // FRONT
        //
        // Logical Right → Left Visual
        // Logical Left  → Right Visual
        // ==================================================

        if (facing ==
            PlayerVisualDirectionController
                .FacingDirection.Front)
        {
            leftHandAimAnchor.position =
                logicalRightWorldPosition;


            rightHandAimAnchor.position =
                logicalLeftWorldPosition;
        }

        // ==================================================
        // BACK / LEFT / RIGHT
        // ==================================================

        else
        {
            leftHandAimAnchor.position =
                logicalLeftWorldPosition;


            rightHandAimAnchor.position =
                logicalRightWorldPosition;
        }
    }


    // ==================================================
    // Facing Transition
    // ==================================================

    private void UpdateFacingTransition()
    {
        PlayerVisualDirectionController
            .FacingDirection currentFacing =
            directionController.CurrentFacing;


        if (!facingInitialized)
        {
            previousFacing =
                currentFacing;

            facingInitialized =
                true;

            return;
        }


        if (currentFacing ==
            previousFacing)
        {
            return;
        }


        BeginArcTransition();


        previousFacing =
            currentFacing;
    }


    // ==================================================
    // Begin Arc
    // ==================================================

    private void BeginArcTransition()
    {
        arcActive =
            true;


        arcElapsed =
            0f;


        Vector3 center =
            handsRoot.position;


        // ==================================================
        // 중요:
        //
        // Visual Hand가 아니라
        // Logical Hand의 현재 위치를 저장.
        //
        // 그래서 FRONT → RIGHT에서
        // Visual 오브젝트가 바뀌어도
        // 실제 오른손의 위치는 끊기지 않는다.
        // ==================================================

        leftArcStartRelative =
            logicalLeftWorldPosition
            - center;


        rightArcStartRelative =
            logicalRightWorldPosition
            - center;


        logicalLeftVelocity =
            Vector3.zero;


        logicalRightVelocity =
            Vector3.zero;
    }


    // ==================================================
    // Update Logical Arc
    // ==================================================

    private void UpdateLogicalArc(
        Vector3 targetLeft,
        Vector3 targetRight
    )
    {
        if (arcDuration <= 0.0001f)
        {
            logicalLeftWorldPosition =
                targetLeft;


            logicalRightWorldPosition =
                targetRight;


            arcActive =
                false;


            return;
        }


        arcElapsed +=
            Time.deltaTime;


        float t =
            Mathf.Clamp01(
                arcElapsed
                / arcDuration
            );


        Vector3 center =
            handsRoot.position;


        Vector3 leftTargetRelative =
            targetLeft
            - center;


        Vector3 rightTargetRelative =
            targetRight
            - center;


        logicalLeftWorldPosition =
            center
            +
            EvaluateTwoStageArc(
                leftArcStartRelative,
                leftTargetRelative,
                t
            );


        logicalRightWorldPosition =
            center
            +
            EvaluateTwoStageArc(
                rightArcStartRelative,
                rightTargetRelative,
                t
            );


        float lift =
            Mathf.Sin(
                t * Mathf.PI
            )
            * arcLiftPixels
            / pixelsPerUnit;


        Vector3 liftOffset =
            new Vector3(
                0f,
                lift,
                0f
            );


        logicalLeftWorldPosition +=
            liftOffset;


        logicalRightWorldPosition +=
            liftOffset;


        if (t >= 1f)
        {
            logicalLeftWorldPosition =
                targetLeft;


            logicalRightWorldPosition =
                targetRight;


            arcActive =
                false;


            arcElapsed =
                0f;
        }
    }


    // ==================================================
    // Two Stage Arc
    // ==================================================

    private Vector3 EvaluateTwoStageArc(
        Vector3 startRelative,
        Vector3 targetRelative,
        float t
    )
    {
        Vector2 start2D =
            new Vector2(
                startRelative.x,
                startRelative.y
            );


        Vector2 target2D =
            new Vector2(
                targetRelative.x,
                targetRelative.y
            );


        float startRadius =
            start2D.magnitude;


        float targetRadius =
            target2D.magnitude;


        if (startRadius <
            0.0001f)
        {
            startRadius =
                targetRadius;
        }


        if (targetRadius <
            0.0001f)
        {
            targetRadius =
                startRadius;
        }


        float startAngle =
            Mathf.Atan2(
                start2D.y,
                start2D.x
            )
            * Mathf.Rad2Deg;


        float targetAngle =
            Mathf.Atan2(
                target2D.y,
                target2D.x
            )
            * Mathf.Rad2Deg;


        float angleDifference =
            Mathf.DeltaAngle(
                startAngle,
                targetAngle
            );


        float midAngle =
            startAngle
            +
            angleDifference
            * 0.55f;


        float minimumRadius =
            minimumArcRadiusPixels
            / pixelsPerUnit;


        float midRadius =
            Mathf.Max(
                minimumRadius,
                Mathf.Min(
                    startRadius,
                    targetRadius
                )
                * retractRadiusMultiplier
            );


        float currentAngle;

        float currentRadius;


        // ==================================================
        // Stage 1
        // ==================================================

        if (t <
            retractStageRatio)
        {
            float stageT =
                t
                /
                Mathf.Max(
                    0.0001f,
                    retractStageRatio
                );


            stageT =
                Smooth01(
                    stageT
                );


            currentAngle =
                Mathf.Lerp(
                    startAngle,
                    midAngle,
                    stageT
                );


            currentRadius =
                Mathf.Lerp(
                    startRadius,
                    midRadius,
                    stageT
                );
        }

        // ==================================================
        // Stage 2
        // ==================================================

        else
        {
            float stageT =
                (
                    t
                    - retractStageRatio
                )
                /
                Mathf.Max(
                    0.0001f,
                    1f
                    - retractStageRatio
                );


            stageT =
                Smooth01(
                    stageT
                );


            currentAngle =
                Mathf.Lerp(
                    midAngle,
                    startAngle
                    + angleDifference,
                    stageT
                );


            currentRadius =
                Mathf.Lerp(
                    midRadius,
                    targetRadius,
                    stageT
                );
        }


        float rad =
            currentAngle
            * Mathf.Deg2Rad;


        return new Vector3(
            Mathf.Cos(rad)
                * currentRadius,

            Mathf.Sin(rad)
                * currentRadius,

            Mathf.Lerp(
                startRelative.z,
                targetRelative.z,
                t
            )
        );
    }


    // ==================================================
    // Normal Follow
    // ==================================================

    private void UpdateLogicalNormalFollow(
        Vector3 targetLeft,
        Vector3 targetRight
    )
    {
        logicalLeftWorldPosition =
            Vector3.SmoothDamp(
                logicalLeftWorldPosition,
                targetLeft,
                ref logicalLeftVelocity,
                normalFollowSmoothTime
            );


        logicalRightWorldPosition =
            Vector3.SmoothDamp(
                logicalRightWorldPosition,
                targetRight,
                ref logicalRightVelocity,
                normalFollowSmoothTime
            );
    }


    // ==================================================
    // Smooth
    // ==================================================

    private float Smooth01(
        float value
    )
    {
        value =
            Mathf.Clamp01(
                value
            );


        return value
            * value
            * (
                3f
                - 2f
                * value
            );
    }


    // ==================================================
    // Initialize Logical Hands
    // ==================================================

    private void InitializeLogicalHands()
    {
        if (directionController == null ||
            leftHandAimAnchor == null ||
            rightHandAimAnchor == null)
        {
            return;
        }


        PlayerVisualDirectionController
            .FacingDirection facing =
            directionController.CurrentFacing;


        if (facing ==
            PlayerVisualDirectionController
                .FacingDirection.Front)
        {
            logicalRightWorldPosition =
                leftHandAimAnchor.position;


            logicalLeftWorldPosition =
                rightHandAimAnchor.position;
        }
        else
        {
            logicalRightWorldPosition =
                rightHandAimAnchor.position;


            logicalLeftWorldPosition =
                leftHandAimAnchor.position;
        }


        logicalLeftVelocity =
            Vector3.zero;


        logicalRightVelocity =
            Vector3.zero;


        arcActive =
            false;


        arcElapsed =
            0f;


        facingInitialized =
            false;
    }


    // ==================================================
    // Auto Find
    // ==================================================

    [ContextMenu(
        "AUTO FIND - Hand Aim References"
    )]
    private void AutoFindReferences()
    {
        if (directionController == null)
        {
            directionController =
                GetComponent<
                    PlayerVisualDirectionController
                >();
        }


        Transform playerRoot =
            transform.root;


        if (aimPivot == null &&
            playerRoot != null)
        {
            aimPivot =
                playerRoot.Find(
                    "AimPivot"
                );
        }


        Transform visualOffset =
            transform.Find(
                "VisualOffset"
            );


        if (visualOffset == null)
        {
            Debug.LogError(
                "[HandAim] VisualOffset을 찾지 못했습니다.",
                this
            );

            return;
        }


        handsRoot =
            visualOffset.Find(
                "Hands"
            );


        if (handsRoot == null)
        {
            Debug.LogError(
                "[HandAim] Hands를 찾지 못했습니다.",
                this
            );

            return;
        }


        Transform leftHandAnchor =
            handsRoot.Find(
                "LeftHandAnchor"
            );


        Transform rightHandAnchor =
            handsRoot.Find(
                "RightHandAnchor"
            );


        if (leftHandAnchor != null)
        {
            leftHandAimAnchor =
                leftHandAnchor.Find(
                    "LeftHandAimAnchor"
                );
        }


        if (rightHandAnchor != null)
        {
            rightHandAimAnchor =
                rightHandAnchor.Find(
                    "RightHandAimAnchor"
                );
        }


#if UNITY_EDITOR

        UnityEditor.EditorUtility.SetDirty(
            this
        );

#endif


        Debug.Log(
            "[HandAim] Logical Hand References 연결 완료.",
            this
        );
    }


    // ==================================================
    // Disable
    // ==================================================

    private void OnDisable()
    {
        arcActive =
            false;


        arcElapsed =
            0f;


        logicalLeftVelocity =
            Vector3.zero;


        logicalRightVelocity =
            Vector3.zero;
    }
}