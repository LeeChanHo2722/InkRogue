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

    [Min(0f)]
    [SerializeField]
    private float encounterReusedSpawnPointDelay = 0.2f;


    [Header("Rush Encounter")]

    [Min(1f)]
    [SerializeField]
    private float rushEncounterDuration = 40f;

    [Min(0.1f)]
    [SerializeField]
    private float rushAssaultInterval = 4f;

    [Min(1)]
    [SerializeField]
    private int rushMaxAlive = 20;


    private readonly EliminationSpawnDirector
        encounterSpawnDirector =
            new EliminationSpawnDirector();

    private readonly System.Random encounterSeedSource =
        new System.Random();

    private EncounterPlan currentEncounterPlan;

    private Coroutine encounterInitialBurstCoroutine;

    private int encounterInitialBurstRemaining;

    private Coroutine encounterRefillCoroutine;

    private Coroutine encounterWaveTransitionCoroutine;

    private bool encounterRuntimeActive;

    private bool encounterWaveTransitioning;


    private readonly RushSpawnDirector rushSpawnDirector =
        new RushSpawnDirector();

    private Coroutine rushTimerCoroutine;

    private Coroutine rushReleaseCoroutine;

    private bool rushRuntimeActive;

    private bool rushSpawningEnabled;

    private float rushRemainingTime;

    private float rushNextAssaultRemaining;

    private int rushAssaultIndex;


    public bool IsRushEncounterActive => rushRuntimeActive;

    public float RushRemainingTime => rushRemainingTime;

    public float RushNextAssaultRemaining =>
        rushNextAssaultRemaining;


    public bool IsFloorCombatComplete
    {
        get
        {
            if (rushRuntimeActive)
            {
                return !rushSpawningEnabled
                    && rushSpawnDirector.AliveCount <= 0;
            }

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

        encounterReusedSpawnPointDelay = Mathf.Max(
            0f,
            encounterReusedSpawnPointDelay);

        rushEncounterDuration = Mathf.Max(
            1f,
            rushEncounterDuration);

        rushAssaultInterval = Mathf.Max(
            0.1f,
            rushAssaultInterval);

        rushMaxAlive = Mathf.Max(
            1,
            rushMaxAlive);
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

        FloorEncounterMode encounterMode =
            floorDefinition.EncounterMode;

        if (encounterMode == FloorEncounterMode.Defense)
        {
            error = "Defense Encounter mode is not implemented yet. "
                + "Floor "
                + CurrentFloor
                + " cannot start.";
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

        if (encounterMode == FloorEncounterMode.Rush)
        {
            if (!TryStartRushEncounter(out error))
            {
                ResetEncounterRuntime();
                return false;
            }

            return true;
        }

        if (!TryStartEncounterWave(0, out error))
        {
            ResetEncounterRuntime();
            return false;
        }

        error = string.Empty;
        return true;
    }


    // ==================================================
    // Rush Encounter
    // ==================================================

    private bool TryStartRushEncounter(
        out string error)
    {
        if (currentEncounterPlan == null
            || currentEncounterPlan.waves == null
            || currentEncounterPlan.waves.Length == 0)
        {
            error = "Rush Encounter requires a generated Encounter Plan.";
            return false;
        }

        if (!rushSpawnDirector.TryBegin(
                rushMaxAlive,
                out error))
        {
            return false;
        }

        rushRuntimeActive = true;
        rushSpawningEnabled = true;
        rushRemainingTime = rushEncounterDuration;
        rushNextAssaultRemaining = 0f;
        rushAssaultIndex = 0;
        currentWaveIndex = 0;
        currentWaveTimer = 0f;
        waveRunning = true;

        HideWaveUI();

        Debug.Log(
            "RUSH START | Floor "
            + CurrentFloor
            + " | Duration "
            + rushEncounterDuration
            + " | Interval "
            + rushAssaultInterval
            + " | MaxAlive "
            + rushMaxAlive,
            this);

        rushTimerCoroutine = StartCoroutine(
            RushTimerRoutine());

        rushReleaseCoroutine = StartCoroutine(
            RushReleaseRoutine());

        error = string.Empty;
        return true;
    }


    // Assaults arrive purely on the interval. A kill never schedules one,
    // and a still-pending backlog never delays the next arrival.
    private IEnumerator RushTimerRoutine()
    {
        while (rushRuntimeActive
            && rushSpawningEnabled
            && !floorCleared)
        {
            if (rushNextAssaultRemaining <= 0f)
            {
                EnqueueNextRushAssault();
                rushNextAssaultRemaining = rushAssaultInterval;
            }

            yield return null;

            float delta = Time.deltaTime;
            rushRemainingTime -= delta;
            rushNextAssaultRemaining -= delta;

            if (rushRemainingTime <= 0f)
            {
                rushRemainingTime = 0f;
                break;
            }
        }

        rushTimerCoroutine = null;

        if (rushRuntimeActive)
        {
            ExpireRushEncounter();
        }
    }


    private void EnqueueNextRushAssault()
    {
        EncounterWavePlan[] waves =
            currentEncounterPlan.waves;

        int templateIndex =
            rushAssaultIndex % waves.Length;

        EncounterWavePlan template =
            waves[templateIndex];

        // Spawn Point cursor and enemy tagging reuse the template index,
        // so the existing SpawnEnemy path needs no Rush-specific branch.
        currentWaveIndex = templateIndex;

        int enqueued = rushSpawnDirector.EnqueueAssault(
            template.spawnBag);

        rushAssaultIndex++;

        Debug.Log(
            "RUSH ASSAULT "
            + rushAssaultIndex
            + " | Template "
            + templateIndex
            + " | Profile "
            + template.profile.Profile
            + " | Size "
            + enqueued
            + " | Alive "
            + rushSpawnDirector.AliveCount
            + " | Pending "
            + rushSpawnDirector.PendingCount,
            this);
    }


    // Releases the backlog into whatever capacity exists. The reused
    // Spawn Point delay only applies when a full round was consumed and
    // more enemies still need the same Spawn Points.
    private IEnumerator RushReleaseRoutine()
    {
        while (rushRuntimeActive
            && !floorCleared)
        {
            int spawned = SpawnEncounterEnemyRound(
                rushSpawnDirector,
                rushSpawnDirector.MaxAlive
                    - rushSpawnDirector.AliveCount);

            if (spawned > 0
                && rushSpawnDirector.HasPendingSpawns
                && rushSpawnDirector.AliveCount
                    < rushSpawnDirector.MaxAlive)
            {
                yield return new WaitForSeconds(
                    encounterReusedSpawnPointDelay);
            }
            else
            {
                yield return null;
            }
        }

        rushReleaseCoroutine = null;
    }


    private void ExpireRushEncounter()
    {
        if (!rushSpawningEnabled)
        {
            return;
        }

        // Order matters: every spawn source is shut down BEFORE the field
        // is purged, so the defeat callbacks below can never release a
        // pending enemy or schedule another Assault.
        rushSpawningEnabled = false;
        rushRemainingTime = 0f;
        rushNextAssaultRemaining = 0f;
        waveRunning = false;

        int discarded = rushSpawnDirector.PendingCount;

        rushSpawnDirector.ClearPending();
        StopRushEncounter();

        int purged = PurgeEncounterEnemies(true);

        Debug.Log(
            "RUSH TIME UP | Discarded "
            + discarded
            + " pending | Purged "
            + purged
            + " | Alive "
            + rushSpawnDirector.AliveCount,
            this);

        CheckFloorClear();
    }


    // playDeathPresentation: Time Up is a win, so the field clears itself
    // through the normal enemy death path (VFX / audio / defeat report)
    // without crediting the Player. Retry cleanup just removes them.
    private int PurgeEncounterEnemies(
        bool playDeathPresentation)
    {
        int purged = 0;

        for (int i = 0; i < activeEncounterEnemies.Count; i++)
        {
            EnemyWaveMember member =
                activeEncounterEnemies[i];

            if (member == null)
            {
                continue;
            }

            if (!playDeathPresentation)
            {
                Destroy(member.gameObject);
                purged++;
                continue;
            }

            EnemyHealth health =
                member.GetComponent<EnemyHealth>();

            if (health != null)
            {
                health.KillForEncounterCleanup();
            }
            else
            {
                member.ReportDeath(false);
                Destroy(member.gameObject);
            }

            purged++;
        }

        activeEncounterEnemies.Clear();
        return purged;
    }


    // Rush death is a Floor retry, not a Run reset: the same Floor starts
    // over from Assault 0 with the same Encounter seed, so SpawnCurrentFloor
    // regenerates an identical Plan.
    public void RestartRushEncounterIfActive()
    {
        if (!rushRuntimeActive)
        {
            return;
        }

        StopRushEncounter();
        rushSpawnDirector.ClearPending();

        int removed = PurgeEncounterEnemies(false);

        Debug.Log(
            "RUSH RETRY | Floor "
            + CurrentFloor
            + " | Removed "
            + removed
            + " enemies",
            this);

        SpawnCurrentFloor();
    }


    private void StopRushRelease()
    {
        if (rushReleaseCoroutine == null)
        {
            return;
        }

        StopCoroutine(rushReleaseCoroutine);
        rushReleaseCoroutine = null;
    }


    private void StopRushEncounter()
    {
        if (rushTimerCoroutine != null)
        {
            StopCoroutine(rushTimerCoroutine);
            rushTimerCoroutine = null;
        }

        StopRushRelease();
        rushSpawningEnabled = false;
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

        StopEncounterInitialBurst();
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
        // The initial burst size is fixed here, at Wave start. Enemies
        // killed later free capacity for the RefillDelay system, not for
        // the remaining initial burst rounds.
        encounterInitialBurstRemaining =
            encounterSpawnDirector.MaxAlive
            - encounterSpawnDirector.AliveCount;

        encounterInitialBurstRemaining -=
            SpawnEncounterEnemyRound(
                encounterInitialBurstRemaining);

        if (encounterInitialBurstRemaining <= 0)
        {
            return;
        }

        encounterInitialBurstCoroutine = StartCoroutine(
            StaggerInitialBurstRoutine(currentWaveIndex));
    }


    // One round fills every Spawn Point at most once, so enemies on
    // different Spawn Points still appear on the same frame. A Wave that
    // needs more than one enemy per Spawn Point runs the extra rounds
    // through StaggerInitialBurstRoutine instead.
    private int SpawnEncounterEnemyRound(int budget)
    {
        return SpawnEncounterEnemyRound(
            encounterSpawnDirector,
            budget);
    }


    private int SpawnEncounterEnemyRound(
        IEncounterSpawnSource source,
        int budget)
    {
        if (budget <= 0)
        {
            return 0;
        }

        int roundLimit =
            spawnPoints != null && spawnPoints.Length > 0
                ? spawnPoints.Length
                : budget;

        if (roundLimit > budget)
        {
            roundLimit = budget;
        }

        int spawnAttempts = 0;
        int spawnedCount = 0;

        while (spawnAttempts < roundLimit
            && source.HasPendingSpawns
            && source.AliveCount < source.MaxAlive
            && source.TryTakeNext(
                out WaveEnemyType enemyType))
        {
            bool spawned = SpawnEncounterEnemy(
                enemyType,
                spawnAttempts);

            if (spawned)
            {
                source.NotifySpawned();
                spawnedCount++;
            }
            else
            {
                source.NotifySpawnFailed();
            }

            spawnAttempts++;
        }

        AdvanceSpawnCursor(spawnAttempts);
        return spawnedCount;
    }


    private IEnumerator StaggerInitialBurstRoutine(
        int waveIndex)
    {
        // Loop on the fixed initial burst budget so the routine ends the
        // moment that budget is spent, instead of holding an extra delay
        // that could spawn a refill enemy ahead of its RefillDelay.
        while (encounterInitialBurstRemaining > 0)
        {
            yield return new WaitForSeconds(
                encounterReusedSpawnPointDelay);

            if (!encounterRuntimeActive
                || floorCleared
                || encounterWaveTransitioning
                || currentWaveIndex != waveIndex
                || !encounterSpawnDirector.HasPendingSpawns)
            {
                break;
            }

            encounterInitialBurstRemaining -=
                SpawnEncounterEnemyRound(
                    encounterInitialBurstRemaining);

            if (encounterSpawnDirector.IsWaveComplete)
            {
                encounterInitialBurstCoroutine = null;
                encounterInitialBurstRemaining = 0;
                CompleteEncounterWave();
                yield break;
            }
        }

        encounterInitialBurstCoroutine = null;
        encounterInitialBurstRemaining = 0;
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
        if (rushRuntimeActive)
        {
            // Rush never refills on a kill. Freed capacity only lets
            // already arrived pending enemies enter the field, which the
            // release routine picks up on its own.
            rushSpawnDirector.NotifyDefeated();
            return;
        }

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

        StopEncounterInitialBurst();
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


    private void StopEncounterInitialBurst()
    {
        encounterInitialBurstRemaining = 0;

        if (encounterInitialBurstCoroutine == null)
        {
            return;
        }

        StopCoroutine(encounterInitialBurstCoroutine);
        encounterInitialBurstCoroutine = null;
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
        StopRushEncounter();
        StopEncounterInitialBurst();
        StopEncounterRefill();
        StopEncounterWaveTransition();
    }


    private void ResetEncounterRuntime()
    {
        StopEncounterCoroutines();
        encounterRuntimeActive = false;
        currentEncounterPlan = null;
        encounterSpawnDirector.Reset();
        rushRuntimeActive = false;
        rushRemainingTime = 0f;
        rushNextAssaultRemaining = 0f;
        rushAssaultIndex = 0;
        rushSpawnDirector.Reset();
        activeEncounterEnemies.Clear();
    }
}
