using UnityEngine;
using UnityEngine.SceneManagement;

public static class RunRestart
{
    public static string ResolveScenePath()
    {
        MapSceneLoader loader = Object.FindAnyObjectByType<MapSceneLoader>(FindObjectsInactive.Include);
        // The active scene is an additive map. Restart the owner of the game systems.
        if (loader != null && !string.IsNullOrEmpty(loader.gameObject.scene.path))
            return loader.gameObject.scene.path;
        return SceneUtility.GetScenePathByBuildIndex(0);
    }

    public static void Restart()
    {
        string path = ResolveScenePath();
        if (string.IsNullOrEmpty(path))
        {
            Debug.LogError("Cannot restart: no bootstrap scene is configured.");
            return;
        }
        PlayerWeaponInputController.CancelActiveWeaponWheel();
        Time.timeScale = 1f;
        Time.fixedDeltaTime = 0.02f;
        SceneManager.LoadScene(path, LoadSceneMode.Single);
    }
}
