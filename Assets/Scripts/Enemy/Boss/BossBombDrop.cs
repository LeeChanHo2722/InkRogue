using System.Collections;
using UnityEngine;

public class BossBombDrop : MonoBehaviour
{
    // ==================================================
    // References
    // ==================================================

    [Header("References")]

    public Transform visual;

    public SpriteRenderer visualRenderer;


    // ==================================================
    // Visual
    // ==================================================

    [Header("Visual")]

    public float dropVisualHeight = 2.2f;

    public float startScale = 0.55f;

    public float endScale = 1.0f;

    public float rotateSpeed = 540f;


    // ==================================================
    // Telegraph
    // ==================================================

    [Header("Telegraph")]

    public int ringSegments = 40;

    public float warningLineWidth = 0.065f;

    public float warningStartScale = 1.65f;


    // ==================================================
    // Runtime
    // ==================================================

    private float warningDuration;

    private float damage;

    private float damageRadius;

    private float inkRadius;

    private int inkSplatCount;


    private PlayerShield playerShield;

    private LineRenderer warningRing;


    private Vector3 originalVisualScale;


    // ==================================================
    // Awake
    // ==================================================

    private void Awake()
    {
        if (visual == null)
        {
            visual =
                transform;
        }


        if (visualRenderer == null)
        {
            visualRenderer =
                GetComponentInChildren<
                    SpriteRenderer
                >();
        }


        originalVisualScale =
            visual.localScale;


        GameObject player =
            GameObject.FindGameObjectWithTag(
                "Player"
            );


        if (player != null)
        {
            playerShield =
                player.GetComponent<PlayerShield>();
        }


        CreateWarningRing();
    }


    // ==================================================
    // Initialize
    // ==================================================

    public void Initialize(
        Vector2 targetPosition,
        float newWarningDuration,
        float newDamage,
        float newDamageRadius,
        float newInkRadius,
        int newInkSplatCount)
    {
        transform.position =
            targetPosition;


        warningDuration =
            newWarningDuration;


        damage =
            newDamage;


        damageRadius =
            newDamageRadius;


        inkRadius =
            newInkRadius;


        inkSplatCount =
            newInkSplatCount;


        StartCoroutine(
            DropRoutine()
        );
    }


    // ==================================================
    // Drop
    // ==================================================

    private IEnumerator DropRoutine()
    {
        float timer =
            0f;


        float safeDuration =
            Mathf.Max(
                warningDuration,
                0.01f
            );


        if (warningRing != null)
        {
            warningRing.enabled =
                true;
        }


        while (timer <
               safeDuration)
        {
            timer +=
                Time.deltaTime;


            float t =
                Mathf.Clamp01(
                    timer
                    / safeDuration
                );


            // ======================================
            // Bomb Visual
            // ======================================

            if (visual != null)
            {
                float height =
                    Mathf.Lerp(
                        dropVisualHeight,
                        0f,
                        t * t
                    );


                visual.localPosition =
                    new Vector3(
                        0f,
                        height,
                        0f
                    );


                float scale =
                    Mathf.Lerp(
                        startScale,
                        endScale,
                        t
                    );


                visual.localScale =
                    originalVisualScale
                    * scale;


                visual.Rotate(
                    0f,
                    0f,
                    rotateSpeed
                    * Time.deltaTime
                );
            }


            // ======================================
            // Warning
            // ======================================

            float radius =
                Mathf.Lerp(
                    damageRadius
                    * warningStartScale,
                    damageRadius,
                    t
                );


            Color color =
                GetWarningColor();


            color.a =
                Mathf.Lerp(
                    0.45f,
                    1f,
                    t
                );


            DrawRing(
                radius,
                color
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
        Vector2 position =
            transform.position;


        // ==========================================
        // VFX
        // ==========================================

        GameObject effectObject =
            new GameObject(
                "Runtime_BossBombExplosion"
            );


        effectObject.transform.position =
            position;


        EnemyBombExplosionVFX effect =
            effectObject.AddComponent<
                EnemyBombExplosionVFX
            >();


        Color effectColor =
            GetWarningColor();


        Material material =
            visualRenderer != null
                ? visualRenderer.sharedMaterial
                : null;


        effect.Initialize(
            effectColor,
            damageRadius,
            material
        );


        // ==========================================
        // Ink
        // ==========================================

        if (InkMap.Instance != null)
        {
            InkMap.Instance.PaintExplosion(
                position,
                inkRadius,
                InkTeam.Enemy,
                inkSplatCount
            );
        }


        // ==========================================
        // Damage
        // ==========================================

        if (playerShield != null)
        {
            float distance =
                Vector2.Distance(
                    position,
                    playerShield.transform.position
                );


            if (distance <=
                damageRadius)
            {
                playerShield.TakeDamage(
                    damage,
                    position
                );
            }
        }


        Destroy(
            gameObject
        );
    }


    // ==================================================
    // Warning Ring
    // ==================================================

    private void CreateWarningRing()
    {
        GameObject ringObject =
            new GameObject(
                "WarningRing"
            );


        ringObject.transform.SetParent(
            transform,
            false
        );


        warningRing =
            ringObject.AddComponent<
                LineRenderer
            >();


        warningRing.useWorldSpace =
            true;


        warningRing.loop =
            true;


        warningRing.positionCount =
            ringSegments;


        warningRing.numCornerVertices =
            4;


        warningRing.startWidth =
            warningLineWidth;


        warningRing.endWidth =
            warningLineWidth;


        warningRing.enabled =
            false;


        if (visualRenderer != null)
        {
            warningRing.sharedMaterial =
                visualRenderer.sharedMaterial;


            warningRing.sortingLayerID =
                visualRenderer.sortingLayerID;


            warningRing.sortingOrder =
                visualRenderer.sortingOrder
                + 2;
        }
    }


    private void DrawRing(
        float radius,
        Color color)
    {
        if (warningRing == null)
            return;


        warningRing.startColor =
            color;


        warningRing.endColor =
            color;


        Vector2 center =
            transform.position;


        for (int i = 0;
             i < ringSegments;
             i++)
        {
            float angle =
                Mathf.PI
                * 2f
                * i
                / ringSegments;


            Vector2 point =
                center
                + new Vector2(
                    Mathf.Cos(angle),
                    Mathf.Sin(angle)
                )
                * radius;


            warningRing.SetPosition(
                i,
                point
            );
        }
    }


    // ==================================================
    // Color
    // ==================================================

    private Color GetWarningColor()
    {
        if (InkMap.Instance != null)
        {
            Color color =
                InkMap.Instance.enemyInkColor;


            // 바닥 Ink보다 진하게
            color =
                Color.Lerp(
                    color,
                    Color.black,
                    0.30f
                );


            color.a =
                1f;


            return color;
        }


        return new Color(
            0.75f,
            0.05f,
            0.20f,
            1f
        );
    }
}