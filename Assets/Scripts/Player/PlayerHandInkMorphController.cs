using System;
using System.Collections.Generic;
using UnityEngine;

public class PlayerHandInkMorphController : MonoBehaviour
{
    // ==================================================
    // Morph Mode
    // ==================================================

    public enum WeaponMorphMode
    {
        Hand,

        // Shooter / Cannon 등
        // 장착 중 계속 Weapon 형태
        Persistent,

        // SplashBomb 등
        // 평상시 Hand
        // 사용할 때만 Weapon 형태
        Throwable
    }


    // ==================================================
    // Morph Phase
    // ==================================================

    private enum MorphPhase
    {
        Hand,

        LiquefyToWeapon,
        MorphToWeapon,
        SettleWeapon,

        Weapon,

        LiquefyToHand,
        MorphToHand,
        SettleHand,

        Missing,
        Respawning
    }


    // ==================================================
    // Weapon Morph Profile
    // ==================================================

    [Serializable]
    public class WeaponMorphProfile
    {
        [Tooltip("이 Morph 설정을 사용할 Weapon")]
        public WeaponDefinition weapon;


        [Tooltip("Weapon의 Visual 동작 방식")]
        public WeaponMorphMode mode =
            WeaponMorphMode.Persistent;


        [Header("Target Shape")]

        [Tooltip(
            "기본 Hand Scale에 곱해지는 " +
            "최종 Weapon Scale"
        )]
        public Vector2 targetScale =
            new Vector2(
                1.4f,
                0.8f
            );


        [Tooltip(
            "기본 Hand Rotation에 더해지는 " +
            "Weapon Z Rotation"
        )]
        public float targetRotation =
            0f;


        [Range(0f, 1f)]
        [Tooltip(
            "Weapon 완성 후 Ink Color 유지량. " +
            "1 = 완전 Ink색"
        )]
        public float finalInkBlend =
            1f;
    }


    // ==================================================
    // Runtime Slot State
    // ==================================================

    private class SlotMorphState
    {
        public WeaponDefinition weapon;

        public WeaponMorphProfile profile;


        public MorphPhase phase =
            MorphPhase.Hand;


        public float phaseTime;


        // 현재 실제 Shape
        public Vector2 scaleFactor =
            Vector2.one;


        public float rotationOffset;


        public float inkBlend;


        public float wobbleAmount;


        public float alpha =
            1f;


        // Phase 시작값
        public Vector2 startScale =
            Vector2.one;


        public float startRotation;

        public float startInk;

        public float startWobble;

        public float startAlpha =
            1f;


        // Throwable
        public float respawnAt;
    }


    // ==================================================
    // References
    // ==================================================

    [Header("References")]

    [SerializeField]
    private PlayerWeaponController
        weaponController;


    [SerializeField]
    private PlayerVisualDirectionController
        directionController;


    [SerializeField]
    private Transform leftHand;


    [SerializeField]
    private Transform rightHand;


    [SerializeField]
    private SpriteRenderer leftHandRenderer;


    [SerializeField]
    private SpriteRenderer rightHandRenderer;


    // ==================================================
    // Profiles
    // ==================================================

    [Header("Weapon Morph Profiles")]

    [SerializeField]
    private List<WeaponMorphProfile>
        weaponProfiles =
        new List<WeaponMorphProfile>();


    // ==================================================
    // Ink
    // ==================================================

    [Header("Ink")]

    [SerializeField]
    private Color inkColor =
        new Color(
            0.10f,
            0.35f,
            1f,
            1f
        );


    // ==================================================
    // Transition
    // ==================================================

    [Header("Weapon Morph Timing")]

    [Tooltip(
        "기본 형태가 액체 Ink 상태가 되는 시간"
    )]
    [SerializeField]
    private float liquefyDuration =
        0.12f;


    [Tooltip(
        "Ink 상태에서 Weapon 형태로 변하는 시간"
    )]
    [SerializeField]
    private float morphDuration =
        0.24f;


    [Tooltip(
        "Weapon 형태 도달 후 일렁임이 안정되는 시간"
    )]
    [SerializeField]
    private float settleDuration =
        0.14f;


