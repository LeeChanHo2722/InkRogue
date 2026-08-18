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

    [SerializeField]
    private InputActionReference
        leftWeaponWheelAction;

    [SerializeField]
    private InputActionReference
        rightWeaponWheelAction;


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

    [SerializeField]
    [Min(0f)]
    private float weaponWheelDeadZone =
        80f;


    public bool InputEnabled =>
        inputEnabled;

    public bool IsWeaponWheelOpen =>
        isWeaponWheelOpen;

    public WeaponSlotSide ActiveWeaponWheelSide =>
        activeWeaponWheelSide;

    public int HighlightedWeaponSlotIndex =>
        highlightedWeaponSlotIndex;

    public Vector2 WeaponWheelOrigin =>
        weaponWheelOrigin;

    private bool isWeaponWheelOpen;

    private WeaponSlotSide activeWeaponWheelSide;

    private int highlightedWeaponSlotIndex = -1;

    private Vector2 weaponWheelOrigin;


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

    private static readonly Vector2[] WeaponWheelSlotDirections =
    {
        Vector2.up,
        new Vector2(0.8660254f, -0.5f),
        new Vector2(-0.8660254f, -0.5f)
    };

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


    private void OnEnable()
    {
        if (leftWeaponWheelAction?.action != null)
        {
            leftWeaponWheelAction.action.started +=
                OnLeftWeaponWheelStarted;
            leftWeaponWheelAction.action.canceled +=
                OnLeftWeaponWheelCanceled;
            leftWeaponWheelAction.action.Enable();
        }

        if (rightWeaponWheelAction?.action != null)
        {
            rightWeaponWheelAction.action.started +=
                OnRightWeaponWheelStarted;
            rightWeaponWheelAction.action.canceled +=
                OnRightWeaponWheelCanceled;
            rightWeaponWheelAction.action.Enable();
        }
    }


    // ==================================================
    // Disable
    //
    // Component 자체가 꺼지는 경우에도
    // 현재 사용 중인 무기를 반드시 취소.
    // ==================================================

    private void OnDisable()
    {
        if (leftWeaponWheelAction?.action != null)
        {
            leftWeaponWheelAction.action.started -=
                OnLeftWeaponWheelStarted;
            leftWeaponWheelAction.action.canceled -=
                OnLeftWeaponWheelCanceled;
            leftWeaponWheelAction.action.Disable();
        }

        if (rightWeaponWheelAction?.action != null)
        {
            rightWeaponWheelAction.action.started -=
                OnRightWeaponWheelStarted;
            rightWeaponWheelAction.action.canceled -=
                OnRightWeaponWheelCanceled;
            rightWeaponWheelAction.action.Disable();
        }

        ResetWeaponWheelState();
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

        if (isWeaponWheelOpen)
        {
            UpdateWeaponWheelSelection();
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


    private void OnLeftWeaponWheelStarted(
        InputAction.CallbackContext context
    )
    {
        TryBeginWeaponWheel(
            WeaponSlotSide.Left
        );
    }


    private void OnLeftWeaponWheelCanceled(
        InputAction.CallbackContext context
    )
    {
        TryEndWeaponWheel(
            WeaponSlotSide.Left
        );
    }


    private void OnRightWeaponWheelStarted(
        InputAction.CallbackContext context
    )
    {
        TryBeginWeaponWheel(
            WeaponSlotSide.Right
        );
    }


    private void OnRightWeaponWheelCanceled(
        InputAction.CallbackContext context
    )
    {
        TryEndWeaponWheel(
            WeaponSlotSide.Right
        );
    }


    private void TryBeginWeaponWheel(
        WeaponSlotSide side
    )
    {
        if (!inputEnabled ||
            Time.timeScale <= 0f ||
            weaponController == null ||
            Mouse.current == null ||
            isWeaponWheelOpen)
        {
            return;
        }

        activeWeaponWheelSide = side;
        isWeaponWheelOpen = true;
        highlightedWeaponSlotIndex = -1;
        weaponWheelOrigin =
            Mouse.current.position.ReadValue();

        CancelAllSlots();
    }


    private void TryEndWeaponWheel(
        WeaponSlotSide side
    )
    {
        if (!isWeaponWheelOpen ||
            activeWeaponWheelSide != side)
        {
            return;
        }

        if (highlightedWeaponSlotIndex >= 0)
        {
            TrySelectWeapon(
                activeWeaponWheelSide,
                highlightedWeaponSlotIndex
            );
        }

        ResetWeaponWheelState();
    }


    private void UpdateWeaponWheelSelection()
    {
        Vector2 mousePosition =
            Mouse.current.position.ReadValue();

        Vector2 direction =
            mousePosition - weaponWheelOrigin;

        float deadZoneSqr =
            weaponWheelDeadZone * weaponWheelDeadZone;

        if (direction.sqrMagnitude <= deadZoneSqr)
        {
            highlightedWeaponSlotIndex = -1;
            return;
        }

        direction.Normalize();

        int closestSlotIndex = 0;
        float closestDot = float.NegativeInfinity;

        for (int i = 0;
             i < WeaponWheelSlotDirections.Length;
             i++)
        {
            float dot = Vector2.Dot(
                direction,
                WeaponWheelSlotDirections[i]
            );

            if (dot > closestDot)
            {
                closestDot = dot;
                closestSlotIndex = i;
            }
        }

        WeaponDefinition[] loadout =
            GetCombatLoadout(
                activeWeaponWheelSide
            );

        highlightedWeaponSlotIndex =
            loadout[closestSlotIndex] != null
                ? closestSlotIndex
                : -1;
    }


    private void ResetWeaponWheelState()
    {
        isWeaponWheelOpen = false;
        activeWeaponWheelSide = default;
        highlightedWeaponSlotIndex = -1;
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
            ResetWeaponWheelState();
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
