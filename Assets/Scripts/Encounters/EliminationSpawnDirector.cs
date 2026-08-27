using System;

public sealed class EliminationSpawnDirector : IEncounterSpawnSource
{
    private EncounterWavePlan currentWave;
    private int spawnCursor;
    private int aliveCount;
    private bool spawnReserved;
    private int maxAliveBonus;

    public bool HasPendingSpawns =>
        currentWave != null
        && (spawnReserved
            || spawnCursor < currentWave.spawnBag.Length);

    public int AliveCount => aliveCount;

    // Spawn bag entries that have not entered the field yet. Together with
    // AliveCount this is what the Wave still needs cleared.
    public int PendingCount =>
        currentWave != null
            ? Math.Max(
                0,
                currentWave.spawnBag.Length - spawnCursor)
            : 0;

    // Plan value plus the current Floor's progression bonus, so the Plan
    // itself keeps its Difficulty-only semantics.
    public int MaxAlive =>
        currentWave != null
            ? Math.Max(1, currentWave.maxAlive + maxAliveBonus)
            : 0;

    public float RefillDelay =>
        currentWave != null
            ? currentWave.refillDelay
            : 0f;

    public bool IsWaveComplete =>
        currentWave != null
        && !spawnReserved
        && spawnCursor >= currentWave.spawnBag.Length
        && aliveCount == 0;

    public bool TryBeginWave(
        EncounterWavePlan plan,
        int floorMaxAliveBonus,
        out string error)
    {
        if (plan == null)
        {
            error = "Encounter Wave Plan is null.";
            return false;
        }

        if (plan.spawnBag == null
            || plan.spawnBag.Length == 0)
        {
            error = "Encounter Wave spawn bag is null or empty.";
            return false;
        }

        if (plan.maxAlive <= 0)
        {
            error = $"Encounter Wave MaxAlive must be positive: "
                + $"{plan.maxAlive}.";
            return false;
        }

        if (float.IsNaN(plan.refillDelay)
            || float.IsInfinity(plan.refillDelay)
            || plan.refillDelay < 0f)
        {
            error = $"Encounter Wave refill delay is invalid: "
                + $"{plan.refillDelay}.";
            return false;
        }

        currentWave = plan;
        maxAliveBonus = Math.Max(0, floorMaxAliveBonus);
        spawnCursor = 0;
        aliveCount = 0;
        spawnReserved = false;
        error = string.Empty;
        return true;
    }

    public bool TryTakeNext(out WaveEnemyType enemyType)
    {
        if (currentWave == null
            || spawnReserved
            || aliveCount >= currentWave.maxAlive
            || spawnCursor >= currentWave.spawnBag.Length)
        {
            enemyType = default;
            return false;
        }

        enemyType = currentWave.spawnBag[spawnCursor];
        spawnReserved = true;
        return true;
    }

    public bool NotifySpawned()
    {
        if (!spawnReserved)
        {
            return false;
        }

        spawnReserved = false;
        spawnCursor++;
        aliveCount++;
        return true;
    }

    public bool NotifySpawnFailed()
    {
        if (!spawnReserved)
        {
            return false;
        }

        spawnReserved = false;
        spawnCursor++;
        return true;
    }

    public void NotifyDefeated()
    {
        aliveCount = Math.Max(0, aliveCount - 1);
    }

    public void Reset()
    {
        currentWave = null;
        maxAliveBonus = 0;
        spawnCursor = 0;
        aliveCount = 0;
        spawnReserved = false;
    }
}
