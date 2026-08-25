using System.Collections;
using UnityEngine;

public class FloorTransitionManager : MonoBehaviour
{
    // ==================================================
    // References
    // ==================================================

    [Header("References")]

    public FloorManager floorManager;

    public UpgradeManager upgradeManager;

    [SerializeField]
    private FloorSelectionUI floorSelectionUI;

    [SerializeField]
    private MapSceneLoader mapSceneLoader;

    public InkScreenWipe screenWipe;

    public FloorStartTitleUI floorStartTitleUI;

    [SerializeField]
    private MapSceneReferences mapReferences;


    // ==================================================
    // Player
    // ==================================================

    [Header("Player")]

    public Rigidbody2D playerRigidbody;

    public PlayerMovement playerMovement;

    public PlayerWeaponInputController
        playerWeaponInputController;

    public PlayerDive playerDive;

    public PlayerFloorSpawnVisual playerSpawnVisual;

    public PlayerShield playerShield;

    public CameraFollow cameraFollow;

    public PlayerInkResource playerInkResource;

    public PlayerLifeManager playerLifeManager;


    // ==================================================
    // Slow Motion
    // ==================================================

    [Header("Floor Clear Slow Motion")]

    public SlowMotionController slowMotion;


    [Range(0.05f, 1f)]
    public float floorClearTimeScale =
        0.35f;


    [Tooltip(
        "Floor Clear가 발생한 뒤 "
        + "Slow 상태를 보여주는 실제 시간"
    )]
    public float floorClearSlowLead =
        0.08f;


    private Transform playerSpawnPoint;


    // ==================================================
    // Timing
    // ==================================================

    [Header("Timing")]

    [Tooltip(
        "게임 시작 파란 화면 유지 시간"
    )]
    public float gameStartHoldDuration =
        0.20f;


    [Tooltip(
        "리스폰 시 죽은 위치에서 Spawn Point까지 "
        + "카메라가 이동하는 시간"
    )]
    public float respawnCameraMoveDuration =
        0.45f;


    [Tooltip(
        "Floor Clear 문구 유지 시간"
    )]
    public float floorClearHoldDuration =
        0.45f;


    [Tooltip(
        "리스폰 시 화면이 완전히 가려진 상태로 "
        + "유지하는 시간"
    )]
    public float respawnCoveredHoldDuration =
        0.15f;


    [Tooltip(
        "Wipe가 완전히 사라진 후 "
        + "Player 등장까지 기다리는 시간"
    )]
    public float playerSpawnDelay =
        0.10f;


    [Tooltip(
        "Player 등장 완료 후 "
        + "Enemy Spawn까지 기다리는 시간"
    )]
    public float enemySpawnDelay =
        0.20f;


    // ==================================================
    // State
    // ==================================================

    private bool transitionRunning =
        false;


    private bool waitingForUpgrade =
        false;


    private bool originalPlayerSimulated =
        true;


    [SerializeField]
    private bool autoStartFirstFloor =
        true;


    private bool firstFloorStarted =
        false;


    // ==================================================
    // Awake
    // ==================================================

    private void Awake()
    {
        if (playerRigidbody != null)
        {
            originalPlayerSimulated =
                playerRigidbody.simulated;


            if (playerLifeManager == null)
            {
                playerLifeManager =
                    FindAnyObjectByType<
                        PlayerLifeManager
                    >();
            }


            if (playerShield == null)
            {
                playerShield =
                    playerRigidbody
                        .GetComponent<
                            PlayerShield
                        >();
            }


            if (playerInkResource == null)
            {
                playerInkResource =
                    playerRigidbody
                        .GetComponent<
                            PlayerInkResource
                        >();
            }


            if (playerWeaponInputController == null)
            {
                playerWeaponInputController =
                    playerRigidbody
                        .GetComponentInChildren<
                            PlayerWeaponInputController
                        >(
                            true
                        );
            }
        }
    }


