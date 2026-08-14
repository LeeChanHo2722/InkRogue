using System;
using UnityEngine;

public class BossHealth : MonoBehaviour
{
    // ==================================================
    // Health
    // ==================================================

    [Header("Health")]

    public int maxHealth = 120;


    // 실제 Runtime HP는 소수점까지 저장
    [SerializeField]
    private float currentHealth;


    // ==================================================
    // Phase
    // ==================================================

    [Header("Phase")]

    [Range(0.01f, 0.99f)]
    public float phase2Threshold = 0.65f;


    [Range(0.01f, 0.99f)]
    public float phase3Threshold = 0.30f;


    // ==================================================
    // Runtime
    // ==================================================

    private int currentPhase = 1;

    private bool isDead = false;

    private bool isInvulnerable = false;


    // ==================================================
    // Events
    //
    // 기존 UI 호환을 위해 int 유지
    // ==================================================

    public event Action<int, int>
        HealthChanged;


    public event Action<int>
        PhaseChanged;


    public event Action
        BossHit;


    public event Action
        BossDied;


    // ==================================================
    // Public
    // ==================================================

    // 기존 코드 호환용
    public int CurrentHealth =>
        Mathf.CeilToInt(
            currentHealth
        );


    // 소수점까지 필요한 시스템용
    public float CurrentHealthExact =>
        currentHealth;


    public int MaxHealth =>
        maxHealth;


    public int CurrentPhase =>
        currentPhase;


    public bool IsDead =>
        isDead;


    public bool IsInvulnerable =>
        isInvulnerable;


    public float CurrentHealthPercent =>
        maxHealth <= 0
            ? 0f
            : Mathf.Clamp01(
                currentHealth
                / maxHealth
            );


    // ==================================================
    // Awake
    // ==================================================

    private void Awake()
    {
        currentHealth =
            maxHealth;


        currentPhase =
            1;


        isDead =
            false;
    }


    // ==================================================
    // Damage
    //
    // int → float
    // ==================================================

    public void TakeDamage(
        float damage
    )
    {
        if (isDead)
        {
            return;
        }


        if (isInvulnerable)
        {
            return;
        }


        if (damage <= 0f)
        {
            return;
        }


        currentHealth -=
            damage;


        currentHealth =
            Mathf.Max(
                0f,
                currentHealth
            );


        BossHit?.Invoke();


        // 기존 UI에는 올림된 정수 HP 전달
        HealthChanged?.Invoke(
            CurrentHealth,
            maxHealth
        );


        // ==========================================
        // Death
        // ==========================================

        if (currentHealth <= 0f)
        {
            Die();

            return;
        }


        // ==========================================
        // Phase
        // ==========================================

        CheckPhase();
    }


    // ==================================================
    // Phase Check
    // ==================================================

    private void CheckPhase()
    {
        float percent =
            CurrentHealthPercent;


        // ==========================================
        // Phase 3
        // ==========================================

        if (currentPhase < 3 &&
            percent <= phase3Threshold)
        {
            currentPhase =
                3;


            PhaseChanged?.Invoke(
                currentPhase
            );


            Debug.Log(
                "BOSS PHASE 3"
            );


            return;
        }


        // ==========================================
        // Phase 2
        // ==========================================

        if (currentPhase < 2 &&
            percent <= phase2Threshold)
        {
            currentPhase =
                2;


            PhaseChanged?.Invoke(
                currentPhase
            );


            Debug.Log(
                "BOSS PHASE 2"
            );
        }
    }


    // ==================================================
    // Invulnerability
    // ==================================================

    public void SetInvulnerable(
        bool value
    )
    {
        isInvulnerable =
            value;
    }


    // ==================================================
    // Death
    // ==================================================

    private void Die()
    {
        if (isDead)
        {
            return;
        }


        isDead =
            true;


        currentHealth =
            0f;


        isInvulnerable =
            true;


        HealthChanged?.Invoke(
            0,
            maxHealth
        );


        BossDied?.Invoke();


        Debug.Log(
            "BOSS DEFEATED"
        );
    }
}