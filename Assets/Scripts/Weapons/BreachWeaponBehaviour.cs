using System.Collections.Generic;
using UnityEngine;

// Breach (working name). Hold to aim, release to throw a weapon that
// pierces enemies and embeds where it stops. Hold time changes nothing -
// it only decides when the throw happens. Dash and Slash arrive later and
// hook onto the deployed weapon this step leaves in the world.
public class BreachWeaponBehaviour
    : PlayerWeaponBehaviour
{
    // ==================================================
    // Slot Runtime
    //
    // Strictly per hand: the same Definition in both hands still tracks
    // two independent thrown weapons.
    // ==================================================

    private class SlotRuntimeState
    {
        public bool isHolding;

        public BreachThrownWeapon deployedWeapon;


        // Taken at throw time. The recovery Slash happens long after the
        // hand may have switched to another weapon, so the slot's damage
        // trait has to travel with the deployed weapon.
        public float damageMultiplier = 1f;
    }


    private readonly SlotRuntimeState
        rightState =
        new SlotRuntimeState();


    private readonly SlotRuntimeState
        leftState =
        new SlotRuntimeState();


    // ==================================================
    // Breach Config
    // ==================================================

    [Header("Breach Config")]

    [Tooltip("Tuning for damage, speed and range lives on this Prefab.")]
    [SerializeField]
    private BreachThrownWeapon thrownWeaponPrefab;


    // ==================================================
    // Dash
    // ==================================================

    [Header("Dash")]

    [Tooltip("Fast enough to read as a charge, slow enough to watch. "
        + "Player move speed is 3.")]
    [Min(1f)]
    [SerializeField]
    private float dashSpeed = 24f;


    [Tooltip("Stops this far off the wall the weapon is stuck in, so the "
        + "Player never ends up inside the wall collider.")]
    [Min(0f)]
    [SerializeField]
    private float dashStopOffset = 0.6f;


    [Min(0.05f)]
    [SerializeField]
    private float dashArrivalDistance = 0.35f;


    [Tooltip("Cast radius of the line of sight check. Narrower than the "
        + "Player so a doorway is not falsely blocked.")]
    [Min(0f)]
    [SerializeField]
    private float dashLineOfSightRadius = 0.25f;


    [Tooltip("Trimmed off the end of the line of sight cast so the wall "
        + "the weapon is embedded in is not read as a blocker.")]
    [Min(0f)]
    [SerializeField]
    private float dashLineOfSightPadding = 0.1f;


    // ==================================================
    // Dash Slash
    // ==================================================

    [Header("Dash Slash")]

    [Tooltip("Half width of the slash corridor. The Player body is 1 x 1, "
        + "so this is deliberately much wider.")]
    [Min(0.05f)]
    [SerializeField]
    private float dashSlashRadius = 1.4f;


    [Tooltip("The weapon's main damage. The throw only opens a path.")]
    [Min(0f)]
    [SerializeField]
    private float dashDamage = 10f;


    [Tooltip("Shoves enemies out sideways, splitting the corridor open.")]
    [Min(0f)]
    [SerializeField]
    private float dashKnockbackForce = 12f;


    [Tooltip("Everything the slash should consider. Filtered by health "
        + "component afterwards, same as the throw.")]
    [SerializeField]
    private LayerMask dashSlashMask = ~0;


    [Tooltip("Leaves Player Ink along the dash path.")]
    [SerializeField]
    private bool paintDashTrail = true;


    // ==================================================
    // Recovery Slash
    // ==================================================

    [Header("Recovery Slash")]

    [Tooltip("How close the Player body has to get to the recovery point. "
        + "Measured from the Player collider surface, not its centre.")]
    [Min(0f)]
    [SerializeField]
    private float recoveryContactRadius = 0.35f;


    [Min(0.05f)]
    [SerializeField]
    private float recoverySlashRadius = 2.2f;


    [Tooltip("The finisher, so it outdamages both the throw and the Dash.")]
    [Min(0f)]
    [SerializeField]
    private float recoverySlashDamage = 14f;


    [Tooltip("Blows everything outward from the slash centre.")]
    [Min(0f)]
    [SerializeField]
    private float recoverySlashKnockbackForce = 16f;


    [Tooltip("Everything the slash should consider. Filtered by health "
        + "component afterwards, same as the throw and the Dash.")]
    [SerializeField]
    private LayerMask recoverySlashMask = ~0;


    [Tooltip("Leaves a swept slash mark. Purely cosmetic: the ink "
        + "footprint is wider than the damage radius and hits nothing.")]
    [SerializeField]
    private bool paintRecoverySlash = true;


    // ==================================================
    // Ink Cost
    // ==================================================

    [Header("Ink Cost")]

    [Tooltip("Percent of Max Ink, charged once per successful throw. "
        + "Holding, flying, hitting and embedding are all free.")]
    [SerializeField]
    [Range(0f, 1f)]
    private float inkUsePerThrowPercent = 0.15f;


    // ==================================================
    // References
    // ==================================================

    [Header("Runtime References")]

    [SerializeField]
    private PlayerInkResource inkResource;


    [SerializeField]
    private PlayerDive playerDive;


    [Tooltip("Death signal. A deployed weapon belongs to the Player who "
        + "threw it, so it does not outlive them.")]
    [SerializeField]
    private PlayerShield playerShield;


    [SerializeField]
    private PlayerMovement playerMovement;


    [SerializeField]
    private Rigidbody2D playerRigidbody;


    [SerializeField]
    private Collider2D playerCollider;


    private Camera mainCamera;


    // ==================================================
    // Dash Runtime
    //
    // The Player has one body, so a Dash is global even though the input
    // that starts it belongs to one hand.
    // ==================================================

    private readonly HashSet<Object> dashHitTargets =
        new HashSet<Object>();

    private readonly HashSet<Object> recoveryHitTargets =
        new HashSet<Object>();

    private SlotRuntimeState dashOwnerState;

    private BreachThrownWeapon dashTargetWeapon;

    private Vector2 dashDirection;

    private Vector2 dashPoint;

    private Vector2 dashPreviousPosition;

    private float dashTimeRemaining;


    // ==================================================
    // State
    // ==================================================

    public override bool IsUsing
    {
        get
        {
            return
                rightState.isHolding
                ||
                leftState.isHolding;
        }
    }


    public bool IsDashing =>
        dashOwnerState != null;


    public override bool IsUsingSlot(
        WeaponSlotSide side
    )
    {
        return
            GetState(side)
                .isHolding;
    }


    // Future Dash hook: null until this hand has a weapon in the world.
    public BreachThrownWeapon GetDeployedWeapon(
        WeaponSlotSide side
    )
    {
        return
            GetState(side)
                .deployedWeapon;
    }


    // ==================================================
    // Awake
    // ==================================================

    private void Awake()
    {
        AutoFindReferences();
    }


    // ==================================================
    // Death
    //
    // A weapon switch keeps a deployed weapon alive, so the cleanup
    // cannot live in CancelUse. Death is a separate signal.
    // ==================================================

    private void OnEnable()
    {
        if (playerShield != null)
        {
            playerShield.PlayerDefeated +=
                HandlePlayerDefeated;
        }
    }


    private void OnDisable()
    {
        if (playerShield != null)
        {
            playerShield.PlayerDefeated -=
                HandlePlayerDefeated;
        }
    }


    // Destroys only what this behaviour threw. Every other Floor object
    // survives, which Defense depends on: a Player death there does not
    // reset the encounter.
    private void HandlePlayerDefeated()
    {
        CancelDash();

        ClearDeployed(rightState);
        ClearDeployed(leftState);
    }


    // ==================================================
    // Press
    // ==================================================

    public override void UsePressed(
        WeaponUseContext context
    )
    {
        SlotRuntimeState state =
            GetState(context.SlotSide);


        ClearDestroyedDeployed(state);


        // One weapon per hand: a press with a weapon already out is a
        // Dash request, never a second throw.
        if (state.deployedWeapon != null)
        {
            state.isHolding = false;


            if (!TryStartDash(state))
            {
                DashBlocked();
            }


            return;
        }


        if (!CanThrow(context))
        {
            state.isHolding = false;

            return;
        }


        state.isHolding = true;
    }


    // ==================================================
    // Held
    //
    // Aim only. Hold length has no effect on the throw.
    // ==================================================

    public override void UseHeld(
        WeaponUseContext context
    )
    {
        SlotRuntimeState state =
            GetState(context.SlotSide);


        if (!state.isHolding)
        {
            return;
        }


        if (!CanThrow(context))
        {
            state.isHolding = false;
        }
    }


    // ==================================================
    // Released
    // ==================================================

    public override void UseReleased(
        WeaponUseContext context
    )
    {
        SlotRuntimeState state =
            GetState(context.SlotSide);


        if (!state.isHolding)
        {
            return;
        }


        state.isHolding = false;


        ClearDestroyedDeployed(state);


        if (state.deployedWeapon != null)
        {
            return;
        }


        TryThrow(context, state);
    }


    // ==================================================
    // Cancel
    //
    // Cancels the hold only. A weapon already in the world belongs to
    // the Floor, not to the equipped weapon.
    // ==================================================

    // Weapon switch, Floor transition and input lock all route here, and
    // a Dash is short enough that stopping it is safer than letting it run
    // on through a lock.
    public override void CancelUse()
    {
        rightState.isHolding = false;
        leftState.isHolding = false;


        CancelDash();
    }


    public override void CancelUse(
        WeaponSlotSide side
    )
    {
        SlotRuntimeState state =
            GetState(side);


        state.isHolding = false;


        if (dashOwnerState == state)
        {
            CancelDash();
        }
    }


    // ==================================================
    // Throw
    // ==================================================

    private void TryThrow(
        WeaponUseContext context,
        SlotRuntimeState state
    )
    {
        if (thrownWeaponPrefab == null ||
            !CanThrow(context))
        {
            return;
        }


        Transform usePoint =
            context.UsePoint;


        if (usePoint == null)
        {
            return;
        }


        float inkCost =
            inkResource.MaxInk
            *
            inkUsePerThrowPercent
            *
            context.InkCostMultiplier;


        // Checked before spawning, so a throw is never created and then
        // billed for ink that was not there.
        if (!inkResource.HasInk(inkCost))
        {
            if (inkResource.IsEmpty)
            {
                context.Controller.SetForcedHand(
                    context.SlotSide,
                    true
                );
            }


            return;
        }


        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }


        if (!WeaponAim.TryGetAimAngle(
                usePoint,
                mainCamera,
                out float aimAngle
            ))
        {
            return;
        }


        Vector2 direction =
            new Vector2(
                Mathf.Cos(aimAngle * Mathf.Deg2Rad),
                Mathf.Sin(aimAngle * Mathf.Deg2Rad)
            );


        BreachThrownWeapon thrown =
            Instantiate(
                thrownWeaponPrefab,
                usePoint.position,
                Quaternion.identity
            );


        thrown.Launch(
            direction,
            context.DamageMultiplier
        );


        state.deployedWeapon = thrown;

        state.damageMultiplier =
            context.DamageMultiplier;


        inkResource.TrySpendInk(inkCost);


        if (inkResource.IsEmpty)
        {
            context.Controller.SetForcedHand(
                context.SlotSide,
                true
            );
        }
    }


    // ==================================================
    // Dash Start
    // ==================================================

    private bool TryStartDash(
        SlotRuntimeState state
    )
    {
        if (IsDashing)
        {
            return false;
        }


        BreachThrownWeapon weapon =
            state.deployedWeapon;


        // Still in flight: there is nothing to dash to yet.
        if (weapon == null ||
            !weapon.IsEmbedded)
        {
            return false;
        }


        if (playerMovement == null ||
            playerRigidbody == null ||
            !playerMovement.enabled)
        {
            return false;
        }


        if (playerDive != null &&
            playerDive.IsSwimForm)
        {
            return false;
        }


        Vector2 target =
            GetRecoveryPoint(weapon);


        Vector2 origin =
            playerRigidbody.position;


        Vector2 toTarget =
            target - origin;


        float distance =
            toTarget.magnitude;


        if (distance <= dashArrivalDistance)
        {
            return false;
        }


        Vector2 direction =
            toTarget / distance;


        if (!HasClearPath(
                origin,
                direction,
                distance,
                weapon
            ))
        {
            return false;
        }


        dashOwnerState = state;
        dashTargetWeapon = weapon;
        dashDirection = direction;
        dashPoint = target;
        dashPreviousPosition = origin;

        // Generous ceiling: only a Dash wedged against geometry uses it.
        dashTimeRemaining =
            distance / dashSpeed * 3f
            + 0.25f;

        dashHitTargets.Clear();


        playerMovement.enabled = false;

        playerRigidbody.linearVelocity =
            dashDirection * dashSpeed;


        return true;
    }


    // The one accessible point next to an embedded weapon: where a Dash
    // lands, and where contact recovery is measured from. Wall Embed stops
    // short along the surface normal; a ground or max range Embed has no
    // wall to keep clear of.
    private Vector2 GetRecoveryPoint(
        BreachThrownWeapon weapon
    )
    {
        if (!weapon.IsWallEmbed)
        {
            return weapon.EmbedPosition;
        }


        return
            weapon.EmbedPosition
            + weapon.EmbedNormal * dashStopOffset;
    }


    // Cast to the dash point rather than to the weapon, and stop just
    // short of it, so the wall the weapon is stuck in never blocks its
    // own Dash.
    private bool HasClearPath(
        Vector2 origin,
        Vector2 direction,
        float distance,
        BreachThrownWeapon weapon
    )
    {
        LayerMask obstacleMask =
            weapon.ObstacleMask;


        if (obstacleMask.value == 0)
        {
            return true;
        }


        float castDistance =
            distance - dashLineOfSightPadding;


        if (castDistance <= 0f)
        {
            return true;
        }


        RaycastHit2D hit =
            Physics2D.CircleCast(
                origin,
                dashLineOfSightRadius,
                direction,
                castDistance,
                obstacleMask
            );


        return hit.collider == null;
    }


    // Single failure hook. The blocked SFX hangs here later.
    private void DashBlocked()
    {
    }


    // ==================================================
    // Dash Step
    // ==================================================

    private void FixedUpdate()
    {
        StepDash();


        // Runs no matter which weapon the hand is holding, which is the
        // whole point: a deployed weapon is owned by the Floor, not by
        // whatever is equipped right now.
        CheckRecoveryContact(rightState);
        CheckRecoveryContact(leftState);
    }


    private void StepDash()
    {
        if (!IsDashing)
        {
            return;
        }


        // Floor cleanup can take the weapon out from under a Dash.
        if (dashTargetWeapon == null ||
            playerRigidbody == null)
        {
            CancelDash();

            return;
        }


        Vector2 current =
            playerRigidbody.position;


        // Swept over the step the physics engine just ran, so nothing is
        // skipped between frames at Dash speed.
        SweepSlash(
            dashPreviousPosition,
            current
        );


        if (paintDashTrail &&
            InkMap.Instance != null)
        {
            InkMap.Instance.PaintTrail(
                dashPreviousPosition,
                current,
                InkTeam.Player
            );
        }


        dashPreviousPosition = current;


        dashTimeRemaining -=
            Time.fixedDeltaTime;


        float remaining =
            Vector2.Dot(
                dashPoint - current,
                dashDirection
            );


        if (remaining <= dashArrivalDistance ||
            dashTimeRemaining <= 0f)
        {
            CancelDash();

            return;
        }


        // Velocity, not a transform move: the Player keeps colliding with
        // walls exactly as it does while walking.
        playerRigidbody.linearVelocity =
            dashDirection * dashSpeed;
    }


    private void CancelDash()
    {
        if (!IsDashing)
        {
            return;
        }


        dashOwnerState = null;
        dashTargetWeapon = null;

        dashHitTargets.Clear();


        if (playerRigidbody != null)
        {
            playerRigidbody.linearVelocity =
                Vector2.zero;
        }


        if (playerMovement != null)
        {
            playerMovement.enabled = true;
        }
    }


    // ==================================================
    // Recovery Contact
    //
    // Walking up to the weapon and dashing into it are the same event:
    // the Dash simply ends next to the recovery point, and the contact
    // check below fires on the very same physics step.
    // ==================================================

    private void CheckRecoveryContact(
        SlotRuntimeState state
    )
    {
        BreachThrownWeapon weapon =
            state.deployedWeapon;


        if (weapon == null ||
            !weapon.IsEmbedded ||
            playerCollider == null)
        {
            return;
        }


        Vector2 recoveryPoint =
            GetRecoveryPoint(weapon);


        // Measured from the body surface, so a wide Player does not have
        // to bury its centre in the wall to count as touching.
        Vector2 closest =
            playerCollider.ClosestPoint(
                recoveryPoint
            );


        float contactSquared =
            recoveryContactRadius
            * recoveryContactRadius;


        if ((closest - recoveryPoint).sqrMagnitude
            > contactSquared)
        {
            return;
        }


        PerformRecoverySlash(
            state,
            recoveryPoint
        );
    }


    private void PerformRecoverySlash(
        SlotRuntimeState state,
        Vector2 center
    )
    {
        float multiplier =
            state.damageMultiplier;


        // The weapon and every trace of it go first, so nothing below can
        // leave a Dash chasing a target that no longer exists.
        ClearDeployed(state);


        recoveryHitTargets.Clear();


        Collider2D[] hits =
            Physics2D.OverlapCircleAll(
                center,
                recoverySlashRadius,
                recoverySlashMask
            );


        for (int i = 0; i < hits.Length; i++)
        {
            TryRecoveryDamage(
                hits[i],
                center,
                multiplier
            );
        }


        if (paintRecoverySlash &&
            InkMap.Instance != null)
        {
            InkMap.Instance.PaintSpinSlash(
                center,
                recoverySlashRadius,
                InkTeam.Player
            );
        }
    }


    // Only Enemy and Boss health answer here, which is what keeps the
    // DefenseTarget and the Player out of it.
    private void TryRecoveryDamage(
        Collider2D collider,
        Vector2 center,
        float multiplier
    )
    {
        if (collider == null)
        {
            return;
        }


        BossHealth boss =
            collider.GetComponentInParent<BossHealth>();


        if (boss != null)
        {
            if (recoveryHitTargets.Add(boss))
            {
                boss.TakeDamage(
                    recoverySlashDamage * multiplier
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


        if (!recoveryHitTargets.Add(enemy))
        {
            return;
        }


        enemy.TakeDamage(
            recoverySlashDamage * multiplier
        );


        // The shared contract pushes away from the source point, so the
        // slash centre gives a clean radial blast for free.
        Vector2 source = center;


        if (((Vector2)collider.transform.position - center)
            .sqrMagnitude <= 0.0001f)
        {
            source = center - Vector2.up;
        }


        KnockbackUtility.TryApply(
            collider,
            source,
            recoverySlashKnockbackForce
        );
    }


    // ==================================================
    // Dash Slash
    // ==================================================

    private void SweepSlash(
        Vector2 from,
        Vector2 to
    )
    {
        Vector2 delta =
            to - from;


        float distance =
            delta.magnitude;


        Vector2 direction =
            distance > 0.0001f
                ? delta / distance
                : dashDirection;


        RaycastHit2D[] hits =
            Physics2D.CircleCastAll(
                from,
                dashSlashRadius,
                direction,
                Mathf.Max(distance, 0.01f),
                dashSlashMask
            );


        for (int i = 0; i < hits.Length; i++)
        {
            Collider2D collider =
                hits[i].collider;


            if (collider == null)
            {
                continue;
            }


            TrySlashDamage(
                collider,
                from,
                to
            );
        }
    }


    // Deduped on the health component, so several colliders or several
    // physics steps still cost a target one hit per Dash.
    private void TrySlashDamage(
        Collider2D collider,
        Vector2 from,
        Vector2 to
    )
    {
        BossHealth boss =
            collider.GetComponentInParent<BossHealth>();


        if (boss != null)
        {
            if (dashHitTargets.Add(boss))
            {
                boss.TakeDamage(
                    dashDamage * dashOwnerState.damageMultiplier
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


        if (!dashHitTargets.Add(enemy))
        {
            return;
        }


        enemy.TakeDamage(
            dashDamage * dashOwnerState.damageMultiplier
        );


        KnockbackUtility.TryApply(
            collider,
            GetSlashKnockbackSource(
                collider.transform.position,
                from,
                to
            ),
            dashKnockbackForce
        );
    }


    // The shared knockback contract pushes away from a source point, so
    // the closest point on the dash line is exactly the source that
    // splits enemies out to either side of the path.
    private Vector2 GetSlashKnockbackSource(
        Vector2 enemyPosition,
        Vector2 from,
        Vector2 to
    )
    {
        Vector2 segment =
            to - from;


        float lengthSquared =
            segment.sqrMagnitude;


        Vector2 closest = from;


        if (lengthSquared > 0.0001f)
        {
            float t =
                Mathf.Clamp01(
                    Vector2.Dot(
                        enemyPosition - from,
                        segment
                    )
                    /
                    lengthSquared
                );


            closest =
                from + segment * t;
        }


        // Dead centre on the line: pick one side, always the same one, so
        // the same situation never resolves two different ways.
        if ((enemyPosition - closest).sqrMagnitude
            <= 0.0001f)
        {
            Vector2 perpendicular =
                new Vector2(
                    -dashDirection.y,
                    dashDirection.x
                );


            return enemyPosition - perpendicular;
        }


        return closest;
    }


    // ==================================================
    // Gates
    // ==================================================

    private bool CanThrow(
        WeaponUseContext context
    )
    {
        if (context.Controller == null ||
            context.Weapon == null)
        {
            return false;
        }


        if (playerDive != null &&
            playerDive.IsSwimForm)
        {
            return false;
        }


        if (inkResource == null ||
            inkResource.IsEmpty)
        {
            return false;
        }


        return true;
    }


    // ==================================================
    // Runtime
    // ==================================================

    private SlotRuntimeState GetState(
        WeaponSlotSide side
    )
    {
        if (side == WeaponSlotSide.Right)
        {
            return rightState;
        }


        return leftState;
    }


    // Floor cleanup destroys deployed weapons behind our back, so the
    // reference is validated through Unity null semantics before use.
    private void ClearDestroyedDeployed(
        SlotRuntimeState state
    )
    {
        if (state.deployedWeapon == null)
        {
            state.deployedWeapon = null;
        }
    }


    private void ClearDeployed(
        SlotRuntimeState state
    )
    {
        state.isHolding = false;
        state.damageMultiplier = 1f;


        if (dashOwnerState == state)
        {
            CancelDash();
        }


        if (state.deployedWeapon != null)
        {
            Destroy(
                state.deployedWeapon.gameObject
            );
        }


        state.deployedWeapon = null;
    }


    // ==================================================
    // Auto Find
    // ==================================================

    [ContextMenu(
        "AUTO FIND - Breach References"
    )]
    private void AutoFindReferences()
    {
        Transform root =
            transform.root;


        if (inkResource == null)
        {
            inkResource =
                root.GetComponentInChildren<
                    PlayerInkResource
                >(
                    true
                );
        }


        if (playerDive == null)
        {
            playerDive =
                root.GetComponentInChildren<
                    PlayerDive
                >(
                    true
                );
        }


        if (playerShield == null)
        {
            playerShield =
                root.GetComponentInChildren<
                    PlayerShield
                >(
                    true
                );
        }


        if (playerMovement == null)
        {
            playerMovement =
                root.GetComponentInChildren<
                    PlayerMovement
                >(
                    true
                );
        }


        if (playerRigidbody == null &&
            playerMovement != null)
        {
            playerRigidbody =
                playerMovement
                    .GetComponent<Rigidbody2D>();
        }


        if (playerCollider == null &&
            playerMovement != null)
        {
            playerCollider =
                playerMovement
                    .GetComponent<Collider2D>();
        }


        mainCamera =
            Camera.main;


#if UNITY_EDITOR

        UnityEditor.EditorUtility.SetDirty(
            this
        );

#endif
    }
}
