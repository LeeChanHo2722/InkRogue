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

    public InkScreenWipe screenWipe;

    public FloorStartTitleUI floorStartTitleUI;


    // ==================================================
    // Player
    // ==================================================

    [Header("Player")]

    public Rigidbody2D playerRigidbody;

    public PlayerMovement playerMovement;

    public PlayerShoot playerShoot;

    public PlayerDive playerDive;

    public PlayerSubWeapon playerSubWeapon;

    public PlayerFloorSpawnVisual playerSpawnVisual;

    public PlayerShield playerShield;

    public PlayerInkResource playerInkResource;


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


    // ==================================================
    // Spawn Position
    // ==================================================

    [Header("Floor Spawn Position")]

    [Tooltip(
        "각 Floor 시작 시 Player가 이동할 위치"
    )]
    public Transform playerSpawnPoint;


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
        "Floor Clear 문구 유지 시간"
    )]
    public float floorClearHoldDuration =
        0.45f;


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


    // ==================================================
    // Awake
    // ==================================================

    private void Awake()
    {
        if (playerRigidbody != null)
        {
            originalPlayerSimulated =
                playerRigidbody.simulated;


            // ==========================================
            // 연결 안 했어도 Player에서 자동 검색
            // ==========================================

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
        }
    }


    // ==================================================
    // Start
    // ==================================================

    private void Start()
    {
        // 이전 Play 종료 등의 이유로
        // TimeScale이 비정상 값에 남는 것 방지
        RestoreTimeScale();


        StartCoroutine(
            StartFirstFloorRoutine()
        );
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


        // ==========================================
        // 처음 파란 화면 잠깐 유지
        // ==========================================

        yield return
            new WaitForSecondsRealtime(
                gameStartHoldDuration
            );


        // ==========================================
        // 화면이 가려져 있을 때
        // Floor 준비
        // ==========================================

        yield return StartCoroutine(
            PrepareFloorStartRoutine()
        );


        // ==========================================
        // 맵 공개
        // ==========================================

        if (screenWipe != null)
        {
            yield return StartCoroutine(
                screenWipe.Reveal()
            );
        }


        // ==========================================
        // Wipe 이후 Player 등장 전 텀
        // ==========================================

        if (playerSpawnDelay > 0f)
        {
            yield return
                new WaitForSecondsRealtime(
                    playerSpawnDelay
                );
        }


        // ==========================================
        // Player Spawn VFX
        // ==========================================

        if (playerSpawnVisual != null)
        {
            yield return StartCoroutine(
                playerSpawnVisual.PlaySpawn()
            );
        }


        // ==========================================
        // Floor Title
        // ==========================================

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


        // ==========================================
        // Enemy Spawn 전 여유
        // ==========================================

        if (enemySpawnDelay > 0f)
        {
            yield return
                new WaitForSecondsRealtime(
                    enemySpawnDelay
                );
        }


        // ==========================================
        // Enemy Spawn
        // ==========================================

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


        // ==========================================
        // Floor Clear 순간
        // 기존 Bullet / Bomb 즉시 제거
        // ==========================================

        ClearFloorCombatObjects();


        // ==========================================
        // Slow Motion 시작
        //
        // 마지막 적이 죽는 순간부터
        // 바로 느려진다.
        // ==========================================

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


        // ==========================================
        // Kill Moment
        //
        // 아주 짧게 마지막 적 사망을 보여줌
        // ==========================================

        if (floorClearSlowLead > 0f)
        {
            yield return
                new WaitForSecondsRealtime(
                    floorClearSlowLead
                );
        }


        // ==========================================
        // Floor Clear Text 유지
        //
        // Realtime을 사용하므로
        // Inspector의 0.45는 실제 0.45초
        // ==========================================

        if (floorClearHoldDuration > 0f)
        {
            yield return
                new WaitForSecondsRealtime(
                    floorClearHoldDuration
                );
        }


        // ==========================================
        // Slow 상태 그대로 Wipe
        // ==========================================

        if (screenWipe != null)
        {
            yield return StartCoroutine(
                screenWipe.Cover()
            );
        }


        // ==========================================
        // 화면이 완전히 덮인 뒤
        // 정상 속도 복구
        // ==========================================

        RestoreTimeScale();


        // ==========================================
        // Floor Clear Text 제거
        // ==========================================

        if (floorManager != null)
        {
            floorManager
                .HideFloorClearText();
        }


        // ==========================================
        // Upgrade 준비
        // ==========================================

        if (upgradeManager != null)
        {
            upgradeManager
                .ShowUpgrades();
        }


        // Upgrade 중 게임 정지
        Time.timeScale =
            0f;


        // ==========================================
        // Upgrade 화면 공개
        // ==========================================

        if (screenWipe != null)
        {
            yield return StartCoroutine(
                screenWipe.Reveal()
            );
        }


        // ==========================================
        // Upgrade Title + Card Animation
        // ==========================================

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
            return;


        if (transitionRunning)
            return;


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


        // ==========================================
        // Upgrade 화면 가리기
        // ==========================================

        if (screenWipe != null)
        {
            yield return StartCoroutine(
                screenWipe.Cover()
            );
        }


        // ==========================================
        // Upgrade UI 제거
        // ==========================================

        if (upgradeManager != null)
        {
            upgradeManager
                .HideUpgrades();
        }


        // ==========================================
        // 정상속도 복구
        // ==========================================

        RestoreTimeScale();


        // ==========================================
        // 다음 Floor
        // ==========================================

        if (floorManager != null)
        {
            floorManager
                .AdvanceFloor();
        }


        // ==========================================
        // Ink Reset
        // Player 이동
        // Player 숨김
        // ==========================================

        yield return StartCoroutine(
            PrepareFloorStartRoutine()
        );


        // ==========================================
        // 새 Floor 공개
        // ==========================================

        if (screenWipe != null)
        {
            yield return StartCoroutine(
                screenWipe.Reveal()
            );
        }


        // ==========================================
        // Player 등장 전 텀
        // ==========================================

        if (playerSpawnDelay > 0f)
        {
            yield return
                new WaitForSecondsRealtime(
                    playerSpawnDelay
                );
        }


        // ==========================================
        // Player Spawn VFX
        // ==========================================

        if (playerSpawnVisual != null)
        {
            yield return StartCoroutine(
                playerSpawnVisual
                    .PlaySpawn()
            );
        }


        // ==========================================
        // Floor Start Title
        // ==========================================

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


        // ==========================================
        // Enemy Spawn 전 여유
        // ==========================================

        if (enemySpawnDelay > 0f)
        {
            yield return
                new WaitForSecondsRealtime(
                    enemySpawnDelay
                );
        }


        // ==========================================
        // Enemy Spawn
        // ==========================================

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
    // Prepare Floor
    // ==================================================

    private IEnumerator PrepareFloorStartRoutine()
    {
        // ==========================================
        // InkMap 준비 기다리기
        // ==========================================

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


        // ==========================================
        // 남아있는 전투 Object 최종 정리
        // ==========================================

        ClearFloorCombatObjects();


        // ==========================================
        // Ink 초기화
        // ==========================================

        if (InkMap.Instance != null &&
            InkMap.Instance.IsReady)
        {
            InkMap.Instance
                .ClearAllInk();
        }


        // ==========================================
        // Player Ink Reset
        // ==========================================

        if (playerInkResource != null)
        {
            playerInkResource
                .FillInk();
        }


        // ==========================================
        // Shield Reset
        // ==========================================

        if (playerShield != null)
        {
            playerShield
                .ResetForNewFloor();
        }


        // ==========================================
        // Player Spawn Point 이동
        // ==========================================

        TeleportPlayerToSpawnPoint();


        // ==========================================
        // Player 숨김
        // ==========================================

        if (playerSpawnVisual != null)
        {
            playerSpawnVisual
                .PrepareHidden();
        }


        // Physics / Transform 반영
        yield return null;
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
                FindObjectsSortMode.None
            );


        foreach (
            FloorCleanupObject cleanupObject
            in cleanupObjects)
        {
            if (cleanupObject == null)
                continue;


            // Destroy는 Frame 끝에 처리될 수 있으므로
            // 먼저 비활성화해서 충돌 / Ink 생성 차단
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
                "FloorTransitionManager: "
                + "Player Spawn Point가 연결되지 않았습니다."
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

    private void LockPlayer()
    {
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


        if (playerShoot != null)
        {
            playerShoot.enabled =
                false;
        }


        if (playerDive != null)
        {
            playerDive.enabled =
                false;
        }


        if (playerSubWeapon != null)
        {
            playerSubWeapon.enabled =
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


        if (playerShoot != null)
        {
            playerShoot.enabled =
                true;
        }


        if (playerDive != null)
        {
            playerDive.enabled =
                true;
        }


        if (playerSubWeapon != null)
        {
            playerSubWeapon.enabled =
                true;
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
        // Scene 종료 / Script Reload 등에서도
        // 느린 상태가 남지 않도록 복구
        RestoreTimeScale();
    }
}