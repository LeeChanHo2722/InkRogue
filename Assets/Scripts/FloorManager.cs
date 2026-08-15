using TMPro;
using UnityEngine;

public class FloorManager : MonoBehaviour
{
    // ==================================================
    // Enemy Prefabs
    // ==================================================

    [Header("Enemy Prefabs")]

    public GameObject chaserPrefab;

    public GameObject shooterPrefab;

    public GameObject tankPrefab;

    public GameObject bomberPrefab;

    public GameObject sprinklerPrefab;


    private Transform[] spawnPoints;


    // ==================================================
    // Floor Definitions
    // ==================================================

    [Header("Floor Definitions")]

    public FloorDefinition[] floorDefinitions;


    [Header("Run")]

    public RunManager runManager;


    // ==================================================
    // UI / Managers
    // ==================================================

    [Header("UI / Managers")]

    public TextMeshProUGUI floorClearText;

    [Tooltip("선택 사항")]
    public TextMeshProUGUI waveText;

    public WaveStartUI waveStartUI;

    public FloorTransitionManager transitionManager;

    public BossArenaTransitionManager
        bossArenaTransitionManager;


    // ==================================================
    // Runtime - Floor
    // ==================================================

    private int remainingEnemies =
        0;


    private bool floorCleared =
        false;


    private FloorObjective floorObjective;


    // ==================================================
    // Runtime - Wave
    // ==================================================

    private int currentWaveIndex =
        -1;


    private float currentWaveTimer =
        0f;


    private bool waveRunning =
        false;


    private int[] waveSpawnCounts;

    private int[] waveKillCounts;


    private int spawnCursor =
        0;


    // ==================================================
    // Public
    // ==================================================

    public int CurrentFloor =>
        runManager.CurrentFloor;


    public int RemainingEnemies =>
        remainingEnemies;


    public int CurrentWave =>
        currentWaveIndex + 1;


    public bool IsLastWaveStarted
    {
        get
        {
            FloorWaveData floorData =
                GetCurrentFloorData();


            return
                floorData != null &&
                floorData.waves != null &&
                floorData.waves.Count > 0 &&
                currentWaveIndex >=
                floorData.waves.Count - 1;
        }
    }


    // ==================================================
    // Awake
    // ==================================================

    private void Awake()
    {
        if (runManager == null)
        {
            Debug.LogError(
                "FloorManager requires an assigned "
                + "RunManager reference.",
                this
            );


            return;
        }


        floorObjective =
            GetComponent<FloorObjective>();


        if (floorObjective == null)
        {
            floorObjective =
                gameObject.AddComponent<
                    EliminateAllObjective>();
        }


        floorObjective.Completed +=
            FloorClear;
    }


    private void OnDestroy()
    {
        if (floorObjective != null)
        {
            floorObjective.Completed -=
                FloorClear;
        }
    }


    // ==================================================
    // Start
    // ==================================================

    private void Start()
    {
        if (floorClearText != null)
        {
            floorClearText
                .gameObject
                .SetActive(false);
        }


        if (waveText != null)
        {
            waveText
                .gameObject
                .SetActive(false);
        }
    }


    // ==================================================
    // Map Binding
    // ==================================================

    public void BindMapReferences(
        MapSceneReferences mapReferences)
    {
        if (mapReferences == null ||
            mapReferences.enemySpawnPoints == null ||
            mapReferences.enemySpawnPoints.Length == 0)
        {
            return;
        }


        spawnPoints =
            mapReferences.enemySpawnPoints;
    }


    // ==================================================
    // Update
    // ==================================================

    private void Update()
    {
        if (floorCleared)
            return;


        if (!waveRunning)
            return;


        FloorWaveData floorData =
            GetCurrentFloorData();


        if (floorData == null)
            return;


        if (floorData.waves == null)
            return;


        if (currentWaveIndex < 0 ||
            currentWaveIndex >=
            floorData.waves.Count)
        {
            return;
        }


        // ==========================================
        // 마지막 Wave에는
        // 다음 Wave가 존재하지 않음
        // ==========================================

        bool isLastWave =
            currentWaveIndex >=
            floorData.waves.Count - 1;


        if (isLastWave)
            return;


        currentWaveTimer +=
            Time.deltaTime;


        WaveData currentWave =
            floorData.waves[
                currentWaveIndex
            ];


        int spawned =
            waveSpawnCounts[
                currentWaveIndex
            ];


        int killed =
            waveKillCounts[
                currentWaveIndex
            ];


        // ==========================================
        // 2/3 처치 조건
        // ==========================================

        int requiredKills =
            Mathf.CeilToInt(
                spawned
                * currentWave
                    .killRatioToAdvance
            );


        bool killConditionMet =
            spawned <= 0
            ||
            killed >= requiredKills;


        // ==========================================
        // 20초 조건
        // ==========================================

        bool timeConditionMet =
            currentWaveTimer >=
            currentWave.maxWaveDuration;


        // ==========================================
        // OR 조건
        // ==========================================

        if (killConditionMet ||
            timeConditionMet)
        {
            string reason =
                killConditionMet
                    ? "KILL RATIO"
                    : "TIME LIMIT";


            Debug.Log(
                "WAVE "
                + (currentWaveIndex + 1)
                + " ADVANCE | "
                + reason
            );


            StartWave(
                currentWaveIndex + 1
            );
        }
    }


