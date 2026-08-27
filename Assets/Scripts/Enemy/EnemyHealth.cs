using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    // ==================================================
    // Health
    // ==================================================

    [Header("Health")]

    public int maxHealth = 12;


    // 실제 HP는 소수점까지 저장
    private float currentHealth;


    // ==================================================
    // Compatibility
    //
    // 기존 코드가 CurrentHealth를 int로 사용하고
    // 있을 가능성이 있으므로 유지.
    // ==================================================

    public int CurrentHealth =>
        Mathf.CeilToInt(
            currentHealth
        );


    // 정확한 소수점 HP가 필요할 때 사용
    public float CurrentHealthExact =>
        currentHealth;


    public int MaxHealth =>
        maxHealth;


    public float CurrentHealthPercent =>
        maxHealth <= 0
            ? 0f
            : Mathf.Clamp01(
                currentHealth
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


    private bool floorHealthApplied = false;


    // ==================================================
    // Floor Scaling
    // ==================================================

    // Applied once, on the spawned instance only. maxHealth here is the
    // Instantiate copy, so the Prefab asset value is never touched.
    public void ApplyFloorHealthMultiplier(
        float multiplier)
    {
        if (floorHealthApplied)
        {
            return;
        }


        floorHealthApplied = true;


        if (multiplier <= 0f)
        {
            return;
        }


        maxHealth =
            Mathf.Max(
                1,
                Mathf.RoundToInt(
                    maxHealth * multiplier
                )
            );


        currentHealth =
            maxHealth;
    }


    // ==================================================
    // Awake
    // ==================================================

    private void Awake()
    {
        currentHealth =
            maxHealth;


        visualFeedback =
            GetComponent<
                EnemyVisualFeedback
            >();


        inkTrail =
            GetComponent<
                EnemyInkTrail
            >();
    }


    // ==================================================
    // Damage
    //
    // int → float
    //
    // 기존 int Damage 호출도
    // 자동으로 float으로 변환되므로 그대로 작동.
    // ==================================================

    public void TakeDamage(
        float damage
    )
    {
        if (isDead)
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


        // ==========================================
        // 아직 살아있음
        // ==========================================

        if (currentHealth > 0f)
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

    // Encounter cleanup death: same presentation, but the kill is not
    // credited to the Player.
    public void KillForEncounterCleanup()
    {
        Die(false);
    }


    private void Die(
        bool grantPlayerCredit = true)
    {
        if (isDead)
        {
            return;
        }


        isDead =
            true;


        currentHealth =
            0f;


        if (GameAudioManager.Instance != null)
        {
            GameAudioManager.Instance
                .PlayEnemyDeath();
        }


        visualFeedback?
            .PlayDeath();


        EnemyWaveMember waveMember =
            GetComponent<
                EnemyWaveMember
            >();


        if (waveMember != null)
        {
            waveMember.ReportDeath(
                grantPlayerCredit);
        }


        Destroy(
            gameObject
        );
    }
}