    // ==================================================
    // Start
    // ==================================================

    private void Start()
    {
        if (mapReferences != null)
        {
            bool mapBindingSucceeded =
                BindMapReferences(
                    mapReferences
                );


            if (autoStartFirstFloor &&
                mapBindingSucceeded)
            {
                BeginFirstFloor();
            }
        }
        else if (autoStartFirstFloor)
        {
            Debug.LogError(
                "FloorTransitionManager requires "
                + "MapSceneReferences.",
                this
            );
        }
    }


    // ==================================================
    // Begin First Floor
    // ==================================================

    public void BeginFirstFloor()
    {
        if (firstFloorStarted)
            return;


        firstFloorStarted =
            true;


        RestoreTimeScale();


        StartCoroutine(
            StartFirstFloorRoutine()
        );
    }


    // ==================================================
    // Map Binding
    // ==================================================

    public bool SetRespawnPoint(
        Transform respawnPoint)
    {
        if (respawnPoint == null)
        {
            Debug.LogError(
                "FloorTransitionManager requires a valid "
                + "Player Respawn Point.",
                this
            );


            return false;
        }


        playerSpawnPoint =
            respawnPoint;


        return true;
    }


    public bool BindMapReferences(
        MapSceneReferences mapReferences)
    {
        if (mapReferences == null)
        {
            Debug.LogError(
                "FloorTransitionManager requires "
                + "MapSceneReferences."
            );


            return false;
        }


        if (floorManager == null)
        {
            Debug.LogError(
                "FloorTransitionManager requires "
                + "FloorManager.",
                this
            );


            return false;
        }


        if (mapReferences.enemySpawnPoints == null ||
            mapReferences.enemySpawnPoints.Length == 0)
        {
            Debug.LogError(
                "MapSceneReferences requires Enemy "
                + "Spawn Points.",
                this
            );


            return false;
        }


        for (int i = 0;
             i < mapReferences.enemySpawnPoints.Length;
             i++)
        {
            if (mapReferences.enemySpawnPoints[i] != null)
                continue;


            Debug.LogError(
                "MapSceneReferences contains a null "
                + "Enemy Spawn Point.",
                this
            );


            return false;
        }


        if (mapReferences.playerSpawnPoint == null)
        {
            Debug.LogError(
                "MapSceneReferences requires a Player "
                + "Spawn Point.",
                this
            );


            return false;
        }


        if (mapReferences.groundTilemap == null)
        {
            Debug.LogError(
                "MapSceneReferences requires a Ground "
                + "Tilemap.",
                this
            );


            return false;
        }
        else if (InkMap.Instance == null)
        {
            Debug.LogError(
                "FloorTransitionManager requires InkMap.",
                this
            );


            return false;
        }


        if (mapReferences.cameraBoundsTilemap == null)
        {
            Debug.LogError(
                "MapSceneReferences requires a Camera "
                + "Bounds Tilemap.",
                this
            );


            return false;
        }
        else if (cameraFollow == null)
        {
            Debug.LogError(
                "FloorTransitionManager requires "
                + "CameraFollow.",
                this
            );


            return false;
        }


        if (!SetRespawnPoint(
                mapReferences.playerSpawnPoint
            ))
        {
            return false;
        }


        floorManager.BindMapReferences(
            mapReferences
        );


        InkMap.Instance.SwitchGroundTilemap(
            mapReferences.groundTilemap
        );


        cameraFollow.SwitchBoundsTilemap(
            mapReferences.cameraBoundsTilemap
        );


        return true;
    }


    // ==================================================
    // First Floor
    // ==================================================

