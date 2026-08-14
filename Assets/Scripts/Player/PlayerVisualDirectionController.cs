using System;
using UnityEngine;

public class PlayerVisualDirectionController : MonoBehaviour
{
    // ==================================================
    // Direction
    // ==================================================

    public enum FacingDirection
    {
        Front,
        Back,
        Left,
        Right
    }


    // ==================================================
    // Serializable Data
    // ==================================================

    [Serializable]
    public class TransformState
    {
        public Vector3 localPosition =
            Vector3.zero;

        public Vector3 localEulerAngles =
            Vector3.zero;

        public Vector3 localScale =
            Vector3.one;
    }


    [Serializable]
    public class RendererState
    {
        public Sprite sprite;

        public int orderInLayer;

        public bool enabled =
            true;
    }


    [Serializable]
    public class PoseData
    {
        // ==========================================
        // Common
        // ==========================================

        [Header("Common Offset")]

        public TransformState visualOffset =
            new TransformState();


        // ==========================================
        // Scarf / Strap
        // ==========================================

        [Header("Scarf / Strap")]

        public TransformState scarfTail =
            new TransformState();

        public TransformState strap =
            new TransformState();


        // ==========================================
        // Hands
        // ==========================================

        [Header("Hands")]

        public TransformState hands =
            new TransformState();

        public TransformState leftHandAnchor =
            new TransformState();

        public TransformState rightHandAnchor =
            new TransformState();


        // ==========================================
        // Sprite / Sorting
        // ==========================================

        [Header("Sprites / Sorting")]

        public RendererState head =
            new RendererState();

        public RendererState torso =
            new RendererState();

        public RendererState scarfBand =
            new RendererState();

        public RendererState scarfTailRenderer =
            new RendererState();

        public RendererState strapRenderer =
            new RendererState();

        public RendererState eyes =
            new RendererState();


        public RendererState leftHand =
            new RendererState();

        public RendererState rightHand =
            new RendererState();
    }


    // ==================================================
    // Hierarchy
    // ==================================================

    [Header("Hierarchy")]

    [SerializeField]
    private Transform visualOffset;


    [SerializeField]
    private Transform scarfTail;


    [SerializeField]
    private Transform strap;


    [SerializeField]
    private Transform hands;


    [SerializeField]
    private Transform leftHandAnchor;


    [SerializeField]
    private Transform rightHandAnchor;


    // ==================================================
    // Sprite Renderers
    // ==================================================

    [Header("Sprite Renderers")]

    [SerializeField]
    private SpriteRenderer headRenderer;


    [SerializeField]
    private SpriteRenderer torsoRenderer;


    [SerializeField]
    private SpriteRenderer scarfBandRenderer;


    [SerializeField]
    private SpriteRenderer scarfTailRenderer;


    [SerializeField]
    private SpriteRenderer strapRenderer;


    [SerializeField]
    private SpriteRenderer eyesRenderer;


    [SerializeField]
    private SpriteRenderer leftHandRenderer;


    [SerializeField]
    private SpriteRenderer rightHandRenderer;


    // ==================================================
    // Direction Poses
    // ==================================================

    [Header("Direction Poses")]

    [SerializeField]
    private PoseData front =
        new PoseData();


    [SerializeField]
    private PoseData back =
        new PoseData();


    [SerializeField]
    private PoseData leftSide =
        new PoseData();


    // ==================================================
    // Runtime
    // ==================================================

    [Header("Runtime")]

    [SerializeField]
    private FacingDirection currentFacing =
        FacingDirection.Front;


    [SerializeField]
    private bool applyOnStart =
        false;


    private bool hasApplied;


    // 외부 Motion Controller에서 읽음
    public FacingDirection CurrentFacing
    {
        get
        {
            return currentFacing;
        }
    }


    // ==================================================
    // Start
    // ==================================================

    private void Start()
    {
        if (applyOnStart)
        {
            ForceApply(
                currentFacing
            );
        }
    }


    // ==================================================
    // Set Facing From Vector
    // ==================================================

