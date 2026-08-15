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


        loadStarted =
            true;


        StartCoroutine(
            LoadMapRoutine()
        );
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


        MapSceneReferences mapReferences =
            null;

        BossSceneReferences bossReferences =
            null;


        GameObject[] rootObjects =
            mapScene.GetRootGameObjects();


        for (int i = 0; i < rootObjects.Length; i++)
        {
            if (mapReferences == null)
            {
                mapReferences =
                    rootObjects[i]
                        .GetComponentInChildren<
                            MapSceneReferences
                        >(
                            true
                        );
            }


            if (bossReferences == null)
            {
                bossReferences =
                    rootObjects[i]
                        .GetComponentInChildren<
                            BossSceneReferences
                        >(
                            true
                        );
            }


            if (mapReferences != null &&
                bossReferences != null)
            {
                break;
            }
        }


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


        floorTransitionManager.BeginFirstFloor();
    }
}
