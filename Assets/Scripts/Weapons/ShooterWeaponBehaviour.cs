using UnityEngine;
using UnityEngine.InputSystem;

public class ShooterWeaponBehaviour
    : PlayerWeaponBehaviour
{
    // ==================================================
    // Slot Runtime
    // ==================================================

    private class SlotRuntimeState
    {
        public float nextFireTime;

        public int continuousShotCount;

        public bool isUsing;
    }


    private readonly SlotRuntimeState
        rightState =
        new SlotRuntimeState();


    private readonly SlotRuntimeState
        leftState =
        new SlotRuntimeState();


    // ==================================================
    // Shooter Config
    //
    // 기존 PlayerShoot에 있던 설정을
    // ShooterWeaponBehaviour가 직접 소유한다.
    // ==================================================

    [Header("Shooter Config")]

    [SerializeField]
    private GameObject bulletPrefab;


    [SerializeField]
    private float fireRate = 6f;


    [SerializeField]
    private float bulletDamage = 1f;


    // ==================================================
    // Ink Cost
    // ==================================================

    [Header("Ink Cost")]

    [SerializeField]
    [Range(0f, 1f)]
    [Tooltip("최대 Ink 기준 초당 소비 비율")]
    private float inkUsePerSecondPercent =
        0.10f;


    // ==================================================
    // Spray
    // ==================================================

    [Header("Spray")]

    [SerializeField]
    private float spreadIncreasePerShot =
        2.5f;


    [SerializeField]
    private float maxSpreadAngle =
        8f;


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


    public float FireRate =>
        fireRate;


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


        fireRate =
            Mathf.Max(
                0.01f,
                fireRate
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
    // Press
    // ==================================================

    public override void UsePressed(
        WeaponUseContext context
    )
    {
        SlotRuntimeState state =
            GetState(
                context.SlotSide
            );


        state.continuousShotCount =
            0;


        state.isUsing =
            false;
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


        // ==========================================
        // Context
        // ==========================================

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


        // ==========================================
        // Ink Empty
        // ==========================================

        if (inkResource.IsEmpty)
        {
            context.Controller.SetForcedHand(
                context.SlotSide,
                true
            );


            StopSlot(state);

            return;
        }


        // 다음 유효한 공격 시
        // Forced Hand 해제
        if (context.Controller.IsForcedHand(
                context.SlotSide
            ))
        {
            context.Controller.SetForcedHand(
                context.SlotSide,
                false
            );
        }


        // ==========================================
        // Bullet Prefab
        // ==========================================

        if (bulletPrefab == null)
        {
            StopSlot(state);

            return;
        }


        // ==========================================
        // Use Point
        //
        // 더 이상 Legacy firePoint를 사용하지 않는다.
        //
        // Right Slot -> RightFirePoint
        // Left Slot  -> LeftFirePoint
        // ==========================================

        Transform usePoint =
            context.UsePoint;


        if (usePoint == null)
        {
            StopSlot(state);

            return;
        }


        // ==========================================
        // Ink Cost
        // ==========================================

        float inkCostThisFrame =
            inkResource.MaxInk
            *
            inkUsePerSecondPercent
            *
            context.InkCostMultiplier
            *
            Time.deltaTime;


        float actualSpent =
            inkResource.SpendInk(
                inkCostThisFrame
            );


        if (actualSpent <= 0f)
        {
            context.Controller.SetForcedHand(
                context.SlotSide,
                true
            );


            StopSlot(state);

            return;
        }


        state.isUsing =
            true;


        // ==========================================
        // Fire Rate
        // ==========================================

        if (Time.time <
            state.nextFireTime)
        {
            return;
        }


        Shoot(
            context,
            usePoint,
            state
        );


        float safeFireRate =
            Mathf.Max(
                0.01f,
                fireRate
            );


        state.nextFireTime =
            Time.time
            +
            1f / safeFireRate;
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
    // ==================================================

    public override void CancelUse()
    {
        StopSlot(
            rightState
        );


        StopSlot(
            leftState
        );
    }


    // ==================================================
    // Cancel One Slot
    // ==================================================

    public override void CancelUse(
        WeaponSlotSide side
    )
    {
        StopSlot(
            GetState(side)
        );
    }


    // ==================================================
    // Shoot
    // ==================================================

    private void Shoot(
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


        if (mainCamera == null ||
            Mouse.current == null)
        {
            return;
        }


        // ==========================================
        // Mouse World Position
        // ==========================================

        Vector2 mouseScreenPosition =
            Mouse.current
                .position
                .ReadValue();


        float cameraDistance =
            Mathf.Abs(
                usePoint.position.z
                -
                mainCamera
                    .transform
                    .position.z
            );


        Vector3 screenPosition =
            new Vector3(
                mouseScreenPosition.x,
                mouseScreenPosition.y,
                cameraDistance
            );


        Vector3 mouseWorldPosition =
            mainCamera.ScreenToWorldPoint(
                screenPosition
            );


        // ==========================================
        // Actual UsePoint -> Mouse Aim
        // ==========================================

        Vector2 aimDirection =
            new Vector2(
                mouseWorldPosition.x
                -
                usePoint.position.x,

                mouseWorldPosition.y
                -
                usePoint.position.y
            );


        if (aimDirection.sqrMagnitude <
            0.0001f)
        {
            return;
        }


        float baseAngle =
            Mathf.Atan2(
                aimDirection.y,
                aimDirection.x
            )
            *
            Mathf.Rad2Deg;


        // ==========================================
        // Spread
        //
        // 첫 발은 정확.
        // 연속 사격부터 확산.
        // ==========================================

        float spreadAngle =
            0f;


        if (state.continuousShotCount > 0)
        {
            float currentMaxSpread =
                Mathf.Min(
                    state.continuousShotCount
                    *
                    spreadIncreasePerShot,

                    maxSpreadAngle
                );


            spreadAngle =
                Random.Range(
                    -currentMaxSpread,
                    currentMaxSpread
                );
        }


        Quaternion bulletRotation =
            Quaternion.Euler(
                0f,
                0f,
                baseAngle
                +
                spreadAngle
            );


        // ==========================================
        // Spawn
        // ==========================================

        GameObject bulletObject =
            Instantiate(
                bulletPrefab,
                usePoint.position,
                bulletRotation
            );


        // ==========================================
        // Slot Damage Multiplier
        // ==========================================

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
        }


        // ==========================================
        // Audio
        // ==========================================

        if (GameAudioManager.Instance != null)
        {
            GameAudioManager.Instance
                .PlayShoot();
        }


        // ==========================================
        // Ink Start
        // ==========================================

        shotInkStart?
            .PaintShotStart(
                usePoint.position
            );


        state.continuousShotCount++;
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


        state.continuousShotCount =
            0;
    }


    // ==================================================
    // Auto Find
    // ==================================================

    [ContextMenu(
        "AUTO FIND - Shooter References"
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