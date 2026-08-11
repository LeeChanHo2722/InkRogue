using System.Collections.Generic;
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


    // ==================================================
    // Spawn Points
    // ==================================================

    [Header("Spawn Points")]

    public Transform[] spawnPoints;


    // ==================================================
    // Floor Wave Data
    // ==================================================

    [Header("Floor Wave Data")]

    public List<FloorWaveData> floors =
        new List<FloorWaveData>();


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
    // Floor
    // ==================================================

    [Header("Floor")]

    public int maxFloor = 3;


    // ==================================================
    // Runtime - Floor
    // ==================================================

    private int currentFloor =
        1;


    private int remainingEnemies =
        0;


    private bool floorCleared =
        false;


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
        currentFloor;


    public int RemainingEnemies =>
        remainingEnemies;


    public int CurrentWave =>
        currentWaveIndex + 1;


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
                + currentFloor
                + " Wave Data가 없습니다."
            );


            return;
        }


        if (floorData.waves == null ||
            floorData.waves.Count == 0)
        {
            Debug.LogError(
                "FLOOR "
                + currentFloor
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
            "================================"
        );


        Debug.Log(
            "FLOOR "
            + currentFloor
            + " START"
        );


        Debug.Log(
            "================================"
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
            "------------------------------"
        );


        Debug.Log(
            "FLOOR "
            + currentFloor
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


        Debug.Log(
            "Wave Enemies: "
            + waveSpawnCounts[
                currentWaveIndex
            ]
            + " | Total Alive: "
            + remainingEnemies
        );


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
                "Spawn Point가 없습니다."
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


        Debug.Log(
            "Enemy Defeated"
            + " | From Wave: "
            + (sourceWaveIndex + 1)
            + " | Total Alive: "
            + remainingEnemies
        );


        CheckFloorClear();
    }


    // ==================================================
    // Check Floor Clear
    // ==================================================

    private void CheckFloorClear()
    {
        if (floorCleared)
            return;


        FloorWaveData floorData =
            GetCurrentFloorData();


        if (floorData == null ||
            floorData.waves == null ||
            floorData.waves.Count == 0)
        {
            return;
        }


        bool lastWaveStarted =
            currentWaveIndex >=
            floorData.waves.Count - 1;


        if (!lastWaveStarted)
            return;


        if (remainingEnemies > 0)
            return;


        FloorClear();
    }


    // ==================================================
    // Get Floor Data
    // ==================================================

    private FloorWaveData
        GetCurrentFloorData()
    {
        int index =
            currentFloor - 1;


        if (floors == null)
            return null;


        if (index < 0 ||
            index >= floors.Count)
        {
            return null;
        }


        return
            floors[index];
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

        if (currentFloor >=
            maxFloor)
        {
            Debug.Log(
                "FLOOR "
                + currentFloor
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
                + currentFloor
                + " CLEAR";


            floorClearText
                .gameObject
                .SetActive(true);
        }


        Debug.Log(
            "FLOOR "
            + currentFloor
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
        currentFloor++;


        Debug.Log(
            "Preparing FLOOR "
            + currentFloor
        );
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