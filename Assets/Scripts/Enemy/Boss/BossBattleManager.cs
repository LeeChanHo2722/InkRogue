using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BossBattleManager : MonoBehaviour
{
    // ==================================================
    // Boss
    // ==================================================

    [Header("Boss")]

    public GameObject bossPrefab;

    public Transform bossSpawnPoint;


    // ==================================================
    // UI
    // ==================================================

    [Header("UI")]

    public BossIntroUI bossIntroUI;

    public BossHealthUI bossHealthUI;


    // ==================================================
    // Result
    // ==================================================

    [Header("Result")]

    public ResultUI resultUI;


    // ==================================================
    // Intro Timing
    // ==================================================

    [Header("Intro Timing")]

    public float arenaHoldDuration = 0.70f;

    public float bossEmergeDuration = 0.45f;

    public float bossOvershoot = 1.15f;

    public float bossSettleDuration = 0.18f;


    // ==================================================
    // Adds
    // ==================================================

    [Header("Boss Adds")]

    public GameObject shooterPrefab;

    public GameObject bomberPrefab;

    public Transform[] addSpawnPoints;


    [Tooltip(
        "Boss 전투 시작 후 첫 Add 등장까지 시간"
    )]
    public float firstAddDelay = 9f;


    [Tooltip(
        "이후 Add Spawn 주기"
    )]
    public float addSpawnInterval = 15f;


    public int shooterCountPerWave = 2;

    public int bomberCountPerWave = 2;


    [Tooltip(
        "Add가 무한 누적되는 것을 막는 상한"
    )]
    public int maxAliveAdds = 8;


    // ==================================================
    // Boss Death
    // ==================================================

    [Header("Boss Death")]

    [Tooltip(
        "보스 사망 후 화면을 덮을 InkScreenWipe"
    )]
    public InkScreenWipe screenWipe;


    [Tooltip(
        "공용 SlowMotionController"
    )]
    public SlowMotionController slowMotion;


    [Range(0.05f, 1f)]
    [Tooltip(
        "보스 사망 순간의 Time Scale"
    )]
    public float bossDeathTimeScale = 0.18f;


    [Tooltip(
        "강한 Slow Motion을 유지하는 실제 시간"
    )]
    public float bossSlowHoldDuration = 1.20f;


    [Tooltip(
        "Slow Motion에서 정상속도로 "
        + "돌아오는 실제 시간"
    )]
    public float bossTimeRestoreDuration = 0.50f;


    [Tooltip(
        "정상속도 복구 후 Wipe 전 여운"
    )]
    public float bossBeforeWipeDelay = 0.18f;


    // ==================================================
    // Runtime
    // ==================================================

    private GameObject currentBossObject;

    private BossHealth currentBossHealth;

    private BossAttackController
        currentAttackController;


    private Coroutine addRoutine;

    private Coroutine bossDeathRoutine;


    private bool bossDeathRunning = false;


    private readonly List<GameObject>
        spawnedAdds =
            new List<GameObject>();


    public BossHealth CurrentBossHealth =>
        currentBossHealth;


    // ==================================================
    // Intro + Spawn
    // ==================================================

    public IEnumerator StartBossBattleRoutine()
    {
        yield return
            new WaitForSecondsRealtime(
                arenaHoldDuration
            );


        // ==========================================
        // WARNING
        // ==========================================

        if (bossIntroUI != null)
        {
            yield return StartCoroutine(
                bossIntroUI.PlayIntro(
                    "INK CORE"
                )
            );
        }


        SpawnBoss();


        if (currentBossObject == null)
        {
            yield break;
        }


        // 등장 중 무적
        if (currentBossHealth != null)
        {
            currentBossHealth
                .SetInvulnerable(
                    true
                );
        }


        Vector3 originalScale =
            currentBossObject
                .transform
                .localScale;


        currentBossObject
            .transform
            .localScale =
            Vector3.zero;


        // ==========================================
        // Emerge
        // ==========================================

        float timer = 0f;


        while (timer <
               bossEmergeDuration)
        {
            timer +=
                Time.unscaledDeltaTime;


            float t =
                Mathf.Clamp01(
                    timer
                    /
                    Mathf.Max(
                        bossEmergeDuration,
                        0.01f
                    )
                );


            float eased =
                EaseOutCubic(
                    t
                );


            float scale =
                Mathf.Lerp(
                    0f,
                    bossOvershoot,
                    eased
                );


            currentBossObject
                .transform
                .localScale =
                originalScale
                * scale;


            yield return null;
        }


        // ==========================================
        // Settle
        // ==========================================

        timer = 0f;


        while (timer <
               bossSettleDuration)
        {
            timer +=
                Time.unscaledDeltaTime;


            float t =
                Mathf.Clamp01(
                    timer
                    /
                    Mathf.Max(
                        bossSettleDuration,
                        0.01f
                    )
                );


            float scale =
                Mathf.Lerp(
                    bossOvershoot,
                    1f,
                    t
                );


            currentBossObject
                .transform
                .localScale =
                originalScale
                * scale;


            yield return null;
        }


        currentBossObject
            .transform
            .localScale =
            originalScale;


        // ==========================================
        // Boss HUD
        // ==========================================

        if (bossHealthUI != null &&
            currentBossHealth != null)
        {
            bossHealthUI.Bind(
                currentBossHealth
            );


            bossHealthUI.Show();
        }


        Debug.Log(
            "BOSS INTRO COMPLETE"
        );
    }


    // ==================================================
    // Begin Combat
    // ==================================================

    public void BeginCombat()
    {
        if (GameAudioManager.Instance != null)
        {
            GameAudioManager.Instance
                .PlayBossBGM();
        }

        if (currentBossHealth == null)
            return;


        if (bossDeathRunning)
            return;


        currentBossHealth
            .SetInvulnerable(
                false
            );


        if (currentAttackController != null)
        {
            currentAttackController
                .BeginCombat();
        }


        if (addRoutine == null)
        {
            addRoutine =
                StartCoroutine(
                    AddSpawnRoutine()
                );
        }


        Debug.Log(
            "BOSS BATTLE START"
        );
    }


    // ==================================================
    // Spawn Boss
    // ==================================================

    private void SpawnBoss()
    {
        if (bossPrefab == null ||
            bossSpawnPoint == null)
        {
            Debug.LogError(
                "Boss Prefab 또는 SpawnPoint가 없습니다."
            );


            return;
        }


        currentBossObject =
            Instantiate(
                bossPrefab,
                bossSpawnPoint.position,
                Quaternion.identity
            );


        currentBossHealth =
            currentBossObject
                .GetComponent<
                    BossHealth
                >();


        currentAttackController =
            currentBossObject
                .GetComponent<
                    BossAttackController
                >();


        if (currentBossHealth == null)
        {
            Debug.LogError(
                "BossHealth가 없습니다."
            );


            return;
        }


        currentBossHealth.BossDied +=
            OnBossDied;


        currentBossHealth.PhaseChanged +=
            OnBossPhaseChanged;
    }


    // ==================================================
    // Add Spawn Routine
    // ==================================================

    private IEnumerator AddSpawnRoutine()
    {
        yield return
            new WaitForSeconds(
                firstAddDelay
            );


        while (
            currentBossHealth != null
            &&
            !currentBossHealth.IsDead
            &&
            !bossDeathRunning
        )
        {
            CleanupAddList();


            if (spawnedAdds.Count <
                maxAliveAdds)
            {
                SpawnAddGroup();
            }


            yield return
                new WaitForSeconds(
                    addSpawnInterval
                );
        }


        addRoutine = null;
    }


    // ==================================================
    // Add Group
    // ==================================================

    private void SpawnAddGroup()
    {
        if (bossDeathRunning)
            return;


        if (addSpawnPoints == null ||
            addSpawnPoints.Length == 0)
        {
            return;
        }


        List<GameObject> spawnQueue =
            new List<GameObject>();


        // ==========================================
        // Shooter
        // ==========================================

        for (int i = 0;
             i < shooterCountPerWave;
             i++)
        {
            if (shooterPrefab != null)
            {
                spawnQueue.Add(
                    shooterPrefab
                );
            }
        }


        // ==========================================
        // Bomber
        // ==========================================

        for (int i = 0;
             i < bomberCountPerWave;
             i++)
        {
            if (bomberPrefab != null)
            {
                spawnQueue.Add(
                    bomberPrefab
                );
            }
        }


        Shuffle(
            spawnQueue
        );


        int availableSlots =
            Mathf.Max(
                0,
                maxAliveAdds
                - spawnedAdds.Count
            );


        int spawnCount =
            Mathf.Min(
                spawnQueue.Count,
                availableSlots
            );


        int startPoint =
            Random.Range(
                0,
                addSpawnPoints.Length
            );


        for (int i = 0;
             i < spawnCount;
             i++)
        {
            Transform point =
                addSpawnPoints[
                    (
                        startPoint + i
                    )
                    % addSpawnPoints.Length
                ];


            if (point == null)
                continue;


            GameObject enemy =
                Instantiate(
                    spawnQueue[i],
                    point.position,
                    Quaternion.identity
                );


            spawnedAdds.Add(
                enemy
            );
        }
    }


    // ==================================================
    // Add Cleanup
    // ==================================================

    private void CleanupAddList()
    {
        for (int i =
                 spawnedAdds.Count - 1;
             i >= 0;
             i--)
        {
            if (spawnedAdds[i] == null)
            {
                spawnedAdds.RemoveAt(
                    i
                );
            }
        }
    }


    private void ClearAllAdds()
    {
        foreach (
            GameObject enemy
            in spawnedAdds)
        {
            if (enemy == null)
                continue;


            enemy.SetActive(
                false
            );


            Destroy(
                enemy
            );
        }


        spawnedAdds.Clear();
    }


    // ==================================================
    // Combat Object Cleanup
    // ==================================================

    private void ClearCombatObjects()
    {
        FloorCleanupObject[] cleanupObjects =
            FindObjectsByType<FloorCleanupObject>(
                FindObjectsInactive.Exclude
            );


        foreach (
            FloorCleanupObject cleanupObject
            in cleanupObjects)
        {
            if (cleanupObject == null)
                continue;


            cleanupObject
                .gameObject
                .SetActive(
                    false
                );


            Destroy(
                cleanupObject.gameObject
            );
        }
    }


    // ==================================================
    // Phase
    // ==================================================

    private void OnBossPhaseChanged(
    int phase)
    {
        if (GameAudioManager.Instance != null)
        {
            GameAudioManager.Instance
                .PlayBossPhase();
        }


        Debug.Log(
            "BOSS MANAGER → PHASE "
            + phase
        );
    }


    // ==================================================
    // Boss Death Event
    // ==================================================

    private void OnBossDied()
    {
        if (bossDeathRunning)
            return;


        bossDeathRunning =
            true;

        if (GameAudioManager.Instance != null)
        {
            GameAudioManager.Instance
                .StopBGM();


            GameAudioManager.Instance
                .PlayBossDeath();
        }


        // ==========================================
        // Add Spawn Stop
        // ==========================================

        if (addRoutine != null)
        {
            StopCoroutine(
                addRoutine
            );


            addRoutine =
                null;
        }


        // ==========================================
        // Boss Attack Stop
        // ==========================================

        if (currentAttackController != null)
        {
            currentAttackController
                .StopCombat();
        }


        // ==========================================
        // Adds 제거
        // ==========================================

        ClearAllAdds();


        // ==========================================
        // Projectile / Bomb 제거
        // ==========================================

        ClearCombatObjects();


        // ==========================================
        // Death Sequence
        // ==========================================

        bossDeathRoutine =
            StartCoroutine(
                BossDeathRoutine()
            );


        Debug.Log(
            "BOSS MANAGER → DEATH SEQUENCE"
        );
    }


    // ==================================================
    // Boss Death Cinematic
    // ==================================================

    private IEnumerator BossDeathRoutine()
    {
        // ==========================================
        // 1. Strong Slow Motion
        // ==========================================

        if (slowMotion != null)
        {
            slowMotion.SetTimeScale(
                bossDeathTimeScale
            );
        }
        else
        {
            Time.timeScale =
                bossDeathTimeScale;
        }


        // ==========================================
        // 2. Boss Death VFX
        // ==========================================

        if (currentBossObject != null)
        {
            BossDeathVFX deathVFX =
                currentBossObject
                    .GetComponent<
                        BossDeathVFX
                    >();


            if (deathVFX != null)
            {
                StartCoroutine(
                    deathVFX
                        .PlayDeathVFX()
                );
            }
            else
            {
                Debug.LogWarning(
                    "BossDeathVFX가 Boss Prefab에 없습니다."
                );
            }
        }


        // ==========================================
        // 3. Slow Hold
        // ==========================================

        if (bossSlowHoldDuration > 0f)
        {
            yield return
                new WaitForSecondsRealtime(
                    bossSlowHoldDuration
                );
        }


        // ==========================================
        // 4. Slow → Normal
        // ==========================================

        if (slowMotion != null)
        {
            yield return StartCoroutine(
                slowMotion
                    .RestoreSmooth(
                        bossTimeRestoreDuration
                    )
            );
        }
        else
        {
            yield return StartCoroutine(
                RestoreTimeScaleFallback(
                    bossTimeRestoreDuration
                )
            );
        }


        // ==========================================
        // 5. Final Fragment Moment
        // ==========================================

        if (bossBeforeWipeDelay > 0f)
        {
            yield return
                new WaitForSecondsRealtime(
                    bossBeforeWipeDelay
                );
        }


        // ==========================================
        // 6. Screen Cover
        // ==========================================

        if (screenWipe != null)
        {
            yield return StartCoroutine(
                screenWipe.Cover()
            );
        }


        // ==========================================
        // 7. Boss 제거
        // ==========================================

        UnbindBossEvents();


        if (currentBossObject != null)
        {
            Destroy(
                currentBossObject
            );


            currentBossObject =
                null;
        }


        currentBossHealth =
            null;


        currentAttackController =
            null;


        // ==========================================
        // 8. Result 준비
        //
        // 아직 Wipe가 화면을 가리고 있음
        // ==========================================

        if (resultUI != null)
        {
            resultUI
                .PrepareResult();
        }
        else
        {
            Debug.LogWarning(
                "BossBattleManager: ResultUI가 연결되지 않았습니다."
            );
        }


        // ==========================================
        // 9. Result Reveal
        //
        // ResultUI가 TimeScale을 0으로 만들어도
        // InkScreenWipe는 Unscaled Time 사용
        // ==========================================

        if (screenWipe != null)
        {
            yield return StartCoroutine(
                screenWipe.Reveal()
            );
        }


        // ==========================================
        // 10. Result Content Animation
        // ==========================================

        if (resultUI != null)
        {
            yield return StartCoroutine(
                resultUI
                    .PlayOpenAnimation()
            );
        }


        bossDeathRoutine =
            null;


        Debug.Log(
            "RESULT SCREEN OPEN"
        );
    }


    // ==================================================
    // Fallback Smooth Restore
    // ==================================================

    private IEnumerator RestoreTimeScaleFallback(
        float duration)
    {
        float startScale =
            Time.timeScale;


        float timer = 0f;


        float safeDuration =
            Mathf.Max(
                duration,
                0.01f
            );


        while (timer <
               safeDuration)
        {
            timer +=
                Time.unscaledDeltaTime;


            float t =
                Mathf.Clamp01(
                    timer
                    / safeDuration
                );


            t =
                Mathf.SmoothStep(
                    0f,
                    1f,
                    t
                );


            Time.timeScale =
                Mathf.Lerp(
                    startScale,
                    1f,
                    t
                );


            yield return null;
        }


        Time.timeScale =
            1f;
    }


    // ==================================================
    // Time Safety
    // ==================================================

    private void RestoreTimeImmediate()
    {
        if (slowMotion != null)
        {
            slowMotion
                .RestoreImmediate();
        }
        else
        {
            Time.timeScale =
                1f;
        }
    }


    // ==================================================
    // Boss Events
    // ==================================================

    private void UnbindBossEvents()
    {
        if (currentBossHealth == null)
            return;


        currentBossHealth.BossDied -=
            OnBossDied;


        currentBossHealth.PhaseChanged -=
            OnBossPhaseChanged;
    }


    // ==================================================
    // Shuffle
    // ==================================================

    private void Shuffle(
        List<GameObject> list)
    {
        for (int i =
                 list.Count - 1;
             i > 0;
             i--)
        {
            int randomIndex =
                Random.Range(
                    0,
                    i + 1
                );


            GameObject temp =
                list[i];


            list[i] =
                list[randomIndex];


            list[randomIndex] =
                temp;
        }
    }


    // ==================================================
    // Ease
    // ==================================================

    private float EaseOutCubic(
        float t)
    {
        float inverse =
            1f - t;


        return
            1f
            - inverse
            * inverse
            * inverse;
    }


    // ==================================================
    // Safety
    // ==================================================

    private void OnDestroy()
    {
        UnbindBossEvents();


        RestoreTimeImmediate();
    }
}