    // ==================================================
    // Return To Hand
    // ==================================================

    [Header("Return To Hand")]

    [SerializeField]
    private float returnLiquefyDuration =
        0.10f;


    [SerializeField]
    private float returnMorphDuration =
        0.18f;


    [SerializeField]
    private float returnSettleDuration =
        0.12f;


    // ==================================================
    // Respawn
    // ==================================================

    [Header("Throwable Hand Respawn")]

    [Tooltip(
        "투척 후 손이 뿅 하고 다시 생기는 시간"
    )]
    [SerializeField]
    private float respawnDuration =
        0.22f;


    [Tooltip(
        "손 재생 순간 최대 크기"
    )]
    [SerializeField]
    private float respawnOvershoot =
        1.15f;


    // ==================================================
    // Liquefy Shape
    // ==================================================

    [Header("Liquefy Shape")]

    [SerializeField]
    private float compressionX =
        1.12f;


    [SerializeField]
    private float compressionY =
        0.72f;


    // ==================================================
    // Wobble
    // ==================================================

    [Header("Ink Wobble")]

    [SerializeField]
    private float wobbleSpeed =
        2.2f;


    [SerializeField]
    private float wobbleScaleX =
        0.14f;


    [SerializeField]
    private float wobbleScaleY =
        0.11f;


    [SerializeField]
    private float wobbleRotation =
        3f;


    // ==================================================
    // Base Data
    // ==================================================

    private Vector3 leftBaseScale;

    private Vector3 rightBaseScale;


    private Vector3 leftBaseEuler;

    private Vector3 rightBaseEuler;


    private Color leftBaseColor;

    private Color rightBaseColor;


    // ==================================================
    // Slot States
    // ==================================================

    private readonly SlotMorphState
        logicalRight =
        new SlotMorphState();


    private readonly SlotMorphState
        logicalLeft =
        new SlotMorphState();


    private bool initialized;


    // ==================================================
    // Awake
    // ==================================================

    private void Awake()
    {
        AutoFindReferences();

        CaptureBaseState();

        initialized = true;
    }


    // ==================================================
    // Enable
    // ==================================================

    private void OnEnable()
    {
        SubscribeEvents();
    }


    // ==================================================
    // Start
    // ==================================================

    private void Start()
    {
        InitializeFromLoadout();
    }


    // ==================================================
    // Update
    // ==================================================

    private void LateUpdate()
    {
        if (!initialized)
        {
            return;
        }


        UpdateSlotState(
            PlayerWeaponController
                .WeaponSlotSide.Right,
            logicalRight
        );


        UpdateSlotState(
            PlayerWeaponController
                .WeaponSlotSide.Left,
            logicalLeft
        );


        ApplyLogicalHands();
    }


    // ==================================================
    // Initialize
    // ==================================================

    private void InitializeFromLoadout()
    {
        if (weaponController == null)
        {
            SetHandImmediate(
                logicalRight
            );


            SetHandImmediate(
                logicalLeft
            );


            return;
        }


        InitializeSlot(
            PlayerWeaponController
                .WeaponSlotSide.Right,
            logicalRight
        );


        InitializeSlot(
            PlayerWeaponController
                .WeaponSlotSide.Left,
            logicalLeft
        );
    }


    private void InitializeSlot(
        PlayerWeaponController
            .WeaponSlotSide side,
        SlotMorphState state
    )
    {
        state.weapon =
            weaponController.GetWeapon(
                side
            );


        state.profile =
            FindProfile(
                state.weapon
            );


        if (weaponController.IsForcedHand(
                side
            ))
        {
            SetHandImmediate(state);

            return;
        }


        if (state.profile == null ||
            state.profile.mode ==
            WeaponMorphMode.Hand)
        {
            SetHandImmediate(state);

            return;
        }


        // Persistent는 장착 즉시 변형
        if (state.profile.mode ==
            WeaponMorphMode.Persistent)
        {
            StartMorphToWeapon(
                state
            );

            return;
        }


        // Throwable은 장착해도 기본 Hand
        SetHandImmediate(state);
    }


