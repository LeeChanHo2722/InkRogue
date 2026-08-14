using System.Collections;
using UnityEngine;
using UnityEngine.Tilemaps;

public class BossArenaTransitionManager : MonoBehaviour
{
    // ==================================================
    // Arena
    // ==================================================

    [Header("Arena")]

    [Tooltip("기존 Floor 1~3 Grid")]
    public GameObject normalArenaRoot;

    [Tooltip("새 BossGrid")]
    public GameObject bossArenaRoot;

    [Tooltip("BossGrid 안의 BossGround")]
    public Tilemap bossGround;

    [Tooltip("Boss Camera 제한에 사용할 BossWalls")]
    public Tilemap bossCameraBounds;


    // ==================================================
    // Boss Spawn
    // ==================================================

    [Header("Boss Spawn")]

    public Transform bossPlayerSpawnPoint;

    public Transform bossSpawnPoint;


    [Header("Boss Battle")]

    public BossBattleManager bossBattleManager;


    // ==================================================
    // Transition
    // ==================================================

    [Header("Transition")]

    public InkScreenWipe screenWipe;

    public float floorClearHold =
        0.50f;

    public float revealEndHold =
        0.15f;


    // ==================================================
    // Camera
    // ==================================================

    [Header("Camera")]

    public CameraFollow cameraFollow;


    // ==================================================
    // Player
    // ==================================================

    [Header("Player")]

    public Rigidbody2D playerRigidbody;

    public PlayerMovement playerMovement;

    public PlayerWeaponInputController
        playerWeaponInputController;

    public PlayerDive playerDive;

    public PlayerShield playerShield;

    public PlayerInkResource playerInkResource;


    // ==================================================
    // Runtime
    // ==================================================

    private bool transitionRunning =
        false;


    private bool originalSimulated =
        true;


    private bool movementWasEnabled;

    private bool diveWasEnabled;

    private bool weaponInputWasEnabled;


    public bool IsTransitionRunning =>
        transitionRunning;


    // ==================================================
    // Awake
    // ==================================================