    private IEnumerator StartFirstFloorRoutine()
    {
        transitionRunning =
            true;


        RestoreTimeScale();


        LockPlayer();


        yield return
            new WaitForSecondsRealtime(
                gameStartHoldDuration
            );


        yield return StartCoroutine(
            PrepareFloorStartRoutine()
        );


        if (screenWipe != null)
        {
            yield return StartCoroutine(
                screenWipe.Reveal()
            );
        }


        if (playerSpawnDelay > 0f)
        {
            yield return
                new WaitForSecondsRealtime(
                    playerSpawnDelay
                );
        }


        if (playerSpawnVisual != null)
        {
            yield return StartCoroutine(
                playerSpawnVisual.PlaySpawn()
            );
        }


        if (playerLifeManager != null)
        {
            playerLifeManager
                .SetHUDVisible(
                    true
                );
        }


        if (floorStartTitleUI != null &&
            floorManager != null)
        {
            yield return StartCoroutine(
                floorStartTitleUI
                    .ShowFloorTitle(
                        floorManager
                            .CurrentFloor
                    )
            );
        }


        if (enemySpawnDelay > 0f)
        {
            yield return
                new WaitForSecondsRealtime(
                    enemySpawnDelay
                );
        }


        if (floorManager != null)
        {
            floorManager
                .SpawnCurrentFloor();
        }


        UnlockPlayer();


        transitionRunning =
            false;
    }


    // ==================================================
    // Floor Clear
    // ==================================================

    public void BeginFloorClearTransition()
    {
        if (transitionRunning ||
            waitingForUpgrade)
        {
            return;
        }


        StartCoroutine(
            FloorClearRoutine()
        );
    }


    private IEnumerator FloorClearRoutine()
    {
        transitionRunning =
            true;


        LockPlayer();


        if (playerLifeManager != null)
        {
            playerLifeManager
                .SetHUDVisible(
                    false
                );
        }


        ClearFloorCombatObjects();


        if (slowMotion != null)
        {
            slowMotion.SetTimeScale(
                floorClearTimeScale
            );
        }
        else
        {
            Time.timeScale =
                floorClearTimeScale;
        }


        if (floorClearSlowLead > 0f)
        {
            yield return
                new WaitForSecondsRealtime(
                    floorClearSlowLead
                );
        }


        if (floorClearHoldDuration > 0f)
        {
            yield return
                new WaitForSecondsRealtime(
                    floorClearHoldDuration
                );
        }


        if (screenWipe != null)
        {
            yield return StartCoroutine(
                screenWipe.Cover()
            );
        }


        if (playerLifeManager != null)
        {
            playerLifeManager
                .SetHUDVisible(
                    false
                );
        }


        RestoreTimeScale();


        if (floorManager != null)
        {
            floorManager
                .HideFloorClearText();
        }


        if (upgradeManager != null)
        {
            upgradeManager
                .ShowUpgrades();
        }


        Time.timeScale =
            0f;


        if (screenWipe != null)
        {
            yield return StartCoroutine(
                screenWipe.Reveal()
            );
        }


        if (upgradeManager != null)
        {
            yield return StartCoroutine(
                upgradeManager
                    .PlayOpenAnimation()
            );
        }


        waitingForUpgrade =
            true;


        transitionRunning =
            false;
    }


    // ==================================================
    // Upgrade Selected
    // ==================================================

    public void UpgradeSelected()
    {
        if (!waitingForUpgrade)
        {
            return;
        }


        if (transitionRunning)
        {
            return;
        }


        RunManager runManager =
            floorManager != null
                ? floorManager.runManager
                : null;


        bool candidatesPrepared =
            runManager != null &&
            runManager.TryPrepareFloorCandidates(
                floorManager.FloorDefinitions
            );


        if (candidatesPrepared &&
            floorSelectionUI != null &&
            floorSelectionUI.ShowCandidates(
                runManager.FloorCandidates,
                FloorCandidateSelected
            ))
        {
            if (upgradeManager != null)
                upgradeManager.HideUpgrades();


            return;
        }


        Debug.LogWarning(
            "Floor Selection is unavailable. Continuing legacy progression.",
            this
        );


        StartCoroutine(
            NextFloorRoutine()
        );
    }


