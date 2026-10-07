using System;
using System.Collections.Generic;
using System.Linq;

public partial class RunManager
{
    public SavedRun CaptureRun()
    {
        return new SavedRun
        {
            mode = currentMode, floor = currentFloor, seed = RunSeed, elapsedSeconds = ElapsedSeconds,
            bosses = BossesDefeated, rewardedFloors = RewardedFloors, lastRewardedFloor = lastRewardedFloor,
            lastBossFloor = lastDefeatedBossFloor, preparedForFloor = preparedForFloor,
            firstEncounterSeed = firstFloorEncounterSeed, hasFirstEncounterSeed = hasFirstFloorEncounterSeed,
            inventory = runInventory.Select(w => w.WeaponId).ToArray(),
            left = leftFloorLoadout.Select(w => w != null ? w.WeaponId : "").ToArray(),
            right = rightFloorLoadout.Select(w => w != null ? w.WeaponId : "").ToArray(),
            selected = SaveRoute(selectedCandidate), candidates = floorCandidates.Select(SaveRoute).ToArray(),
            recentRoutes = recentRoutes.Select(SaveRoute).ToArray()
        };
    }
    public static SavedRoute SaveRoute(FloorCandidate c) => c == null ? null : new SavedRoute
    { floorId = c.Floor.FloorId, difficulty = (int)c.Difficulty, encounterMode = (int)c.EncounterMode, encounterSeed = c.EncounterSeed };

    public void RestoreRun(SavedRun saved, IReadOnlyList<WeaponDefinition> weapons, IReadOnlyList<FloorDefinition> floors)
    {
        // Resolve everything before changing live state. Missing content leaves the save intact.
        var roster = saved.inventory.Select(id => weapons.FirstOrDefault(w => w != null && w.WeaponId == id)
            ?? throw new InvalidOperationException("저장된 무기를 찾을 수 없습니다: " + id)).ToArray();
        WeaponDefinition ResolveWeapon(string id) => string.IsNullOrEmpty(id) ? null : roster.First(w => w.WeaponId == id);
        FloorCandidate ResolveRoute(SavedRoute s)
        {
            if (s == null) return null;
            var floor = floors.FirstOrDefault(f => f != null && f.FloorId == s.floorId);
            if (floor == null || !IsMapAvailable(floor.Map)) throw new InvalidOperationException("저장된 구역을 찾을 수 없습니다: " + s.floorId);
            return new FloorCandidate(floor, (FloorDifficulty)s.difficulty, s.encounterSeed, (FloorEncounterMode)s.encounterMode);
        }
        var candidates = saved.candidates.Select(ResolveRoute).ToArray();
        var recent = saved.recentRoutes.Select(ResolveRoute).ToArray();
        var selection = ResolveRoute(saved.selected);
        var left = saved.left.Select(ResolveWeapon).ToArray(); var right = saved.right.Select(ResolveWeapon).ToArray();
        InitializeRun(saved.mode, roster, saved.seed);
        currentFloor = saved.floor; ElapsedSeconds = saved.elapsedSeconds;
        BossesDefeated = saved.bosses; RewardedFloors = saved.rewardedFloors;
        lastRewardedFloor = saved.lastRewardedFloor; lastDefeatedBossFloor = saved.lastBossFloor;
        preparedForFloor = saved.preparedForFloor;
        firstFloorEncounterSeed = saved.firstEncounterSeed; hasFirstFloorEncounterSeed = saved.hasFirstEncounterSeed;
        floorCandidates.Clear(); floorCandidates.AddRange(candidates);
        selectedCandidate = selection;
        recentRoutes.Clear(); foreach (var c in recent) recentRoutes.Enqueue(c);
        Array.Copy(left, leftFloorLoadout, 3); Array.Copy(right, rightFloorLoadout, 3);
    }
}
