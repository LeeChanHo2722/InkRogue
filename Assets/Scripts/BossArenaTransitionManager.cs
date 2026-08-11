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

    public float floorClearHold = 0.50f;

    public float revealEndHold = 0.15f;


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

    public PlayerShoot playerShoot;

    public PlayerDive playerDive;

    public PlayerSubWeapon playerSubWeapon;

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

    private bool shootWasEnabled;

    private bool diveWasEnabled;

    private bool subWeaponWasEnabled;


    public bool IsTransitionRunning =>
        transitionRunning;


    // ==================================================
    // Start
    // ==================================================

    private void Start()
    {
        if (normalArenaRoot != null)
        {
            normalArenaRoot
                .SetActive(true);
        }


        if (bossArenaRoot != null)
        {
            bossArenaRoot
                .SetActive(false);
        }
    }


    // ==================================================
    // Begin
    // ==================================================

    public void BeginBossTransition()
    {
        if (transitionRunning)
            return;


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
        // 9. Camera도 BossGround로 전환
        //
        // 화면이 아직 Wipe로 덮여 있으므로
        // 순간이동이 보이지 않음
        // ==========================================

        SwitchCameraToBossArena();


        // ==========================================
        // 10. Physics / Tilemap 갱신
        // ==========================================

        yield return null;


        Physics2D.SyncTransforms();


        // ==========================================
        // 11. Boss Arena Reveal
        // ==========================================

        if (screenWipe != null)
        {
            yield return StartCoroutine(
                screenWipe.Reveal()
            );
        }


        // ==========================================
        // 12. Reveal 후 잠깐 여유
        // ==========================================

        if (revealEndHold > 0f)
        {
            yield return
                new WaitForSecondsRealtime(
                    revealEndHold
                );
        }


        // ==========================================
        // Boss Intro + Spawn
        //
        // 이 동안 Player는 계속 잠금 상태
        // ==========================================

        if (bossBattleManager != null)
        {
            yield return StartCoroutine(
                bossBattleManager
                    .StartBossBattleRoutine()
            );
        }


        // ==========================================
        // Player 먼저 조작 허용
        // ==========================================

        UnlockPlayer();


        // ==========================================
        // 그 직후 실제 Boss Combat 시작
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
                .SetActive(false);
        }


        if (bossArenaRoot != null)
        {
            bossArenaRoot
                .SetActive(true);
        }


        // ==========================================
        // InkMap
        // Ground → BossGround
        // ==========================================

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


        Debug.Log(
            "PLAYER TELEPORT"
            + " | Before: "
            + beforePosition
            + " | Target: "
            + targetPosition
        );


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


        Debug.Log(
            "PLAYER TELEPORT COMPLETE"
            + " | After: "
            + playerRigidbody
                .transform
                .position
        );
    }


    // ==================================================
    // Camera Boss Arena 전환
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
        if (playerRigidbody != null)
        {
            originalSimulated =
                playerRigidbody.simulated;


            playerRigidbody.linearVelocity =
                Vector2.zero;


            playerRigidbody.simulated =
                false;
        }


        if (playerMovement != null)
        {
            movementWasEnabled =
                playerMovement.enabled;


            playerMovement.enabled =
                false;
        }


        if (playerShoot != null)
        {
            shootWasEnabled =
                playerShoot.enabled;


            playerShoot.enabled =
                false;
        }


        if (playerDive != null)
        {
            diveWasEnabled =
                playerDive.enabled;


            playerDive.enabled =
                false;
        }


        if (playerSubWeapon != null)
        {
            subWeaponWasEnabled =
                playerSubWeapon.enabled;


            playerSubWeapon.enabled =
                false;
        }
    }


    // ==================================================
    // Unlock Player
    // ==================================================

    private void UnlockPlayer()
    {
        if (playerRigidbody != null)
        {
            playerRigidbody.simulated =
                originalSimulated;


            playerRigidbody.linearVelocity =
                Vector2.zero;
        }


        if (playerMovement != null)
        {
            playerMovement.enabled =
                movementWasEnabled;
        }


        if (playerShoot != null)
        {
            playerShoot.enabled =
                shootWasEnabled;
        }


        if (playerDive != null)
        {
            playerDive.enabled =
                diveWasEnabled;
        }


        if (playerSubWeapon != null)
        {
            playerSubWeapon.enabled =
                subWeaponWasEnabled;
        }


        Physics2D.SyncTransforms();
    }


    // ==================================================
    // Projectile Cleanup
    // ==================================================

    private void ClearFloorCombatObjects()
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
                .SetActive(false);


            Destroy(
                cleanupObject.gameObject
            );
        }
    }
}