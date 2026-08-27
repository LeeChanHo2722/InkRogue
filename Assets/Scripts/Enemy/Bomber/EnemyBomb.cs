using System.Collections;
using UnityEngine;

public class EnemyBomb : MonoBehaviour
{
    // ==================================================
    // Visual
    // ==================================================

    [Header("Visual")]

    public float arcHeight = 1.2f;

    public float rotateSpeed = 500f;


    // ==================================================
    // Runtime Data
    // ==================================================

    private Vector2 startPosition;

    private Vector2 targetPosition;

    private float flightDuration;

    private float damage;

    private float damageRadius;

    [Header("Knockback")]

    [Min(0f)]
    public float knockbackForce =
        10f;

    private float inkRadius;

    private int inkSplatCount;


    private IEncounterDamageTarget damageTarget;

    // An explosion is an area attack, so the real Player is tracked
    // separately from the thrown-at target. On a Defense Floor those are
    // two different objects and both must be able to take the blast.
    private IEncounterDamageTarget playerDamageTarget;

    private SpriteRenderer spriteRenderer;

    private bool initialized =
        false;


    // ==================================================
    // Awake
    // ==================================================

    private void Awake()
    {
        spriteRenderer =
            GetComponent<SpriteRenderer>();
    }


    // ==================================================
    // Initialize
    // ==================================================

    public void Initialize(
        Vector2 start,
        Vector2 target,
        float duration,
        float bombDamage,
        float bombDamageRadius,
        float bombInkRadius,
        int bombInkSplatCount)
    {
        startPosition =
            start;


        targetPosition =
            target;


        flightDuration =
            Mathf.Max(
                duration,
                0.05f
            );


        damage =
            bombDamage;


        damageRadius =
            bombDamageRadius;


        inkRadius =
            bombInkRadius;


        inkSplatCount =
            bombInkSplatCount;


        GameObject player =
            EncounterTarget.ResolveGameObject();


        if (player != null)
        {
            damageTarget =
                player.GetComponent<IEncounterDamageTarget>();
        }


        GameObject actualPlayer =
            GameObject.FindGameObjectWithTag(
                "Player"
            );


        if (actualPlayer != null)
        {
            playerDamageTarget =
                actualPlayer
                    .GetComponent<IEncounterDamageTarget>();
        }


        initialized =
            true;


        StartCoroutine(
            FlightRoutine()
        );
    }


    // ==================================================
    // Flight
    // ==================================================

    private IEnumerator FlightRoutine()
    {
        if (!initialized)
            yield break;


        float timer =
            0f;


        while (timer <
               flightDuration)
        {
            timer +=
                Time.deltaTime;


            float t =
                Mathf.Clamp01(
                    timer
                    / flightDuration
                );


            Vector2 basePosition =
                Vector2.Lerp(
                    startPosition,
                    targetPosition,
                    t
                );


            // ======================================
            // 가상의 포물선 높이
            // ======================================

            float height =
                4f
                * arcHeight
                * t
                * (1f - t);


            transform.position =
                new Vector3(
                    basePosition.x,
                    basePosition.y
                    + height,
                    transform.position.z
                );


            transform.Rotate(
                0f,
                0f,
                rotateSpeed
                * Time.deltaTime
            );


            yield return null;
        }


        Explode();
    }


    // ==================================================
    // Explosion
    // ==================================================

    private void Explode()
    {
        // ==========================================
        // 1. Explosion VFX
        // ==========================================

        CreateExplosionVFX();


        // ==========================================
        // 2. Enemy Ink
        // ==========================================

        if (InkMap.Instance != null)
        {
            InkMap.Instance.PaintExplosion(
                targetPosition,
                inkRadius,
                InkTeam.Enemy,
                inkSplatCount
            );
        }


        // ==========================================
        // 3. Player Damage
        // ==========================================

        TryApplyExplosionTo(
            damageTarget
        );


        if (playerDamageTarget != null &&
            !ReferenceEquals(
                playerDamageTarget,
                damageTarget
            ))
        {
            TryApplyExplosionTo(
                playerDamageTarget
            );
        }


        // ==========================================
        // 4. Bomb 제거
        // ==========================================

        Destroy(
            gameObject
        );
    }


    private void TryApplyExplosionTo(
        IEncounterDamageTarget target)
    {
        if (target == null)
        {
            return;
        }


        Transform body =
            target.TargetTransform;


        if (body == null)
        {
            return;
        }


        float distance =
            Vector2.Distance(
                targetPosition,
                body.position
            );


        if (distance >
            damageRadius)
        {
            return;
        }


        target.TakeDamage(
            damage,
            targetPosition
        );


        KnockbackUtility.TryApply(
            body,
            targetPosition,
            knockbackForce
        );
    }


    // ==================================================
    // Explosion VFX
    // ==================================================

    private void CreateExplosionVFX()
    {
        GameObject effectObject =
            new GameObject(
                "Runtime_EnemyBombExplosionVFX"
            );


        effectObject.transform.position =
            targetPosition;


        EnemyBombExplosionVFX effect =
            effectObject.AddComponent<
                EnemyBombExplosionVFX
            >();


        Color effectColor =
            new Color(
                1f,
                0.1f,
                0.35f,
                1f
            );


        if (InkMap.Instance != null)
        {
            effectColor =
                InkMap.Instance
                    .enemyInkColor;


            effectColor.a =
                1f;
        }


        Material material =
            null;


        if (spriteRenderer != null)
        {
            material =
                spriteRenderer
                    .sharedMaterial;
        }


        effect.Initialize(
            effectColor,
            damageRadius,
            material
        );
    }
}