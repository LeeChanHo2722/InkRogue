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
    // Melee Reach
    // ==================================================

    [Header("Melee Reach")]

    [Tooltip(
        "0 keeps the original collision-only hit. Above 0 the hit is "
        + "decided by distance to the target's collider body instead, so "
        + "a target that drifts or is pushed back still gets hit."
    )]
    [Min(0f)]
    public float meleeReach = 0f;


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


    private Transform primaryTarget;

    private Collider2D primaryTargetCollider;


    private Transform playerBody;

    private Collider2D playerBodyCollider;


    private void Start()
    {
        if (meleeReach <= 0f)
        {
            return;
        }


        GameObject target =
            EncounterTarget.ResolveGameObject();


        if (target != null)
        {
            primaryTarget =
                target.transform;


            primaryTargetCollider =
                ResolveBodyCollider(target);
        }


        GameObject actualPlayer =
            GameObject.FindGameObjectWithTag(
                "Player"
            );


        if (actualPlayer != null)
        {
            playerBody =
                actualPlayer.transform;


            playerBodyCollider =
                ResolveBodyCollider(actualPlayer);
        }
    }


    // Reach-based hit. Runs only when meleeReach is configured, and then it
    // replaces the collision path entirely so nothing is damaged twice.
    private void Update()
    {
        if (meleeReach <= 0f)
        {
            return;
        }


        if (!CanDamageNow())
        {
            return;
        }


        if (TryReachDamage(
                primaryTarget,
                primaryTargetCollider))
        {
            return;
        }


        if (playerBody != null &&
            playerBody != primaryTarget)
        {
            TryReachDamage(
                playerBody,
                playerBodyCollider
            );
        }
    }


    private bool TryReachDamage(
        Transform body,
        Collider2D bodyCollider)
    {
        if (body == null)
            return false;


        Vector2 origin =
            transform.position;


        Vector2 closest =
            bodyCollider != null
                ? bodyCollider.ClosestPoint(origin)
                : (Vector2)body.position;


        if (Vector2.Distance(origin, closest) >
            meleeReach)
        {
            return false;
        }


        return ApplyDamageTo(
            body.gameObject,
            bodyCollider
        );
    }


    private bool CanDamageNow()
    {
        if (requiresAttackWindow)
        {
            return attackWindowActive
                && !damagedThisWindow;
        }


        return Time.time >= nextDamageTime;
    }


    private bool ApplyDamageTo(
        GameObject targetObject,
        Component hitComponent)
    {
        IEncounterDamageTarget damageTarget =
            targetObject
                .GetComponent<IEncounterDamageTarget>();


        if (damageTarget == null)
            return false;


        damageTarget.TakeDamage(
            damage,
            transform.position
        );


        KnockbackUtility.TryApply(
            hitComponent != null
                ? hitComponent
                : targetObject.transform,
            transform.position,
            knockbackForce
        );


        if (requiresAttackWindow)
        {
            damagedThisWindow =
                true;
        }
        else
        {
            nextDamageTime =
                Time.time
                + damageInterval;
        }


        return true;
    }


    private static Collider2D ResolveBodyCollider(
        GameObject bodyObject)
    {
        Collider2D bodyCollider =
            bodyObject.GetComponent<Collider2D>();


        if (bodyCollider == null)
        {
            bodyCollider =
                bodyObject
                    .GetComponentInChildren<Collider2D>();
        }


        return bodyCollider;
    }


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
        // Reach mode owns the hit decision; skip so nothing lands twice.
        if (meleeReach > 0f)
        {
            return;
        }


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