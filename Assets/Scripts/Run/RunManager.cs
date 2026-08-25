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
    public const int FloorCandidateCount = 3;

    [Min(1)]
    public int maxFloor = 3;

    [SerializeField]
    private int currentFloor = 1;

    [SerializeField]
    private RunMode currentMode = RunMode.None;

    private readonly List<WeaponDefinition> runInventory =
        new List<WeaponDefinition>();

    private static readonly FloorDifficulty[] CandidateDifficulties =
    {
        FloorDifficulty.Easy,
        FloorDifficulty.Normal,
        FloorDifficulty.Hard
    };

    private readonly List<FloorCandidate> floorCandidates =
        new List<FloorCandidate>(FloorCandidateCount);

    private FloorCandidate selectedCandidate;

    private int firstFloorEncounterSeed;

    private bool hasFirstFloorEncounterSeed;

    private readonly WeaponDefinition[] leftFloorLoadout =
        new WeaponDefinition[FloorLoadoutSlotsPerHand];

    private readonly WeaponDefinition[] rightFloorLoadout =
        new WeaponDefinition[FloorLoadoutSlotsPerHand];

    public int CurrentFloor => currentFloor;

    public RunMode CurrentMode => currentMode;

    public bool IsInitialized => CurrentMode != RunMode.None;

    public IReadOnlyList<WeaponDefinition> RunInventory =>
        runInventory;

    public IReadOnlyList<FloorCandidate> FloorCandidates =>
        floorCandidates;

    public FloorCandidate SelectedCandidate =>
        selectedCandidate;

    public FloorDefinition SelectedNextFloor =>
        selectedCandidate?.Floor;

    public int FirstFloorEncounterSeed =>
        firstFloorEncounterSeed;

    public bool HasFirstFloorEncounterSeed =>
        hasFirstFloorEncounterSeed;

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
        ClearFloorSelection();
        currentMode = mode;
        firstFloorEncounterSeed = CreateEncounterSeed();
        hasFirstFloorEncounterSeed = true;

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

    public bool TryPrepareFloorCandidates(
        IReadOnlyList<FloorDefinition> source)
    {
        ClearFloorSelection();

        if (source == null)
            return false;

        List<FloorDefinition> uniqueCandidates =
            new List<FloorDefinition>();

        for (int i = 0; i < source.Count; i++)
        {
            FloorDefinition candidate = source[i];

            if (candidate != null &&
                !uniqueCandidates.Contains(candidate))
            {
                uniqueCandidates.Add(candidate);
            }
        }

        if (uniqueCandidates.Count < FloorCandidateCount)
            return false;

        for (int i = 0; i < FloorCandidateCount; i++)
        {
            int swapIndex =
                Random.Range(i, uniqueCandidates.Count);

            FloorDefinition candidate = uniqueCandidates[i];
            uniqueCandidates[i] = uniqueCandidates[swapIndex];
            uniqueCandidates[swapIndex] = candidate;

            floorCandidates.Add(
                new FloorCandidate(
                    uniqueCandidates[i],
                    CandidateDifficulties[
                        Random.Range(
                            0,
                            CandidateDifficulties.Length)],
                    CreateEncounterSeed()));
        }

        return true;
    }

    public bool TrySelectFloorCandidate(
        FloorCandidate candidate)
    {
        if (candidate == null ||
            !floorCandidates.Contains(candidate))
        {
            return false;
        }

        selectedCandidate = candidate;
        return true;
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

    private void ClearFloorSelection()
    {
        floorCandidates.Clear();
        selectedCandidate = null;
        hasFirstFloorEncounterSeed = false;
    }

    private static int CreateEncounterSeed()
    {
        return Random.Range(1, int.MaxValue);
    }

    public void AdvanceFloor()
    {
        currentFloor++;
    }
}
