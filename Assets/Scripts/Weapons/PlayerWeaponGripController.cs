using UnityEngine;

public class PlayerWeaponGripController : MonoBehaviour
{
    // ==================================================
    // References
    // ==================================================

    [Header("References")]

    [SerializeField]
    private PlayerVisualDirectionController
        directionController;


    [SerializeField]
    private Transform rightGripPoint;


    [SerializeField]
    private Transform leftGripPoint;


    // ==================================================
    // RIGHT Grip Poses
    //
    // 기존 오른손 데이터.
    // 필드 이름을 유지해서 기존 Capture 값 보존.
    // ==================================================

    [Header("Right Grip - FRONT")]

    [SerializeField]
    private Vector3 rightFrontPosition;


    [Header("Right Grip - BACK")]

    [SerializeField]
    private Vector3 rightBackPosition;


    [Header("Right Grip - LEFT")]

    [SerializeField]
    private Vector3 rightLeftPosition;


    [Header("Right Grip - RIGHT")]

    [SerializeField]
    private Vector3 rightRightPosition;


    // ==================================================
    // LEFT Grip Poses
    // ==================================================

    [Header("Left Grip - FRONT")]

    [SerializeField]
    private Vector3 leftFrontPosition;


    [Header("Left Grip - BACK")]

    [SerializeField]
    private Vector3 leftBackPosition;


    [Header("Left Grip - LEFT")]

    [SerializeField]
    private Vector3 leftLeftPosition;


    [Header("Left Grip - RIGHT")]

    [SerializeField]
    private Vector3 leftRightPosition;


    // ==================================================
    // Transition
    // ==================================================

    [Header("Transition")]

    [Tooltip(
        "방향 변경 시 Grip이 새 위치로 따라가는 시간"
    )]
    [SerializeField]
    private float smoothTime =
        0.12f;


    private Vector3 rightVelocity;

    private Vector3 leftVelocity;


    // ==================================================
    // Awake
    // ==================================================

    private void Awake()
    {
        AutoFindReferences();
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
        // RIGHT
        // ==========================================

        if (rightGripPoint != null)
        {
            Vector3 rightTarget =
                GetRightGripTarget();


            rightGripPoint.localPosition =
                Vector3.SmoothDamp(
                    rightGripPoint.localPosition,
                    rightTarget,
                    ref rightVelocity,
                    smoothTime
                );
        }


        // ==========================================
        // LEFT
        // ==========================================

        if (leftGripPoint != null)
        {
            Vector3 leftTarget =
                GetLeftGripTarget();


            leftGripPoint.localPosition =
                Vector3.SmoothDamp(
                    leftGripPoint.localPosition,
                    leftTarget,
                    ref leftVelocity,
                    smoothTime
                );
        }
    }


    // ==================================================
    // RIGHT Target
    // ==================================================

    private Vector3 GetRightGripTarget()
    {
        PlayerVisualDirectionController
            .FacingDirection facing =
            directionController.CurrentFacing;


        switch (facing)
        {
            case PlayerVisualDirectionController
                .FacingDirection.Front:

                return rightFrontPosition;


            case PlayerVisualDirectionController
                .FacingDirection.Back:

                return rightBackPosition;


            case PlayerVisualDirectionController
                .FacingDirection.Left:

                return rightLeftPosition;


            case PlayerVisualDirectionController
                .FacingDirection.Right:

                return rightRightPosition;
        }


        return rightFrontPosition;
    }


    // ==================================================
    // LEFT Target
    // ==================================================

    private Vector3 GetLeftGripTarget()
    {
        PlayerVisualDirectionController
            .FacingDirection facing =
            directionController.CurrentFacing;


        switch (facing)
        {
            case PlayerVisualDirectionController
                .FacingDirection.Front:

                return leftFrontPosition;


            case PlayerVisualDirectionController
                .FacingDirection.Back:

                return leftBackPosition;


            case PlayerVisualDirectionController
                .FacingDirection.Left:

                return leftLeftPosition;


            case PlayerVisualDirectionController
                .FacingDirection.Right:

                return leftRightPosition;
        }


        return leftFrontPosition;
    }


    // ==================================================
    // RIGHT Capture
    // ==================================================

    [ContextMenu(
        "CAPTURE - Right Grip As FRONT"
    )]
    private void CaptureRightFront()
    {
        if (rightGripPoint == null)
        {
            return;
        }


        rightFrontPosition =
            rightGripPoint.localPosition;


        MarkDirty();
    }


    [ContextMenu(
        "CAPTURE - Right Grip As BACK"
    )]
    private void CaptureRightBack()
    {
        if (rightGripPoint == null)
        {
            return;
        }


        rightBackPosition =
            rightGripPoint.localPosition;


        MarkDirty();
    }


    [ContextMenu(
        "CAPTURE - Right Grip As LEFT"
    )]
    private void CaptureRightLeft()
    {
        if (rightGripPoint == null)
        {
            return;
        }


        rightLeftPosition =
            rightGripPoint.localPosition;


        MarkDirty();
    }


    [ContextMenu(
        "CAPTURE - Right Grip As RIGHT"
    )]
    private void CaptureRightRight()
    {
        if (rightGripPoint == null)
        {
            return;
        }


        rightRightPosition =
            rightGripPoint.localPosition;


        MarkDirty();
    }


    // ==================================================
    // LEFT Capture
    // ==================================================

    [ContextMenu(
        "CAPTURE - Left Grip As FRONT"
    )]
    private void CaptureLeftFront()
    {
        if (leftGripPoint == null)
        {
            return;
        }


        leftFrontPosition =
            leftGripPoint.localPosition;


        MarkDirty();
    }