    private void FloorCandidateSelected(
        FloorCandidate candidate)
    {
        if (!waitingForUpgrade ||
            transitionRunning)
        {
            return;
        }


        RunManager runManager =
            floorManager != null
                ? floorManager.runManager
                : null;


        if (runManager == null ||
            !runManager.TrySelectFloorCandidate(candidate))
        {
            Debug.LogWarning(
                "Floor Selection candidate could not be selected.",
                this
            );
            return;
        }


        if (floorSelectionUI != null)
            floorSelectionUI.Hide();


        StartCoroutine(
            NextFloorRoutine()
        );
    }


    // ==================================================
    // Next Floor
    // ==================================================

    private IEnumerator NextFloorRoutine()
    {
        transitionRunning =
            true;


        waitingForUpgrade =
            false;


        if (screenWipe != null)
        {
            yield return StartCoroutine(
                screenWipe.Cover()
            );
        }


        RunManager runManager =
            floorManager != null
                ? floorManager.runManager
                : null;


        FloorDefinition selectedFloor =
            runManager != null
                ? runManager.SelectedNextFloor
                : null;


        if (selectedFloor != null)
        {
            MapDefinition selectedMap =
                selectedFloor.Map;


            if (selectedMap == null)
            {
                Debug.LogWarning(
                    "Selected FloorDefinition has no MapDefinition. "
                    + "Continuing on the current Map.",
                    this
                );
            }
            else
            {
                if (mapSceneLoader == null)
                {
                    Debug.LogError(
                        "FloorTransitionManager requires MapSceneLoader "
                        + "to apply the selected Floor Map.",
                        this
                    );


                    yield return StartCoroutine(
                        RestoreFloorSelectionAfterMapSwitchFailure(
                            runManager
                        )
                    );
                    yield break;
                }


                bool mapSwitchCompleted =
                    false;


                bool mapSwitchSucceeded =
                    false;


                mapSceneLoader.SwitchMap(
                    selectedMap,
                    succeeded =>
                    {
                        mapSwitchSucceeded =
                            succeeded;


                        mapSwitchCompleted =
                            true;
                    }
                );


                while (!mapSwitchCompleted)
                    yield return null;


                if (!mapSwitchSucceeded)
                {
                    yield return StartCoroutine(
                        RestoreFloorSelectionAfterMapSwitchFailure(
                            runManager
                        )
                    );
                    yield break;
                }
            }
        }


        if (playerLifeManager != null)
        {
            playerLifeManager
                .SetHUDVisible(
                    true
                );
        }


        if (upgradeManager != null)
        {
            upgradeManager
                .HideUpgrades();
        }


        RestoreTimeScale();


        if (floorManager != null)
        {
            floorManager
                .AdvanceFloor();
        }


        yield return StartCoroutine(
            PrepareFloorStartRoutine()
        );


        if (screenWipe != null)
        {
            yield return StartCoroutine(
                screenWipe.Reveal()
            );
        }


        if (playerSpawnDelay > 0f)
        {
            yield return
                new WaitForSecondsRealtime(
                    playerSpawnDelay
                );
        }


        if (playerSpawnVisual != null)
        {
            yield return StartCoroutine(
                playerSpawnVisual
                    .PlaySpawn()
            );
        }


        if (playerLifeManager != null)
        {
            playerLifeManager
                .SetHUDVisible(
                    true
                );
        }


        if (floorStartTitleUI != null &&
            floorManager != null)
        {
            yield return StartCoroutine(
                floorStartTitleUI
                    .ShowFloorTitle(
                        floorManager
                            .CurrentFloor
                    )
            );
        }


        if (enemySpawnDelay > 0f)
        {
            yield return
                new WaitForSecondsRealtime(
                    enemySpawnDelay
                );
        }


        if (floorManager != null)
        {
            floorManager
                .SpawnCurrentFloor();
        }


        UnlockPlayer();


        transitionRunning =
            false;
    }


