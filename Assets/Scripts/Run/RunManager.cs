using System.Collections.Generic;
using UnityEngine;

public enum RunMode
{
    None,
    TwentyFloor,
    Endless
}

public partial class RunManager : MonoBehaviour
{
    public const int FloorLoadoutSlotsPerHand = 3;
    public const int FloorCandidateCount = 3;

    [Min(1)]
    public int maxFloor = 20;

    [SerializeField]
    private int currentFloor = 1;

    [SerializeField]
    private RunMode currentMode = RunMode.None;

    private readonly List<WeaponDefinition> runInventory =
        new List<WeaponDefinition>();

    private readonly List<FloorCandidate> floorCandidates =
        new List<FloorCandidate>(FloorCandidateCount);

    private FloorCandidate selectedCandidate;

    public int RunSeed { get; private set; }
    public int CurrentPowerOfferCount => currentFloor == 1 ? 3 : selectedCandidate?.PowerOfferCount ?? 3;
    private readonly Queue<FloorCandidate> recentRoutes = new();
    private int preparedForFloor = -1;
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
        currentMode == RunMode.TwentyFloor && currentFloor >= maxFloor;

    public bool IsBossFloor => IsBossFloorFor(currentMode, currentFloor);

    public static bool IsBossFloorFor(RunMode mode, int floor) =>
        floor > 0 && (mode == RunMode.TwentyFloor
            ? floor % 5 == 0
            : mode == RunMode.Endless && floor % 10 == 0);

    public float ElapsedSeconds { get; private set; }
    public int BossesDefeated { get; private set; }
    public int RewardedFloors { get; private set; }
    // Floor rewards grant a power choice; damage belongs to the selected owner.
    private int lastRewardedFloor;
    private int lastDefeatedBossFloor;

    public void GrantFloorReward()
    {
        if (lastRewardedFloor == currentFloor) return;
        lastRewardedFloor = currentFloor;
        RewardedFloors++;
    }

    public void RecordBossDefeated()
    {
        if (!IsInitialized || !IsBossFloor || lastDefeatedBossFloor == currentFloor) return;
        lastDefeatedBossFloor = currentFloor;
        BossesDefeated++;
        FindAnyObjectByType<PlayerLifeManager>()?.RestoreBossLife();
    }

    private void Update()
    {
        if (IsInitialized)
            ElapsedSeconds += Time.deltaTime;
    }

    public void InitializeRun(
        RunMode mode,
        IEnumerable<WeaponDefinition> startingWeapons, int? seed = null)
    {
        if (startingWeapons == null)
            throw new System.ArgumentNullException(nameof(startingWeapons));

        currentFloor = 1;
        ElapsedSeconds = 0f;
        BossesDefeated = 0;
        RewardedFloors = 0;
        lastRewardedFloor = 0;
        lastDefeatedBossFloor = 0;
        maxFloor = 20;
        runInventory.Clear();
        ClearFloorLoadouts();
        ClearFloorSelection();
        currentMode = mode;
        RunSeed = seed ?? CreateEncounterSeed();
        recentRoutes.Clear(); preparedForFloor = -1;
        firstFloorEncounterSeed = new System.Random(RunSeed).Next(1, int.MaxValue);
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
        // Reopening/cancelling cannot reroll the route or its encounter seed.
        if (preparedForFloor == currentFloor && floorCandidates.Count == FloorCandidateCount)
            return true;
        var pool = new List<FloorDefinition>();
        if (source != null)
            foreach (var floor in source)
                if (floor != null && IsMapAvailable(floor.Map) && !pool.Contains(floor)) pool.Add(floor);
        if (pool.Count == 0) return false;

        var generated = FloorRouteGenerator.Generate(pool, recentRoutes, RunSeed, currentFloor);
        ClearFloorSelection();
        floorCandidates.AddRange(generated);
        preparedForFloor = currentFloor;
        return true;
    }

    private static bool IsMapAvailable(MapDefinition map)
    {
        if (map == null || string.IsNullOrWhiteSpace(map.SceneName)) return false;
        // CanStreamedLevelBeLoaded returns false in Edit mode; build-scene paths are stable in both.
        for (int i=0;i<UnityEngine.SceneManagement.SceneManager.sceneCountInBuildSettings;i++)
        {
            string path=UnityEngine.SceneManagement.SceneUtility.GetScenePathByBuildIndex(i);
            if (path==map.SceneName || System.IO.Path.GetFileNameWithoutExtension(path)==map.SceneName) return true;
        }
        return false;
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

        if (WeaponLoadoutRules.ContainsInOtherSlot(loadout, slotIndex, weapon))
            return false;

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
        if (selectedCandidate != null)
        {
            recentRoutes.Enqueue(selectedCandidate);
            while (recentRoutes.Count > 2) recentRoutes.Dequeue();
        }
        currentFloor++;
    }
}
