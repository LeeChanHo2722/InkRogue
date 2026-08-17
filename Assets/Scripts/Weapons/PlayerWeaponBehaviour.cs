using UnityEngine;


// ==================================================
// Weapon Use Context
// ==================================================

public struct WeaponUseContext
{
    public PlayerWeaponController Controller;

    public WeaponSlotSide SlotSide;

    public WeaponDefinition Weapon;

    public Transform UsePoint;

    public float DamageMultiplier;

    public float InkCostMultiplier;


    public WeaponUseContext(
        PlayerWeaponController controller,
        WeaponSlotSide slotSide,
        WeaponDefinition weapon,
        Transform usePoint,
        float damageMultiplier,
        float inkCostMultiplier
    )
    {
        Controller = controller;
        SlotSide = slotSide;
        Weapon = weapon;
        UsePoint = usePoint;
        DamageMultiplier = damageMultiplier;
        InkCostMultiplier = inkCostMultiplier;
    }


    public Vector3 Origin
    {
        get
        {
            if (UsePoint != null)
            {
                return UsePoint.position;
            }

            if (Controller != null)
            {
                return Controller.transform.position;
            }

            return Vector3.zero;
        }
    }
}


// ==================================================
// Base Weapon Behaviour
// ==================================================

public abstract class PlayerWeaponBehaviour
    : MonoBehaviour
{
    [Header("Weapon Definition")]

    [SerializeField]
    protected WeaponDefinition weaponDefinition;


    public WeaponDefinition Definition
    {
        get
        {
            return weaponDefinition;
        }
    }


    public abstract bool IsUsing
    {
        get;
    }


    public virtual bool IsUsingSlot(
        WeaponSlotSide side
    )
    {
        return IsUsing;
    }


    public abstract void UsePressed(
        WeaponUseContext context
    );


    public abstract void UseHeld(
        WeaponUseContext context
    );


    public abstract void UseReleased(
        WeaponUseContext context
    );


    // 전체 사용 취소
    public abstract void CancelUse();


    // 특정 Slot만 취소
    public virtual void CancelUse(
        WeaponSlotSide side
    )
    {
        CancelUse();
    }
}