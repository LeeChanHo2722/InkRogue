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
    [Min(1)]
    public int maxFloor = 3;

    [SerializeField]
    private int currentFloor = 1;

    [SerializeField]
    private RunMode currentMode = RunMode.None;

    private readonly List<WeaponDefinition> runInventory =
        new List<WeaponDefinition>();

    public int CurrentFloor => currentFloor;

    public RunMode CurrentMode => currentMode;

    public IReadOnlyList<WeaponDefinition> RunInventory =>
        runInventory;

    public bool IsLastFloor =>
        currentFloor >= maxFloor;

    public void InitializeRun(
        RunMode mode,
        IEnumerable<WeaponDefinition> startingWeapons)
    {
        runInventory.Clear();
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

    public void AdvanceFloor()
    {
        currentFloor++;
    }
}