    // ==================================================
    // Spawn Current Floor
    // ==================================================

    public void SpawnCurrentFloor()
    {
        if (spawnPoints == null ||
            spawnPoints.Length == 0)
        {
            Debug.LogError(
                "FloorManager requires enemy spawn "
                + "points from MapSceneReferences.",
                this
            );


            return;
        }


        floorCleared =
            false;


        waveRunning =
            false;


        currentWaveIndex =
            -1;


        currentWaveTimer =
            0f;


        remainingEnemies =
            0;


        floorObjective.Begin();


        if (floorClearText != null)
        {
            floorClearText
                .gameObject
                .SetActive(false);
        }


        FloorWaveData floorData =
            GetCurrentFloorData();


        if (floorData == null)
        {
            Debug.LogError(
                "FLOOR "
                + CurrentFloor
                + " Wave Data가 없습니다."
            );


            return;
        }


        if (floorData.waves == null ||
            floorData.waves.Count == 0)
        {
            Debug.LogError(
                "FLOOR "
                + CurrentFloor
                + "에 Wave가 없습니다."
            );


            return;
        }


        waveSpawnCounts =
            new int[
                floorData.waves.Count
            ];


        waveKillCounts =
            new int[
                floorData.waves.Count
            ];


        if (spawnPoints != null &&
            spawnPoints.Length > 0)
        {
            spawnCursor =
                Random.Range(
                    0,
                    spawnPoints.Length
                );
        }



        Debug.Log(
            "FLOOR "
            + CurrentFloor
            + " START"
        );


        StartWave(
            0
        );
    }


    // ==================================================
    // Start Wave
    // ==================================================

    private void StartWave(
        int waveIndex)
    {
        FloorWaveData floorData =
            GetCurrentFloorData();


        if (floorData == null)
            return;


        if (waveIndex < 0 ||
            waveIndex >=
            floorData.waves.Count)
        {
            return;
        }


        currentWaveIndex =
            waveIndex;


        currentWaveTimer =
            0f;


        waveRunning =
            true;


        WaveData wave =
            floorData.waves[
                currentWaveIndex
            ];


        // ==========================================
        // Wave HUD
        // ==========================================

        UpdateWaveUI(
            floorData.waves.Count
        );


        // ==========================================
        // Wave 시작 팝업
        // ==========================================

        if (waveStartUI != null)
        {
            waveStartUI.ShowWave(
                currentWaveIndex + 1,
                floorData.waves.Count
            );
        }

        Debug.Log(
            "FLOOR "
            + CurrentFloor
            + " | WAVE "
            + (currentWaveIndex + 1)
            + " START"
        );


        int localSpawnIndex =
            0;


        // ==========================================
        // Enemy Spawn
        // ==========================================

        if (wave.enemies != null)
        {
            foreach (
                WaveEnemyEntry entry
                in wave.enemies)
            {
                if (entry == null)
                    continue;


                GameObject prefab =
                    GetEnemyPrefab(
                        entry.enemyType
                    );


                if (prefab == null)
                {
                    Debug.LogWarning(
                        entry.enemyType
                        + " Prefab이 연결되지 않았습니다."
                    );


                    continue;
                }


                int count =
                    Mathf.Max(
                        0,
                        entry.count
                    );


                for (int i = 0;
                     i < count;
                     i++)
                {
                    SpawnEnemy(
                        prefab,
                        currentWaveIndex,
                        localSpawnIndex
                    );


                    localSpawnIndex++;
                }
            }
        }


        // 다음 Wave가 같은 위치부터
        // 시작하지 않도록 Cursor 이동
        if (spawnPoints != null &&
            spawnPoints.Length > 0)
        {
            spawnCursor =
                (
                    spawnCursor
                    + localSpawnIndex
                )
                % spawnPoints.Length;
        }


        CheckFloorClear();
    }


    // ==================================================
    // Spawn Enemy
    // ==================================================