    // ==================================================
    // Weapon Changed
    // ==================================================

    private void OnWeaponChanged(
        PlayerWeaponController
            .WeaponSlotSide side,
        WeaponDefinition weapon
    )
    {
        SlotMorphState state =
            GetState(side);


        state.weapon =
            weapon;


        state.profile =
            FindProfile(
                weapon
            );


        // 이미 투척해서 Hand가 없는 상태라면
        // Cooldown 종료까지 기다린다.
        if (state.phase ==
            MorphPhase.Missing ||
            state.phase ==
            MorphPhase.Respawning)
        {
            return;
        }


        EvaluateNormalTarget(
            side,
            state
        );
    }


    // ==================================================
    // Forced Hand
    // ==================================================

    private void OnForcedHandChanged(
        PlayerWeaponController
            .WeaponSlotSide side,
        bool forced
    )
    {
        SlotMorphState state =
            GetState(side);


        if (state.phase ==
            MorphPhase.Missing ||
            state.phase ==
            MorphPhase.Respawning)
        {
            return;
        }


        if (forced)
        {
            StartMorphToHand(
                state
            );

            return;
        }


        EvaluateNormalTarget(
            side,
            state
        );
    }


    // ==================================================
    // Evaluate Current Weapon
    // ==================================================

    private void EvaluateNormalTarget(
        PlayerWeaponController
            .WeaponSlotSide side,
        SlotMorphState state
    )
    {
        if (weaponController != null &&
            weaponController.IsForcedHand(
                side
            ))
        {
            StartMorphToHand(state);

            return;
        }


        if (state.profile == null ||
            state.profile.mode ==
            WeaponMorphMode.Hand)
        {
            StartMorphToHand(state);

            return;
        }


        if (state.profile.mode ==
            WeaponMorphMode.Persistent)
        {
            StartMorphToWeapon(state);

            return;
        }


        // Throwable
        // 장착 상태의 기본 모습은 Hand
        StartMorphToHand(state);
    }


    // ==================================================
    // Throwable Public API
    //
    // SplashBombWeaponBehaviour에서 호출
    // ==================================================

    public void NotifyThrowableChargeStarted(
        PlayerWeaponController
            .WeaponSlotSide side
    )
    {
        SlotMorphState state =
            GetState(side);


        RefreshProfile(
            side,
            state
        );


        if (state.profile == null ||
            state.profile.mode !=
            WeaponMorphMode.Throwable)
        {
            return;
        }


        StartMorphToWeapon(
            state
        );
    }


    public void NotifyThrowableChargeCancelled(
        PlayerWeaponController
            .WeaponSlotSide side
    )
    {
        SlotMorphState state =
            GetState(side);


        if (state.phase ==
            MorphPhase.Missing)
        {
            return;
        }


        StartMorphToHand(
            state
        );
    }


    public void NotifyThrowableThrown(
        PlayerWeaponController
            .WeaponSlotSide side,
        float cooldownDuration
    )
    {
        SlotMorphState state =
            GetState(side);


        state.phase =
            MorphPhase.Missing;


        state.phaseTime =
            0f;


        state.alpha =
            0f;


        state.wobbleAmount =
            0f;


        state.respawnAt =
            Time.time
            +
            Mathf.Max(
                0f,
                cooldownDuration
            );
    }


    // ==================================================
    // Slot State Update
    // ==================================================

