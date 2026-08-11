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

    private float inkRadius;

    private int inkSplatCount;


    private PlayerShield playerShield;

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
            GameObject.FindGameObjectWithTag(
                "Player"
            );


        if (player != null)
        {
            playerShield =
                player.GetComponent<PlayerShield>();
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

        if (playerShield != null)
        {
            float distance =
                Vector2.Distance(
                    targetPosition,
                    playerShield
                        .transform
                        .position
                );


            if (distance <=
                damageRadius)
            {
                playerShield.TakeDamage(
                    damage,
                    targetPosition
                );
            }
        }


        // ==========================================
        // 4. Bomb 제거
        // ==========================================

        Destroy(
            gameObject
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