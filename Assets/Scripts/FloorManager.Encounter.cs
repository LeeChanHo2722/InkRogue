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

    [Tooltip("Rush cap = Difficulty BaseMaxAlive * this + bonus. "
        + "Rush deliberately shows far more enemies at once than "
        + "Elimination, so it scales the same Difficulty value up.")]
    [Min(1)]
    [SerializeField]
    private int rushMaxAlivePerBase = 3;

    [Min(0)]
    [SerializeField]
    private int rushMaxAliveBonus = 2;

    [Tooltip("Seconds added per point of BaseMaxAlive below Normal, and "
        + "subtracted per point above. Normal keeps the tuned interval.")]
    [Min(0f)]
    [SerializeField]
    private float rushAssaultIntervalPerBase = 0.5f;


    [Header("Defense Encounter")]

    [Min(1f)]
    [SerializeField]
    private float defenseAssaultDuration = 10f;

    [Min(0f)]
    [SerializeField]
    private float defenseRestDuration = 3f;

    [Min(0f)]
    [SerializeField]
    private float defenseRetryDelay = 0.4f;

    [Min(0f)]
    [SerializeField]
    private float defenseRetryInvulnerability = 1.5f;

    [Tooltip("Spawned at Defense Floor start. Maps carry no Target.")]
    [SerializeField]
    private DefenseTarget defenseTargetPrefab;

    [Tooltip("Only used when the Map has a single Player Spawn Point.")]
    [SerializeField]
    private Vector2 defenseTargetFallbackOffset =
        new Vector2(1.5f, 0f);


    [Header("Floor Progression")]

    [Tooltip("Quota multiplier gained per Floor beyond the first.")]
    [Min(0f)]
    [SerializeField]
    private float quotaGrowthPerFloor = 0.04f;

    [Tooltip("Caps the multiplier, never the absolute quota, so the "
        + "Easy/Normal/Hard gap survives at depth.")]
    [Min(1f)]
    [SerializeField]
    private float maxQuotaMultiplier = 2.2f;

    [Tooltip("Enemy HP multiplier gained per Floor up to the knee.")]
    [Min(0f)]
    [SerializeField]
    private float hpGrowthPerFloor = 0.03f;

    [Min(1)]
    [SerializeField]
    private int hpSoftKneeFloor = 30;

    [Tooltip("Much slower growth past the knee Floor. No cap.")]
    [Min(0f)]
    [SerializeField]
    private float hpTailGrowthPerFloor = 0.01f;

    [Header("Floor Progression - Elimination / Rush")]

    [Min(1)]
    [SerializeField]
    private int combatFastStepFloors = 5;

    [Min(0)]
    [SerializeField]
    private int combatSoftKneeBonus = 5;

    [Min(1)]
    [SerializeField]
    private int combatTailStepFloors = 20;

    [Header("Floor Progression - Defense")]

    [Tooltip("Defense grows far slower: Map geometry and a Target to "
        + "protect make simultaneous pressure hit much harder.")]
    [Min(1)]
    [SerializeField]
    private int defenseFastStepFloors = 8;

    [Min(0)]
    [SerializeField]
    private int defenseSoftKneeBonus = 3;

    [Min(1)]
    [SerializeField]
    private int defenseTailStepFloors = 30;


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

    private FloorEncounterMode currentEncounterMode =
        FloorEncounterMode.Elimination;

    private int currentCombatMaxAliveBonus;

    private int currentDefenseMaxAliveBonus;


    // Third progression axis. Pure function of the Floor, so it needs no
    // reset and every spawn path reads the same value.
    public float CurrentEnemyHealthMultiplier =>
        EncounterFloorScaling.HealthMultiplier(
            CurrentFloor,
            hpGrowthPerFloor,
            hpSoftKneeFloor,
            hpTailGrowthPerFloor);


    private readonly RushSpawnDirector rushSpawnDirector =
        new RushSpawnDirector();

    private Coroutine rushTimerCoroutine;

    private Coroutine rushReleaseCoroutine;

    private bool rushRuntimeActive;

    private bool rushSpawningEnabled;

    private float rushRemainingTime;

    private float rushNextAssaultRemaining;

    private int rushAssaultIndex;

    private float runtimeRushAssaultInterval;


    public bool IsRushEncounterActive => rushRuntimeActive;

    public float RushRemainingTime => rushRemainingTime;

    public float RushNextAssaultRemaining =>
        rushNextAssaultRemaining;


    // Defense reuses the Rush consumption rules (queue, MaxAlive, backlog,
    // no kill-refill); only the arrival timing differs, and that lives in
    // DefenseFlowRoutine rather than in the director.
    private readonly RushSpawnDirector defenseSpawnDirector =
        new RushSpawnDirector();

    private Coroutine defenseFlowCoroutine;

    private Coroutine defenseReleaseCoroutine;

    private bool defenseRuntimeActive;

    private bool defenseSpawningEnabled;

    private bool defenseSucceeded;

    private bool defenseFailed;

    private bool defenseRetryPending;

    private int defenseAssaultIndex;

    private DefenseTarget defenseTarget;

    private readonly System.Collections.Generic.List<Transform>
        defenseSpawnCandidates =
            new System.Collections.Generic.List<Transform>();

    private bool defenseRestPhase;

    private float defensePhaseRemaining;


    public bool IsDefenseEncounterActive => defenseRuntimeActive;

    public int DefenseAssaultIndex => defenseAssaultIndex;

    public bool IsDefenseRestPhase => defenseRestPhase;

    public float DefensePhaseRemaining => defensePhaseRemaining;

    public DefenseTarget CurrentDefenseTarget => defenseTarget;


    // ==================================================
    // Encounter State (read-only, for HUD)
    // ==================================================

    public FloorEncounterMode CurrentEncounterMode =>
        currentEncounterMode;

    // Authoritative "a fight is running" flag, so UI never has to infer it
    // from floorCleared / wave indices.
    public bool IsEncounterActive =>
        encounterRuntimeActive && !floorCleared;

    public int EncounterWaveCount =>
        currentEncounterPlan != null
            && currentEncounterPlan.waves != null
            ? currentEncounterPlan.waves.Length
            : 0;

    // What the current Elimination Wave still needs cleared: enemies on the
    // field plus the ones its spawn bag has not released yet.
    public int RemainingWaveEnemies =>
        Mathf.Max(
            0,
            encounterSpawnDirector.AliveCount
                + encounterSpawnDirector.PendingCount);


    public bool IsFloorCombatComplete
    {
        get
        {
            if (defenseRuntimeActive)
            {
                return defenseSucceeded
                    && !defenseFailed
                    && defenseSpawnDirector.AliveCount <= 0;
            }

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

        rushMaxAlivePerBase = Mathf.Max(
            1,
            rushMaxAlivePerBase);

        rushMaxAliveBonus = Mathf.Max(
            0,
            rushMaxAliveBonus);

        rushAssaultIntervalPerBase = Mathf.Max(
            0f,
            rushAssaultIntervalPerBase);

        quotaGrowthPerFloor = Mathf.Max(
            0f,
            quotaGrowthPerFloor);

        maxQuotaMultiplier = Mathf.Max(
            1f,
            maxQuotaMultiplier);

        combatFastStepFloors = Mathf.Max(
            1,
            combatFastStepFloors);

        combatSoftKneeBonus = Mathf.Max(
            0,
            combatSoftKneeBonus);

        combatTailStepFloors = Mathf.Max(
            1,
            combatTailStepFloors);

        defenseFastStepFloors = Mathf.Max(
            1,
            defenseFastStepFloors);

        defenseSoftKneeBonus = Mathf.Max(
            0,
            defenseSoftKneeBonus);

        defenseTailStepFloors = Mathf.Max(
            1,
            defenseTailStepFloors);

        hpGrowthPerFloor = Mathf.Max(
            0f,
            hpGrowthPerFloor);

        hpSoftKneeFloor = Mathf.Max(
            1,
            hpSoftKneeFloor);

        hpTailGrowthPerFloor = Mathf.Max(
            0f,
            hpTailGrowthPerFloor);

        defenseAssaultDuration = Mathf.Max(
            1f,
            defenseAssaultDuration);

        defenseRestDuration = Mathf.Max(
            0f,
            defenseRestDuration);

        defenseRetryDelay = Mathf.Max(
            0f,
            defenseRetryDelay);

        defenseRetryInvulnerability = Mathf.Max(
            0f,
            defenseRetryInvulnerability);
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

        if (encounterMode != FloorEncounterMode.Defense)
        {
            EncounterTarget.UsePlayerTarget();
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

        float quotaMultiplier =
            EncounterFloorScaling.QuotaMultiplier(
                CurrentFloor,
                quotaGrowthPerFloor,
                maxQuotaMultiplier);

        int scaledMinQuota = Mathf.RoundToInt(
            difficulty.MinTotalQuota * quotaMultiplier);

        int scaledMaxQuota = Mathf.Max(
            scaledMinQuota,
            Mathf.RoundToInt(
                difficulty.MaxTotalQuota * quotaMultiplier));

        currentCombatMaxAliveBonus =
            EncounterFloorScaling.MaxAliveBonus(
                CurrentFloor,
                combatFastStepFloors,
                combatSoftKneeBonus,
                combatTailStepFloors);

        currentDefenseMaxAliveBonus =
            EncounterFloorScaling.MaxAliveBonus(
                CurrentFloor,
                defenseFastStepFloors,
                defenseSoftKneeBonus,
                defenseTailStepFloors);

        if (!EncounterPlanGenerator.TryGenerate(
                difficulty,
                seed,
                isFirstFloor,
                scaledMinQuota,
                scaledMaxQuota,
                out EncounterPlan plan,
                out string generationError))
        {
            error = $"Seed {seed} | {generationError}";
            return false;
        }

        currentEncounterPlan = plan;
        currentEncounterMode = encounterMode;
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
            + plan.totalQuota
            + " | QuotaMultiplier x"
            + quotaMultiplier.ToString("0.00")
            + " ("
            + scaledMinQuota
            + "~"
            + scaledMaxQuota
            + ") | EnemyHP x"
            + CurrentEnemyHealthMultiplier.ToString("0.00"),
            this);

        if (encounterMode == FloorEncounterMode.Defense)
        {
            if (!TryStartDefenseEncounter(out error))
            {
                ResetEncounterRuntime();
                return false;
            }

            return true;
        }

        if (encounterMode == FloorEncounterMode.Rush)
        {
            if (!TryStartRushEncounter(
                    difficulty,
                    out error))
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
    // Defense Encounter
    // ==================================================

    private bool TryStartDefenseEncounter(
        out string error)
    {
        if (currentEncounterPlan == null
            || currentEncounterPlan.waves == null
            || currentEncounterPlan.waves.Length == 0)
        {
            error = "Defense Encounter requires a generated "
                + "Encounter Plan.";
            return false;
        }

        if (defenseTargetPrefab == null)
        {
            error = "Defense Encounter requires a DefenseTarget Prefab "
                + "on FloorManager.";
            return false;
        }

        if (currentMapReferences == null)
        {
            error = "Defense Encounter requires bound MapSceneReferences.";
            return false;
        }

        if (!TrySpawnDefenseTarget(out DefenseTarget spawnedTarget))
        {
            error = "Defense Encounter could not place the DefenseTarget: "
                + "no usable Player Spawn Point on the current Map.";
            return false;
        }

        // One MaxAlive for the whole Floor: the director must never be
        // restarted per Assault, because that would reset AliveCount while
        // earlier Assault enemies are still on the field.
        int defenseMaxAlive = 1;

        foreach (EncounterWavePlan wave in currentEncounterPlan.waves)
        {
            if (wave != null && wave.maxAlive > defenseMaxAlive)
            {
                defenseMaxAlive = wave.maxAlive;
            }
        }

        // Defense uses its own, much slower Floor curve.
        defenseMaxAlive += currentDefenseMaxAliveBonus;

        if (!defenseSpawnDirector.TryBegin(
                defenseMaxAlive,
                out error))
        {
            return false;
        }

        BindDefenseTarget(spawnedTarget);

        defenseTarget.ResetForFloor();
        EncounterTarget.SetDefenseTarget(defenseTarget);

        defenseRuntimeActive = true;
        defenseSpawningEnabled = false;
        defenseRestPhase = false;
        defensePhaseRemaining = 0f;
        defenseSucceeded = false;
        defenseFailed = false;
        defenseRetryPending = false;
        defenseAssaultIndex = 0;
        currentWaveIndex = 0;
        currentWaveTimer = 0f;
        waveRunning = true;

        HideWaveUI();

        Debug.Log(
            "DEFENSE START | Floor "
            + CurrentFloor
            + " | Assaults "
            + currentEncounterPlan.waves.Length
            + " | DefenseBonus +"
            + currentDefenseMaxAliveBonus
            + " | MaxAlive "
            + defenseMaxAlive
            + " | AssaultDuration "
            + defenseAssaultDuration
            + " | RestDuration "
            + defenseRestDuration,
            this);

        defenseFlowCoroutine = StartCoroutine(
            DefenseFlowRoutine());

        defenseReleaseCoroutine = StartCoroutine(
            DefenseReleaseRoutine());

        error = string.Empty;
        return true;
    }


    // Places the Target on the Player Spawn Point nearest to the one this
    // Floor already chose, so a normal combat Map needs no Defense-specific
    // data. Deterministic: no Random, and the Encounter seed is untouched.
    private bool TrySpawnDefenseTarget(
        out DefenseTarget spawned)
    {
        spawned = null;

        DestroyRuntimeDefenseTarget();

        Transform playerSpawn =
            transitionManager != null
                ? transitionManager.CurrentPlayerSpawnPoint
                : null;

        if (playerSpawn == null)
        {
            return false;
        }

        currentMapReferences.CollectPlayerSpawnPoints(
            defenseSpawnCandidates);

        Transform best = null;
        float bestDistance = float.MaxValue;

        for (int i = 0; i < defenseSpawnCandidates.Count; i++)
        {
            Transform candidate = defenseSpawnCandidates[i];

            if (candidate == null
                || candidate == playerSpawn)
            {
                continue;
            }

            float distance =
                ((Vector2)candidate.position
                    - (Vector2)playerSpawn.position)
                        .sqrMagnitude;

            // Strict less-than keeps the earlier array entry on a tie.
            if (distance < bestDistance)
            {
                bestDistance = distance;
                best = candidate;
            }
        }

        Vector2 spawnPosition =
            best != null
                ? (Vector2)best.position
                : (Vector2)playerSpawn.position
                    + defenseTargetFallbackOffset;

        // Position is applied before Awake, so the Target caches this as the
        // origin its retry reset returns to.
        spawned = Instantiate(
            defenseTargetPrefab,
            spawnPosition,
            Quaternion.identity);

        // Parenting into the Map keeps it on the Map Scene's lifetime, so a
        // Map unload takes it with it.
        spawned.transform.SetParent(
            currentMapReferences.transform,
            true);

        Debug.Log(
            "DEFENSE TARGET SPAWNED | Floor "
            + CurrentFloor
            + " | Position "
            + spawnPosition
            + (best != null
                ? " | Nearest Player Spawn"
                : " | Fallback offset"),
            this);

        return true;
    }


    private void DestroyRuntimeDefenseTarget()
    {
        if (defenseTarget == null)
        {
            return;
        }

        GameObject targetObject =
            defenseTarget.gameObject;

        UnbindDefenseTarget();

        // Destroy only takes effect at end of frame, so deactivate first:
        // otherwise the old Collider/Rigidbody would briefly coexist with a
        // replacement spawned in the same frame.
        if (targetObject.activeSelf)
        {
            targetObject.SetActive(false);
        }

        Destroy(targetObject);
    }


    private void BindDefenseTarget(
        DefenseTarget target)
    {
        UnbindDefenseTarget();

        defenseTarget = target;
        defenseTarget.Destroyed += HandleDefenseTargetDestroyed;
    }


    private void UnbindDefenseTarget()
    {
        if (defenseTarget == null)
        {
            return;
        }

        defenseTarget.Destroyed -= HandleDefenseTargetDestroyed;
        defenseTarget = null;
    }


    // Assault N ends as soon as its enemies are gone, or when the Assault
    // duration runs out. Rest never releases pending enemies, which is what
    // makes it an actual breather.
    private IEnumerator DefenseFlowRoutine()
    {
        EncounterWavePlan[] waves =
            currentEncounterPlan.waves;

        for (int index = 0; index < waves.Length; index++)
        {
            if (!IsDefenseFlowRunning())
            {
                defenseFlowCoroutine = null;
                yield break;
            }

            defenseAssaultIndex = index;
            currentWaveIndex = index;
            defenseSpawningEnabled = true;
            defenseRestPhase = false;
            defensePhaseRemaining = defenseAssaultDuration;

            int enqueued = defenseSpawnDirector.EnqueueAssault(
                waves[index].spawnBag);

            Debug.Log(
                "DEFENSE ASSAULT "
                + (index + 1)
                + " START | Profile "
                + waves[index].profile.Profile
                + " | Size "
                + enqueued
                + " | Alive "
                + defenseSpawnDirector.AliveCount
                + " | Pending "
                + defenseSpawnDirector.PendingCount,
                this);

            float assaultElapsed = 0f;

            while (assaultElapsed < defenseAssaultDuration)
            {
                if (!IsDefenseFlowRunning())
                {
                    defenseFlowCoroutine = null;
                    yield break;
                }

                if (defenseSpawnDirector.AliveCount <= 0
                    && defenseSpawnDirector.PendingCount <= 0)
                {
                    break;
                }

                yield return null;
                assaultElapsed += Time.deltaTime;

                defensePhaseRemaining = Mathf.Max(
                    0f,
                    defenseAssaultDuration - assaultElapsed);
            }

            defenseSpawningEnabled = false;
            defensePhaseRemaining = 0f;

            Debug.Log(
                "DEFENSE ASSAULT "
                + (index + 1)
                + " END | Alive "
                + defenseSpawnDirector.AliveCount
                + " | Pending "
                + defenseSpawnDirector.PendingCount,
                this);

            if (index >= waves.Length - 1)
            {
                break;
            }

            defenseRestPhase = true;
            defensePhaseRemaining = defenseRestDuration;

            float restElapsed = 0f;

            while (restElapsed < defenseRestDuration)
            {
                if (!IsDefenseFlowRunning())
                {
                    defenseFlowCoroutine = null;
                    yield break;
                }

                yield return null;
                restElapsed += Time.deltaTime;

                defensePhaseRemaining = Mathf.Max(
                    0f,
                    defenseRestDuration - restElapsed);
            }

            defenseRestPhase = false;
            defensePhaseRemaining = 0f;
        }

        defenseFlowCoroutine = null;

        if (IsDefenseFlowRunning())
        {
            CompleteDefenseEncounter();
        }
    }


    private bool IsDefenseFlowRunning()
    {
        return defenseRuntimeActive
            && !defenseFailed
            && !defenseSucceeded
            && !floorCleared;
    }


    private IEnumerator DefenseReleaseRoutine()
    {
        while (defenseRuntimeActive
            && !defenseFailed
            && !floorCleared)
        {
            if (!defenseSpawningEnabled)
            {
                yield return null;
                continue;
            }

            int spawned = SpawnEncounterEnemyRound(
                defenseSpawnDirector,
                defenseSpawnDirector.MaxAlive
                    - defenseSpawnDirector.AliveCount);

            if (spawned > 0
                && defenseSpawnDirector.HasPendingSpawns
                && defenseSpawnDirector.AliveCount
                    < defenseSpawnDirector.MaxAlive)
            {
                yield return new WaitForSeconds(
                    encounterReusedSpawnPointDelay);
            }
            else
            {
                yield return null;
            }
        }

        defenseReleaseCoroutine = null;
    }


    private void CompleteDefenseEncounter()
    {
        if (defenseSucceeded || defenseFailed)
        {
            return;
        }

        // Same ordering rule as Rush Time Up: shut every spawn source down
        // before the purge, so defeat callbacks cannot release a backlog.
        defenseSucceeded = true;
        defenseSpawningEnabled = false;
        waveRunning = false;

        int discarded = defenseSpawnDirector.PendingCount;

        defenseSpawnDirector.ClearPending();
        StopDefenseEncounter();

        int purged = PurgeEncounterEnemies(true);

        Debug.Log(
            "DEFENSE SUCCESS | Discarded "
            + discarded
            + " pending | Purged "
            + purged,
            this);

        CheckFloorClear();
    }


    private void HandleDefenseTargetDestroyed()
    {
        if (!defenseRuntimeActive
            || defenseFailed
            || defenseSucceeded)
        {
            return;
        }

        defenseFailed = true;
        defenseSpawningEnabled = false;
        waveRunning = false;

        defenseSpawnDirector.ClearPending();
        StopDefenseEncounter();

        int removed = PurgeEncounterEnemies(false);

        PlayerLifeManager lifeManager =
            transitionManager != null
                ? transitionManager.playerLifeManager
                : null;

        if (lifeManager == null)
        {
            Debug.LogError(
                "Defense failure needs PlayerLifeManager to consume a "
                + "life. Floor cannot be retried.",
                this);
            return;
        }

        bool canRetry = lifeManager.TryConsumeLife();

        Debug.Log(
            "DEFENSE FAILED | Floor "
            + CurrentFloor
            + " | Removed "
            + removed
            + " enemies | Retry "
            + canRetry,
            this);

        if (!canRetry)
        {
            // TryConsumeLife already entered the existing Game Over path.
            return;
        }

        defenseRetryPending = true;

        transitionManager.StartCoroutine(
            transitionManager.RespawnPlayerRoutine(
                defenseRetryDelay,
                defenseRetryInvulnerability));
    }


    private void StopDefenseRelease()
    {
        if (defenseReleaseCoroutine == null)
        {
            return;
        }

        StopCoroutine(defenseReleaseCoroutine);
        defenseReleaseCoroutine = null;
    }


    private void StopDefenseEncounter()
    {
        if (defenseFlowCoroutine != null)
        {
            StopCoroutine(defenseFlowCoroutine);
            defenseFlowCoroutine = null;
        }

        StopDefenseRelease();
        defenseSpawningEnabled = false;
    }


    // ==================================================
    // Rush Encounter
    // ==================================================

    private bool TryStartRushEncounter(
        EncounterDifficultyDefinition difficulty,
        out string error)
    {
        if (currentEncounterPlan == null
            || currentEncounterPlan.waves == null
            || currentEncounterPlan.waves.Length == 0)
        {
            error = "Rush Encounter requires a generated Encounter Plan.";
            return false;
        }

        int rushMaxAlive =
            GetRushMaxAlive(difficulty);

        runtimeRushAssaultInterval =
            GetRushAssaultInterval(difficulty);

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
            + runtimeRushAssaultInterval
            + " | Difficulty "
            + difficulty.Difficulty
            + " | CombatBonus +"
            + currentCombatMaxAliveBonus
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


    // Difficulty already drives the Assault spawn bags through the Plan;
    // this is the second axis, how much of that backlog can be on the field
    // at once. Deterministic, and it consumes no Encounter seed.
    private int GetRushMaxAlive(
        EncounterDifficultyDefinition difficulty)
    {
        int baseMaxAlive =
            difficulty != null
                ? difficulty.BaseMaxAlive
                : 1;

        // Floor progression raises the Difficulty base first, then Rush
        // amplifies it, so depth and Difficulty stay on the same axis.
        baseMaxAlive += currentCombatMaxAliveBonus;

        return Mathf.Max(
            1,
            baseMaxAlive * rushMaxAlivePerBase
                + rushMaxAliveBonus);
    }


    // Third pressure axis: how fast the next Assault arrives. Uses the same
    // BaseMaxAlive the cap does, measured against Normal so Normal keeps the
    // tuned interval exactly. Deterministic, no Encounter seed consumed.
    private float GetRushAssaultInterval(
        EncounterDifficultyDefinition difficulty)
    {
        if (difficulty == null
            || normalEncounterDifficulty == null)
        {
            return rushAssaultInterval;
        }

        int step =
            normalEncounterDifficulty.BaseMaxAlive
                - difficulty.BaseMaxAlive;

        return Mathf.Max(
            0.1f,
            rushAssaultInterval
                + step * rushAssaultIntervalPerBase);
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
                rushNextAssaultRemaining =
                    runtimeRushAssaultInterval;
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
    public void RestartEncounterFloorIfNeeded()
    {
        if (defenseRetryPending)
        {
            RestartDefenseFloor();
            return;
        }

        RestartRushEncounterIfActive();
    }


    // Restarting the SAME Floor has to look like a first attempt, so the
    // failed run's ink and in-flight projectiles are wiped. A plain
    // respawn never calls this and keeps the world as it is.
    private void PrepareEncounterRetryWorld()
    {
        if (transitionManager != null)
        {
            transitionManager.ClearFloorCombatObjects();
        }

        if (InkMap.Instance != null
            && InkMap.Instance.IsReady)
        {
            InkMap.Instance.ClearAllInk();
        }
    }


    private void RestartDefenseFloor()
    {
        defenseRetryPending = false;

        StopDefenseEncounter();
        defenseSpawnDirector.ClearPending();
        PurgeEncounterEnemies(false);
        PrepareEncounterRetryWorld();

        Debug.Log(
            "DEFENSE RETRY | Floor "
            + CurrentFloor,
            this);

        SpawnCurrentFloor();
    }


    private void RestartRushEncounterIfActive()
    {
        if (!rushRuntimeActive)
        {
            return;
        }

        StopRushEncounter();
        rushSpawnDirector.ClearPending();

        int removed = PurgeEncounterEnemies(false);

        PrepareEncounterRetryWorld();

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
                currentCombatMaxAliveBonus,
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
            + encounterSpawnDirector.MaxAlive
            + " (Plan "
            + wave.maxAlive
            + " + Floor "
            + currentCombatMaxAliveBonus
            + ") | RefillDelay "
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
        if (defenseRuntimeActive)
        {
            // Defense never refills on a kill either. Freed capacity is
            // only used while an Assault phase is active.
            defenseSpawnDirector.NotifyDefeated();
            return;
        }

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
        StopDefenseEncounter();
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
        currentCombatMaxAliveBonus = 0;
        currentDefenseMaxAliveBonus = 0;
        encounterSpawnDirector.Reset();
        rushRuntimeActive = false;
        rushRemainingTime = 0f;
        rushNextAssaultRemaining = 0f;
        rushAssaultIndex = 0;
        rushSpawnDirector.Reset();
        defenseRuntimeActive = false;
        defenseSucceeded = false;
        defenseFailed = false;
        defenseAssaultIndex = 0;
        defenseRestPhase = false;
        defensePhaseRemaining = 0f;
        defenseSpawnDirector.Reset();
        DestroyRuntimeDefenseTarget();
        EncounterTarget.UsePlayerTarget();
        activeEncounterEnemies.Clear();
    }
}