    private void UpdateSlotState(
        PlayerWeaponController
            .WeaponSlotSide side,
        SlotMorphState state
    )
    {
        // ==========================================
        // Throwable Cooldown
        // ==========================================

        if (state.phase ==
            MorphPhase.Missing)
        {
            state.alpha =
                0f;


            if (Time.time >=
                state.respawnAt)
            {
                StartRespawn(
                    state
                );
            }


            return;
        }


        // ==========================================
        // Hand
        // ==========================================

        if (state.phase ==
            MorphPhase.Hand)
        {
            state.scaleFactor =
                Vector2.one;

            state.rotationOffset =
                0f;

            state.inkBlend =
                0f;

            state.wobbleAmount =
                0f;

            state.alpha =
                1f;

            return;
        }


        // ==========================================
        // Weapon
        // ==========================================

        if (state.phase ==
            MorphPhase.Weapon)
        {
            Vector2 targetScale =
                GetTargetScale(
                    state
                );


            state.scaleFactor =
                targetScale;


            state.rotationOffset =
                GetTargetRotation(
                    state
                );


            state.inkBlend =
                GetFinalInkBlend(
                    state
                );


            state.wobbleAmount =
                0f;


            state.alpha =
                1f;


            return;
        }


        // ==========================================
        // Respawn
        // ==========================================

        if (state.phase ==
            MorphPhase.Respawning)
        {
            UpdateRespawn(
                side,
                state
            );

            return;
        }


        // ==========================================
        // Transition
        // ==========================================

        float duration =
            GetPhaseDuration(
                state.phase
            );


        if (duration <=
            0.0001f)
        {
            CompleteCurrentPhase(
                side,
                state
            );

            return;
        }


        state.phaseTime +=
            Time.deltaTime;


        float t =
            Mathf.Clamp01(
                state.phaseTime
                /
                duration
            );


        t =
            Smooth01(t);


        UpdateTransitionValues(
            state,
            t
        );


        if (state.phaseTime >=
            duration)
        {
            CompleteCurrentPhase(
                side,
                state
            );
        }
    }


    // ==================================================
    // Transition Values
    // ==================================================

    private void UpdateTransitionValues(
        SlotMorphState state,
        float t
    )
    {
        // ==========================================
        // Liquefy → Weapon
        // ==========================================

        if (state.phase ==
            MorphPhase.LiquefyToWeapon)
        {
            Vector2 compressed =
                new Vector2(
                    compressionX,
                    compressionY
                );


            state.scaleFactor =
                Vector2.Lerp(
                    state.startScale,
                    compressed,
                    t
                );


            state.rotationOffset =
                Mathf.Lerp(
                    state.startRotation,
                    0f,
                    t
                );


            state.inkBlend =
                Mathf.Lerp(
                    state.startInk,
                    1f,
                    t
                );


            state.wobbleAmount =
                Mathf.Lerp(
                    state.startWobble,
                    1f,
                    t
                );


            state.alpha =
                1f;

            return;
        }


        // ==========================================
        // Morph → Weapon Shape
        // ==========================================

        if (state.phase ==
            MorphPhase.MorphToWeapon)
        {
            state.scaleFactor =
                Vector2.Lerp(
                    state.startScale,
                    GetTargetScale(state),
                    t
                );


            state.rotationOffset =
                Mathf.Lerp(
                    state.startRotation,
                    GetTargetRotation(state),
                    t
                );


            state.inkBlend =
                1f;


            state.wobbleAmount =
                1f;


            state.alpha =
                1f;

            return;
        }


        // ==========================================
        // Settle Weapon
        // ==========================================

        if (state.phase ==
            MorphPhase.SettleWeapon)
        {
            state.scaleFactor =
                Vector2.Lerp(
                    state.startScale,
                    GetTargetScale(state),
                    t
                );


            state.rotationOffset =
                Mathf.Lerp(
                    state.startRotation,
                    GetTargetRotation(state),
                    t
                );


            state.inkBlend =
                Mathf.Lerp(
                    state.startInk,
                    GetFinalInkBlend(state),
                    t
                );


            state.wobbleAmount =
                Mathf.Lerp(
                    state.startWobble,
                    0f,
                    t
                );


            state.alpha =
                1f;

            return;
        }


        // ==========================================
        // Weapon → Liquefy
        // ==========================================

        if (state.phase ==
            MorphPhase.LiquefyToHand)
        {
            state.scaleFactor =
                state.startScale;


            state.rotationOffset =
                state.startRotation;


            state.inkBlend =
                Mathf.Lerp(
                    state.startInk,
                    1f,
                    t
                );


            state.wobbleAmount =
                Mathf.Lerp(
                    state.startWobble,
                    1f,
                    t
                );


            state.alpha =
                1f;

            return;
        }


        // ==========================================
        // Ink → Hand Shape
        // ==========================================

        if (state.phase ==
            MorphPhase.MorphToHand)
        {
            state.scaleFactor =
                Vector2.Lerp(
                    state.startScale,
                    Vector2.one,
                    t
                );


            state.rotationOffset =
                Mathf.Lerp(
                    state.startRotation,
                    0f,
                    t
                );


            state.inkBlend =
                1f;


            state.wobbleAmount =
                1f;


            state.alpha =
                1f;

            return;
        }


        // ==========================================
        // Settle Hand
        // ==========================================

        if (state.phase ==
            MorphPhase.SettleHand)
        {
            state.scaleFactor =
                Vector2.Lerp(
                    state.startScale,
                    Vector2.one,
                    t
                );


            state.rotationOffset =
                Mathf.Lerp(
                    state.startRotation,
                    0f,
                    t
                );


            state.inkBlend =
                Mathf.Lerp(
                    state.startInk,
                    0f,
                    t
                );


            state.wobbleAmount =
                Mathf.Lerp(
                    state.startWobble,
                    0f,
                    t
                );


            state.alpha =
                1f;
        }
    }