    private void Awake()
    {
        if (playerWeaponInputController == null &&
            playerRigidbody != null)
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


    // ==================================================
    // Start
    // ==================================================

    private void Start()
    {
        if (normalArenaRoot != null)
        {
            normalArenaRoot
                .SetActive(
                    true
                );
        }


        if (bossArenaRoot != null)
        {
            bossArenaRoot
                .SetActive(
                    false
                );
        }
    }


    // ==================================================
    // Begin
    // ==================================================

    public void BeginBossTransition()
    {
        if (transitionRunning)
        {
            return;
        }


        StartCoroutine(
            BossTransitionRoutine()
        );
    }


    // ==================================================
    // Transition Routine
    // ==================================================

    private IEnumerator BossTransitionRoutine()
    {
        transitionRunning =
            true;


        Debug.Log(
            "BOSS TRANSITION START"
        );


        // ==========================================
        // 1. Player 잠금
        // ==========================================

        LockPlayer();


        // ==========================================
        // 2. 기존 Projectile 삭제
        // ==========================================

        ClearFloorCombatObjects();


        // ==========================================
        // 3. Floor Clear Hold
        // ==========================================

        if (floorClearHold > 0f)
        {
            yield return
                new WaitForSecondsRealtime(
                    floorClearHold
                );
        }


        // ==========================================
        // 4. 화면 Cover
        // ==========================================

        if (screenWipe != null)
        {
            yield return StartCoroutine(
                screenWipe.Cover()
            );
        }


        // ==========================================
        // 5. Boss Arena 활성화
        // ==========================================

        ActivateBossArena();


        // ==========================================
        // 6. 남아있는 Projectile 재정리
        // ==========================================

        ClearFloorCombatObjects();


        // ==========================================
        // 7. Player Resource Reset
        // ==========================================

        if (playerInkResource != null)
        {
            playerInkResource
                .FillInk();
        }


        if (playerShield != null)
        {
            playerShield
                .ResetForNewFloor();
        }


        // ==========================================
        // 8. Player Boss Spawn으로 이동
        // ==========================================

        TeleportPlayerToBossSpawn();


        // ==========================================
        // 9. Camera 전환
        // ==========================================

        SwitchCameraToBossArena();


        // ==========================================
        // 10. Physics / Tilemap 갱신
        // ==========================================

        yield return null;


        Physics2D.SyncTransforms();


        // ==========================================
        // 11. Arena Reveal
        // ==========================================

        if (screenWipe != null)
        {
            yield return StartCoroutine(
                screenWipe.Reveal()
            );
        }


        // ==========================================
        // 12. Reveal Hold
        // ==========================================

        if (revealEndHold > 0f)
        {
            yield return
                new WaitForSecondsRealtime(
                    revealEndHold
                );
        }


        // ==========================================
        // Boss Intro
        // ==========================================

        if (bossBattleManager != null)
        {
            yield return StartCoroutine(
                bossBattleManager
                    .StartBossBattleRoutine()
            );
        }


        // ==========================================
        // Player 조작 허용
        // ==========================================

        UnlockPlayer();


        // ==========================================
        // Boss Combat
        // ==========================================

        if (bossBattleManager != null)
        {
            bossBattleManager
                .BeginCombat();
        }


        transitionRunning =
            false;


        Debug.Log(
            "BOSS ARENA READY"
        );
    }


    // ==================================================
    // Activate Boss Arena
    // ==================================================

    private void ActivateBossArena()
    {
        if (normalArenaRoot != null)
        {
            normalArenaRoot
                .SetActive(
                    false
                );
        }


        if (bossArenaRoot != null)
        {
            bossArenaRoot
                .SetActive(
                    true
                );
        }


        if (InkMap.Instance != null &&
            bossGround != null)
        {
            InkMap.Instance
                .SwitchGroundTilemap(
                    bossGround
                );


            InkMap.Instance
                .ClearAllInk();
        }
        else
        {
            Debug.LogError(
                "Boss Arena: InkMap 또는 BossGround가 없습니다."
            );
        }
    }


    // ==================================================
    // Teleport Player
    // ==================================================

    private void TeleportPlayerToBossSpawn()
    {
        if (playerRigidbody == null)
        {
            Debug.LogError(
                "Boss Arena: Player Rigidbody가 없습니다."
            );


            return;
        }


        if (bossPlayerSpawnPoint == null)
        {
            Debug.LogError(
                "Boss Arena: BossPlayerSpawnPoint가 없습니다."
            );


            return;
        }


        Vector3 beforePosition =
            playerRigidbody
                .transform
                .position;


        Vector3 targetPosition =
            bossPlayerSpawnPoint
                .position;


        playerRigidbody.position =
            new Vector2(
                targetPosition.x,
                targetPosition.y
            );


        playerRigidbody
            .transform
            .position =
            new Vector3(
                targetPosition.x,
                targetPosition.y,
                beforePosition.z
            );


        playerRigidbody.linearVelocity =
            Vector2.zero;


        Physics2D.SyncTransforms();
    }


    // ==================================================
    // Camera Boss Arena
    // ==================================================

    private void SwitchCameraToBossArena()
    {
        if (cameraFollow == null)
        {
            Debug.LogError(
                "Boss Arena: CameraFollow가 연결되지 않았습니다."
            );


            return;
        }


        if (bossCameraBounds == null)
        {
            Debug.LogError(
                "Boss Arena: Boss Camera Bounds가 없습니다."
            );


            return;
        }


        cameraFollow.SwitchBoundsTilemap(
            bossCameraBounds,
            true
        );
    }


    // ==================================================
    // Lock Player
    // ==================================================

    private void LockPlayer()
    {
        // ==========================================
        // Weapon Input
        // ==========================================

        if (playerWeaponInputController != null)
        {
            weaponInputWasEnabled =
                playerWeaponInputController
                    .InputEnabled;


            playerWeaponInputController
                .SetInputEnabled(
                    false
                );
        }


        // ==========================================
        // Rigidbody
        // ==========================================

        if (playerRigidbody != null)
        {
            originalSimulated =
                playerRigidbody.simulated;


            playerRigidbody.linearVelocity =
                Vector2.zero;


            playerRigidbody.simulated =
                false;
        }


        // ==========================================
        // Movement
        // ==========================================

        if (playerMovement != null)
        {
            movementWasEnabled =
                playerMovement.enabled;


            playerMovement.enabled =
                false;
        }


        // ==========================================
        // Dive
        // ==========================================

        if (playerDive != null)
        {
            diveWasEnabled =
                playerDive.enabled;


            playerDive.enabled =
                false;
        }
    }


    // ==================================================
    // Unlock Player
    // ==================================================

    private void UnlockPlayer()
    {
        // ==========================================
        // Rigidbody
        // ==========================================

        if (playerRigidbody != null)
        {
            playerRigidbody.simulated =
                originalSimulated;


            playerRigidbody.linearVelocity =
                Vector2.zero;
        }


        // ==========================================
        // Movement
        // ==========================================

        if (playerMovement != null)
        {
            playerMovement.enabled =
                movementWasEnabled;
        }


        // ==========================================
        // Dive
        // ==========================================

        if (playerDive != null)
        {
            playerDive.enabled =
                diveWasEnabled;
        }


        // ==========================================
        // Weapon Input
        // ==========================================

        if (playerWeaponInputController != null)
        {
            playerWeaponInputController
                .SetInputEnabled(
                    weaponInputWasEnabled
                );
        }


        Physics2D.SyncTransforms();
    }


    // ==================================================
    // Projectile Cleanup
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
}