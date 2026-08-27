using UnityEngine;

// Second main weapon. Slower than the Shooter on the first shot, but a
// sustained trigger spins it up to a very high rate of fire - at the cost
// of accuracy. One spinRatio drives both fire rate and spread, so the two
// can never disagree.
public class MachineGunWeaponBehaviour
    : PlayerWeaponBehaviour
{
    // ==================================================
    // Slot Runtime
    // ==================================================

    private class SlotRuntimeState
    {
        public float nextFireTime;

        public float spinRatio;

        public bool heldThisFrame;

        public bool isUsing;
    }


    private readonly SlotRuntimeState
        rightState =
        new SlotRuntimeState();


    private readonly SlotRuntimeState
        leftState =
        new SlotRuntimeState();


    // ==================================================
    // Machine Gun Config
    // ==================================================

    [Header("Machine Gun Config")]

    [SerializeField]
    private GameObject bulletPrefab;


    [Tooltip("Damage matches the Shooter: the strength is the ramp.")]
    [SerializeField]
    private float bulletDamage = 1f;


    [Min(0.01f)]
    [SerializeField]
    private float initialFireRate = 4f;


    [Min(0.01f)]
    [SerializeField]
    private float maxFireRate = 12f;


    [Tooltip("Seconds of continuous fire to reach max spin.")]
    [Min(0.01f)]
    [SerializeField]
    private float accelerationTime = 1.5f;


    [Tooltip("Seconds to fall back from max spin to none.")]
    [Min(0.01f)]
    [SerializeField]
    private float spinDownTime = 0.5f;


    // ==================================================
    // Spread
    // ==================================================

    [Header("Spread")]

    [Min(0f)]
    [SerializeField]
    private float initialSpreadAngle = 4f;


    [Min(0f)]
    [SerializeField]
    private float maxSpreadAngle = 15f;


    // ==================================================
    // Range
    // ==================================================

    [Header("Range")]

    [Tooltip("Multiplies the Bullet Prefab lifetime, which is what "
        + "actually decides range (speed x lifeTime).")]
    [Min(0.1f)]
    [SerializeField]
    private float rangeMultiplier = 1.25f;


    // ==================================================
    // Ink Cost
    // ==================================================

    [Header("Ink Cost")]

    [Tooltip("Percent of Max Ink per fired bullet. Per shot, not per "
        + "second: spinning up to 12 shots/sec must actually cost more "
        + "ink, not become three times more efficient.")]
    [SerializeField]
    [Range(0f, 1f)]
    private float inkUsePerShotPercent =
        0.0125f;


    // ==================================================
    // References
    // ==================================================

    [Header("Runtime References")]

    [SerializeField]
    private PlayerInkResource inkResource;


    [SerializeField]
    private PlayerDive playerDive;


    [SerializeField]
    private PlayerShotInkStart shotInkStart;


    private Camera mainCamera;


    // ==================================================
    // State
    // ==================================================

    public override bool IsUsing
    {
        get
        {
            return
                rightState.isUsing
                ||
                leftState.isUsing;
        }
    }


    public override bool IsUsingSlot(
        WeaponSlotSide side
    )
    {
        return
            GetState(side)
                .isUsing;
    }


    // ==================================================
    // Upgrade API
    // ==================================================

    public float BulletDamage =>
        bulletDamage;


    public float InitialFireRate =>
        initialFireRate;


    public float MaxFireRate =>
        maxFireRate;


    public float SpinRatio =>
        Mathf.Max(
            rightState.spinRatio,
            leftState.spinRatio
        );


    public void AddBulletDamage(
        float amount
    )
    {
        bulletDamage =
            Mathf.Max(
                0f,
                bulletDamage
                +
                amount
            );
    }


    public void MultiplyFireRate(
        float multiplier
    )
    {
        if (multiplier <= 0f)
        {
            return;
        }


        initialFireRate =
            Mathf.Max(
                0.01f,
                initialFireRate
                *
                multiplier
            );


        maxFireRate =
            Mathf.Max(
                initialFireRate,
                maxFireRate
                *
                multiplier
            );
    }


    // ==================================================
    // Awake
    // ==================================================

    private void Awake()
    {
        AutoFindReferences();
    }


    // ==================================================
    // Spin Down
    //
    // Runs after every slot had its chance to fire this frame, so a slot
    // that was not held decays instead of dropping to zero instantly.
    // ==================================================

    private void LateUpdate()
    {
        TickSpin(rightState);
        TickSpin(leftState);
    }


    private void TickSpin(
        SlotRuntimeState state
    )
    {
        if (!state.heldThisFrame &&
            state.spinRatio > 0f)
        {
            state.spinRatio =
                Mathf.Max(
                    0f,
                    state.spinRatio
                    -
                    Time.deltaTime
                    /
                    Mathf.Max(
                        0.01f,
                        spinDownTime
                    )
                );
        }


        state.heldThisFrame =
            false;
    }


    // ==================================================
    // Press
    // ==================================================

    public override void UsePressed(
        WeaponUseContext context
    )
    {
        // Spin is deliberately NOT reset here: a short tap-off keeps the
        // rhythm going, a long pause decays back to the initial rate.
        GetState(context.SlotSide)
            .isUsing = false;
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


        if (context.Controller == null ||
            context.Weapon == null)
        {
            StopSlot(state);

            return;
        }


        // ==========================================
        // Dive
        // ==========================================

        if (playerDive != null &&
            playerDive.IsSwimForm)
        {
            StopSlot(state);

            return;
        }


        // ==========================================
        // Ink Resource
        // ==========================================

        if (inkResource == null)
        {
            StopSlot(state);

            return;
        }


        if (inkResource.IsEmpty)
        {
            context.Controller.SetForcedHand(
                context.SlotSide,
                true
            );


            StopSlot(state);

            return;
        }


        if (context.Controller.IsForcedHand(
                context.SlotSide
            ))
        {
            context.Controller.SetForcedHand(
                context.SlotSide,
                false
            );
        }


        if (bulletPrefab == null)
        {
            StopSlot(state);

            return;
        }


        Transform usePoint =
            context.UsePoint;


        if (usePoint == null)
        {
            StopSlot(state);

            return;
        }


        state.isUsing =
            true;


        // ==========================================
        // Spin Up
        // ==========================================

        state.heldThisFrame =
            true;


        state.spinRatio =
            Mathf.Min(
                1f,
                state.spinRatio
                +
                Time.deltaTime
                /
                Mathf.Max(
                    0.01f,
                    accelerationTime
                )
            );


        // ==========================================
        // Fire Rate
        // ==========================================

        if (Time.time <
            state.nextFireTime)
        {
            return;
        }


        // Ink is charged per bullet, and only once one actually spawned.
        // A frame blocked by the fire-rate cooldown costs nothing.
        if (!Shoot(
                context,
                usePoint,
                state
            ))
        {
            return;
        }


        float inkCostThisShot =
            inkResource.MaxInk
            *
            inkUsePerShotPercent
            *
            context.InkCostMultiplier;


        if (inkResource.SpendInk(
                inkCostThisShot
            ) <= 0f)
        {
            context.Controller.SetForcedHand(
                context.SlotSide,
                true
            );
        }


        float currentFireRate =
            Mathf.Max(
                0.01f,
                Mathf.Lerp(
                    initialFireRate,
                    maxFireRate,
                    state.spinRatio
                )
            );


        state.nextFireTime =
            Time.time
            +
            1f / currentFireRate;
    }


    // ==================================================
    // Released
    // ==================================================

    public override void UseReleased(
        WeaponUseContext context
    )
    {
        StopSlot(
            GetState(
                context.SlotSide
            )
        );
    }


    // ==================================================
    // Cancel All
    //
    // A weapon swap or a death is a hard stop, so the spin is cleared.
    // ==================================================

    public override void CancelUse()
    {
        ResetSlot(rightState);
        ResetSlot(leftState);
    }


    public override void CancelUse(
        WeaponSlotSide side
    )
    {
        ResetSlot(
            GetState(side)
        );
    }


    // ==================================================
    // Shoot
    // ==================================================

    private bool Shoot(
        WeaponUseContext context,
        Transform usePoint,
        SlotRuntimeState state
    )
    {
        if (mainCamera == null)
        {
            mainCamera =
                Camera.main;
        }


        if (!WeaponAim.TryGetAimAngle(
                usePoint,
                mainCamera,
                out float baseAngle
            ))
        {
            return false;
        }


        // ==========================================
        // Spread
        //
        // Same spinRatio as the fire rate: faster always means wider.
        // ==========================================

        float currentSpread =
            Mathf.Lerp(
                initialSpreadAngle,
                maxSpreadAngle,
                state.spinRatio
            );


        float spreadAngle =
            currentSpread > 0f
                ? Random.Range(
                    -currentSpread,
                    currentSpread
                )
                : 0f;


        Quaternion bulletRotation =
            Quaternion.Euler(
                0f,
                0f,
                baseAngle
                +
                spreadAngle
            );


        GameObject bulletObject =
            Instantiate(
                bulletPrefab,
                usePoint.position,
                bulletRotation
            );


        Bullet bulletComponent =
            bulletObject.GetComponent<
                Bullet
            >();


        if (bulletComponent != null)
        {
            bulletComponent.damage =
                bulletDamage
                *
                context.DamageMultiplier;


            // Range = speed x lifeTime, and Start() has not run yet, so
            // stretching the lifetime here is what extends the reach.
            bulletComponent.lifeTime *=
                rangeMultiplier;
        }


        if (GameAudioManager.Instance != null)
        {
            GameAudioManager.Instance
                .PlayShoot();
        }


        shotInkStart?
            .PaintShotStart(
                usePoint.position
            );


        return true;
    }


    // ==================================================
    // Runtime
    // ==================================================

    private SlotRuntimeState GetState(
        WeaponSlotSide side
    )
    {
        if (side ==
            WeaponSlotSide.Right)
        {
            return rightState;
        }


        return leftState;
    }


    private void StopSlot(
        SlotRuntimeState state
    )
    {
        state.isUsing =
            false;
    }


    private void ResetSlot(
        SlotRuntimeState state
    )
    {
        state.isUsing =
            false;


        state.spinRatio =
            0f;


        state.heldThisFrame =
            false;
    }


    // ==================================================
    // Auto Find
    // ==================================================

    [ContextMenu(
        "AUTO FIND - Machine Gun References"
    )]
    private void AutoFindReferences()
    {
        Transform root =
            transform.root;


        if (inkResource == null)
        {
            inkResource =
                root.GetComponentInChildren<
                    PlayerInkResource
                >(
                    true
                );
        }


        if (playerDive == null)
        {
            playerDive =
                root.GetComponentInChildren<
                    PlayerDive
                >(
                    true
                );
        }


        if (shotInkStart == null)
        {
            shotInkStart =
                root.GetComponentInChildren<
                    PlayerShotInkStart
                >(
                    true
                );
        }


        mainCamera =
            Camera.main;


#if UNITY_EDITOR

        UnityEditor.EditorUtility.SetDirty(
            this
        );

#endif
    }
}
