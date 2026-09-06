using System.Collections.Generic;
using UnityEngine;

// Blade (working name). Not a sword: a cleaning brush. Every click drags a
// water splash across the front of the Player, and the next click drags it
// back the other way. Nothing about the brush itself hits - only the band
// of angles the splash has actually swept through this attack does.
//
// Everything about a swing lives in per-hand state: one behaviour serves
// both slots, so a shared field would let the right hand's swing overwrite
// the left hand's.
public class BladeWeaponBehaviour
    : PlayerWeaponBehaviour
{
    // ==================================================
    // Slot Runtime
    // ==================================================

    private sealed class SlotRuntimeState
    {
        public readonly WeaponSlotSide Side;

        public readonly HashSet<Object> HitTargets =
            new HashSet<Object>();

        public bool Sweeping;

        public bool BufferedAttack;

        public WeaponUseContext BufferedContext;

        // Every hand opens its combo the same way.
        public bool NextSweepIsLeftToRight = true;

        public float ComboExpireTime;

        public Transform Origin;

        public Vector2 AttackForward = Vector2.right;

        public float AttackForwardAngle;

        public float StartAngle;

        public float EndAngle;

        public float PreviousAngle;

        public float Elapsed;

        public float DamageMultiplier = 1f;


        public SlotRuntimeState(
            WeaponSlotSide side
        )
        {
            Side = side;
        }
    }


    private readonly SlotRuntimeState rightState =
        new SlotRuntimeState(WeaponSlotSide.Right);

    private readonly SlotRuntimeState leftState =
        new SlotRuntimeState(WeaponSlotSide.Left);


    // ==================================================
    // Sweep Shape
    // ==================================================

    [Header("Sweep Shape")]

    [Min(0.1f)]
    [SerializeField]
    private float range = 3.2f;


    [Tooltip("Full width of the fan, in degrees.")]
    [Range(10f, 180f)]
    [SerializeField]
    private float arcAngle = 110f;


    [Min(0.05f)]
    [SerializeField]
    private float sweepDuration = 0.32f;


    [Tooltip("Thickness of the splash itself. Widens the band swept each "
        + "physics step so a target is not missed between two of them.")]
    [Range(0f, 45f)]
    [SerializeField]
    private float splashAngularWidth = 12f;


    // ==================================================
    // Combo
    // ==================================================

    [Header("Combo")]

    [Tooltip("Idle longer than this and the next attack starts over from "
        + "the left edge instead of continuing the alternation.")]
    [Min(0.05f)]
    [SerializeField]
    private float comboResetTime = 0.7f;


    // ==================================================
    // Damage
    // ==================================================

    [Header("Damage")]

    [Tooltip("A repeat-fire close range weapon, so one sweep lands well "
        + "under a Breach Dash. Regular enemies have 12 health.")]
    [Min(0f)]
    [SerializeField]
    private float damage = 3f;


    [Tooltip("Sweeps enemies sideways along the swing. Deliberately weak "
        + "next to Breach, which is the weapon that moves things.")]
    [Min(0f)]
    [SerializeField]
    private float knockbackForce = 5f;


    [Tooltip("Everything the splash should consider. Filtered by health "
        + "component afterwards.")]
    [SerializeField]
    private LayerMask hitMask = ~0;


    // ==================================================
    // Cleaning Paint
    // ==================================================

    [Header("Cleaning Paint")]

    [SerializeField]
    private bool paintCleaning = true;


    [Tooltip("Marks laid down along the splash each physics step.")]
    [Range(2, 12)]
    [SerializeField]
    private int paintSampleCount = 6;


    [Min(0.02f)]
    [SerializeField]
    private float paintMarkRadius = 0.22f;


    // ==================================================
    // Ink Cost
    // ==================================================

    [Header("Ink Cost")]

    [Tooltip("Percent of Max Ink, charged once when a sweep actually "
        + "starts. A blocked or cancelled attack costs nothing.")]
    [Range(0f, 1f)]
    [SerializeField]
    private float inkUsePerSweepPercent = 0.03f;


    // ==================================================
    // Sweep Bar
    //
    // A placeholder for the water splash: a plain sprite driven straight
    // off the gameplay angle, so what is on screen and what is being hit
    // can never drift apart. Purely cosmetic - no collider, no damage.
    // One per hand, because both can be swinging at once.
    // ==================================================

    [System.Serializable]
    private sealed class SweepBarVisual
    {
        public Transform Bar;

        public SpriteRenderer Renderer;
    }


    [Header("Sweep Bar Visual")]

    [SerializeField]
    private SweepBarVisual rightSweepBar =
        new SweepBarVisual();


    [SerializeField]
    private SweepBarVisual leftSweepBar =
        new SweepBarVisual();


    [Tooltip("Distance from the Player to the centre of the bar: half the "
        + "bar's own length, so its near end pivots on the Player.")]
    [Min(0f)]
    [SerializeField]
    private float sweepBarOffset = 1.25f;


    // ==================================================
    // References
    // ==================================================

    [Header("Runtime References")]

    [SerializeField]
    private PlayerInkResource inkResource;


    [SerializeField]
    private PlayerDive playerDive;


    private Camera mainCamera;


    // ==================================================
    // State
    // ==================================================

    public override bool IsUsing =>
        rightState.Sweeping
        ||
        leftState.Sweeping;


    public override bool IsUsingSlot(
        WeaponSlotSide side
    )
    {
        return GetState(side).Sweeping;
    }


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


    private SweepBarVisual GetVisual(
        WeaponSlotSide side
    )
    {
        if (side == WeaponSlotSide.Right)
        {
            return rightSweepBar;
        }


        return leftSweepBar;
    }


    private void Awake()
    {
        AutoFindReferences();

        SetSweepBarVisible(rightState, false);
        SetSweepBarVisible(leftState, false);
    }


    // Nothing should be left hanging in the world if the weapon goes away
    // mid swing.
    private void OnDisable()
    {
        StopSweep(rightState);
        StopSweep(leftState);
    }


    // ==================================================
    // Press
    //
    // Click driven. Holding the button does nothing on purpose.
    // ==================================================

    public override void UsePressed(
        WeaponUseContext context
    )
    {
        SlotRuntimeState state =
            GetState(context.SlotSide);


        if (state.Sweeping)
        {
            // Depth of exactly one, per hand: spamming the button cannot
            // stack up a queue of attacks that keep playing after the
            // player stops, and the other hand's buffer is untouched.
            state.BufferedAttack = true;
            state.BufferedContext = context;


            return;
        }


        TryStartSweep(context, state);
    }


    public override void UseHeld(
        WeaponUseContext context
    )
    {
    }


    public override void UseReleased(
        WeaponUseContext context
    )
    {
    }


    // ==================================================
    // Cancel
    //
    // A cancelled sweep is not a completed one, so it never flips the
    // direction. That hand's combo restarts from the left edge instead,
    // which is the one outcome that is the same every time.
    // ==================================================

    public override void CancelUse()
    {
        StopSweep(rightState);
        StopSweep(leftState);
    }


    public override void CancelUse(
        WeaponSlotSide side
    )
    {
        StopSweep(
            GetState(side)
        );
    }


    private void StopSweep(
        SlotRuntimeState state
    )
    {
        state.Sweeping = false;
        state.BufferedAttack = false;

        state.HitTargets.Clear();

        state.NextSweepIsLeftToRight = true;
        state.ComboExpireTime = 0f;

        SetSweepBarVisible(state, false);
    }


    // ==================================================
    // Start
    // ==================================================

    private bool TryStartSweep(
        WeaponUseContext context,
        SlotRuntimeState state
    )
    {
        if (context.Controller == null ||
            context.Weapon == null ||
            inkResource == null)
        {
            return false;
        }


        if (playerDive != null &&
            playerDive.IsSwimForm)
        {
            return false;
        }


        Transform usePoint =
            context.UsePoint;


        if (usePoint == null)
        {
            return false;
        }


        float inkCost =
            inkResource.MaxInk
            *
            inkUsePerSweepPercent
            *
            context.InkCostMultiplier;


        // Checked before the sweep exists, so an attack is never played
        // and then billed for ink that was not there.
        if (!inkResource.HasInk(inkCost))
        {
            if (inkResource.IsEmpty)
            {
                context.Controller.SetForcedHand(
                    context.SlotSide,
                    true
                );
            }


            return false;
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
            return false;
        }


        // Snapshot, per hand: once a swing is under way its fan no longer
        // turns with the mouse, so the other hand can aim somewhere else
        // without dragging this one along.
        state.AttackForwardAngle = aimAngle;

        state.AttackForward =
            new Vector2(
                Mathf.Cos(aimAngle * Mathf.Deg2Rad),
                Mathf.Sin(aimAngle * Mathf.Deg2Rad)
            );


        if (Time.time > state.ComboExpireTime)
        {
            state.NextSweepIsLeftToRight = true;
        }


        float halfArc =
            arcAngle * 0.5f;


        state.StartAngle =
            state.NextSweepIsLeftToRight
                ? -halfArc
                : halfArc;

        state.EndAngle =
            state.NextSweepIsLeftToRight
                ? halfArc
                : -halfArc;


        state.PreviousAngle = state.StartAngle;
        state.Elapsed = 0f;

        state.DamageMultiplier =
            context.DamageMultiplier;

        // The Player keeps walking during the swing, so the fan is anchored
        // to the body rather than to a hand that animates around it.
        state.Origin =
            context.Controller.transform;

        state.HitTargets.Clear();

        state.Sweeping = true;


        // Placed on the starting edge before it appears, so the first
        // frame is never a leftover pose from the previous swing.
        UpdateSweepBar(
            state,
            state.Origin.position,
            state.StartAngle
        );

        SetSweepBarVisible(state, true);


        inkResource.TrySpendInk(inkCost);


        if (inkResource.IsEmpty)
        {
            context.Controller.SetForcedHand(
                context.SlotSide,
                true
            );
        }


        return true;
    }


    // ==================================================
    // Sweep Step
    // ==================================================

    private void FixedUpdate()
    {
        // Both hands always get their turn: an early return for one would
        // freeze the other mid swing.
        StepSweep(rightState);
        StepSweep(leftState);
    }


    private void StepSweep(
        SlotRuntimeState state
    )
    {
        if (!state.Sweeping)
        {
            return;
        }


        if (state.Origin == null)
        {
            StopSweep(state);

            return;
        }


        state.Elapsed +=
            Time.fixedDeltaTime;


        float t =
            Mathf.Clamp01(
                state.Elapsed / sweepDuration
            );


        // An arm winds up, whips through the middle and pulls up short.
        // Linear time here read as a windscreen wiper.
        float easedT =
            Mathf.SmoothStep(
                0f,
                1f,
                t
            );


        float currentAngle =
            Mathf.Lerp(
                state.StartAngle,
                state.EndAngle,
                easedT
            );


        Vector2 origin =
            state.Origin.position;


        // The band actually crossed this step, not just where the splash
        // happens to be right now: a fast sweep must not step over anyone.
        ApplySweepBand(
            state,
            origin,
            state.PreviousAngle,
            currentAngle
        );


        PaintSweepBand(
            state,
            origin,
            currentAngle
        );


        UpdateSweepBar(
            state,
            origin,
            currentAngle
        );


        state.PreviousAngle = currentAngle;


        if (t < 1f)
        {
            return;
        }


        CompleteSweep(state);
    }


    private void CompleteSweep(
        SlotRuntimeState state
    )
    {
        state.Sweeping = false;

        state.NextSweepIsLeftToRight =
            !state.NextSweepIsLeftToRight;

        state.ComboExpireTime =
            Time.time + comboResetTime;


        if (state.BufferedAttack)
        {
            state.BufferedAttack = false;


            // The next swing takes the bar over directly, so a fast combo
            // never blinks it off for a frame.
            if (TryStartSweep(
                    state.BufferedContext,
                    state
                ))
            {
                return;
            }
        }


        SetSweepBarVisible(state, false);
    }


    // ==================================================
    // Sweep Bar
    // ==================================================

    private void UpdateSweepBar(
        SlotRuntimeState state,
        Vector2 origin,
        float sweepAngle
    )
    {
        SweepBarVisual visual =
            GetVisual(state.Side);


        if (visual.Bar == null)
        {
            return;
        }


        float visualAngle =
            state.AttackForwardAngle + sweepAngle;


        Vector2 direction =
            Rotate(
                state.AttackForward,
                sweepAngle
            );


        // Set in world space, so whichever parent the bar is put under
        // cannot rotate it a second time.
        visual.Bar.SetPositionAndRotation(
            origin + direction * sweepBarOffset,
            Quaternion.Euler(
                0f,
                0f,
                visualAngle
            )
        );
    }


    private void SetSweepBarVisible(
        SlotRuntimeState state,
        bool visible
    )
    {
        SweepBarVisual visual =
            GetVisual(state.Side);


        if (visual.Renderer == null)
        {
            return;
        }


        visual.Renderer.enabled = visible;
    }


    // ==================================================
    // Hit
    // ==================================================

    private void ApplySweepBand(
        SlotRuntimeState state,
        Vector2 origin,
        float fromAngle,
        float toAngle
    )
    {
        float halfWidth =
            splashAngularWidth * 0.5f;


        float lowAngle =
            Mathf.Min(fromAngle, toAngle)
            - halfWidth;

        float highAngle =
            Mathf.Max(fromAngle, toAngle)
            + halfWidth;


        Collider2D[] hits =
            Physics2D.OverlapCircleAll(
                origin,
                range,
                hitMask
            );


        for (int i = 0; i < hits.Length; i++)
        {
            Collider2D collider = hits[i];


            if (collider == null)
            {
                continue;
            }


            Vector2 toTarget =
                (Vector2)collider.transform.position
                - origin;


            if (toTarget.sqrMagnitude < 0.0001f)
            {
                continue;
            }


            float targetAngle =
                Vector2.SignedAngle(
                    state.AttackForward,
                    toTarget
                );


            if (targetAngle < lowAngle ||
                targetAngle > highAngle)
            {
                continue;
            }


            TryDamage(state, collider);
        }
    }


    // Deduped on the health component, so several colliders or several
    // physics steps still cost a target one hit per sweep. The next sweep
    // starts a fresh set, and so does the other hand: two swings crossing
    // the same enemy are two separate attacks.
    private void TryDamage(
        SlotRuntimeState state,
        Collider2D collider
    )
    {
        BossHealth boss =
            collider.GetComponentInParent<BossHealth>();


        if (boss != null)
        {
            if (state.HitTargets.Add(boss))
            {
                boss.TakeDamage(
                    damage * state.DamageMultiplier
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


        if (!state.HitTargets.Add(enemy))
        {
            return;
        }


        enemy.TakeDamage(
            damage * state.DamageMultiplier
        );


        // Pushed the way the brush is travelling. The shared contract
        // pushes away from a source point, so the source is placed one
        // unit back along the direction we want.
        Vector2 push =
            GetSweepPushDirection(state);


        KnockbackUtility.TryApply(
            collider,
            (Vector2)collider.transform.position
                - push,
            knockbackForce
        );
    }


    // Perpendicular to the swing, in the frame of this attack rather than
    // of the screen: left to right pushes one way, right to left the other.
    private static Vector2 GetSweepPushDirection(
        SlotRuntimeState state
    )
    {
        Vector2 perpendicular =
            new Vector2(
                -state.AttackForward.y,
                state.AttackForward.x
            );


        // Read off the angles the sweep is actually running between, so
        // this can never disagree with what is on screen.
        return
            state.EndAngle > state.StartAngle
                ? perpendicular
                : -perpendicular;
    }


    // ==================================================
    // Cleaning Paint
    //
    // Only where the splash has reached, so the floor is cleaned in the
    // direction of the stroke instead of being stamped all at once.
    // ==================================================

    private void PaintSweepBand(
        SlotRuntimeState state,
        Vector2 origin,
        float currentAngle
    )
    {
        if (!paintCleaning ||
            InkMap.Instance == null)
        {
            return;
        }


        for (int i = 0; i < paintSampleCount; i++)
        {
            float step =
                paintSampleCount > 1
                    ? i / (float)(paintSampleCount - 1)
                    : 0f;


            float distance =
                Mathf.Lerp(
                    range * 0.25f,
                    range,
                    step
                )
                * Random.Range(
                    0.94f,
                    1.06f
                );


            float angle =
                currentAngle
                + Random.Range(
                    -3f,
                    3f
                );


            InkMap.Instance.PaintCircle(
                origin
                + Rotate(
                    state.AttackForward,
                    angle
                )
                * distance,
                paintMarkRadius
                * Random.Range(
                    0.7f,
                    1.3f
                ),
                InkTeam.Player
            );
        }
    }


    private static Vector2 Rotate(
        Vector2 value,
        float degrees
    )
    {
        float radians =
            degrees * Mathf.Deg2Rad;


        float cos = Mathf.Cos(radians);
        float sin = Mathf.Sin(radians);


        return new Vector2(
            value.x * cos - value.y * sin,
            value.x * sin + value.y * cos
        );
    }


    // ==================================================
    // Auto Find
    // ==================================================

    [ContextMenu(
        "AUTO FIND - Blade References"
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


        mainCamera =
            Camera.main;


#if UNITY_EDITOR

        UnityEditor.EditorUtility.SetDirty(
            this
        );

#endif
    }


    // ==================================================
    // Debug
    // ==================================================

    private void OnDrawGizmosSelected()
    {
        SlotRuntimeState state =
            rightState.Sweeping
                ? rightState
                : leftState;


        Vector2 origin =
            state.Sweeping && state.Origin != null
                ? (Vector2)state.Origin.position
                : (Vector2)transform.position;


        Vector2 forward =
            state.Sweeping
                ? state.AttackForward
                : (Vector2)transform.right;


        float halfArc =
            arcAngle * 0.5f;


        Gizmos.color = Color.cyan;

        Gizmos.DrawLine(
            origin,
            origin + Rotate(forward, -halfArc) * range
        );

        Gizmos.DrawLine(
            origin,
            origin + Rotate(forward, halfArc) * range
        );


        Vector2 previous =
            origin + Rotate(forward, -halfArc) * range;


        for (int i = 1; i <= 16; i++)
        {
            Vector2 point =
                origin
                + Rotate(
                    forward,
                    Mathf.Lerp(
                        -halfArc,
                        halfArc,
                        i / 16f
                    )
                )
                * range;


            Gizmos.DrawLine(previous, point);

            previous = point;
        }


        if (!state.Sweeping)
        {
            return;
        }


        Gizmos.color = Color.yellow;

        Gizmos.DrawLine(
            origin,
            origin
            + Rotate(forward, state.PreviousAngle) * range
        );
    }
}
