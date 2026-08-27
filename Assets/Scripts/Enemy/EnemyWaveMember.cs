using UnityEngine;

public class EnemyWaveMember : MonoBehaviour
{
    // ==================================================
    // Runtime
    // ==================================================

    private FloorManager floorManager;

    private int waveIndex = -1;

    private bool deathReported = false;


    // ==================================================
    // Initialize
    // ==================================================

    public void Initialize(
        FloorManager manager,
        int spawnedWaveIndex)
    {
        floorManager =
            manager;


        waveIndex =
            spawnedWaveIndex;


        deathReported =
            false;
    }


    // ==================================================
    // Report Death
    // ==================================================

    public void ReportDeath(
        bool grantPlayerCredit = true)
    {
        if (deathReported)
            return;


        deathReported =
            true;


        if (floorManager != null)
        {
            floorManager.EnemyDefeated(
                waveIndex,
                grantPlayerCredit
            );
        }
    }
}