using System;
using UnityEngine;

public class PlayerWeaponController : MonoBehaviour
{

    // ==================================================
    // Weapon Slot
    // ==================================================

    [Serializable]
    public class WeaponSlot
    {
        [SerializeField]
        private WeaponDefinition weapon;


        [Tooltip(
            "Ink 부족 등의 이유로 현재 Weapon 대신 " +
            "일반 손 상태가 강제되었는지"
        )]
        [SerializeField]
        private bool forcedHand;


        // ==========================================
        // Public
        // ==========================================

        public WeaponDefinition Weapon
        {
            get
            {
                return weapon;
            }
        }


        public bool ForcedHand
        {
            get
            {
                return forcedHand;
            }
        }


        // ==========================================
        // Set Weapon
        // ==========================================

        public void SetWeapon(
            WeaponDefinition newWeapon
        )
        {
            weapon =
                newWeapon;


            forcedHand =
                false;
        }


        // ==========================================
        // Force Hand
        // ==========================================

        public void SetForcedHand(
            bool value
        )
        {
            forcedHand =
                value;
        }
    }


    // ==================================================
    // Current Loadout
    // ==================================================

    [Header("Current Loadout")]

    [Tooltip("캐릭터 기준 오른손 슬롯")]
    [SerializeField]
    private WeaponSlot rightSlot =
        new WeaponSlot();


    [Tooltip("캐릭터 기준 왼손 슬롯")]
    [SerializeField]
    private WeaponSlot leftSlot =
        new WeaponSlot();


    // ==================================================
    // Slot Use Points
    //
    // 실제 공격이 시작되는 위치.
    //
    // Right
    // RightWeaponPivot / FirePoint
    //
    // Left
    // LeftWeaponPivot / LeftFirePoint
    // ==================================================

    [Header("Slot Use Points")]

    [SerializeField]
    private Transform rightUsePoint;


    [SerializeField]
    private Transform leftUsePoint;


    // ==================================================
    // Right Slot Trait
    // ==================================================

    [Header("Right Slot - Power")]

    [Tooltip("오른손 Damage 배율")]
    [SerializeField]
    private float rightDamageMultiplier =
        1f;


    [Tooltip("오른손 Ink 소비 배율")]
    [SerializeField]
    private float rightInkCostMultiplier =
        1f;


    // ==================================================
    // Left Slot Trait
    // ==================================================

    [Header("Left Slot - Efficiency")]

    [Tooltip("왼손 Damage 배율")]
    [SerializeField]
    private float leftDamageMultiplier =
        0.8f;


    [Tooltip("왼손 Ink 소비 배율")]
    [SerializeField]
    private float leftInkCostMultiplier =
        0.65f;


    // ==================================================
    // Events
    // ==================================================

    public event Action<
        WeaponSlotSide,
        WeaponDefinition
    > WeaponChanged;


    public event Action<
        WeaponSlotSide,
        bool
    > ForcedHandChanged;


    // ==================================================
    // Public Weapon Access
    // ==================================================

    public WeaponDefinition RightWeapon
    {
        get
        {
            return rightSlot.Weapon;
        }
    }


    public WeaponDefinition LeftWeapon
    {
        get
        {
            return leftSlot.Weapon;
        }
    }


    public bool IsRightHandForced
    {
        get
        {
            return rightSlot.ForcedHand;
        }
    }


    public bool IsLeftHandForced
    {
        get
        {
            return leftSlot.ForcedHand;
        }
    }


    public Transform RightUsePoint
    {
        get
        {
            return rightUsePoint;
        }
    }


    public Transform LeftUsePoint
    {
        get
        {
            return leftUsePoint;
        }
    }


    // ==================================================
    // Get Weapon
    // ==================================================

    public WeaponDefinition GetWeapon(
        WeaponSlotSide side
    )
    {
        return GetSlot(
            side
        ).Weapon;
    }


    // ==================================================
    // Get Use Point
    // ==================================================

    public Transform GetUsePoint(
        WeaponSlotSide side
    )
    {
        if (side ==
            WeaponSlotSide.Right)
        {
            return rightUsePoint;
        }


        return leftUsePoint;
    }


    // ==================================================
    // Build Runtime Context
    //
    // 앞으로 모든 Weapon은
    // 이 Context를 받아 작동한다.
    // ==================================================

    public WeaponUseContext CreateUseContext(
        WeaponSlotSide side
    )
    {
        return new WeaponUseContext(
            this,
            side,
            GetWeapon(side),
            GetUsePoint(side),
            GetDamageMultiplier(side),
            GetInkCostMultiplier(side)
        );
    }


    // ==================================================
    // Set Weapon
    // ==================================================

    public void SetWeapon(
        WeaponSlotSide side,
        WeaponDefinition weapon
    )
    {
        WeaponSlot slot =
            GetSlot(
                side
            );


        if (slot.Weapon ==
            weapon)
        {
            return;
        }


        slot.SetWeapon(
            weapon
        );


        WeaponChanged?.Invoke(
            side,
            weapon
        );


        ForcedHandChanged?.Invoke(
            side,
            false
        );


        Debug.Log(
            "[Weapon] "
            + side
            + " Slot -> "
            + GetWeaponName(
                weapon
            ),
            this
        );
    }


