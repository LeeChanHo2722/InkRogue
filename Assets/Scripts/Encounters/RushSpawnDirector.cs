using System;
using System.Collections.Generic;

// Rush arrival semantics: assaults are enqueued on a timer, never on a
// kill. Enemies that cannot enter the field because MaxAlive is full stay
// queued as a backlog and are released as capacity frees up.
public sealed class RushSpawnDirector : IEncounterSpawnSource
{
    private readonly Queue<WaveEnemyType> pendingEnemies =
        new Queue<WaveEnemyType>();

    private int maxAlive;

    private int aliveCount;

    private bool spawnReserved;

    public int MaxAlive => maxAlive;

    public int AliveCount => aliveCount;

    public int PendingCount =>
        pendingEnemies.Count;

    public bool HasPendingSpawns =>
        spawnReserved || pendingEnemies.Count > 0;

    public bool TryBegin(
        int rushMaxAlive,
        out string error)
    {
        if (rushMaxAlive <= 0)
        {
            error = $"Rush MaxAlive must be positive: {rushMaxAlive}.";
            return false;
        }

        pendingEnemies.Clear();
        maxAlive = rushMaxAlive;
        aliveCount = 0;
        spawnReserved = false;
        error = string.Empty;
        return true;
    }

    public int EnqueueAssault(
        WaveEnemyType[] spawnBag)
    {
        if (spawnBag == null)
        {
            return 0;
        }

        for (int i = 0; i < spawnBag.Length; i++)
        {
            pendingEnemies.Enqueue(spawnBag[i]);
        }

        return spawnBag.Length;
    }

    public bool TryTakeNext(
        out WaveEnemyType enemyType)
    {
        if (spawnReserved
            || aliveCount >= maxAlive
            || pendingEnemies.Count == 0)
        {
            enemyType = default;
            return false;
        }

        enemyType = pendingEnemies.Peek();
        spawnReserved = true;
        return true;
    }

    public bool NotifySpawned()
    {
        if (!spawnReserved)
        {
            return false;
        }

        pendingEnemies.Dequeue();
        spawnReserved = false;
        aliveCount++;
        return true;
    }

    public bool NotifySpawnFailed()
    {
        if (!spawnReserved)
        {
            return false;
        }

        pendingEnemies.Dequeue();
        spawnReserved = false;
        return true;
    }

    public void NotifyDefeated()
    {
        aliveCount = Math.Max(0, aliveCount - 1);
    }

    public void ClearPending()
    {
        pendingEnemies.Clear();
        spawnReserved = false;
    }

    public void Reset()
    {
        pendingEnemies.Clear();
        maxAlive = 0;
        aliveCount = 0;
        spawnReserved = false;
    }
}