    private IEnumerator RestoreFloorSelectionAfterMapSwitchFailure(
        RunManager runManager)
    {
        waitingForUpgrade =
            true;


        bool selectionRestored =
            floorSelectionUI != null &&
            runManager != null &&
            floorSelectionUI.ShowCandidates(
                runManager.FloorCandidates,
                FloorCandidateSelected
            );


        if (!selectionRestored)
        {
            Debug.LogError(
                "FloorTransitionManager could not restore "
                + "Floor Selection after Map switching failed.",
                this
            );
        }


        if (screenWipe != null)
        {
            yield return StartCoroutine(
                screenWipe.Reveal()
            );
        }


        transitionRunning =
            false;
    }


    // ==================================================
    // Prepare Floor
    // ==================================================

    private IEnumerator PrepareFloorStartRoutine()
    {
        RunManager runManager =
            floorManager != null
                ? floorManager.runManager
                : null;


        if (runManager != null &&
            runManager.IsInitialized)
        {
            if (playerWeaponInputController == null)
            {
                Debug.LogError(
                    "FloorTransitionManager requires "
                    + "PlayerWeaponInputController to apply "
                    + "the Run Floor Loadout.",
                    this
                );
            }
            else
            {
                playerWeaponInputController
                    .ConfigureCombatLoadout(
                        runManager.LeftFloorLoadout,
                        runManager.RightFloorLoadout
                    );
            }
        }


        float waitTimer =
            0f;


        const float maxWaitTime =
            1f;


        while (
            (
                InkMap.Instance == null ||
                !InkMap.Instance.IsReady
            )
            &&
            waitTimer < maxWaitTime
        )
        {
            waitTimer +=
                Time.unscaledDeltaTime;


            yield return null;
        }


        ClearFloorCombatObjects();


        if (InkMap.Instance != null &&
            InkMap.Instance.IsReady)
        {
            InkMap.Instance
                .ClearAllInk();
        }


        if (playerInkResource != null)
        {
            playerInkResource
                .FillInk();
        }


        if (playerLifeManager != null)
        {
            playerLifeManager
                .ResetForNewFloor();
        }


        if (playerShield != null)
        {
            playerShield
                .ResetForNewFloor();
        }


        TeleportPlayerToSpawnPoint();


        if (playerSpawnVisual != null)
        {
            playerSpawnVisual
                .PrepareHidden();
        }


        yield return null;
    }


    // ==================================================
    // Respawn
    // ==================================================

    public IEnumerator RespawnPlayerRoutine(
        float respawnDelay,
        float invulnerabilityDuration
    )
    {
        if (transitionRunning)
        {
            yield break;
        }


        transitionRunning =
            true;


        float previousTimeScale =
            Time.timeScale;


        Time.timeScale =
            0f;


        LockPlayer();


        if (cameraFollow != null)
        {
            cameraFollow
                .BeginCinematicMode();
        }


        if (respawnDelay > 0f)
        {
            yield return
                new WaitForSecondsRealtime(
                    respawnDelay
                );
        }


        if (screenWipe != null)
        {
            yield return StartCoroutine(
                screenWipe.Cover()
            );
        }


        if (playerSpawnVisual != null)
        {
            playerSpawnVisual
                .PrepareHidden();
        }


        TeleportPlayerToSpawnPoint();


        if (playerInkResource != null)
        {
            playerInkResource
                .FillInk();
        }


        yield return null;


        if (respawnCoveredHoldDuration > 0f)
        {
            yield return
                new WaitForSecondsRealtime(
                    respawnCoveredHoldDuration
                );
        }


        if (screenWipe != null)
        {
            yield return StartCoroutine(
                screenWipe.Reveal()
            );
        }


        if (cameraFollow != null)
        {
            yield return StartCoroutine(
                cameraFollow
                    .MoveToPlayerRealtime(
                        respawnCameraMoveDuration
                    )
            );
        }


        if (playerShield != null)
        {
            playerShield
                .ResetAfterRespawn(
                    invulnerabilityDuration
                );
        }


        if (playerSpawnVisual != null)
        {
            playerSpawnVisual
                .PrepareHidden();
        }


        yield return null;


        if (playerSpawnVisual != null)
        {
            yield return StartCoroutine(
                playerSpawnVisual
                    .PlaySpawn()
            );
        }


        if (cameraFollow != null)
        {
            cameraFollow
                .EndCinematicMode();
        }


        Time.timeScale =
            previousTimeScale > 0f
                ? previousTimeScale
                : 1f;


        UnlockPlayer();


        transitionRunning =
            false;
    }


