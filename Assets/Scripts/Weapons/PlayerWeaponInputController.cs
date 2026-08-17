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