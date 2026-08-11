using UnityEngine;

public class EnemyBomberAttack : MonoBehaviour
{
    // ==================================================
    // References
    // ==================================================

    [Header("References")]

    public GameObject bombPrefab;

    public Transform firePoint;

    public EnemyBomberMovement movement;

    public EnemyBomberTelegraph telegraph;


    // ==================================================
    // Attack
    // ==================================================

    [Header("Attack")]

    public float attackRange = 7f;

    [Tooltip("폭탄 하나를 던진 후 다음 공격까지 시간")]
    public float attackCooldown = 2.4f;


    // ==================================================
    // Windup
    // ==================================================

    [Header("Windup")]

    public float windupDuration = 0.85f;

    [Tooltip(
        "마지막 이 시간 동안 착탄 위치가 고정됩니다."
    )]
    public float aimLockDuration = 0.25f;

    public float windupMoveMultiplier = 0.35f;


    // ==================================================
    // Bomb
    // ==================================================

    [Header("Bomb")]

    public float flightDuration = 0.55f;

    public float damage = 2f;

    public float damageRadius = 1.35f;

    public float inkRadius = 2.15f;

    public int inkSplatCount = 30;


    // ==================================================
    // Runtime
    // ==================================================

    private Transform player;

    private EnemySpawnVisual spawnVisual;

    private float cooldownTimer = 0f;

    private bool windingUp = false;

    private float windupTimer = 0f;

    private bool aimLocked = false;

    private Vector2 targetPosition;


    // ==================================================
    // Awake
    // ==================================================

    private void Awake()
    {
        if (movement == null)
        {
            movement =
                GetComponent<EnemyBomberMovement>();
        }


        if (telegraph == null)
        {
            telegraph =
                GetComponent<EnemyBomberTelegraph>();
        }


        spawnVisual =
            GetComponent<EnemySpawnVisual>();
    }


    // ==================================================
    // Start
    // ==================================================

    private void Start()
    {
        GameObject playerObject =
            GameObject.FindGameObjectWithTag(
                "Player"
            );


        if (playerObject != null)
        {
            player =
                playerObject.transform;
        }


        cooldownTimer =
            Random.Range(
                0.4f,
                1.0f
            );
    }


    // ==================================================
    // Update
    // ==================================================

    private void Update()
    {
        if (player == null)
            return;


        // ==========================================
        // Spawn 완료 전에는 절대 공격하지 않음
        // ==========================================

        if (spawnVisual != null &&
            !spawnVisual.IsSpawnFinished)
        {
            return;
        }


        if (windingUp)
        {
            UpdateWindup();

            return;
        }


        cooldownTimer -=
            Time.deltaTime;


        if (cooldownTimer > 0f)
            return;


        float distance =
            Vector2.Distance(
                transform.position,
                player.position
            );


        if (distance <=
            attackRange)
        {
            BeginWindup();
        }
    }


    // ==================================================
    // Begin Windup
    // ==================================================

    private void BeginWindup()
    {
        windingUp =
            true;


        windupTimer =
            0f;


        aimLocked =
            false;


        targetPosition =
            player.position;


        if (movement != null)
        {
            movement
                .SetAttackSpeedMultiplier(
                    windupMoveMultiplier
                );
        }
    }


    // ==================================================
    // Windup
    // ==================================================

    private void UpdateWindup()
    {
        windupTimer +=
            Time.deltaTime;


        float lockStartTime =
            Mathf.Max(
                0f,
                windupDuration
                - aimLockDuration
            );


        // ==========================================
        // Lock 전에는 Player를 계속 추적
        // ==========================================

        if (!aimLocked &&
            windupTimer <
            lockStartTime)
        {
            targetPosition =
                player.position;
        }


        // ==========================================
        // Lock
        // ==========================================

        if (!aimLocked &&
            windupTimer >=
            lockStartTime)
        {
            aimLocked =
                true;


            targetPosition =
                player.position;
        }


        // ==========================================
        // Telegraph
        // ==========================================

        float progress =
            Mathf.Clamp01(
                windupTimer
                / Mathf.Max(
                    windupDuration,
                    0.01f
                )
            );


        if (telegraph != null)
        {
            telegraph.Show(
                targetPosition,
                damageRadius,
                progress,
                aimLocked
            );
        }


        // ==========================================
        // Throw
        // ==========================================

        if (windupTimer >=
            windupDuration)
        {
            ThrowBomb();
        }
    }


    // ==================================================
    // Throw
    // ==================================================

    private void ThrowBomb()
    {
        windingUp =
            false;


        if (telegraph != null)
        {
            telegraph.Hide();
        }


        if (movement != null)
        {
            movement
                .SetAttackSpeedMultiplier(
                    1f
                );
        }


        Vector2 startPosition =
            firePoint != null
                ? firePoint.position
                : transform.position;


        if (bombPrefab != null)
        {
            GameObject bombObject =
                Instantiate(
                    bombPrefab,
                    startPosition,
                    Quaternion.identity
                );


            EnemyBomb bomb =
                bombObject
                    .GetComponent<EnemyBomb>();


            if (bomb != null)
            {
                bomb.Initialize(
                    startPosition,
                    targetPosition,
                    flightDuration,
                    damage,
                    damageRadius,
                    inkRadius,
                    inkSplatCount
                );
            }
        }


        cooldownTimer =
            attackCooldown;
    }


    // ==================================================
    // Disable
    // ==================================================

    private void OnDisable()
    {
        windingUp =
            false;


        if (telegraph != null)
        {
            telegraph.Hide();
        }


        if (movement != null)
        {
            movement
                .SetAttackSpeedMultiplier(
                    1f
                );
        }
    }
}