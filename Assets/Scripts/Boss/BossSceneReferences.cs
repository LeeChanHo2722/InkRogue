using UnityEngine;
using UnityEngine.Tilemaps;

public class BossSceneReferences : MonoBehaviour
{
    public GameObject normalArenaRoot;

    public GameObject bossArenaRoot;

    public Tilemap bossGround;

    public Tilemap bossCameraBounds;

    public Transform bossPlayerSpawnPoint;

    public Transform bossSpawnPoint;

    public Transform[] addSpawnPoints;
}
