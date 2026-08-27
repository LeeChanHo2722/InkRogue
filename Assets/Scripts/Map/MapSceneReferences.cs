using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class MapSceneReferences : MonoBehaviour
{
    public Tilemap groundTilemap;

    public Tilemap cameraBoundsTilemap;

    public Transform[] playerSpawnPoints;

    public Transform[] enemySpawnPoints;

    [Header("Legacy")]

    [Tooltip("Used only when Player Spawn Points is empty. "
        + "Move the Transform into Player Spawn Points and clear this.")]
    public Transform playerSpawnPoint;

    // Fills buffer with the valid Player Spawn candidates in Inspector
    // order and returns how many were found. Selection itself belongs to
    // the caller, which knows the Floor's Encounter seed.
    public int CollectPlayerSpawnPoints(
        List<Transform> buffer)
    {
        if (buffer == null)
            return 0;

        buffer.Clear();

        if (playerSpawnPoints != null)
        {
            for (int i = 0; i < playerSpawnPoints.Length; i++)
            {
                if (playerSpawnPoints[i] != null)
                    buffer.Add(playerSpawnPoints[i]);
            }
        }

        if (buffer.Count == 0 && playerSpawnPoint != null)
            buffer.Add(playerSpawnPoint);

        return buffer.Count;
    }
}