    public void SetFacingFromVector(
        Vector2 direction
    )
    {
        if (direction.sqrMagnitude <
            0.0001f)
        {
            return;
        }


        FacingDirection nextDirection;


        // ==========================================
        // Horizontal
        // ==========================================

        if (Mathf.Abs(direction.x) >
            Mathf.Abs(direction.y))
        {
            nextDirection =
                direction.x < 0f
                ? FacingDirection.Left
                : FacingDirection.Right;
        }

        // ==========================================
        // Vertical
        // ==========================================

        else
        {
            nextDirection =
                direction.y > 0f
                ? FacingDirection.Back
                : FacingDirection.Front;
        }


        SetFacing(
            nextDirection
        );
    }


    // ==================================================
    // Set Facing
    // ==================================================

    public void SetFacing(
        FacingDirection direction
    )
    {
        // 같은 방향이면 반복 적용하지 않는다.
        // Motion Controller의 애니메이션을 보호하기 위함.
        if (hasApplied &&
            currentFacing == direction)
        {
            return;
        }


        currentFacing =
            direction;


        switch (direction)
        {
            // ======================================
            // FRONT
            // ======================================

            case FacingDirection.Front:

                ApplyPose(
                    front,
                    false,
                    false
                );

                break;


            // ======================================
            // BACK
            // ======================================

            case FacingDirection.Back:

                ApplyPose(
                    back,
                    false,
                    false
                );

                break;


            // ======================================
            // LEFT
            // ======================================

            case FacingDirection.Left:

                ApplyPose(
                    leftSide,
                    false,
                    false
                );

                break;


            // ======================================
            // RIGHT
            //
            // LEFT SIDE Pose를 좌우 반전.
            // 손의 가까운/먼 관계도 교환.
            // ======================================

            case FacingDirection.Right:

                ApplyPose(
                    leftSide,
                    true,
                    true
                );

                break;
        }


        hasApplied =
            true;
    }


    // ==================================================
    // Force Apply
    // ==================================================

    public void ForceApply(
        FacingDirection direction
    )
    {
        hasApplied =
            false;


        SetFacing(
            direction
        );
    }


    // ==================================================
    // Apply Pose
    // ==================================================

    private void ApplyPose(
        PoseData pose,
        bool mirrorX,
        bool swapHands
    )
    {
        if (pose == null)
        {
            return;
        }


        // ==========================================
        // Visual Offset
        // ==========================================

        ApplyVisualOffset(
            pose.visualOffset,
            mirrorX
        );


        // ==========================================
        // Scarf / Strap
        // ==========================================

        ApplyTransform(
            scarfTail,
            pose.scarfTail
        );


        ApplyTransform(
            strap,
            pose.strap
        );


        // ==========================================
        // Hands Root
        // ==========================================

        ApplyTransform(
            hands,
            pose.hands
        );


        // ==========================================
        // LEFT / FRONT / BACK
        // ==========================================

        if (!swapHands)
        {
            ApplyTransform(
                leftHandAnchor,
                pose.leftHandAnchor
            );


            ApplyTransform(
                rightHandAnchor,
                pose.rightHandAnchor
            );


            ApplyRenderer(
                leftHandRenderer,
                pose.leftHand
            );


            ApplyRenderer(
                rightHandRenderer,
                pose.rightHand
            );
        }

        // ==========================================
        // RIGHT
        //
        // LEFT_SIDE 원근 관계 반전
        // ==========================================

        else
        {
            ApplyTransform(
                leftHandAnchor,
                pose.rightHandAnchor
            );


            ApplyTransform(
                rightHandAnchor,
                pose.leftHandAnchor
            );


            ApplyRenderer(
                leftHandRenderer,
                pose.rightHand
            );


            ApplyRenderer(
                rightHandRenderer,
                pose.leftHand
            );
        }


        // ==========================================
        // Main Parts
        // ==========================================

        ApplyRenderer(
            headRenderer,
            pose.head
        );


        ApplyRenderer(
            torsoRenderer,
            pose.torso
        );


        ApplyRenderer(
            scarfBandRenderer,
            pose.scarfBand
        );


        ApplyRenderer(
            scarfTailRenderer,
            pose.scarfTailRenderer
        );


        ApplyRenderer(
            strapRenderer,
            pose.strapRenderer
        );


        ApplyRenderer(
            eyesRenderer,
            pose.eyes
        );
    }