    [ContextMenu(
        "CAPTURE - Left Grip As BACK"
    )]
    private void CaptureLeftBack()
    {
        if (leftGripPoint == null)
        {
            return;
        }


        leftBackPosition =
            leftGripPoint.localPosition;


        MarkDirty();
    }


    [ContextMenu(
        "CAPTURE - Left Grip As LEFT"
    )]
    private void CaptureLeftLeft()
    {
        if (leftGripPoint == null)
        {
            return;
        }


        leftLeftPosition =
            leftGripPoint.localPosition;


        MarkDirty();
    }


    [ContextMenu(
        "CAPTURE - Left Grip As RIGHT"
    )]
    private void CaptureLeftRight()
    {
        if (leftGripPoint == null)
        {
            return;
        }


        leftRightPosition =
            leftGripPoint.localPosition;


        MarkDirty();
    }


    // ==================================================
    // RIGHT Test
    // ==================================================

    [ContextMenu(
        "TEST - Right Grip FRONT"
    )]
    private void TestRightFront()
    {
        ApplyImmediateRight(
            rightFrontPosition
        );
    }


    [ContextMenu(
        "TEST - Right Grip BACK"
    )]
    private void TestRightBack()
    {
        ApplyImmediateRight(
            rightBackPosition
        );
    }


    [ContextMenu(
        "TEST - Right Grip LEFT"
    )]
    private void TestRightLeft()
    {
        ApplyImmediateRight(
            rightLeftPosition
        );
    }


    [ContextMenu(
        "TEST - Right Grip RIGHT"
    )]
    private void TestRightRight()
    {
        ApplyImmediateRight(
            rightRightPosition
        );
    }


    // ==================================================
    // LEFT Test
    // ==================================================

    [ContextMenu(
        "TEST - Left Grip FRONT"
    )]
    private void TestLeftFront()
    {
        ApplyImmediateLeft(
            leftFrontPosition
        );
    }


    [ContextMenu(
        "TEST - Left Grip BACK"
    )]
    private void TestLeftBack()
    {
        ApplyImmediateLeft(
            leftBackPosition
        );
    }


    [ContextMenu(
        "TEST - Left Grip LEFT"
    )]
    private void TestLeftLeft()
    {
        ApplyImmediateLeft(
            leftLeftPosition
        );
    }


    [ContextMenu(
        "TEST - Left Grip RIGHT"
    )]
    private void TestLeftRight()
    {
        ApplyImmediateLeft(
            leftRightPosition
        );
    }


    // ==================================================
    // Immediate Apply
    // ==================================================

    private void ApplyImmediateRight(
        Vector3 position
    )
    {
        if (rightGripPoint == null)
        {
            return;
        }


        rightVelocity =
            Vector3.zero;


        rightGripPoint.localPosition =
            position;
    }


    private void ApplyImmediateLeft(
        Vector3 position
    )
    {
        if (leftGripPoint == null)
        {
            return;
        }


        leftVelocity =
            Vector3.zero;


        leftGripPoint.localPosition =
            position;
    }


    // ==================================================
    // Auto Find
    // ==================================================

    [ContextMenu(
        "AUTO FIND - Weapon Grip References"
    )]
    private void AutoFindReferences()
    {
        Transform playerRoot =
            transform.root;


        Transform playerVisual =
            playerRoot.Find(
                "PlayerVisual"
            );


        if (playerVisual == null)
        {
            Debug.LogError(
                "[WeaponGrip] PlayerVisual을 찾지 못했습니다.",
                this
            );

            return;
        }


        // ==========================================
        // Direction
        // ==========================================

        directionController =
            playerVisual.GetComponent<
                PlayerVisualDirectionController
            >();


        // ==========================================
        // RIGHT
        // ==========================================

        rightGripPoint =
            playerVisual.Find(
                "VisualOffset/Hands/"
                + "RightHandAnchor/"
                + "RightHandAimAnchor/"
                + "RightWeaponGripPoint"
            );


        // ==========================================
        // LEFT
        // ==========================================

        leftGripPoint =
            playerVisual.Find(
                "VisualOffset/Hands/"
                + "LeftHandAnchor/"
                + "LeftHandAimAnchor/"
                + "LeftWeaponGripPoint"
            );


        // ==========================================
        // Validation
        // ==========================================

        if (directionController == null)
        {
            Debug.LogError(
                "[WeaponGrip] "
                + "PlayerVisualDirectionController를 "
                + "찾지 못했습니다.",
                this
            );
        }


        if (rightGripPoint == null)
        {
            Debug.LogError(
                "[WeaponGrip] "
                + "RightWeaponGripPoint를 "
                + "찾지 못했습니다.",
                this
            );
        }


        if (leftGripPoint == null)
        {
            Debug.LogError(
                "[WeaponGrip] "
                + "LeftWeaponGripPoint를 "
                + "찾지 못했습니다.",
                this
            );
        }


#if UNITY_EDITOR

        UnityEditor.EditorUtility.SetDirty(
            this
        );

#endif


        Debug.Log(
            "[WeaponGrip] "
            + "양손 Grip Reference 연결 완료.",
            this
        );
    }


    // ==================================================
    // Editor
    // ==================================================

    private void MarkDirty()
    {
#if UNITY_EDITOR

        UnityEditor.EditorUtility.SetDirty(
            this
        );

#endif
    }


    // ==================================================
    // Gizmos
    //
    // Scene에서 두 Grip 위치 확인용.
    // ==================================================

    private void OnDrawGizmosSelected()
    {
        if (rightGripPoint != null)
        {
            Gizmos.DrawWireSphere(
                rightGripPoint.position,
                0.06f
            );
        }


        if (leftGripPoint != null)
        {
            Gizmos.DrawWireSphere(
                leftGripPoint.position,
                0.06f
            );
        }
    }
}