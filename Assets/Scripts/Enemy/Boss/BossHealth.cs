using System;
using UnityEngine;

public class BossHealth : MonoBehaviour
{
    // ==================================================
    // Health
    // ==================================================

    [Header("Health")]

    public int maxHealth = 120;


    [SerializeField]
    private int currentHealth;


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

    public int CurrentHealth =>
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
                (float)currentHealth
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
    // ==================================================

    public void TakeDamage(
        int damage)
    {
        if (isDead)
            return;


        if (isInvulnerable)
            return;


        if (damage <= 0)
            return;


        currentHealth -=
            damage;


        currentHealth =
            Mathf.Max(
                0,
                currentHealth
            );


        BossHit?.Invoke();


        HealthChanged?.Invoke(
            currentHealth,
            maxHealth
        );


        // ==========================================
        // Death
        // ==========================================

        if (currentHealth <= 0)
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
        bool value)
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
            return;


        isDead =
            true;


        currentHealth =
            0;


        isInvulnerable =
            true;


        HealthChanged?.Invoke(
            currentHealth,
            maxHealth
        );


        BossDied?.Invoke();


        Debug.Log(
            "BOSS DEFEATED"
        );
    }
}