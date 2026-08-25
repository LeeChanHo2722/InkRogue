using System.Collections;
using UnityEngine;

public partial class FloorManager
{
    [Header("Encounter Difficulties")]

    [SerializeField]
    private EncounterDifficultyDefinition easyEncounterDifficulty;

    [SerializeField]
    private EncounterDifficultyDefinition normalEncounterDifficulty;

    [SerializeField]
    private EncounterDifficultyDefinition hardEncounterDifficulty;

    [Min(0f)]
    [SerializeField]
    private float encounterInterWaveDelay = 0.25f;


    private readonly EliminationSpawnDirector
        encounterSpawnDirector =
            new EliminationSpawnDirector();

    private readonly System.Random encounterSeedSource =
        new System.Random();

    private EncounterPlan currentEncounterPlan;

    private Coroutine encounterRefillCoroutine;

    private Coroutine encounterWaveTransitionCoroutine;

    private bool encounterRuntimeActive;

    private bool encounterWaveTransitioning;


    public bool IsFloorCombatComplete
    {
        get
        {
            if (encounterRuntimeActive)
            {
                return !encounterWaveTransitioning
                    && currentEncounterPlan != null
                    && currentEncounterPlan.waves != null
                    && currentEncounterPlan.waves.Length > 0
                    && currentWaveIndex >=
                        currentEncounterPlan.waves.Length - 1
                    && encounterSpawnDirector.IsWaveComplete;
            }

            return IsLastWaveStarted
                && remainingEnemies <= 0;
        }
    }


    private void OnValidate()
    {
        encounterInterWaveDelay = Mathf.Max(
            0f,
            encounterInterWaveDelay);
    }


    private void OnDisable()
    {
        StopEncounterCoroutines();
    }


    private bool TryStartEncounterFloor(
        out string error)
    {
        FloorDefinition floorDefinition =
            GetCurrentFloorDefinition();

        if (floorDefinition == null)
        {
            error = "Current FloorDefinition is missing.";
            return false;
        }

        bool isFirstFloor = CurrentFloor == 1;
        FloorCandidate candidate =
            runManager != null
                ? runManager.SelectedCandidate
                : null;

        FloorDifficulty floorDifficulty;

        if (isFirstFloor)
        {
            floorDifficulty = FloorDifficulty.Easy;
        }
        else if (candidate != null)
        {
            floorDifficulty = candidate.Difficulty;
        }
        else
        {
            // Legacy fallback: no runtime candidate, so the
            // FloorDefinition difficulty is used instead.
            floorDifficulty = floorDefinition.Difficulty;
        }

        EncounterDifficultyDefinition difficulty =
            GetEncounterDifficulty(floorDifficulty);

        if (difficulty == null)
        {
            error = $"Encounter Difficulty is not assigned for "
                + $"{floorDifficulty}.";
            return false;
        }

        int seed;

        if (isFirstFloor
            && runManager != null
            && runManager.HasFirstFloorEncounterSeed)
        {
            seed = runManager.FirstFloorEncounterSeed;
        }
        else if (!isFirstFloor
            && candidate != null)
        {
            seed = candidate.EncounterSeed;
        }
        else
        {
            // Migration fallback: no fixed candidate seed is available,
            // so this Floor uses a temporary runtime seed.
            seed = encounterSeedSource.Next();

            Debug.LogError(
                "Encounter seed is not fixed for Floor "
                + CurrentFloor
                + ". Falling back to a temporary runtime seed "
                + seed
                + ".",
                this);
        }

        if (!EncounterPlanGenerator.TryGenerate(
                difficulty,
                seed,
                isFirstFloor,
                out EncounterPlan plan,
                out string generationError))
        {
            error = $"Seed {seed} | {generationError}";
            return false;
        }

        currentEncounterPlan = plan;
        encounterRuntimeActive = true;
        waveSpawnCounts = new int[plan.waves.Length];
        waveKillCounts = new int[plan.waves.Length];
        InitializeSpawnCursor();

        Debug.Log(
            "FLOOR "
            + CurrentFloor
            + " START",
            this);

        Debug.Log(
            "ENCOUNTER START | Floor "
            + CurrentFloor
            + " | Difficulty "
            + difficulty.Difficulty
            + " | Seed "
            + seed
            + " | TotalQuota "
            + plan.totalQuota,
            this);

        if (!TryStartEncounterWave(0, out error))
        {
            ResetEncounterRuntime();
            return false;
        }

        error = string.Empty;
        return true;
    }