    // ==================================================
    // Floor Combat Object Cleanup
    // ==================================================

    private void ClearFloorCombatObjects()
    {
        FloorCleanupObject[] cleanupObjects =
            FindObjectsByType<
                FloorCleanupObject
            >(
                FindObjectsInactive.Exclude
            );


        foreach (
            FloorCleanupObject cleanupObject
            in cleanupObjects
        )
        {
            if (cleanupObject == null)
            {
                continue;
            }


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
    // Player Teleport
    // ==================================================

    private void TeleportPlayerToSpawnPoint()
    {
        if (playerSpawnPoint == null)
        {
            Debug.LogError(
                "FloorTransitionManager requires a player "
                + "spawn point from MapSceneReferences.",
                this
            );


            return;
        }


        Vector2 spawnPosition =
            playerSpawnPoint.position;


        if (playerRigidbody != null)
        {
            playerRigidbody.linearVelocity =
                Vector2.zero;


            playerRigidbody.position =
                spawnPosition;


            Vector3 position =
                playerRigidbody
                    .transform
                    .position;


            position.x =
                spawnPosition.x;


            position.y =
                spawnPosition.y;


            playerRigidbody
                .transform
                .position =
                position;


            Physics2D
                .SyncTransforms();
        }
        else if (playerMovement != null)
        {
            Vector3 position =
                playerMovement
                    .transform
                    .position;


            position.x =
                spawnPosition.x;


            position.y =
                spawnPosition.y;


            playerMovement
                .transform
                .position =
                position;


            Physics2D
                .SyncTransforms();
        }
    }


    // ==================================================
    // Player Lock
    // ==================================================

    public void LockPlayerForDeath()
    {
        LockPlayer();
    }


    private void LockPlayer()
    {
        // ==========================================
        // Weapon Input
        //
        // 현재 사용 중인 Shooter / Bomb을
        // 먼저 취소하고 이후 입력 차단.
        // ==========================================

        if (playerWeaponInputController != null)
        {
            playerWeaponInputController
                .SetInputEnabled(
                    false
                );
        }


        if (playerRigidbody != null)
        {
            playerRigidbody.linearVelocity =
                Vector2.zero;


            playerRigidbody.simulated =
                false;
        }


        if (playerMovement != null)
        {
            playerMovement.enabled =
                false;
        }


        if (playerDive != null)
        {
            playerDive.enabled =
                false;
        }
    }


    // ==================================================
    // Player Unlock
    // ==================================================

    private void UnlockPlayer()
    {
        if (playerRigidbody != null)
        {
            playerRigidbody.simulated =
                originalPlayerSimulated;


            playerRigidbody.linearVelocity =
                Vector2.zero;
        }


        if (playerMovement != null)
        {
            playerMovement.enabled =
                true;
        }


        if (playerDive != null)
        {
            playerDive.enabled =
                true;
        }


        if (playerWeaponInputController != null)
        {
            playerWeaponInputController
                .SetInputEnabled(
                    true
                );
        }
    }


    // ==================================================
    // Time Scale Safety
    // ==================================================

    private void RestoreTimeScale()
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
    // Safety
    // ==================================================

    private void OnDestroy()
    {
        RestoreTimeScale();
    }
}
