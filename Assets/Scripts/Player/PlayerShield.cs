using System;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerShield : MonoBehaviour, IEncounterDamageTarget
{
    public Transform TargetTransform => transform;


    [Header("Shield")]
    public float maxShield = 5f;

    [SerializeField]
    private float currentShield;

    [Header("Human Auto Recovery")]
    public float humanRecoveryDelay = 3f;
    public float humanRecoveryPerSecond = 1f;

    [Header("Emergency")]
    public float emergencyDuration = 5f;
    public float emergencyGraceTime = 0.35f;

    [Header("Shield Break Ink")]
    public float breakInkRadius = 2.5f;
    public int breakInkSplatCount = 40;

    [Header("UI")]
    public TMP_Text shieldText;
    public GameObject gameOverPanel;

    public event Action ShieldHit;
    public event Action ShieldBroken;
    public event Action ShieldRestored;
    public event Action PlayerDefeated;
    public event Action<float, float> ShieldChanged;

    // 신규: 공격이 들어온 위치
    public event Action<Vector2> ShieldHitDirectional;

    private bool isEmergency = false;
    private bool isGameOver = false;
    private bool isDefeated = false;

    private float invulnerableUntil = 0f;

    private float emergencyTimer = 0f;
    private float emergencyGraceTimer = 0f;

    private float lastHitTime;

    private PlayerDive playerDive;

    public float CurrentShield => currentShield;
    public float MaxShield => maxShield;
    public bool IsEmergency => isEmergency;
    public float TimeSinceLastHit => Time.time - lastHitTime;

    public float EmergencyTimeRemaining =>
        Mathf.Max(
            0f,
            emergencyDuration - emergencyTimer
        );

    private void Awake()
    {
        currentShield = maxShield;
        lastHitTime = Time.time;
        playerDive = GetComponent<PlayerDive>();
    }

    private void Start()
    {
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }

        NotifyShieldChanged();
        UpdateUI();
    }

    private void Update()
    {
        if (isGameOver)
            return;

        if (isEmergency)
        {
            UpdateEmergency();
            return;
        }

        UpdateHumanRecovery();
    }

    private void UpdateHumanRecovery()
    {
        if (currentShield >= maxShield)
            return;

        if (playerDive != null &&
            playerDive.IsDiving)
        {
            return;
        }

        if (TimeSinceLastHit <
            humanRecoveryDelay)
        {
            return;
        }

        RecoverShield(
            humanRecoveryPerSecond
            * Time.deltaTime
        );
    }

    private void UpdateEmergency()
    {
        emergencyTimer +=
            Time.deltaTime;

        if (emergencyGraceTimer > 0f)
        {
            emergencyGraceTimer -=
                Time.deltaTime;
        }

        if (emergencyTimer >=
            emergencyDuration)
        {
            RestoreShieldAfterEmergency();
        }

        UpdateUI();
    }

    // 기존 호출과 호환
    public void TakeDamage(
        float damage)
    {
        TakeDamageInternal(
            damage,
            transform.position
        );
    }

    // 공격 위치를 받는 신규 버전
    public void TakeDamage(
        float damage,
        Vector2 hitSourcePosition)
    {
        TakeDamageInternal(
            damage,
            hitSourcePosition
        );
    }

    private void TakeDamageInternal(
        float damage,
        Vector2 hitSourcePosition)
    {
        if (damage <= 0f ||
            isGameOver ||
            isDefeated)
        {
            return;
        }

        if (Time.unscaledTime < invulnerableUntil)
        {
            return;
        }

        lastHitTime =
            Time.time;

        if (isEmergency)
        {
            if (emergencyGraceTimer > 0f)
            {
                return;
            }

            ShieldHit?.Invoke();

            ShieldHitDirectional?.Invoke(
                hitSourcePosition
            );

            isDefeated = true;

            PlayerDefeated?.Invoke();

            return;
        }

        currentShield -=
            damage;

        currentShield =
            Mathf.Max(
                0f,
                currentShield
            );

        ShieldHit?.Invoke();

        ShieldHitDirectional?.Invoke(
            hitSourcePosition
        );

        NotifyShieldChanged();

        if (currentShield <= 0f)
        {
            BreakShield();
        }

        UpdateUI();
    }

    public void RecoverShield(
        float amount)
    {
        if (amount <= 0f)
            return;

        if (isEmergency ||
            isGameOver)
        {
            return;
        }

        float previousShield =
            currentShield;

        currentShield +=
            amount;

        currentShield =
            Mathf.Clamp(
                currentShield,
                0f,
                maxShield
            );

        if (!Mathf.Approximately(
                previousShield,
                currentShield))
        {
            NotifyShieldChanged();
        }

        UpdateUI();
    }

    private void BreakShield()
    {
        currentShield =
            0f;

        isEmergency =
            true;

        emergencyTimer =
            0f;

        emergencyGraceTimer =
            emergencyGraceTime;

        NotifyShieldChanged();

        ShieldBroken?.Invoke();

        if (InkMap.Instance != null)
        {
            InkMap.Instance.PaintExplosion(
                transform.position,
                breakInkRadius,
                InkTeam.Player,
                breakInkSplatCount
            );
        }
    }

    private void RestoreShieldAfterEmergency()
    {
        isEmergency =
            false;

        emergencyTimer =
            0f;

        emergencyGraceTimer =
            0f;

        currentShield =
            maxShield;

        NotifyShieldChanged();

        ShieldRestored?.Invoke();

        UpdateUI();
    }

    private void NotifyShieldChanged()
    {
        ShieldChanged?.Invoke(
            currentShield,
            maxShield
        );
    }

    // ==================================================
    // New Floor Reset
    // ==================================================
    public void ResetAfterRespawn(
    float invulnerabilityDuration)
    {
        bool wasEmergency =
            isEmergency;

        currentShield =
            maxShield;

        isEmergency =
            false;

        isDefeated =
            false;

        isGameOver =
            false;

        emergencyTimer =
            0f;

        emergencyGraceTimer =
            0f;

        lastHitTime =
            Time.time;

        invulnerableUntil =
            Time.unscaledTime
            + Mathf.Max(
                0f,
                invulnerabilityDuration
            );

        NotifyShieldChanged();

        if (wasEmergency)
        {
            ShieldRestored?.Invoke();
        }

        UpdateUI();
    }

    public void ResetForNewFloor()
    {
        bool wasEmergency =
            isEmergency;


        // ==========================================
        // Shield 완전 회복
        // ==========================================

        currentShield =
            maxShield;


        // ==========================================
        // Emergency / GameOver 상태 초기화
        // ==========================================

        isEmergency =
            false;


        isGameOver =
            false;

        isDefeated = false;
        invulnerableUntil = 0f;


        emergencyTimer =
            0f;


        emergencyGraceTimer =
            0f;


        // 새 Floor 시작 직후 기준으로
        // 피격 시간도 초기화
        lastHitTime =
            Time.time;


        // ==========================================
        // Game Over UI가 혹시 켜져 있다면 제거
        // ==========================================

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(
                false
            );
        }


        // ==========================================
        // Overlay / UI 등에 현재 Shield 전달
        // ==========================================

        NotifyShieldChanged();


        // Emergency 상태였다면
        // Shield Visual도 정상 상태로 복구
        if (wasEmergency)
        {
            ShieldRestored?.Invoke();
        }


        UpdateUI();
    }

    public void TriggerGameOver()
    {
        if (isGameOver)
            return;

        isGameOver =
            true;

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(
                true
            );
        }

        Time.timeScale =
            0f;
    }

    public void Retry()
    {
        Time.timeScale =
            1f;

        SceneManager.LoadScene(
            SceneManager
                .GetActiveScene()
                .buildIndex
        );
    }

    private void UpdateUI()
    {
        if (shieldText == null)
            return;

        if (isEmergency)
        {
            shieldText.text =
                "SHIELD : BROKEN\n"
                + "EMERGENCY : "
                + EmergencyTimeRemaining
                    .ToString("0.0");
        }
        else
        {
            shieldText.text =
                "SHIELD : "
                + currentShield
                    .ToString("0.0")
                + " / "
                + maxShield
                    .ToString("0.0");
        }
    }
}