    // ==================================================
    // Force Hand
    //
    // Selected Weapon은 유지하면서
    // Visual / 사용만 Hand 상태로 만든다.
    // ==================================================

    public void SetForcedHand(
        WeaponSlotSide side,
        bool forced
    )
    {
        WeaponSlot slot =
            GetSlot(
                side
            );


        if (slot.ForcedHand ==
            forced)
        {
            return;
        }


        slot.SetForcedHand(
            forced
        );


        ForcedHandChanged?.Invoke(
            side,
            forced
        );


        Debug.Log(
            "[Weapon] "
            + side
            + " Forced Hand = "
            + forced,
            this
        );
    }


    // ==================================================
    // Is Forced Hand
    // ==================================================

    public bool IsForcedHand(
        WeaponSlotSide side
    )
    {
        return GetSlot(
            side
        ).ForcedHand;
    }


    // ==================================================
    // Damage Multiplier
    // ==================================================

    public float GetDamageMultiplier(
        WeaponSlotSide side
    )
    {
        if (side ==
            WeaponSlotSide.Right)
        {
            return rightDamageMultiplier;
        }


        return leftDamageMultiplier;
    }


    // ==================================================
    // Ink Multiplier
    // ==================================================

    public float GetInkCostMultiplier(
        WeaponSlotSide side
    )
    {
        if (side ==
            WeaponSlotSide.Right)
        {
            return rightInkCostMultiplier;
        }


        return leftInkCostMultiplier;
    }


    // ==================================================
    // Damage Calculation
    // ==================================================

    public float CalculateDamage(
        WeaponSlotSide side,
        float baseDamage
    )
    {
        return baseDamage
            * GetDamageMultiplier(
                side
            );
    }


    // ==================================================
    // Ink Calculation
    // ==================================================

    public float CalculateInkCost(
        WeaponSlotSide side,
        float baseInkCost
    )
    {
        return baseInkCost
            * GetInkCostMultiplier(
                side
            );
    }


    // ==================================================
    // Internal Slot
    // ==================================================

    private WeaponSlot GetSlot(
        WeaponSlotSide side
    )
    {
        if (side ==
            WeaponSlotSide.Right)
        {
            return rightSlot;
        }


        return leftSlot;
    }


    // ==================================================
    // AUTO FIND
    // ==================================================

    [ContextMenu(
        "AUTO FIND - Weapon Slot References"
    )]
    private void AutoFindReferences()
    {
        Transform playerRoot =
            transform.root;


        // ==========================================
        // WeaponRig
        // ==========================================

        Transform weaponRig =
            playerRoot.Find(
                "WeaponRig"
            );


        if (weaponRig == null)
        {
            Debug.LogError(
                "[WeaponController] WeaponRig을 찾지 못했습니다.",
                this
            );

            return;
        }


        // ==========================================
        // RIGHT
        // ==========================================

        Transform rightPivot =
            weaponRig.Find(
                "RightWeaponPivot"
            );


        if (rightPivot != null)
        {
            rightUsePoint =
                rightPivot.Find(
                    "FirePoint"
                );
        }


        // ==========================================
        // LEFT
        // ==========================================

        Transform leftPivot =
            weaponRig.Find(
                "LeftWeaponPivot"
            );


        if (leftPivot != null)
        {
            leftUsePoint =
                leftPivot.Find(
                    "LeftFirePoint"
                );
        }


#if UNITY_EDITOR

        UnityEditor.EditorUtility.SetDirty(
            this
        );

#endif


        Debug.Log(
            "[WeaponController] Slot Use Point 연결 완료.",
            this
        );
    }


    // ==================================================
    // DEBUG
    // ==================================================

    [ContextMenu(
        "DEBUG - Print Current Loadout"
    )]
    private void DebugCurrentLoadout()
    {
        Debug.Log(
            "============================\n"
            + "PLAYER WEAPON LOADOUT\n"
            + "============================\n"
            + "RIGHT : "
            + GetWeaponName(
                rightSlot.Weapon
            )
            + "\n"
            + "Damage x"
            + rightDamageMultiplier
            + " / Ink x"
            + rightInkCostMultiplier
            + "\n"
            + "UsePoint : "
            + GetTransformName(
                rightUsePoint
            )
            + "\n\n"
            + "LEFT : "
            + GetWeaponName(
                leftSlot.Weapon
            )
            + "\n"
            + "Damage x"
            + leftDamageMultiplier
            + " / Ink x"
            + leftInkCostMultiplier
            + "\n"
            + "UsePoint : "
            + GetTransformName(
                leftUsePoint
            )
            + "\n"
            + "============================",
            this
        );
    }


    // ==================================================
    // Helpers
    // ==================================================

    private string GetWeaponName(
        WeaponDefinition weapon
    )
    {
        if (weapon == null)
        {
            return "HAND / EMPTY";
        }


        if (!string.IsNullOrEmpty(
            weapon.DisplayName
        ))
        {
            return weapon.DisplayName;
        }


        return weapon.name;
    }


    private string GetTransformName(
        Transform target
    )
    {
        if (target == null)
        {
            return "NONE";
        }


        return target.name;
    }
}