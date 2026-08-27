// Shared spawn-reservation contract so Elimination and Rush can use the
// same Spawn Point round logic without duplicating placement code.
public interface IEncounterSpawnSource
{
    int MaxAlive { get; }

    int AliveCount { get; }

    bool HasPendingSpawns { get; }

    bool TryTakeNext(out WaveEnemyType enemyType);

    bool NotifySpawned();

    bool NotifySpawnFailed();
}