    // ==================================================
    // Complete Phase
    // ==================================================

    private void CompleteCurrentPhase(
        PlayerWeaponController
            .WeaponSlotSide side,
        SlotMorphState state
    )
    {
        if (state.phase ==
            MorphPhase.LiquefyToWeapon)
        {
            BeginPhase(
                state,
                MorphPhase.MorphToWeapon
            );

            return;
        }


        if (state.phase ==
            MorphPhase.MorphToWeapon)
        {
            BeginPhase(
                state,
                MorphPhase.SettleWeapon
            );

            return;
        }


        if (state.phase ==
            MorphPhase.SettleWeapon)
        {
            state.phase =
                MorphPhase.Weapon;

            return;
        }


        if (state.phase ==
            MorphPhase.LiquefyToHand)
        {
            BeginPhase(
                state,
                MorphPhase.MorphToHand
            );

            return;
        }


        if (state.phase ==
            MorphPhase.MorphToHand)
        {
            BeginPhase(
                state,
                MorphPhase.SettleHand
            );

            return;
        }


        if (state.phase ==
            MorphPhase.SettleHand)
        {
            state.phase =
                MorphPhase.Hand;

            return;
        }
    }


    // ==================================================
    // Start Weapon Morph
    // ==================================================

    private void StartMorphToWeapon(
        SlotMorphState state
    )
    {
        if (state.profile == null)
        {
            StartMorphToHand(state);

            return;
        }


        if (state.phase ==
            MorphPhase.Weapon)
        {
            // 이미 같은 Target이면 유지
            return;
        }


        BeginPhase(
            state,
            MorphPhase.LiquefyToWeapon
        );
    }


    // ==================================================
    // Start Hand Morph
    // ==================================================

    private void StartMorphToHand(
        SlotMorphState state
    )
    {
        if (state.phase ==
            MorphPhase.Hand)
        {
            return;
        }


        if (state.phase ==
            MorphPhase.Missing ||
            state.phase ==
            MorphPhase.Respawning)
        {
            return;
        }


        BeginPhase(
            state,
            MorphPhase.LiquefyToHand
        );
    }


    // ==================================================
    // Begin Phase
    // ==================================================

    private void BeginPhase(
        SlotMorphState state,
        MorphPhase newPhase
    )
    {
        state.phase =
            newPhase;


        state.phaseTime =
            0f;


        state.startScale =
            state.scaleFactor;


        state.startRotation =
            state.rotationOffset;


        state.startInk =
            state.inkBlend;


        state.startWobble =
            state.wobbleAmount;


        state.startAlpha =
            state.alpha;
    }


    // ==================================================
    // Respawn
    // ==================================================

    private void StartRespawn(
        SlotMorphState state
    )
    {
        state.phase =
            MorphPhase.Respawning;


        state.phaseTime =
            0f;


        state.scaleFactor =
            Vector2.zero;


        state.rotationOffset =
            0f;


        state.inkBlend =
            1f;


        state.wobbleAmount =
            0.8f;


        state.alpha =
            0f;
    }


