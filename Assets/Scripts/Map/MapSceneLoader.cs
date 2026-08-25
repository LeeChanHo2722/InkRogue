using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MapSceneLoader : MonoBehaviour
{
    [SerializeField]
    private string mapSceneName =
        "Map_Prototype";

    [SerializeField]
    private FloorTransitionManager floorTransitionManager;

    [SerializeField]
    private BossArenaTransitionManager bossArenaTransitionManager;

    private bool loadStarted =
        false;

    private bool mapSwitchInProgress;

    private Scene currentMapScene;

    private MapSceneReferences currentMapReferences;

    private BossSceneReferences currentBossReferences;


    private void Start()
    {
        LoadMapAndBeginFirstFloor();
    }


    public void LoadMapAndBeginFirstFloor()
    {
        if (loadStarted)
            return;


        if (floorTransitionManager == null)
        {
            Debug.LogError(
                "MapSceneLoader requires "
                + "FloorTransitionManager.",
                this
            );


            return;
        }


        if (bossArenaTransitionManager == null)
        {
            Debug.LogError(
                "MapSceneLoader requires "
                + "BossArenaTransitionManager.",
                this
            );


            return;
        }


        if (string.IsNullOrWhiteSpace(mapSceneName))
        {
            Debug.LogError(
                "MapSceneLoader requires a map Scene name.",
                this
            );


            return;
        }


        if (!TryInitializeMigrationRun())
            return;


        loadStarted =
            true;


        StartCoroutine(
            LoadMapRoutine()
        );
    }


    public void SwitchMap(
        MapDefinition mapDefinition,
        Action<bool> onCompleted)
    {
        if (mapDefinition == null)
        {
            Debug.LogError(
                "MapSceneLoader cannot switch to a null MapDefinition.",
                this
            );
            onCompleted?.Invoke(false);
            return;
        }

        if (string.IsNullOrWhiteSpace(mapDefinition.SceneName))
        {
            Debug.LogError(
                "MapSceneLoader requires MapDefinition.SceneName.",
                this
            );
            onCompleted?.Invoke(false);
            return;
        }

        if (mapSwitchInProgress)
        {
            Debug.LogError(
                "MapSceneLoader cannot start another map switch "
                + "while one is in progress.",
                this
            );
            onCompleted?.Invoke(false);
            return;
        }

        if (!currentMapScene.IsValid() ||
            !currentMapScene.isLoaded)
        {
            Debug.LogError(
                "MapSceneLoader cannot switch maps before the initial "
                + "Map Scene finishes loading.",
                this
            );
            onCompleted?.Invoke(false);
            return;
        }

        if (currentMapScene.name == mapDefinition.SceneName)
        {
            onCompleted?.Invoke(TryReuseCurrentMap());
            return;
        }

        mapSwitchInProgress = true;
        StartCoroutine(
            SwitchMapRoutine(
                mapDefinition.SceneName,
                onCompleted
            )
        );
    }


    private bool TryInitializeMigrationRun()
    {
        if (floorTransitionManager == null)
            return LogMigrationRunError(
                "FloorTransitionManager is missing."
            );

        FloorManager floorManager =
            floorTransitionManager.floorManager;

        if (floorManager == null)
            return LogMigrationRunError("FloorManager is missing.");

        RunManager runManager = floorManager.runManager;

        if (runManager == null)
            return LogMigrationRunError("RunManager is missing.");

        if (runManager.IsInitialized)
            return true;

        Rigidbody2D playerRigidbody =
            floorTransitionManager.playerRigidbody;

        if (playerRigidbody == null)
            return LogMigrationRunError("Player Rigidbody2D is missing.");

        PlayerWeaponController weaponController =
            playerRigidbody.GetComponent<PlayerWeaponController>();

        if (weaponController == null)
            return LogMigrationRunError("PlayerWeaponController is missing.");

        WeaponDefinition rightWeapon = weaponController.RightWeapon;
        WeaponDefinition leftWeapon = weaponController.LeftWeapon;

        if (rightWeapon == null)
            return LogMigrationRunError("Right WeaponDefinition is missing.");

        if (leftWeapon == null)
            return LogMigrationRunError("Left WeaponDefinition is missing.");

        runManager.InitializeRun(
            RunMode.TwentyFloor,
            new[] { rightWeapon, leftWeapon }
        );

        bool rightAssigned =
            runManager.TrySetFloorLoadoutWeapon(
                WeaponSlotSide.Right,
                0,
                rightWeapon
            );

        bool leftAssigned =
            runManager.TrySetFloorLoadoutWeapon(
                WeaponSlotSide.Left,
                0,
                leftWeapon
            );

        bool alternateWeaponsAssigned =
            rightWeapon == leftWeapon ||
            (
                runManager.TrySetFloorLoadoutWeapon(
                    WeaponSlotSide.Right,
                    1,
                    leftWeapon
                ) &&
                runManager.TrySetFloorLoadoutWeapon(
                    WeaponSlotSide.Left,
                    1,
                    rightWeapon
                )
            );

        if (rightAssigned &&
            leftAssigned &&
            alternateWeaponsAssigned)
            return true;

        runManager.InitializeRun(
            RunMode.None,
            System.Array.Empty<WeaponDefinition>()
        );

        return LogMigrationRunError(
            "Floor Loadout initialization failed."
        );
    }


    private bool LogMigrationRunError(string message)
    {
        Debug.LogError(
            "MapSceneLoader could not initialize the migration Run: "
            + message,
            this
        );

        return false;
    }


    private IEnumerator LoadMapRoutine()
    {
        Scene mapScene =
            SceneManager.GetSceneByName(
                mapSceneName
            );


        if (!mapScene.isLoaded)
        {
            AsyncOperation loadOperation =
                SceneManager.LoadSceneAsync(
                    mapSceneName,
                    LoadSceneMode.Additive
                );


            if (loadOperation == null)
            {
                Debug.LogError(
                    "MapSceneLoader failed to start loading Scene '"
                    + mapSceneName
                    + "'.",
                    this
                );


                yield break;
            }


            yield return loadOperation;


            mapScene =
                SceneManager.GetSceneByName(
                    mapSceneName
                );
        }


        if (!mapScene.IsValid() || !mapScene.isLoaded)
        {
            Debug.LogError(
                "MapSceneLoader could not access loaded Scene '"
                + mapSceneName
                + "'.",
                this
            );


            yield break;
        }


        if (!SceneManager.SetActiveScene(mapScene))
        {
            Debug.LogError(
                "MapSceneLoader failed to set active Scene '"
                + mapSceneName
                + "'.",
                this
            );


            yield break;
        }


        FindSceneReferences(
            mapScene,
            out MapSceneReferences mapReferences,
            out BossSceneReferences bossReferences
        );


        if (mapReferences == null)
        {
            Debug.LogError(
                "MapSceneLoader could not find "
                + "MapSceneReferences in Scene '"
                + mapSceneName
                + "'.",
                this
            );


            yield break;
        }


        if (bossReferences == null)
        {
            Debug.LogError(
                "MapSceneLoader could not find "
                + "BossSceneReferences in Scene '"
                + mapSceneName
                + "'.",
                this
            );


            yield break;
        }


        if (!floorTransitionManager.BindMapReferences(
                mapReferences
            ))
        {
            Debug.LogError(
                "MapSceneLoader failed to bind "
                + "MapSceneReferences for Scene '"
                + mapSceneName
                + "'.",
                this
            );


            yield break;
        }


        if (!bossArenaTransitionManager.BindBossReferences(
                bossReferences
            ))
        {
            Debug.LogError(
                "MapSceneLoader failed to bind "
                + "BossSceneReferences for Scene '"
                + mapSceneName
                + "'.",
                this
            );


            yield break;
        }


        currentMapScene = mapScene;
        currentMapReferences = mapReferences;
        currentBossReferences = bossReferences;


        floorTransitionManager.BeginFirstFloor();
    }


    private IEnumerator SwitchMapRoutine(
        string sceneName,
        Action<bool> onCompleted)
    {
        Scene previousMapScene = currentMapScene;
        Scene targetScene = SceneManager.GetSceneByName(sceneName);
        bool loadedForSwitch = false;

        if (!targetScene.isLoaded)
        {
            AsyncOperation loadOperation =
                SceneManager.LoadSceneAsync(
                    sceneName,
                    LoadSceneMode.Additive
                );

            if (loadOperation == null)
            {
                FailMapSwitch(
                    sceneName,
                    "failed to start loading the target Scene.",
                    targetScene,
                    false,
                    false,
                    onCompleted
                );
                yield break;
            }

            loadedForSwitch = true;
            yield return loadOperation;
            targetScene = SceneManager.GetSceneByName(sceneName);
        }

        if (!targetScene.IsValid() || !targetScene.isLoaded)
        {
            FailMapSwitch(
                sceneName,
                "could not access the loaded target Scene.",
                targetScene,
                loadedForSwitch,
                false,
                onCompleted
            );
            yield break;
        }

        FindSceneReferences(
            targetScene,
            out MapSceneReferences mapReferences,
            out BossSceneReferences bossReferences
        );

        if (mapReferences == null ||
            !HasCompleteBossReferences(bossReferences))
        {
            FailMapSwitch(
                sceneName,
                "requires complete MapSceneReferences and "
                + "BossSceneReferences.",
                targetScene,
                loadedForSwitch,
                false,
                onCompleted
            );
            yield break;
        }

        if (!floorTransitionManager.BindMapReferences(mapReferences))
        {
            FailMapSwitch(
                sceneName,
                "failed to bind MapSceneReferences.",
                targetScene,
                loadedForSwitch,
                false,
                onCompleted
            );
            yield break;
        }

        if (!bossArenaTransitionManager.BindBossReferences(
                bossReferences
            ))
        {
            FailMapSwitch(
                sceneName,
                "failed to bind BossSceneReferences.",
                targetScene,
                loadedForSwitch,
                true,
                onCompleted
            );
            yield break;
        }

        if (!SceneManager.SetActiveScene(targetScene))
        {
            FailMapSwitch(
                sceneName,
                "failed to set the target Scene active.",
                targetScene,
                loadedForSwitch,
                true,
                onCompleted
            );
            yield break;
        }

        currentMapScene = targetScene;
        currentMapReferences = mapReferences;
        currentBossReferences = bossReferences;

        if (previousMapScene.IsValid() &&
            previousMapScene.isLoaded &&
            previousMapScene.handle != targetScene.handle &&
            previousMapScene.handle != gameObject.scene.handle)
        {
            AsyncOperation unloadOperation =
                SceneManager.UnloadSceneAsync(previousMapScene);

            if (unloadOperation == null)
            {
                Debug.LogError(
                    "MapSceneLoader switched to Scene '"
                    + sceneName
                    + "' but could not unload previous Map Scene '"
                    + previousMapScene.name
                    + "'.",
                    this
                );
                CompleteMapSwitch(onCompleted, false);
                yield break;
            }

            yield return unloadOperation;
        }

        CompleteMapSwitch(onCompleted, true);
    }


    private void FailMapSwitch(
        string sceneName,
        string message,
        Scene targetScene,
        bool unloadTarget,
        bool restoreBindings,
        Action<bool> onCompleted)
    {
        Debug.LogError(
            "MapSceneLoader could not switch to Scene '"
            + sceneName
            + "': "
            + message,
            this
        );

        if (restoreBindings && !RestoreCurrentMap())
        {
            Debug.LogError(
                "MapSceneLoader failed to restore the current Map "
                + "bindings after a switch failure.",
                this
            );
        }

        if (unloadTarget &&
            targetScene.IsValid() &&
            targetScene.isLoaded &&
            targetScene.handle != currentMapScene.handle)
        {
            SceneManager.UnloadSceneAsync(targetScene);
        }

        CompleteMapSwitch(onCompleted, false);
    }


    private bool TryReuseCurrentMap()
    {
        if (currentMapReferences == null ||
            !HasCompleteBossReferences(currentBossReferences))
        {
            Debug.LogError(
                "MapSceneLoader cannot reuse Scene '"
                + currentMapScene.name
                + "' because its Scene references are incomplete.",
                this
            );
            return false;
        }

        if (!floorTransitionManager.BindMapReferences(
                currentMapReferences
            ))
        {
            Debug.LogError(
                "MapSceneLoader failed to rebind MapSceneReferences "
                + "for Scene '"
                + currentMapScene.name
                + "'.",
                this
            );
            return false;
        }

        if (!bossArenaTransitionManager.BindBossReferences(
                currentBossReferences
            ))
        {
            Debug.LogError(
                "MapSceneLoader failed to rebind BossSceneReferences "
                + "for Scene '"
                + currentMapScene.name
                + "'.",
                this
            );
            return false;
        }

        return true;
    }


    private bool RestoreCurrentMap()
    {
        bool mapRestored =
            currentMapReferences != null &&
            floorTransitionManager.BindMapReferences(
                currentMapReferences
            );

        bool bossRestored =
            currentBossReferences != null &&
            bossArenaTransitionManager.BindBossReferences(
                currentBossReferences
            );

        bool activeSceneRestored =
            currentMapScene.IsValid() &&
            currentMapScene.isLoaded &&
            SceneManager.SetActiveScene(currentMapScene);

        return mapRestored &&
            bossRestored &&
            activeSceneRestored;
    }


    private static void FindSceneReferences(
        Scene scene,
        out MapSceneReferences mapReferences,
        out BossSceneReferences bossReferences)
    {
        mapReferences = null;
        bossReferences = null;

        GameObject[] rootObjects = scene.GetRootGameObjects();

        for (int i = 0; i < rootObjects.Length; i++)
        {
            if (mapReferences == null)
            {
                mapReferences =
                    rootObjects[i]
                        .GetComponentInChildren<MapSceneReferences>(true);
            }

            if (bossReferences == null)
            {
                bossReferences =
                    rootObjects[i]
                        .GetComponentInChildren<BossSceneReferences>(true);
            }

            if (mapReferences != null && bossReferences != null)
                return;
        }
    }


    private static bool HasCompleteBossReferences(
        BossSceneReferences bossReferences)
    {
        if (bossReferences == null ||
            bossReferences.normalArenaRoot == null ||
            bossReferences.bossArenaRoot == null ||
            bossReferences.bossGround == null ||
            bossReferences.bossCameraBounds == null ||
            bossReferences.bossPlayerSpawnPoint == null ||
            bossReferences.bossSpawnPoint == null ||
            bossReferences.addSpawnPoints == null ||
            bossReferences.addSpawnPoints.Length == 0)
        {
            return false;
        }

        for (int i = 0; i < bossReferences.addSpawnPoints.Length; i++)
        {
            if (bossReferences.addSpawnPoints[i] == null)
                return false;
        }

        return true;
    }


    private void CompleteMapSwitch(
        Action<bool> onCompleted,
        bool succeeded)
    {
        mapSwitchInProgress = false;
        onCompleted?.Invoke(succeeded);
    }


}
