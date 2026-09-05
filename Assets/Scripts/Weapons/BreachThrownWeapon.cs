using System.Collections.Generic;
using UnityEngine;

// Thrown Breach weapon. Flies at a fixed speed, pierces every enemy it
// passes through, and stops only on a wall or at max range - where it
// embeds and stays in the world. Steps with a swept cast so a fast throw
// cannot tunnel past an enemy or through a wall.
public class BreachThrownWeapon : MonoBehaviour
{
    // ==================================================
    // Flight
    // ==================================================

    [Header("Flight")]

    [Min(0.1f)]
    [SerializeField]
    private float speed = 22f;


    [Min(0.1f)]
    [SerializeField]
    private float maxThrowDistance = 9f;


    [Tooltip("Sweep radius. Roughly the weapon's own half width.")]
    [Min(0.01f)]
    [SerializeField]
    private float sweepRadius = 0.25f;


    // ==================================================
    // Damage
    // ==================================================

    [Header("Damage")]

    [Min(0f)]
    [SerializeField]
    private float damage = 4f;


    [Tooltip("Pushes along the throw direction: this is about clearing "
        + "a path, not about scattering enemies.")]
    [Min(0f)]
    [SerializeField]
    private float knockbackForce = 8f;


    // ==================================================
    // Layers
    // ==================================================

    [Header("Layers")]

    [Tooltip("Everything the sweep should consider. Enemies and walls.")]
    [SerializeField]
    private LayerMask sweepMask = ~0;


    [Tooltip("Layers that stop the throw and embed it.")]
    [SerializeField]
    private LayerMask obstacleLayer;


    // ==================================================
    // Runtime
    // ==================================================

    private readonly HashSet<Object> damagedTargets =
        new HashSet<Object>();

    private readonly List<RaycastHit2D> sweepHits =
        new List<RaycastHit2D>();

    private Vector2 throwDirection = Vector2.right;

    private float travelled;

    private float damageMultiplier = 1f;

    private bool launched;

    private bool embedded;

    private bool wallEmbed;

    private Vector2 embedNormal;


    // ==================================================
    // Public State
    //
    // Read by the future Dash step: where it landed, and whether it is
    // stuck in a wall or planted at max range.
    // ==================================================

    public bool IsEmbedded => embedded;

    // The authoritative wall mask for this weapon, reused by the Dash line
    // of sight check so there is only one place to wire it.
    public LayerMask ObstacleMask => obstacleLayer;

    public bool IsWallEmbed => wallEmbed;

    public Vector2 EmbedNormal => embedNormal;

    public Vector2 EmbedPosition => transform.position;

    public Vector2 ThrowDirection => throwDirection;


    // ==================================================
    // Launch
    // ==================================================

    public void Launch(
        Vector2 direction,
        float slotDamageMultiplier)
    {
        if (direction.sqrMagnitude < 0.0001f)
        {
            direction = Vector2.right;
        }


        throwDirection =
            direction.normalized;


        damageMultiplier =
            slotDamageMultiplier;


        travelled = 0f;
        launched = true;
        embedded = false;
        wallEmbed = false;
        embedNormal = Vector2.zero;


        transform.rotation =
            Quaternion.Euler(
                0f,
                0f,
                Mathf.Atan2(
                    throwDirection.y,
                    throwDirection.x
                )
                *
                Mathf.Rad2Deg
            );
    }


    // ==================================================
    // Flight Step
    // ==================================================

    private void FixedUpdate()
    {
        if (!launched || embedded)
        {
            return;
        }


        float step =
            speed * Time.fixedDeltaTime;


        float remaining =
            maxThrowDistance - travelled;


        bool reachedMaxRange = false;


        if (step >= remaining)
        {
            step = Mathf.Max(0f, remaining);
            reachedMaxRange = true;
        }


        Vector2 origin =
            transform.position;


        // Resolve everything crossed this step in distance order, so a
        // wall always stops the throw before anything behind it is hit.
        if (step > 0f &&
            SweepStep(origin, step))
        {
            return;
        }


        transform.position =
            origin + throwDirection * step;


        travelled += step;


        if (reachedMaxRange)
        {
            Embed(
                transform.position,
                false,
                Vector2.zero
            );
        }
    }


    // Returns true when the throw embedded during this step.
    private bool SweepStep(
        Vector2 origin,
        float step)
    {
        sweepHits.Clear();


        RaycastHit2D[] hits =
            Physics2D.CircleCastAll(
                origin,
                sweepRadius,
                throwDirection,
                step,
                sweepMask
            );


        sweepHits.AddRange(hits);


        sweepHits.Sort(
            (a, b) =>
                a.distance.CompareTo(b.distance)
        );


        for (int i = 0; i < sweepHits.Count; i++)
        {
            RaycastHit2D hit = sweepHits[i];


            if (hit.collider == null)
            {
                continue;
            }


            if (IsObstacle(hit.collider))
            {
                Vector2 embedPoint =
                    hit.point
                    - throwDirection * sweepRadius;


                travelled += hit.distance;


                Embed(
                    embedPoint,
                    true,
                    hit.normal
                );


                return true;
            }


            TryDamage(hit.collider);
        }


        return false;
    }


    private bool IsObstacle(
        Collider2D collider)
    {
        return (obstacleLayer.value
            & (1 << collider.gameObject.layer)) != 0;
    }


    // ==================================================
    // Damage
    //
    // Deduped on the health component, so an enemy with several colliders
    // still only takes one hit per throw.
    // ==================================================

    private void TryDamage(
        Collider2D collider)
    {
        BossHealth boss =
            collider.GetComponentInParent<BossHealth>();


        if (boss != null)
        {
            if (damagedTargets.Add(boss))
            {
                boss.TakeDamage(
                    damage * damageMultiplier
                );
            }


            return;
        }


        EnemyHealth enemy =
            collider.GetComponent<EnemyHealth>();


        if (enemy == null)
        {
            enemy =
                collider.GetComponentInParent<EnemyHealth>();
        }


        if (enemy == null)
        {
            return;
        }


        if (!damagedTargets.Add(enemy))
        {
            return;
        }


        enemy.TakeDamage(
            damage * damageMultiplier
        );


        // Pushed along the throw, using the shared knockback contract so
        // any receiver added later works without touching this weapon.
        KnockbackUtility.TryApply(
            collider,
            (Vector2)transform.position
                - throwDirection,
            knockbackForce
        );
    }


    // ==================================================
    // Embed
    // ==================================================

    private void Embed(
        Vector2 position,
        bool isWall,
        Vector2 normal)
    {
        embedded = true;
        launched = false;
        wallEmbed = isWall;
        embedNormal = normal;


        transform.position = position;
    }
}