    private void UpdateRespawn(
        PlayerWeaponController
            .WeaponSlotSide side,
        SlotMorphState state
    )
    {
        float safeDuration =
            Mathf.Max(
                respawnDuration,
                0.01f
            );


        state.phaseTime +=
            Time.deltaTime;


        float t =
            Mathf.Clamp01(
                state.phaseTime
                /
                safeDuration
            );


        // ==========================================
        // Alpha
        // ==========================================

        state.alpha =
            Smooth01(
                Mathf.Clamp01(
                    t / 0.35f
                )
            );


        // ==========================================
        // Scale
        //
        // 0 → 1.15 → 1
        // ==========================================

        float scale;


        if (t <
            0.65f)
        {
            float firstT =
                Smooth01(
                    t / 0.65f
                );


            scale =
                Mathf.Lerp(
                    0f,
                    respawnOvershoot,
                    firstT
                );
        }
        else
        {
            float secondT =
                Smooth01(
                    (t - 0.65f)
                    / 0.35f
                );


            scale =
                Mathf.Lerp(
                    respawnOvershoot,
                    1f,
                    secondT
                );
        }


        state.scaleFactor =
            new Vector2(
                scale,
                scale
            );


        state.rotationOffset =
            0f;


        state.inkBlend =
            Mathf.Lerp(
                1f,
                0f,
                Smooth01(t)
            );


        state.wobbleAmount =
            Mathf.Lerp(
                0.8f,
                0f,
                Smooth01(t)
            );


        if (t >= 1f)
        {
            SetHandImmediate(
                state
            );


            // Respawn 완료 후
            // 현재 장착 Weapon을 다시 확인.
            //
            // Persistent라면 다시 Weapon으로 변형.
            // Throwable이라면 Hand 유지.
            EvaluateNormalTarget(
                side,
                state
            );
        }
    }


    // ==================================================
    // Immediate Hand
    // ==================================================

    private void SetHandImmediate(
        SlotMorphState state
    )
    {
        state.phase =
            MorphPhase.Hand;


        state.phaseTime =
            0f;


        state.scaleFactor =
            Vector2.one;


        state.rotationOffset =
            0f;


        state.inkBlend =
            0f;


        state.wobbleAmount =
            0f;


        state.alpha =
            1f;
    }


    // ==================================================
    // Apply Logical Hands
    // ==================================================

    private void ApplyLogicalHands()
    {
        bool front =
            directionController != null &&
            directionController.CurrentFacing ==
            PlayerVisualDirectionController
                .FacingDirection.Front;


        // ==========================================
        // FRONT
        //
        // Character Right Hand = Left Visual
        // Character Left Hand  = Right Visual
        // ==========================================

        if (front)
        {
            ApplyStateToVisual(
                logicalRight,
                leftHand,
                leftHandRenderer,
                leftBaseScale,
                leftBaseEuler,
                leftBaseColor,
                0f
            );


            ApplyStateToVisual(
                logicalLeft,
                rightHand,
                rightHandRenderer,
                rightBaseScale,
                rightBaseEuler,
                rightBaseColor,
                1.73f
            );


            return;
        }


        // ==========================================
        // BACK / LEFT / RIGHT
        //
        // 기존 프로젝트에서 확정한 매핑
        // ==========================================

        ApplyStateToVisual(
            logicalLeft,
            leftHand,
            leftHandRenderer,
            leftBaseScale,
            leftBaseEuler,
            leftBaseColor,
            1.73f
        );


        ApplyStateToVisual(
            logicalRight,
            rightHand,
            rightHandRenderer,
            rightBaseScale,
            rightBaseEuler,
            rightBaseColor,
            0f
        );
    }


    // ==================================================
    // Apply Visual
    // ==================================================