    private EncounterDifficultyDefinition
        GetEncounterDifficulty(
            FloorDifficulty difficulty)
    {
        switch (difficulty)
        {
            case FloorDifficulty.Easy:
                return easyEncounterDifficulty;
            case FloorDifficulty.Normal:
                return normalEncounterDifficulty;
            case FloorDifficulty.Hard:
                return hardEncounterDifficulty;
            default:
                return null;
        }
    }


    private bool TryStartEncounterWave(
        int waveIndex,
        out string error)
    {
        if (!encounterRuntimeActive
            || currentEncounterPlan == null
            || currentEncounterPlan.waves == null
            || waveIndex < 0
            || waveIndex >= currentEncounterPlan.waves.Length)
        {
            error = $"Encounter Wave index is invalid: {waveIndex}.";
            return false;
        }

        if (waveIndex > 0
            && !encounterSpawnDirector.IsWaveComplete)
        {
            error = "The previous Encounter Wave is still active.";
            return false;
        }

        StopEncounterRefill();

        EncounterWavePlan wave =
            currentEncounterPlan.waves[waveIndex];

        if (!encounterSpawnDirector.TryBeginWave(
                wave,
                out error))
        {
            return false;
        }

        currentWaveIndex = waveIndex;
        currentWaveTimer = 0f;
        waveRunning = true;

        UpdateWaveUI(currentEncounterPlan.waves.Length);

        if (waveStartUI != null)
        {
            waveStartUI.ShowWave(
                currentWaveIndex + 1,
                currentEncounterPlan.waves.Length);
        }

        Debug.Log(
            "WAVE "
            + (currentWaveIndex + 1)
            + " START | Profile "
            + wave.profile.Profile
            + " | Quota "
            + wave.waveQuota
            + " | MaxAlive "
            + wave.maxAlive
            + " | RefillDelay "
            + wave.refillDelay,
            this);

        SpawnEncounterEnemiesToCapacity();

        if (encounterSpawnDirector.IsWaveComplete)
        {
            CompleteEncounterWave();
        }

        CheckFloorClear();
        error = string.Empty;
        return true;
    }


    private void SpawnEncounterEnemiesToCapacity()
    {
        int spawnAttempts = 0;

        while (encounterSpawnDirector.HasPendingSpawns
            && encounterSpawnDirector.AliveCount
                < encounterSpawnDirector.MaxAlive
            && encounterSpawnDirector.TryTakeNext(
                out WaveEnemyType enemyType))
        {
            bool spawned = SpawnEncounterEnemy(
                enemyType,
                spawnAttempts);

            if (spawned)
            {
                encounterSpawnDirector.NotifySpawned();
            }
            else
            {
                encounterSpawnDirector.NotifySpawnFailed();
            }

            spawnAttempts++;
        }

        AdvanceSpawnCursor(spawnAttempts);
    }


    private bool SpawnEncounterEnemy(
        WaveEnemyType enemyType,
        int localSpawnIndex)
    {
        GameObject prefab = GetEnemyPrefab(enemyType);

        if (prefab == null)
        {
            Debug.LogError(
                "Encounter enemy Prefab is missing: "
                + enemyType
                + ". Spawn entry was skipped.",
                this);
            return false;
        }

        return SpawnEnemy(
            prefab,
            currentWaveIndex,
            localSpawnIndex);
    }


    private void EnsureEncounterRefill()
    {
        if (!encounterRuntimeActive
            || floorCleared
            || encounterWaveTransitioning
            || encounterRefillCoroutine != null
            || !encounterSpawnDirector.HasPendingSpawns
            || encounterSpawnDirector.AliveCount
                >= encounterSpawnDirector.MaxAlive)
        {
            return;
        }

        encounterRefillCoroutine = StartCoroutine(
            RefillEncounterWaveRoutine(currentWaveIndex));
    }


