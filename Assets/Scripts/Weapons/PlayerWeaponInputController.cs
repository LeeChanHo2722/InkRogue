using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerWeaponInputController
    : MonoBehaviour
{
    // ==================================================
    // References
    // ==================================================

    [Header("References")]

    [SerializeField]
    private PlayerWeaponController
        weaponController;


    // ==================================================
    // Input State
    //
    // Floor Transition,
    // Boss Transition,
    // 향후 Weapon Wheel 등에서
    // 이 값을 통해 무기 입력을 잠근다.
    // ==================================================

    [Header("Input State")]

    [SerializeField]
    private bool inputEnabled =
        true;


    public bool InputEnabled =>
        inputEnabled;


    // ==================================================
    // Behaviours
    // ==================================================

    private PlayerWeaponBehaviour[]
        weaponBehaviours;


    private PlayerWeaponBehaviour
        activeRightBehaviour;


    private PlayerWeaponBehaviour
        activeLeftBehaviour;


    // ==================================================
    // Combat Floor Loadout
    // ==================================================

    private const int CombatLoadoutSlotCount = 3;

    private readonly WeaponDefinition[] leftCombatLoadout =
        new WeaponDefinition[CombatLoadoutSlotCount];

    private readonly WeaponDefinition[] rightCombatLoadout =
        new WeaponDefinition[CombatLoadoutSlotCount];

    private int activeLeftLoadoutIndex = -1;
    private int activeRightLoadoutIndex = -1;

    public IReadOnlyList<WeaponDefinition> LeftCombatLoadout =>
        leftCombatLoadout;

    public IReadOnlyList<WeaponDefinition> RightCombatLoadout =>
        rightCombatLoadout;

    public int ActiveLeftLoadoutIndex => activeLeftLoadoutIndex;
    public int ActiveRightLoadoutIndex => activeRightLoadoutIndex;


    // ==================================================
    // Awake
    // ==================================================

    private void Awake()
    {
        AutoFindReferences();

        RefreshBehaviours();
    }


    // ==================================================
    // Disable
    //
    // Component 자체가 꺼지는 경우에도
    // 현재 사용 중인 무기를 반드시 취소.
    // ==================================================

    private void OnDisable()
    {
        CancelAllSlots();
    }


    // ==================================================
    // Update
    // ==================================================

    private void Update()
    {
        // ==========================================
        // External Input Lock
        // ==========================================

        if (!inputEnabled)
        {
            CancelAllSlots();

            return;
        }


        // ==========================================
        // Mouse
        // ==========================================

        if (Mouse.current == null)
        {
            CancelAllSlots();

            return;
        }


        // ==========================================
        // Pause
        // ==========================================

        if (Time.timeScale <= 0f)
        {
            CancelAllSlots();

            return;
        }


        // ==========================================
        // Weapon Controller
        // ==========================================

        if (weaponController == null)
        {
            CancelAllSlots();

            return;
        }


        // ==========================================
        // RIGHT SLOT
        //
        // Left Mouse Button
        // ==========================================

        UpdateSlot(
            WeaponSlotSide.Right,

            Mouse.current
                .leftButton
                .wasPressedThisFrame,

            Mouse.current
                .leftButton
                .isPressed,

            Mouse.current
                .leftButton
                .wasReleasedThisFrame,

            ref activeRightBehaviour
        );


        // ==========================================
        // LEFT SLOT
        //
        // Right Mouse Button
        // ==========================================

        UpdateSlot(
            WeaponSlotSide.Left,

            Mouse.current
                .rightButton
                .wasPressedThisFrame,

            Mouse.current
                .rightButton
                .isPressed,

            Mouse.current
                .rightButton
                .wasReleasedThisFrame,

            ref activeLeftBehaviour
        );
    }


    // ==================================================
    // Update Slot
    // ==================================================

    private void UpdateSlot(
        WeaponSlotSide side,

        bool pressed,

        bool held,

        bool released,

        ref PlayerWeaponBehaviour
            activeBehaviour
    )
    {
        WeaponDefinition weapon =
            weaponController.GetWeapon(
                side
            );


        PlayerWeaponBehaviour behaviour =
            FindBehaviour(
                weapon
            );


        // ==========================================
        // Weapon Change
        // ==========================================

        if (behaviour !=
            activeBehaviour)
        {
            activeBehaviour
                ?.CancelUse(
                    side
                );


            activeBehaviour =
                behaviour;
        }


        if (activeBehaviour == null)
        {
            return;
        }


        WeaponUseContext context =
            weaponController
                .CreateUseContext(
                    side
                );


        // ==========================================
        // Press
        // ==========================================

        if (pressed)
        {
            activeBehaviour
                .UsePressed(
                    context
                );
        }


        // ==========================================
        // Held
        // ==========================================

        if (held)
        {
            activeBehaviour
                .UseHeld(
                    context
                );
        }


        // ==========================================
        // Release
        // ==========================================

        if (released)
        {
            activeBehaviour
                .UseReleased(
                    context
                );
        }
    }


    // ==================================================
    // Configure Combat Loadout
    // ==================================================

    public void ConfigureCombatLoadout(
        IReadOnlyList<WeaponDefinition> leftLoadout,
        IReadOnlyList<WeaponDefinition> rightLoadout
    )
    {
        CopyCombatLoadout(leftLoadout, leftCombatLoadout);
        CopyCombatLoadout(rightLoadout, rightCombatLoadout);

        if (weaponController == null)
        {
            activeLeftLoadoutIndex = -1;
            activeRightLoadoutIndex = -1;

            Debug.LogError(
                "[WeaponInput] Weapon Controller is missing. Combat loadout could not be applied.",
                this
            );

            return;
        }

        ConfigureCombatSlot(
            WeaponSlotSide.Left,
            leftCombatLoadout
        );

        ConfigureCombatSlot(
            WeaponSlotSide.Right,
            rightCombatLoadout
        );
    }


    // ==================================================
    // Cycle Combat Weapon
    // ==================================================

    public bool TryCycleWeapon(
        WeaponSlotSide side
    )
    {
        if (weaponController == null)
        {
            Debug.LogError(
                "[WeaponInput] Weapon Controller is missing. Weapon cannot be cycled.",
                this
            );

            return false;
        }

        WeaponDefinition[] loadout =
            GetCombatLoadout(side);

        WeaponDefinition currentWeapon =
            weaponController.GetWeapon(side);

        int currentIndex =
            currentWeapon != null
                ? System.Array.IndexOf(
                    loadout,
                    currentWeapon
                )
                : -1;

        SetActiveLoadoutIndex(side, currentIndex);

        int nextIndex =
            FindNextValidIndex(loadout, currentIndex);

        return TrySelectWeapon(
            side,
            nextIndex
        );
    }


    // ==================================================
    // Select Combat Weapon
    // ==================================================

    public bool TrySelectWeapon(
        WeaponSlotSide side,
        int slotIndex
    )
    {
        if (weaponController == null)
        {
            Debug.LogError(
                "[WeaponInput] Weapon Controller is missing. Weapon cannot be selected.",
                this
            );

            return false;
        }

        WeaponDefinition[] loadout =
            GetCombatLoadout(side);

        if (slotIndex < 0 ||
            slotIndex >= loadout.Length)
        {
            return false;
        }

        WeaponDefinition weapon =
            loadout[slotIndex];

        if (weapon == null)
        {
            return false;
        }

        if (weaponController.GetWeapon(side) == weapon)
        {
            SetActiveLoadoutIndex(side, slotIndex);
            return false;
        }

        SetActiveLoadoutIndex(side, slotIndex);
        EquipCombatWeapon(side, weapon);

        return true;
    }


    // ==================================================
    // Combat Loadout Helpers
    // ==================================================

    private static void CopyCombatLoadout(
        IReadOnlyList<WeaponDefinition> source,
        WeaponDefinition[] destination
    )
    {
        for (int i = 0; i < destination.Length; i++)
        {
            destination[i] =
                source != null && i < source.Count
                    ? source[i]
                    : null;
        }
    }


    private void ConfigureCombatSlot(
        WeaponSlotSide side,
        WeaponDefinition[] loadout
    )
    {
        WeaponDefinition currentWeapon =
            weaponController.GetWeapon(side);

        int currentIndex =
            currentWeapon != null
                ? System.Array.IndexOf(
                    loadout,
                    currentWeapon
                )
                : -1;

        if (currentIndex >= 0)
        {
            SetActiveLoadoutIndex(side, currentIndex);
            return;
        }

        int firstValidIndex =
            FindNextValidIndex(loadout, -1);

        SetActiveLoadoutIndex(side, firstValidIndex);

        EquipCombatWeapon(
            side,
            firstValidIndex >= 0
                ? loadout[firstValidIndex]
                : null
        );
    }


    private void EquipCombatWeapon(
        WeaponSlotSide side,
        WeaponDefinition weapon
    )
    {
        if (side == WeaponSlotSide.Right)
        {
            activeRightBehaviour?.CancelUse(side);
            activeRightBehaviour = null;
        }
        else
        {
            activeLeftBehaviour?.CancelUse(side);
            activeLeftBehaviour = null;
        }

        weaponController.SetWeapon(side, weapon);
    }


    private WeaponDefinition[] GetCombatLoadout(
        WeaponSlotSide side
    )
    {
        return side == WeaponSlotSide.Right
            ? rightCombatLoadout
            : leftCombatLoadout;
    }


    private void SetActiveLoadoutIndex(
        WeaponSlotSide side,
        int index
    )
    {
        if (side == WeaponSlotSide.Right)
        {
            activeRightLoadoutIndex = index;
            return;
        }

        activeLeftLoadoutIndex = index;
    }


    private static int FindNextValidIndex(
        WeaponDefinition[] loadout,
        int currentIndex
    )
    {
        for (int offset = 1;
             offset <= loadout.Length;
             offset++)
        {
            int index =
                (currentIndex + offset)
                % loadout.Length;

            if (loadout[index] != null)
            {
                return index;
            }
        }

        return -1;
    }


    // ==================================================
    // Find Behaviour
    // ==================================================

    private PlayerWeaponBehaviour FindBehaviour(
        WeaponDefinition definition
    )
    {
        if (definition == null ||
            weaponBehaviours == null)
        {
            return null;
        }


        for (int i = 0;
             i < weaponBehaviours.Length;
             i++)
        {
            PlayerWeaponBehaviour behaviour =
                weaponBehaviours[i];


            if (behaviour == null)
            {
                continue;
            }


            if (behaviour.Definition ==
                definition)
            {
                return behaviour;
            }
        }


        return null;
    }


    // ==================================================
    // Refresh Behaviours
    // ==================================================

    [ContextMenu(
        "REFRESH - Weapon Behaviours"
    )]
    private void RefreshBehaviours()
    {
        weaponBehaviours =
            GetComponentsInChildren<
                PlayerWeaponBehaviour
            >(
                true
            );


        Debug.Log(
            "[WeaponInput] "
            +
            weaponBehaviours.Length
            +
            " Weapon Behaviour(s) 발견.",
            this
        );
    }


    // ==================================================
    // External Input Control
    //
    // Transition / UI / Cutscene 등에서 사용.
    // ==================================================

    public void SetInputEnabled(
        bool value
    )
    {
        if (inputEnabled ==
            value)
        {
            return;
        }


        inputEnabled =
            value;


        // 입력을 잠그는 순간
        // 현재 무기 사용도 즉시 취소.
        if (!inputEnabled)
        {
            CancelAllSlots();
        }
    }


    // ==================================================
    // Cancel All
    // ==================================================

    public void CancelAllSlots()
    {
        activeRightBehaviour
            ?.CancelUse(
                WeaponSlotSide.Right
            );


        activeLeftBehaviour
            ?.CancelUse(
                WeaponSlotSide.Left
            );
    }


    // ==================================================
    // Auto Find
    // ==================================================

    [ContextMenu(
        "AUTO FIND - Weapon Input References"
    )]
    private void AutoFindReferences()
    {
        if (weaponController == null)
        {
            weaponController =
                GetComponent<
                    PlayerWeaponController
                >();
        }


        if (weaponController == null)
        {
            weaponController =
                transform.root
                    .GetComponentInChildren<
                        PlayerWeaponController
                    >(
                        true
                    );
        }


#if UNITY_EDITOR

        UnityEditor.EditorUtility.SetDirty(
            this
        );

#endif
    }
}
