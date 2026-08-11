using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    // ==================================================
    // Health
    // ==================================================

    [Header("Health")]

    public int maxHealth = 12;


    private int currentHealth;


    public int CurrentHealth =>
        currentHealth;


    public int MaxHealth =>
        maxHealth;


    public float CurrentHealthPercent =>
        maxHealth <= 0
            ? 0f
            : Mathf.Clamp01(
                (float)currentHealth
                / maxHealth
            );


    // ==================================================
    // References
    // ==================================================

    private EnemyVisualFeedback visualFeedback;

    private EnemyInkTrail inkTrail;


    // ==================================================
    // State
    // ==================================================

    private bool isDead = false;


    // ==================================================
    // Awake
    // ==================================================

    private void Awake()
    {
        currentHealth =
            maxHealth;


        visualFeedback =
            GetComponent<EnemyVisualFeedback>();


        // 길을 만드는 적만 존재.
        // Shooter / Bomber / Sprinkler에는
        // 없어도 문제 없음.
        inkTrail =
            GetComponent<EnemyInkTrail>();
    }


    // ==================================================
    // Damage
    // ==================================================

    public void TakeDamage(
        int damage)
    {
        if (isDead)
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


        // ==========================================
        // 아직 살아있음
        // ==========================================

        if (currentHealth > 0)
        {
            inkTrail?
                .OnHitByPlayerInk();


            visualFeedback?
                .PlayHit();


            return;
        }


        // ==========================================
        // HP 0
        // ==========================================

        Die();
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

        if (GameAudioManager.Instance != null)
        {
            GameAudioManager.Instance
                .PlayEnemyDeath();
        }


        visualFeedback?
            .PlayDeath();


        // ==========================================
        // 자신이 어느 Wave에서 Spawn됐는지 보고
        // ==========================================

        EnemyWaveMember waveMember =
            GetComponent<EnemyWaveMember>();


        if (waveMember != null)
        {
            waveMember.ReportDeath();
        }


        Destroy(
            gameObject
        );
    }
}