    private IEnumerator RefillEncounterWaveRoutine(
        int waveIndex)
    {
        while (encounterRuntimeActive
            && !floorCleared
            && currentWaveIndex == waveIndex
            && encounterSpawnDirector.HasPendingSpawns
            && encounterSpawnDirector.AliveCount
                < encounterSpawnDirector.MaxAlive)
        {
            yield return new WaitForSeconds(
                encounterSpawnDirector.RefillDelay);

            if (!encounterRuntimeActive
                || floorCleared
                || currentWaveIndex != waveIndex)
            {
                break;
            }

            if (!encounterSpawnDirector.TryTakeNext(
                    out WaveEnemyType enemyType))
            {
                break;
            }

            bool spawned = SpawnEncounterEnemy(enemyType, 0);

            if (spawned)
            {
                encounterSpawnDirector.NotifySpawned();
            }
            else
            {
                encounterSpawnDirector.NotifySpawnFailed();
            }

            AdvanceSpawnCursor(1);

            if (encounterSpawnDirector.IsWaveComplete)
            {
                encounterRefillCoroutine = null;
                CompleteEncounterWave();
                yield break;
            }
        }

        encounterRefillCoroutine = null;
    }


    private void HandleEncounterEnemyDefeated(
        int sourceWaveIndex)
    {
        if (sourceWaveIndex != currentWaveIndex)
        {
            Debug.LogWarning(
                "Encounter enemy reported an unexpected Wave index: "
                + sourceWaveIndex
                + ". Current Wave is "
                + currentWaveIndex
                + ".",
                this);
        }

        encounterSpawnDirector.NotifyDefeated();

        if (encounterSpawnDirector.IsWaveComplete)
        {
            CompleteEncounterWave();
            return;
        }

        EnsureEncounterRefill();
    }


    private void CompleteEncounterWave()
    {
        if (!encounterRuntimeActive
            || encounterWaveTransitioning
            || !encounterSpawnDirector.IsWaveComplete)
        {
            return;
        }

        StopEncounterRefill();

        Debug.Log(
            "WAVE "
            + (currentWaveIndex + 1)
            + " COMPLETE",
            this);

        int nextWaveIndex = currentWaveIndex + 1;

        if (currentEncounterPlan != null
            && currentEncounterPlan.waves != null
            && nextWaveIndex < currentEncounterPlan.waves.Length)
        {
            encounterWaveTransitioning = true;
            waveRunning = false;

            float delay = Mathf.Max(
                0f,
                encounterInterWaveDelay);

            if (delay <= 0f)
            {
                StartNextEncounterWave(nextWaveIndex);
                return;
            }

            encounterWaveTransitionCoroutine =
                StartCoroutine(
                    StartNextEncounterWaveRoutine(
                        nextWaveIndex,
                        delay));

            return;
        }

        waveRunning = false;
        CheckFloorClear();
    }


    private IEnumerator StartNextEncounterWaveRoutine(
        int nextWaveIndex,
        float delay)
    {
        yield return new WaitForSeconds(delay);

        if (!encounterRuntimeActive || floorCleared)
        {
            encounterWaveTransitionCoroutine = null;
            encounterWaveTransitioning = false;
            yield break;
        }

        StartNextEncounterWave(nextWaveIndex);
    }


    private void StartNextEncounterWave(
        int nextWaveIndex)
    {
        encounterWaveTransitionCoroutine = null;
        encounterWaveTransitioning = false;

        if (!TryStartEncounterWave(
                nextWaveIndex,
                out string error))
        {
            waveRunning = false;
            Debug.LogError(
                "Encounter next Wave failed: "
                + error,
                this);
        }
    }


    private void InitializeSpawnCursor()
    {
        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            spawnCursor = 0;
            return;
        }

        spawnCursor = Random.Range(0, spawnPoints.Length);
    }


    private void AdvanceSpawnCursor(int amount)
    {
        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            return;
        }

        spawnCursor = (spawnCursor + amount)
            % spawnPoints.Length;
    }


    private void StopEncounterRefill()
    {
        if (encounterRefillCoroutine == null)
        {
            return;
        }

        StopCoroutine(encounterRefillCoroutine);
        encounterRefillCoroutine = null;
    }


    private void StopEncounterWaveTransition()
    {
        if (encounterWaveTransitionCoroutine != null)
        {
            StopCoroutine(
                encounterWaveTransitionCoroutine);
            encounterWaveTransitionCoroutine = null;
        }

        encounterWaveTransitioning = false;
    }


    private void StopEncounterCoroutines()
    {
        StopEncounterRefill();
        StopEncounterWaveTransition();
    }


    private void ResetEncounterRuntime()
    {
        StopEncounterCoroutines();
        encounterRuntimeActive = false;
        currentEncounterPlan = null;
        encounterSpawnDirector.Reset();
    }
}