    private void SpawnEnemy(
        GameObject prefab,
        int waveIndex,
        int localSpawnIndex)
    {
        if (prefab == null)
            return;


        if (spawnPoints == null ||
            spawnPoints.Length == 0)
        {
            Debug.LogError(
                "FloorManager requires enemy spawn "
                + "points from MapSceneReferences.",
                this
            );


            return;
        }


        int pointIndex =
            (
                spawnCursor
                + localSpawnIndex
            )
            % spawnPoints.Length;


        Transform spawnPoint =
            spawnPoints[
                pointIndex
            ];


        if (spawnPoint == null)
            return;


        GameObject enemy =
            Instantiate(
                prefab,
                spawnPoint.position,
                Quaternion.identity
            );


        // ==========================================
        // Wave 정보 부여
        // ==========================================

        EnemyWaveMember member =
            enemy.GetComponent<
                EnemyWaveMember
            >();


        if (member == null)
        {
            member =
                enemy.AddComponent<
                    EnemyWaveMember
                >();
        }


        member.Initialize(
            this,
            waveIndex
        );


        // ==========================================
        // Count
        // ==========================================

        remainingEnemies++;


        waveSpawnCounts[
            waveIndex
        ]++;
    }


    // ==================================================
    // Enemy Prefab
    // ==================================================

    private GameObject GetEnemyPrefab(
        WaveEnemyType type)
    {
        switch (type)
        {
            case WaveEnemyType.Chaser:

                return
                    chaserPrefab;


            case WaveEnemyType.Shooter:

                return
                    shooterPrefab;


            case WaveEnemyType.Tank:

                return
                    tankPrefab;


            case WaveEnemyType.Bomber:

                return
                    bomberPrefab;


            case WaveEnemyType.Sprinkler:

                return
                    sprinklerPrefab;
        }


        return null;
    }


    // ==================================================
    // Enemy Defeated
    // ==================================================

    public void EnemyDefeated(
        int sourceWaveIndex)
    {
        if (floorCleared)
            return;


        // ==========================================
        // 전체 생존 Enemy 수
        // ==========================================

        remainingEnemies =
            Mathf.Max(
                0,
                remainingEnemies - 1
            );


        // ==========================================
        // 자신이 태어난 Wave의 Kill Count
        // ==========================================

        if (waveKillCounts != null &&
            sourceWaveIndex >= 0 &&
            sourceWaveIndex <
            waveKillCounts.Length)
        {
            waveKillCounts[
                sourceWaveIndex
            ]++;
        }



        CheckFloorClear();
    }


    // ==================================================
    // Check Floor Clear
    // ==================================================

    private void CheckFloorClear()
    {
        if (floorCleared ||
            floorObjective == null)
            return;


        floorObjective.Evaluate();
    }


    // ==================================================
    // Get Floor Data
    // ==================================================

    private FloorWaveData
        GetCurrentFloorData()
    {
        int index =
            CurrentFloor - 1;


        if (floorDefinitions == null ||
            index < 0 ||
            index >= floorDefinitions.Length)
        {
            return null;
        }


        FloorDefinition floorDefinition =
            floorDefinitions[index];


        return floorDefinition != null
            ? floorDefinition.floorData
            : null;
    }


    // ==================================================
    // Wave UI
    // ==================================================

    private void UpdateWaveUI(
        int totalWaveCount)
    {
        if (waveText == null)
            return;


        waveText
            .gameObject
            .SetActive(true);


        waveText.text =
            "WAVE "
            + (currentWaveIndex + 1)
            + " / "
            + totalWaveCount;
    }


    private void HideWaveUI()
    {
        if (waveText == null)
            return;


        waveText
            .gameObject
            .SetActive(false);
    }


    // ==================================================
    // Floor Clear
    // ==================================================

    private void FloorClear()
    {
        if (floorCleared)
            return;


        floorCleared =
            true;


        waveRunning =
            false;


        HideWaveUI();


        // ==========================================
        // FLOOR 3 완료
        // → Boss Arena
        // ==========================================

        if (runManager.IsLastFloor)
        {
            Debug.Log(
                "FLOOR "
                + CurrentFloor
                + " CLEAR"
                + " → BOSS ARENA"
            );


            if (bossArenaTransitionManager != null)
            {
                bossArenaTransitionManager
                    .BeginBossTransition();
            }
            else
            {
                Debug.LogError(
                    "BossArenaTransitionManager가 "
                    + "연결되지 않았습니다."
                );
            }


            return;
        }


        // ==========================================
        // FLOOR 1 / 2
        // ==========================================

        if (floorClearText != null)
        {
            floorClearText.text =
                "FLOOR "
                + CurrentFloor
                + " CLEAR";


            floorClearText
                .gameObject
                .SetActive(true);
        }


        Debug.Log(
            "FLOOR "
            + CurrentFloor
            + " CLEAR!"
        );


        if (transitionManager != null)
        {
            transitionManager
                .BeginFloorClearTransition();
        }
    }


    // ==================================================
    // Advance Floor
    // ==================================================

    public void AdvanceFloor()
    {
        runManager.AdvanceFloor();
    }


    // ==================================================
    // Hide Floor Clear
    // ==================================================

    public void HideFloorClearText()
    {
        if (floorClearText != null)
        {
            floorClearText
                .gameObject
                .SetActive(false);
        }
    }
}