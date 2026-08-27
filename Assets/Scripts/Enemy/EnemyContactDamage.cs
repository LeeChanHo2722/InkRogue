using UnityEngine;

public class EnemyContactDamage : MonoBehaviour
{
    // ==================================================
    // Damage
    // ==================================================

    [Header("Damage")]
    public int damage = 1;

    public float damageInterval = 1f;


    [Header("Knockback")]

    [Min(0f)]
    public float knockbackForce = 4f;


    // ==================================================
    // Attack Window
    // ==================================================

    [Header("Attack Window")]

    [Tooltip(
        "켜면 일반 접촉으로는 피해를 주지 않고 "
        + "공격 Window 중에만 피해를 줍니다."
    )]
    public bool requiresAttackWindow = false;


    // ==================================================
    // Runtime
    // ==================================================

    private float nextDamageTime = 0f;


    private bool attackWindowActive =
        false;


    private bool damagedThisWindow =
        false;


    // ==================================================
    // Public
    // ==================================================

    public void BeginAttackWindow()
    {
        attackWindowActive =
            true;


        damagedThisWindow =
            false;
    }


    public void EndAttackWindow()
    {
        attackWindowActive =
            false;


        damagedThisWindow =
            false;
    }


    // ==================================================
    // Collision
    // ==================================================

    private void OnCollisionStay2D(
        Collision2D collision)
    {
        if (!collision.gameObject
            .CompareTag("Player"))
        {
            return;
        }


        // ==========================================
        // Chaser 방식
        // ==========================================

        if (requiresAttackWindow)
        {
            if (!attackWindowActive)
                return;


            if (damagedThisWindow)
                return;
        }

        // ==========================================
        // Tank 등 기존 방식
        // ==========================================

        else
        {
            if (Time.time <
                nextDamageTime)
            {
                return;
            }
        }


        IEncounterDamageTarget damageTarget =
            collision.gameObject
                .GetComponent<IEncounterDamageTarget>();


        if (damageTarget == null)
            return;


        damageTarget.TakeDamage(
            damage,
            transform.position
        );


        KnockbackUtility.TryApply(
            collision.collider,
            transform.position,
            knockbackForce
        );


        // ==========================================
        // Chaser:
        // Dash 1회당 딱 한 번
        // ==========================================

        if (requiresAttackWindow)
        {
            damagedThisWindow =
                true;
        }

        // ==========================================
        // 기존 적:
        // DamageInterval 사용
        // ==========================================

        else
        {
            nextDamageTime =
                Time.time
                + damageInterval;
        }
    }


    // ==================================================
    // Safety
    // ==================================================

    private void OnDisable()
    {
        attackWindowActive =
            false;


        damagedThisWindow =
            false;
    }
}