    private void ApplyStateToVisual(
        SlotMorphState state,
        Transform hand,
        SpriteRenderer renderer,
        Vector3 baseScale,
        Vector3 baseEuler,
        Color baseColor,
        float phaseOffset
    )
    {
        if (hand == null ||
            renderer == null)
        {
            return;
        }


        float time =
            Time.time;


        float wobbleX =
            Mathf.Sin(
                time
                * wobbleSpeed
                * Mathf.PI
                * 2f
                + phaseOffset
            );


        float wobbleY =
            Mathf.Sin(
                time
                * wobbleSpeed
                * 1.27f
                * Mathf.PI
                * 2f
                + 1.31f
                + phaseOffset
            );


        float wobbleRot =
            Mathf.Sin(
                time
                * wobbleSpeed
                * 0.73f
                * Mathf.PI
                * 2f
                + 0.61f
                + phaseOffset
            );


        float scaleX =
            state.scaleFactor.x
            *
            (
                1f
                +
                wobbleX
                * wobbleScaleX
                * state.wobbleAmount
            );


        float scaleY =
            state.scaleFactor.y
            *
            (
                1f
                +
                wobbleY
                * wobbleScaleY
                * state.wobbleAmount
            );


        hand.localScale =
            new Vector3(
                baseScale.x * scaleX,
                baseScale.y * scaleY,
                baseScale.z
            );


        Vector3 rotation =
            baseEuler;


        rotation.z +=
            state.rotationOffset;


        rotation.z +=
            wobbleRot
            * wobbleRotation
            * state.wobbleAmount;


        hand.localEulerAngles =
            rotation;


        Color targetColor =
            Color.Lerp(
                baseColor,
                inkColor,
                Mathf.Clamp01(
                    state.inkBlend
                )
            );


        targetColor.a =
            baseColor.a
            *
            Mathf.Clamp01(
                state.alpha
            );


        renderer.color =
            targetColor;
    }


    // ==================================================
    // Profiles
    // ==================================================

    private WeaponMorphProfile FindProfile(
        WeaponDefinition weapon
    )
    {
        if (weapon == null)
        {
            return null;
        }


        for (int i = 0;
             i < weaponProfiles.Count;
             i++)
        {
            WeaponMorphProfile profile =
                weaponProfiles[i];


            if (profile == null)
            {
                continue;
            }


            if (profile.weapon ==
                weapon)
            {
                return profile;
            }
        }


        return null;
    }


    private void RefreshProfile(
        PlayerWeaponController
            .WeaponSlotSide side,
        SlotMorphState state
    )
    {
        if (weaponController == null)
        {
            return;
        }


        state.weapon =
            weaponController.GetWeapon(
                side
            );


        state.profile =
            FindProfile(
                state.weapon
            );
    }


    private Vector2 GetTargetScale(
        SlotMorphState state
    )
    {
        if (state.profile == null)
        {
            return Vector2.one;
        }


        return state.profile.targetScale;
    }


    private float GetTargetRotation(
        SlotMorphState state
    )
    {
        if (state.profile == null)
        {
            return 0f;
        }


        return state.profile.targetRotation;
    }


    private float GetFinalInkBlend(
        SlotMorphState state
    )
    {
        if (state.profile == null)
        {
            return 0f;
        }


        return state.profile.finalInkBlend;
    }


    // ==================================================
    // State
    // ==================================================

    private SlotMorphState GetState(
        PlayerWeaponController
            .WeaponSlotSide side
    )
    {
        if (side ==
            PlayerWeaponController
                .WeaponSlotSide.Right)
        {
            return logicalRight;
        }


        return logicalLeft;
    }


    // ==================================================
    // Durations
    // ==================================================

    private float GetPhaseDuration(
        MorphPhase phase
    )
    {
        if (phase ==
            MorphPhase.LiquefyToWeapon)
        {
            return liquefyDuration;
        }


        if (phase ==
            MorphPhase.MorphToWeapon)
        {
            return morphDuration;
        }


        if (phase ==
            MorphPhase.SettleWeapon)
        {
            return settleDuration;
        }


        if (phase ==
            MorphPhase.LiquefyToHand)
        {
            return returnLiquefyDuration;
        }


        if (phase ==
            MorphPhase.MorphToHand)
        {
            return returnMorphDuration;
        }


        if (phase ==
            MorphPhase.SettleHand)
        {
            return returnSettleDuration;
        }


        return 0f;
    }


