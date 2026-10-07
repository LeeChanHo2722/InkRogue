using System;
using System.Collections.Generic;
using System.Linq;

// Run-local progression with no scene, clock, physics or Unity random dependency.
// TowerPowers remains the runtime facade and owns combat effects/cooldowns.
public sealed class TowerPowerProgression
{
    private readonly Dictionary<TowerPowerId, int> levels = new();

    public int Level(TowerPowerId id) => levels.TryGetValue(id, out int value) ? value : 0;

    public int TotalLevels(TowerPowerOwner owner) =>
        levels.Where(p => TowerPowerCatalog.Get(p.Key).owner == owner).Sum(p => p.Value);

    public bool TryAdd(TowerPowerId id)
    {
        if (Level(id) >= TowerPowerCatalog.Get(id).maxLevel) return false;
        levels[id] = Level(id) + 1;
        return true;
    }

    public TowerPowerDefinition[] Offers(int count, int seed)
    {
        count = Math.Max(2, Math.Min(4, count));
        var random = new Random(seed);
        // Preserve catalog order and the original stable random-key ordering.
        var pool = TowerPowerCatalog.All.Where(p => Level(p.id) < p.maxLevel)
            .OrderBy(_ => random.Next()).ToList();
        var result = new List<TowerPowerDefinition>();
        foreach (var owner in new[] { TowerPowerOwner.Player, TowerPowerOwner.Robot })
        {
            var power = pool.FirstOrDefault(p => p.owner == owner);
            if (power != null) { result.Add(power); pool.Remove(power); }
        }
        result.AddRange(pool.Take(count - result.Count));
        return result.ToArray();
    }

    public string Summary(TowerPowerOwner owner) => string.Join("  ·  ",
        TowerPowerCatalog.All.Where(p => p.owner == owner && Level(p.id) > 0)
            .Select(p => p.title + " " + Level(p.id)));
}