    // ==================================================
    // Apply Visual Offset
    // ==================================================

    private void ApplyVisualOffset(
        TransformState state,
        bool mirrorX
    )
    {
        if (visualOffset == null ||
            state == null)
        {
            return;
        }


        Vector3 position =
            state.localPosition;


        Vector3 scale =
            state.localScale;


        // ==========================================
        // RIGHT MIRROR
        // ==========================================

        if (mirrorX)
        {
            position.x *=
                -1f;


            scale.x =
                -Mathf.Abs(
                    scale.x
                );
        }
        else
        {
            scale.x =
                Mathf.Abs(
                    scale.x
                );
        }


        visualOffset.localPosition =
            position;


        visualOffset.localEulerAngles =
            state.localEulerAngles;


        visualOffset.localScale =
            scale;
    }


    // ==================================================
    // Apply Transform
    // ==================================================

    private void ApplyTransform(
        Transform target,
        TransformState state
    )
    {
        if (target == null ||
            state == null)
        {
            return;
        }


        target.localPosition =
            state.localPosition;


        target.localEulerAngles =
            state.localEulerAngles;


        target.localScale =
            state.localScale;
    }


    // ==================================================
    // Apply Renderer
    // ==================================================

    private void ApplyRenderer(
        SpriteRenderer renderer,
        RendererState state
    )
    {
        if (renderer == null ||
            state == null)
        {
            return;
        }


        renderer.sprite =
            state.sprite;


        renderer.sortingOrder =
            state.orderInLayer;


        renderer.enabled =
            state.enabled &&
            state.sprite != null;
    }


    // ==================================================
    // AUTO FIND
    // ==================================================

    [ContextMenu(
        "AUTO FIND - PlayerVisual References"
    )]
    private void AutoFindReferences()
    {
        visualOffset =
            transform.Find(
                "VisualOffset"
            );


        if (visualOffset == null)
        {
            Debug.LogError(
                "[PlayerVisual] VisualOffset을 찾을 수 없습니다.",
                this
            );

            return;
        }


        // ==========================================
        // Main Transform References
        // ==========================================

        scarfTail =
            visualOffset.Find(
                "ScarfTail"
            );


        strap =
            visualOffset.Find(
                "Strap"
            );


        hands =
            visualOffset.Find(
                "Hands"
            );


        leftHandAnchor =
            visualOffset.Find(
                "Hands/LeftHandAnchor"
            );


        rightHandAnchor =
            visualOffset.Find(
                "Hands/RightHandAnchor"
            );


        // ==========================================
        // Sprite Renderers
        // ==========================================

        headRenderer =
            FindRenderer(
                "VisualOffset/Head"
            );


        torsoRenderer =
            FindRenderer(
                "VisualOffset/Torso"
            );


        scarfBandRenderer =
            FindRenderer(
                "VisualOffset/ScarfBand"
            );


        scarfTailRenderer =
            FindRenderer(
                "VisualOffset/ScarfTail"
            );


        strapRenderer =
            FindRenderer(
                "VisualOffset/Strap"
            );


        eyesRenderer =
            FindRenderer(
                "VisualOffset/Eyes"
            );


        leftHandRenderer =
            FindRenderer(
                "VisualOffset/Hands/LeftHandAnchor/LeftHand"
            );


        rightHandRenderer =
            FindRenderer(
                "VisualOffset/Hands/RightHandAnchor/RightHand"
            );


        MarkDirty();


        Debug.Log(
            "[PlayerVisual] References 자동 연결 완료.",
            this
        );
    }


    // ==================================================
    // Find Renderer
    // ==================================================

    private SpriteRenderer FindRenderer(
        string path
    )
    {
        Transform target =
            transform.Find(
                path
            );


        if (target == null)
        {
            Debug.LogWarning(
                $"[PlayerVisual] 찾지 못함: {path}",
                this
            );

            return null;
        }


        return target
            .GetComponent<SpriteRenderer>();
    }