    // ==================================================
    // Events
    // ==================================================

    private void SubscribeEvents()
    {
        if (weaponController == null)
        {
            return;
        }


        weaponController.WeaponChanged -=
            OnWeaponChanged;


        weaponController.WeaponChanged +=
            OnWeaponChanged;


        weaponController.ForcedHandChanged -=
            OnForcedHandChanged;


        weaponController.ForcedHandChanged +=
            OnForcedHandChanged;
    }


    private void UnsubscribeEvents()
    {
        if (weaponController == null)
        {
            return;
        }


        weaponController.WeaponChanged -=
            OnWeaponChanged;


        weaponController.ForcedHandChanged -=
            OnForcedHandChanged;
    }


    // ==================================================
    // Capture
    // ==================================================

    [ContextMenu(
        "CAPTURE - Current Hands As Base"
    )]
    private void CaptureBaseState()
    {
        if (leftHand != null)
        {
            leftBaseScale =
                leftHand.localScale;


            leftBaseEuler =
                leftHand.localEulerAngles;
        }


        if (rightHand != null)
        {
            rightBaseScale =
                rightHand.localScale;


            rightBaseEuler =
                rightHand.localEulerAngles;
        }


        leftBaseColor =
            leftHandRenderer != null
            ? leftHandRenderer.color
            : Color.white;


        rightBaseColor =
            rightHandRenderer != null
            ? rightHandRenderer.color
            : Color.white;


#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(
            this
        );
#endif
    }


    // ==================================================
    // Auto Find
    // ==================================================

    [ContextMenu(
        "AUTO FIND - Hand Morph References"
    )]
    private void AutoFindReferences()
    {
        if (weaponController == null)
        {
            weaponController =
                GetComponentInParent<
                    PlayerWeaponController
                >();
        }


        if (directionController == null)
        {
            directionController =
                GetComponent<
                    PlayerVisualDirectionController
                >();
        }


        Transform visualOffset =
            transform.Find(
                "VisualOffset"
            );


        if (visualOffset == null)
        {
            Debug.LogError(
                "[HandInkMorph] VisualOffset을 찾을 수 없습니다.",
                this
            );

            return;
        }


        // ==========================================
        // LEFT
        // 현재 Hierarchy 대응
        // ==========================================

        leftHand =
            visualOffset.Find(
                "Hands/"
                + "LeftHandAnchor/"
                + "LeftHandAimAnchor/"
                + "LeftHand"
            );


        if (leftHand != null)
        {
            leftHandRenderer =
                leftHand.GetComponent<
                    SpriteRenderer
                >();
        }


        // ==========================================
        // RIGHT
        // ==========================================

        rightHand =
            visualOffset.Find(
                "Hands/"
                + "RightHandAnchor/"
                + "RightHandAimAnchor/"
                + "RightHand"
            );


        if (rightHand != null)
        {
            rightHandRenderer =
                rightHand.GetComponent<
                    SpriteRenderer
                >();
        }


#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(
            this
        );
#endif


        Debug.Log(
            "[HandInkMorph] "
            + "Slot 기반 References 연결 완료.",
            this
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


        return
            value
            * value
            * (
                3f
                -
                2f
                * value
            );
    }


    // ==================================================
    // Restore
    // ==================================================

    private void RestoreHands()
    {
        if (leftHand != null)
        {
            leftHand.localScale =
                leftBaseScale;


            leftHand.localEulerAngles =
                leftBaseEuler;
        }


        if (rightHand != null)
        {
            rightHand.localScale =
                rightBaseScale;


            rightHand.localEulerAngles =
                rightBaseEuler;
        }


        if (leftHandRenderer != null)
        {
            leftHandRenderer.color =
                leftBaseColor;
        }


        if (rightHandRenderer != null)
        {
            rightHandRenderer.color =
                rightBaseColor;
        }
    }


    // ==================================================
    // Disable
    // ==================================================

    private void OnDisable()
    {
        UnsubscribeEvents();


        if (!initialized)
        {
            return;
        }


        RestoreHands();
    }
}