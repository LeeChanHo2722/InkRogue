using System.Collections.Generic;
using UnityEngine;

public enum RunMode
{
    None,
    TwentyFloor,
    Endless
}

public class RunManager : MonoBehaviour
{
    public const int FloorLoadoutSlotsPerHand = 3;

    [Min(1)]
    public int maxFloor = 3;

    [SerializeField]
    private int currentFloor = 1;

    [SerializeField]
    private RunMode currentMode = RunMode.None;

    private readonly List<WeaponDefinition> runInventory =
        new List<WeaponDefinition>();

    private readonly WeaponDefinition[] leftFloorLoadout =
        new WeaponDefinition[FloorLoadoutSlotsPerHand];

    private readonly WeaponDefinition[] rightFloorLoadout =
        new WeaponDefinition[FloorLoadoutSlotsPerHand];

    public int CurrentFloor => currentFloor;

    public RunMode CurrentMode => currentMode;

    public IReadOnlyList<WeaponDefinition> RunInventory =>
        runInventory;

    public IReadOnlyList<WeaponDefinition> LeftFloorLoadout =>
        leftFloorLoadout;

    public IReadOnlyList<WeaponDefinition> RightFloorLoadout =>
        rightFloorLoadout;

    public bool IsLastFloor =>
        currentFloor >= maxFloor;

    public void InitializeRun(
        RunMode mode,
        IEnumerable<WeaponDefinition> startingWeapons)
    {
        runInventory.Clear();
        ClearFloorLoadouts();
        currentMode = mode;

        if (startingWeapons == null)
        {
            Debug.LogError(
                "RunManager requires a non-null starting weapon collection.",
                this
            );
            return;
        }

        foreach (WeaponDefinition weapon in startingWeapons)
            TryAddRunWeapon(weapon);
    }

    public bool TryAddRunWeapon(WeaponDefinition weapon)
    {
        if (weapon == null)
        {
            Debug.LogError(
                "RunManager cannot add a null WeaponDefinition.",
                this
            );
            return false;
        }

        if (runInventory.Contains(weapon))
            return false;

        runInventory.Add(weapon);
        return true;
    }

    public bool HasRunWeapon(WeaponDefinition weapon)
    {
        return weapon != null && runInventory.Contains(weapon);
    }

    public bool TrySetFloorLoadoutWeapon(
        WeaponSlotSide hand,
        int slotIndex,
        WeaponDefinition weapon)
    {
        if (!IsValidFloorLoadoutSlot(slotIndex))
            return false;

        if (weapon == null)
        {
            Debug.LogError(
                "RunManager cannot assign a null Floor Loadout weapon.",
                this
            );
            return false;
        }

        if (!HasRunWeapon(weapon))
        {
            Debug.LogError(
                "RunManager cannot assign a weapon outside Run Inventory.",
                this
            );
            return false;
        }

        if (!TryGetFloorLoadout(hand, out WeaponDefinition[] loadout))
            return false;

        for (int i = 0; i < loadout.Length; i++)
        {
            if (i != slotIndex && loadout[i] == weapon)
                return false;
        }

        loadout[slotIndex] = weapon;
        return true;
    }

    public bool ClearFloorLoadoutSlot(
        WeaponSlotSide hand,
        int slotIndex)
    {
        if (!IsValidFloorLoadoutSlot(slotIndex))
            return false;

        if (!TryGetFloorLoadout(hand, out WeaponDefinition[] loadout))
            return false;

        loadout[slotIndex] = null;
        return true;
    }

    private bool IsValidFloorLoadoutSlot(int slotIndex)
    {
        if (slotIndex >= 0 &&
            slotIndex < FloorLoadoutSlotsPerHand)
        {
            return true;
        }

        Debug.LogError(
            "RunManager Floor Loadout slot index must be between 0 and "
            + (FloorLoadoutSlotsPerHand - 1) + ".",
            this
        );
        return false;
    }

    private bool TryGetFloorLoadout(
        WeaponSlotSide hand,
        out WeaponDefinition[] loadout)
    {
        switch (hand)
        {
            case WeaponSlotSide.Left:
                loadout = leftFloorLoadout;
                return true;

            case WeaponSlotSide.Right:
                loadout = rightFloorLoadout;
                return true;

            default:
                loadout = null;
                Debug.LogError(
                    "RunManager received an invalid Floor Loadout hand.",
                    this
                );
                return false;
        }
    }

    private void ClearFloorLoadouts()
    {
        System.Array.Clear(
            leftFloorLoadout,
            0,
            leftFloorLoadout.Length
        );

        System.Array.Clear(
            rightFloorLoadout,
            0,
            rightFloorLoadout.Length
        );
    }

    public void AdvanceFloor()
    {
        currentFloor++;
    }
}
