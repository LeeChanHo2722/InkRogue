using System;
using System.Collections.Generic;

// Deterministic route policy. RunManager owns eligibility, cache and run state;
// this calculation owns no Unity random stream, scene lookup or mutable run state.
public static class FloorRouteGenerator
{
    private static readonly FloorDifficulty[] Difficulties =
    {
        FloorDifficulty.Easy, FloorDifficulty.Normal, FloorDifficulty.Hard
    };

    public static FloorCandidate[] Generate(IReadOnlyList<FloorDefinition> eligibleFloors,
        IEnumerable<FloorCandidate> recentRoutes, int runSeed, int currentFloor)
    {
        if (eligibleFloors.Count == 0) return Array.Empty<FloorCandidate>();
        var pool = new List<FloorDefinition>(eligibleFloors);
        // Preserve the seed derivation and every RNG call/order from RunManager.
        var random = new Random(unchecked(runSeed * 397 ^ currentFloor * 7919 ^ 0x4F17));
        for (int i = pool.Count - 1; i > 0; i--)
        {
            int j = random.Next(i + 1);
            var item = pool[i]; pool[i] = pool[j]; pool[j] = item;
        }
        var modes = new[] { FloorEncounterMode.Elimination, FloorEncounterMode.Rush, FloorEncounterMode.Defense };
        var permutations = new List<int[]>();
        // Pairs may repeat; exclude only three equal objectives.
        for (int a = 0; a < 3; a++) for (int b = 0; b < 3; b++) for (int c = 0; c < 3; c++)
            if (a != b || b != c) permutations.Add(new[] { a, b, c });
        var weights = new List<int>();
        int totalWeight = 0;
        foreach (var order in permutations)
        {
            int repeats = 0;
            for (int i = 0; i < Difficulties.Length; i++)
                foreach (var previous in recentRoutes)
                    if (previous.Floor == pool[i % pool.Count] && previous.EncounterMode == modes[order[i]]) repeats++;
            int weight = Math.Max(1, 16 >> repeats);
            weights.Add(weight); totalWeight += weight;
        }
        int roll = random.Next(totalWeight), choice = 0;
        while (roll >= weights[choice]) { roll -= weights[choice]; choice++; }
        var chosen = permutations[choice];
        var result = new FloorCandidate[Difficulties.Length];
        for (int i = 0; i < result.Length; i++)
            result[i] = new FloorCandidate(pool[i % pool.Count], Difficulties[i],
                random.Next(1, int.MaxValue), modes[chosen[i]]);
        return result;
    }
}
