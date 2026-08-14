using System.Collections.Generic;
using UnityEngine;

public class SplashBombWeaponBehaviour
    : PlayerWeaponBehaviour
{
    // ==================================================
    // Runtime State
    // ==================================================

    private class SlotRuntimeState
    {
        public bool isCharging;

        public float chargeStartTime;

        public float nextUseTime;
    }


    private readonly SlotRuntimeState
        rightState =
        new SlotRuntimeState();


    private readonly SlotRuntimeState
        leftState =
        new SlotRuntimeState();


    // ==================================================
    // Bomb Config
    //
    // 기존 PlayerSubWeapon이 보관하던 값을
    // 이제 SplashBombWeaponBehaviour가 직접 소유한다.
    // ==================================================

    [Header("Bomb")]

    [SerializeField]
    private GameObject splashBombPrefab;


    // ==================================================
    // Ink Cost
    // ==================================================

    [Header("Ink Cost")]

    [SerializeField]
    [Range(0f, 1f)]
    [Tooltip("최대 Ink 기준 폭탄 1회 소비 비율")]
    private float bombInkCostPercent =
        0.5f;


    // ==================================================
    // Charge
    // ==================================================

    [Header("Charge")]

    [SerializeField]
    private float minRange =
        2.5f;


    [SerializeField]
    private float maxRange =
        8f;


    [SerializeField]
    [Tooltip(
        "최대 사거리에 도달하는 데 걸리는 시간"
    )]
    private float maxChargeTime =
        1f;


    // ==================================================
    // Cooldown
    // ==================================================

    [Header("Cooldown")]

    [SerializeField]
    private float cooldown =
        2.5f;


    // ==================================================
    // Trajectory Preview
    // ==================================================

    [Header("Trajectory Preview")]

    [SerializeField]
    private LineRenderer rangeLine;


    [SerializeField]
    private Transform landingIndicator;


    [SerializeField]
    private LayerMask obstacleLayer;


    [SerializeField]
    [Tooltip(
        "SplashBomb Collider Radius와 동일하게 설정"
    )]
    private float previewRadius =
        0.18f;


    [SerializeField]
    [Tooltip(
        "작을수록 Preview가 정확하지만 계산량 증가"
    )]
    private float simulationStep =
        0.02f;


    // ==================================================
    // Preview Appearance
    // ==================================================

    [Header("Preview Appearance")]

    [SerializeField]
    private float lineWidth =
        0.08f;


    [SerializeField]
    private Color previewColor =
        new Color(
            0.15f,
            0.75f,
            1f,
            0.9f
        );


    // ==================================================
    // Morph
    // ==================================================

    [Header("Morph")]

    [SerializeField]
    private PlayerHandInkMorphController
        handMorphController;


    // ==================================================
    // Runtime References
    // ==================================================

    [Header("Runtime References")]

    [SerializeField]
    private PlayerInkResource inkResource;


    [SerializeField]
    private PlayerDive playerDive;


    private SplashBomb bombTemplate;


    // ==================================================
    // Preview Owner
    //
    // 현재 Preview Renderer가 하나이므로
    // Bomb 두 개를 동시에 Charge하지는 않는다.
    //
    // Slot Runtime / Cooldown 자체는 독립적이다.
    // ==================================================

    private bool hasPreviewOwner;


    private PlayerWeaponController
        .WeaponSlotSide previewOwnerSide;


    // ==================================================
    // Public State
    // ==================================================

    public override bool IsUsing
    {
        get
        {
            return IsCharging;
        }
    }


    public bool IsCharging
    {
        get
        {
            return
                rightState.isCharging
                ||
                leftState.isCharging;
        }
    }


    public float Charge01
    {
        get
        {
            float right =
                GetCharge01(
                    rightState
                );


            float left =
                GetCharge01(
                    leftState
                );


            return Mathf.Max(
                right,
                left
            );
        }
    }


    public override bool IsUsingSlot(
        PlayerWeaponController
            .WeaponSlotSide side
    )
    {
        return
            GetState(side)
                .isCharging;
    }


    // ==================================================
    // Awake
    // ==================================================

    private void Awake()
    {
        AutoFindReferences();

        RefreshBombTemplate();
    }


    // ==================================================
    // Start
    // ==================================================

    private void Start()
    {
        SetupLineRenderer();

        HidePreview();
    }


    // ==================================================
    // Press
    // ==================================================

    public override void UsePressed(
        WeaponUseContext context
    )
    {
        BeginCharge(
            context
        );
    }


    // ==================================================
    // Held
    // ==================================================

    public override void UseHeld(
        WeaponUseContext context
    )
    {
        SlotRuntimeState state =
            GetState(
                context.SlotSide
            );


        if (!state.isCharging)
        {
            return;
        }


        UpdatePreview(
            context,
            state
        );
    }


    // ==================================================
    // Released
    // ==================================================

    public override void UseReleased(
        WeaponUseContext context
    )
    {
        SlotRuntimeState state =
            GetState(
                context.SlotSide
            );


        if (!state.isCharging)
        {
            return;
        }


        ReleaseBomb(
            context,
            state
        );
    }


    // ==================================================
    // Cancel All
    // ==================================================

    public override void CancelUse()
    {
        CancelSlot(
            PlayerWeaponController
                .WeaponSlotSide.Right
        );


        CancelSlot(
            PlayerWeaponController
                .WeaponSlotSide.Left
        );
    }


    // ==================================================
    // Cancel One Slot
    // ==================================================

    public override void CancelUse(
        PlayerWeaponController
            .WeaponSlotSide side
    )
    {
        CancelSlot(
            side
        );
    }


    // ==================================================
    // Cancel Slot
    // ==================================================

    private void CancelSlot(
        PlayerWeaponController
            .WeaponSlotSide side
    )
    {
        SlotRuntimeState state =
            GetState(
                side
            );


        bool wasCharging =
            state.isCharging;


        state.isCharging =
            false;


        if (hasPreviewOwner &&
            previewOwnerSide ==
            side)
        {
            hasPreviewOwner =
                false;


            HidePreview();
        }


        // 실제 차징 중 취소된 경우
        // Bomb → Ink → Hand
        if (wasCharging &&
            handMorphController != null)
        {
            handMorphController
                .NotifyThrowableChargeCancelled(
                    side
                );
        }
    }


    // ==================================================
    // Begin Charge
    // ==================================================

    private void BeginCharge(
        WeaponUseContext context
    )
    {
        if (context.Controller == null ||
            context.Weapon == null)
        {
            return;
        }


        SlotRuntimeState state =
            GetState(
                context.SlotSide
            );


        if (state.isCharging)
        {
            return;
        }


        // ==========================================
        // 잠수 중 사용 금지
        // ==========================================

        if (playerDive != null &&
            playerDive.IsSwimForm)
        {
            return;
        }


        // ==========================================
        // 다른 손에서 Bomb 차지 중
        //
        // 현재 Preview Renderer가 하나이므로
        // 동시 Bomb 차지만 제한.
        // ==========================================

        if (IsOtherSlotCharging(
                context.SlotSide
            ))
        {
            return;
        }


        // ==========================================
        // Cooldown
        // ==========================================

        if (Time.time <
            state.nextUseTime)
        {
            return;
        }


        // ==========================================
        // Required References
        // ==========================================

        if (bombTemplate == null ||
            context.UsePoint == null ||
            inkResource == null)
        {
            return;
        }


        // ==========================================
        // Ink Empty
        // ==========================================

        if (inkResource.IsEmpty)
        {
            context.Controller
                .SetForcedHand(
                    context.SlotSide,
                    true
                );


            return;
        }


        // ==========================================
        // Ink Cost
        // ==========================================

        float inkCost =
            GetBombInkCost(
                context
            );


        if (!inkResource.HasInk(
                inkCost
            ))
        {
            return;
        }


        // ==========================================
        // Forced Hand 해제
        // ==========================================

        if (context.Controller
            .IsForcedHand(
                context.SlotSide
            ))
        {
            context.Controller
                .SetForcedHand(
                    context.SlotSide,
                    false
                );
        }


        // ==========================================
        // Charge Start
        // ==========================================

        state.isCharging =
            true;


        state.chargeStartTime =
            Time.time;


        hasPreviewOwner =
            true;


        previewOwnerSide =
            context.SlotSide;


        // ==========================================
        // Morph
        //
        // Hand → Ink → SplashBomb
        // ==========================================

        if (handMorphController != null)
        {
            handMorphController
                .NotifyThrowableChargeStarted(
                    context.SlotSide
                );
        }


        ShowPreview();


        UpdatePreview(
            context,
            state
        );
    }


    // ==================================================
    // Ink Cost
    // ==================================================

    private float GetBombInkCost(
        WeaponUseContext context
    )
    {
        if (inkResource == null)
        {
            return Mathf.Infinity;
        }


        return
            inkResource.MaxInk
            *
            bombInkCostPercent
            *
            context.InkCostMultiplier;
    }


    // ==================================================
    // Charge
    // ==================================================

    private float GetCharge01(
        SlotRuntimeState state
    )
    {
        if (!state.isCharging)
        {
            return 0f;
        }


        float safeChargeTime =
            Mathf.Max(
                maxChargeTime,
                0.01f
            );


        return Mathf.Clamp01(
            (
                Time.time
                -
                state.chargeStartTime
            )
            /
            safeChargeTime
        );
    }


    private float GetCurrentRange(
        SlotRuntimeState state
    )
    {
        return Mathf.Lerp(
            minRange,
            maxRange,
            GetCharge01(
                state
            )
        );
    }


    // ==================================================
    // Preview
    // ==================================================

    private void UpdatePreview(
        WeaponUseContext context,
        SlotRuntimeState state
    )
    {
        if (context.UsePoint == null ||
            bombTemplate == null)
        {
            return;
        }


        if (!hasPreviewOwner ||
            previewOwnerSide !=
            context.SlotSide)
        {
            return;
        }


        Vector2 start =
            context.UsePoint.position;


        Vector2 direction =
            context.UsePoint.right;


        if (direction.sqrMagnitude <
            0.001f)
        {
            direction =
                Vector2.right;
        }


        direction.Normalize();


        float targetRange =
            GetCurrentRange(
                state
            );


        List<Vector3> points =
            SimulateTrajectory(
                start,
                direction,
                targetRange
            );


        if (points.Count == 0)
        {
            return;
        }


        if (rangeLine != null)
        {
            rangeLine.positionCount =
                points.Count;


            rangeLine.SetPositions(
                points.ToArray()
            );
        }


        Vector3 landingPoint =
            points[
                points.Count - 1
            ];


        if (landingIndicator != null)
        {
            landingIndicator.position =
                new Vector3(
                    landingPoint.x,
                    landingPoint.y,
                    0f
                );
        }
    }


    // ==================================================
    // Preview Physics
    // ==================================================

    private List<Vector3> SimulateTrajectory(
        Vector2 start,
        Vector2 direction,
        float targetRange
    )
    {
        List<Vector3> points =
            new List<Vector3>();


        bombTemplate
            .CalculateLaunchVelocity(
                targetRange,
                out float horizontalSpeed,
                out float verticalVelocity
            );


        Vector2 position =
            start;


        Vector2 groundVelocity =
            direction.normalized
            *
            horizontalSpeed;


        float height =
            0.01f;


        points.Add(
            new Vector3(
                position.x,
                position.y,
                0f
            )
        );


        float safeStep =
            Mathf.Clamp(
                simulationStep,
                0.005f,
                0.05f
            );


        const int maxSteps =
            300;


        for (int step = 0;
             step < maxSteps;
             step++)
        {
            verticalVelocity -=
                bombTemplate.gravity
                *
                safeStep;


            height +=
                verticalVelocity
                *
                safeStep;


            SimulateHorizontalMovement(
                ref position,
                ref groundVelocity,
                safeStep,
                points
            );


            points.Add(
                new Vector3(
                    position.x,
                    position.y,
                    0f
                )
            );


            if (height <= 0f &&
                verticalVelocity < 0f)
            {
                break;
            }
        }


        return points;
    }


    // ==================================================
    // Horizontal Simulation
    // ==================================================

    private void SimulateHorizontalMovement(
        ref Vector2 position,
        ref Vector2 velocity,
        float deltaTime,
        List<Vector3> points
    )
    {
        float remainingTime =
            deltaTime;


        const int maxBouncesPerStep =
            4;


        for (int bounce = 0;
             bounce < maxBouncesPerStep;
             bounce++)
        {
            float speed =
                velocity.magnitude;


            if (speed <
                bombTemplate
                    .minimumGroundSpeed)
            {
                velocity =
                    Vector2.zero;


                return;
            }


            float moveDistance =
                speed
                *
                remainingTime;


            if (moveDistance <=
                0.0001f)
            {
                return;
            }


            Vector2 moveDirection =
                velocity
                /
                speed;


            RaycastHit2D hit =
                Physics2D.CircleCast(
                    position,
                    previewRadius,
                    moveDirection,
                    moveDistance,
                    obstacleLayer
                );


            // ==========================================
            // No Collision
            // ==========================================

            if (hit.collider == null)
            {
                position +=
                    velocity
                    *
                    remainingTime;


                return;
            }


            // ==========================================
            // Collision
            // ==========================================

            float travelDistance =
                Mathf.Max(
                    0f,
                    hit.distance
                    -
                    0.005f
                );


            position +=
                moveDirection
                *
                travelDistance;


            points.Add(
                new Vector3(
                    position.x,
                    position.y,
                    0f
                )
            );


            float timeToHit =
                hit.distance
                /
                speed;


            remainingTime -=
                Mathf.Clamp(
                    timeToHit,
                    0f,
                    remainingTime
                );


            // ==========================================
            // Bounce
            // ==========================================

            velocity =
                Vector2.Reflect(
                    velocity,
                    hit.normal
                );


            velocity *=
                bombTemplate
                    .wallBounceRetention;


            position +=
                hit.normal
                *
                0.01f;


            if (remainingTime <=
                0.0001f)
            {
                return;
            }
        }
    }


    // ==================================================
    // Release Bomb
    // ==================================================

    private void ReleaseBomb(
        WeaponUseContext context,
        SlotRuntimeState state
    )
    {
        if (!state.isCharging)
        {
            return;
        }


        // 아직 Charging 상태일 때
        // 현재 사거리를 먼저 확보
        float targetRange =
            GetCurrentRange(
                state
            );


        state.isCharging =
            false;


        // ==========================================
        // Preview End
        // ==========================================

        if (hasPreviewOwner &&
            previewOwnerSide ==
            context.SlotSide)
        {
            hasPreviewOwner =
                false;


            HidePreview();
        }


        // ==========================================
        // Validation Failure
        //
        // 실제로 던지지 않았으므로
        // Bomb → Ink → Hand
        // ==========================================

        if (splashBombPrefab == null ||
            context.UsePoint == null ||
            inkResource == null)
        {
            CancelMorphToHand(
                context.SlotSide
            );


            return;
        }


        // ==========================================
        // Ink Spend
        // ==========================================

        float inkCost =
            GetBombInkCost(
                context
            );


        if (!inkResource.TrySpendInk(
                inkCost
            ))
        {
            CancelMorphToHand(
                context.SlotSide
            );


            if (inkResource.IsEmpty &&
                context.Controller != null)
            {
                context.Controller
                    .SetForcedHand(
                        context.SlotSide,
                        true
                    );
            }


            return;
        }


        // ==========================================
        // Direction
        // ==========================================

        Vector2 direction =
            context.UsePoint.right;


        if (direction.sqrMagnitude <
            0.001f)
        {
            direction =
                Vector2.right;
        }


        direction.Normalize();


        // ==========================================
        // Spawn
        // ==========================================

        GameObject bombObject =
            Instantiate(
                splashBombPrefab,
                context.UsePoint.position,
                Quaternion.identity
            );


        SplashBomb bomb =
            bombObject.GetComponent<
                SplashBomb
            >();


        if (bomb != null)
        {
            bomb.Launch(
                direction,
                targetRange,
                context.DamageMultiplier
            );
        }


        // ==========================================
        // Cooldown
        // ==========================================

        state.nextUseTime =
            Time.time
            +
            cooldown;


        // ==========================================
        // Morph
        //
        // SplashBomb 형태의 손 자체가
        // 투척됐다고 간주.
        //
        // 투척 순간 손 사라짐.
        // Cooldown 종료 후 손 재생.
        // ==========================================

        if (handMorphController != null)
        {
            handMorphController
                .NotifyThrowableThrown(
                    context.SlotSide,
                    cooldown
                );
        }
    }


    // ==================================================
    // Cancel Morph
    // ==================================================

    private void CancelMorphToHand(
        PlayerWeaponController
            .WeaponSlotSide side
    )
    {
        if (handMorphController == null)
        {
            return;
        }


        handMorphController
            .NotifyThrowableChargeCancelled(
                side
            );
    }


    // ==================================================
    // Line Renderer Setup
    // ==================================================

    private void SetupLineRenderer()
    {
        if (rangeLine == null)
        {
            return;
        }


        rangeLine.useWorldSpace =
            true;


        rangeLine.startWidth =
            lineWidth;


        rangeLine.endWidth =
            lineWidth;


        rangeLine.startColor =
            previewColor;


        rangeLine.endColor =
            previewColor;


        rangeLine.sortingOrder =
            50;


        rangeLine.numCornerVertices =
            4;


        rangeLine.numCapVertices =
            4;


        if (rangeLine.sharedMaterial == null)
        {
            Shader spriteShader =
                Shader.Find(
                    "Sprites/Default"
                );


            if (spriteShader != null)
            {
                rangeLine.material =
                    new Material(
                        spriteShader
                    );
            }
        }
    }


    // ==================================================
    // Show Preview
    // ==================================================

    private void ShowPreview()
    {
        if (rangeLine != null)
        {
            rangeLine.enabled =
                true;
        }


        if (landingIndicator != null)
        {
            landingIndicator
                .gameObject
                .SetActive(
                    true
                );
        }
    }


    // ==================================================
    // Hide Preview
    // ==================================================

    private void HidePreview()
    {
        if (rangeLine != null)
        {
            rangeLine.enabled =
                false;
        }


        if (landingIndicator != null)
        {
            landingIndicator
                .gameObject
                .SetActive(
                    false
                );
        }
    }


    // ==================================================
    // Get Slot State
    // ==================================================

    private SlotRuntimeState GetState(
        PlayerWeaponController
            .WeaponSlotSide side
    )
    {
        if (side ==
            PlayerWeaponController
                .WeaponSlotSide.Right)
        {
            return rightState;
        }


        return leftState;
    }


    // ==================================================
    // Other Slot Charging
    //
    // Preview Renderer가 현재 하나뿐이므로
    // Bomb + Bomb 동시 차지만 제한.
    // ==================================================

    private bool IsOtherSlotCharging(
        PlayerWeaponController
            .WeaponSlotSide side
    )
    {
        if (side ==
            PlayerWeaponController
                .WeaponSlotSide.Right)
        {
            return
                leftState.isCharging;
        }


        return
            rightState.isCharging;
    }


    // ==================================================
    // Bomb Template
    // ==================================================

    private void RefreshBombTemplate()
    {
        bombTemplate =
            null;


        if (splashBombPrefab == null)
        {
            return;
        }


        bombTemplate =
            splashBombPrefab
                .GetComponent<
                    SplashBomb
                >();
    }


    // ==================================================
    // Auto Find
    // ==================================================

    [ContextMenu(
        "AUTO FIND - Splash Bomb References"
    )]
    private void AutoFindReferences()
    {
        Transform root =
            transform.root;


        // ==========================================
        // Player Ink
        // ==========================================

        if (inkResource == null)
        {
            inkResource =
                root.GetComponentInChildren<
                    PlayerInkResource
                >(
                    true
                );
        }


        // ==========================================
        // Player Dive
        // ==========================================

        if (playerDive == null)
        {
            playerDive =
                root.GetComponentInChildren<
                    PlayerDive
                >(
                    true
                );
        }


        // ==========================================
        // Hand Morph
        //
        // AimPivot과 PlayerVisual은 형제이므로
        // Root 전체 검색.
        // ==========================================

        if (handMorphController == null)
        {
            handMorphController =
                root.GetComponentInChildren<
                    PlayerHandInkMorphController
                >(
                    true
                );
        }


        RefreshBombTemplate();


#if UNITY_EDITOR

        UnityEditor.EditorUtility.SetDirty(
            this
        );

#endif


        Debug.Log(
            "[SplashBombWeapon] "
            +
            "SplashBomb References 연결 완료.",
            this
        );
    }
}