    // ==================================================
    // CAPTURE
    // ==================================================

    [ContextMenu(
        "CAPTURE - Current As FRONT"
    )]
    private void CaptureFront()
    {
        front =
            CaptureCurrentPose();


        MarkDirty();


        Debug.Log(
            "[PlayerVisual] FRONT Pose 저장 완료.",
            this
        );
    }


    [ContextMenu(
        "CAPTURE - Current As BACK"
    )]
    private void CaptureBack()
    {
        back =
            CaptureCurrentPose();


        MarkDirty();


        Debug.Log(
            "[PlayerVisual] BACK Pose 저장 완료.",
            this
        );
    }


    [ContextMenu(
        "CAPTURE - Current As LEFT SIDE"
    )]
    private void CaptureLeftSide()
    {
        leftSide =
            CaptureCurrentPose();


        MarkDirty();


        Debug.Log(
            "[PlayerVisual] LEFT SIDE Pose 저장 완료.",
            this
        );
    }


    // ==================================================
    // Capture Current Pose
    // ==================================================

    private PoseData CaptureCurrentPose()
    {
        PoseData pose =
            new PoseData();


        pose.visualOffset =
            CaptureTransform(
                visualOffset
            );


        pose.scarfTail =
            CaptureTransform(
                scarfTail
            );


        pose.strap =
            CaptureTransform(
                strap
            );


        pose.hands =
            CaptureTransform(
                hands
            );


        pose.leftHandAnchor =
            CaptureTransform(
                leftHandAnchor
            );


        pose.rightHandAnchor =
            CaptureTransform(
                rightHandAnchor
            );


        pose.head =
            CaptureRenderer(
                headRenderer
            );


        pose.torso =
            CaptureRenderer(
                torsoRenderer
            );


        pose.scarfBand =
            CaptureRenderer(
                scarfBandRenderer
            );


        pose.scarfTailRenderer =
            CaptureRenderer(
                scarfTailRenderer
            );


        pose.strapRenderer =
            CaptureRenderer(
                strapRenderer
            );


        pose.eyes =
            CaptureRenderer(
                eyesRenderer
            );


        pose.leftHand =
            CaptureRenderer(
                leftHandRenderer
            );


        pose.rightHand =
            CaptureRenderer(
                rightHandRenderer
            );


        return pose;
    }


    // ==================================================
    // Capture Transform
    // ==================================================

    private TransformState CaptureTransform(
        Transform target
    )
    {
        TransformState state =
            new TransformState();


        if (target == null)
        {
            return state;
        }


        state.localPosition =
            target.localPosition;


        state.localEulerAngles =
            target.localEulerAngles;


        state.localScale =
            target.localScale;


        return state;
    }


    // ==================================================
    // Capture Renderer
    // ==================================================

    private RendererState CaptureRenderer(
        SpriteRenderer renderer
    )
    {
        RendererState state =
            new RendererState();


        if (renderer == null)
        {
            state.enabled =
                false;

            return state;
        }


        state.sprite =
            renderer.sprite;


        state.orderInLayer =
            renderer.sortingOrder;


        state.enabled =
            renderer.enabled;


        return state;
    }


    // ==================================================
    // EDITOR TEST
    // ==================================================

    [ContextMenu("TEST - FRONT")]
    private void TestFront()
    {
        ForceApply(
            FacingDirection.Front
        );


        MarkDirty();
    }


    [ContextMenu("TEST - BACK")]
    private void TestBack()
    {
        ForceApply(
            FacingDirection.Back
        );


        MarkDirty();
    }


    [ContextMenu("TEST - LEFT")]
    private void TestLeft()
    {
        ForceApply(
            FacingDirection.Left
        );


        MarkDirty();
    }


    [ContextMenu("TEST - RIGHT")]
    private void TestRight()
    {
        ForceApply(
            FacingDirection.Right
        );


        MarkDirty();
    }


    // ==================================================
    // Editor Dirty
    // ==================================================

    private void MarkDirty()
    {
#if UNITY_EDITOR

        UnityEditor.EditorUtility
            .SetDirty(
                this
            );

#endif
    }
}