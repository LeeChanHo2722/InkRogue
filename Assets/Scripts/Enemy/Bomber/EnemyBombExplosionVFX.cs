using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyBombExplosionVFX : MonoBehaviour
{
    // ==================================================
    // Runtime
    // ==================================================

    private Material sharedMaterial;

    private Color effectColor;

    private float explosionRadius;


    private LineRenderer innerRing;

    private LineRenderer outerRing;

    private LineRenderer flashRing;


    private readonly List<Droplet>
        droplets =
            new List<Droplet>();


    // ==================================================
    // Settings
    // ==================================================

    private int ringSegments = 36;

    private float effectDuration = 0.32f;


    // ==================================================
    // Droplet
    // ==================================================

    private class Droplet
    {
        public LineRenderer line;

        public Vector2 direction;

        public float distance;

        public float size;

        public float rotation;
    }


    // ==================================================
    // Initialize
    // ==================================================

    public void Initialize(
        Color color,
        float radius,
        Material material)
    {
        // ==========================================
        // 바닥 Enemy Ink보다 진한 색 사용
        // ==========================================

        effectColor =
            Color.Lerp(
                color,
                Color.black,
                0.35f
            );


        effectColor.a =
            1f;


        explosionRadius =
            radius;


        sharedMaterial =
            material;


        CreateRings();

        CreateDroplets();


        StartCoroutine(
            PlayRoutine()
        );
    }


    // ==================================================
    // Create Rings
    // ==================================================

    private void CreateRings()
    {
        flashRing =
            CreateRing(
                "FlashRing",
                10
            );


        innerRing =
            CreateRing(
                "InnerImpactRing",
                9
            );


        outerRing =
            CreateRing(
                "OuterImpactRing",
                8
            );
    }


    private LineRenderer CreateRing(
        string objectName,
        int sortingOrder)
    {
        GameObject ringObject =
            new GameObject(
                objectName
            );


        ringObject.transform.SetParent(
            transform,
            false
        );


        LineRenderer line =
            ringObject
                .AddComponent<LineRenderer>();


        line.useWorldSpace =
            true;


        line.loop =
            true;


        line.positionCount =
            ringSegments;


        line.numCornerVertices =
            4;


        line.numCapVertices =
            4;


        line.sortingOrder =
            sortingOrder;


        if (sharedMaterial != null)
        {
            line.sharedMaterial =
                sharedMaterial;
        }


        return line;
    }


    // ==================================================
    // Droplets
    // ==================================================

    private void CreateDroplets()
    {
        int dropletCount =
            10;


        for (int i = 0;
             i < dropletCount;
             i++)
        {
            GameObject objectRoot =
                new GameObject(
                    "InkDroplet_"
                    + i
                );


            objectRoot.transform.SetParent(
                transform,
                false
            );


            LineRenderer line =
                objectRoot
                    .AddComponent<LineRenderer>();


            line.useWorldSpace =
                true;


            line.loop =
                true;


            line.positionCount =
                8;


            line.numCornerVertices =
                2;


            line.sortingOrder =
                11;


            if (sharedMaterial != null)
            {
                line.sharedMaterial =
                    sharedMaterial;
            }


            Vector2 direction =
                Random.insideUnitCircle;


            if (direction.sqrMagnitude <
                0.001f)
            {
                direction =
                    Vector2.right;
            }


            direction.Normalize();


            Droplet droplet =
                new Droplet();


            droplet.line =
                line;


            droplet.direction =
                direction;


            droplet.distance =
                Random.Range(
                    explosionRadius * 0.45f,
                    explosionRadius * 1.0f
                );


            droplet.size =
                Random.Range(
                    0.05f,
                    0.12f
                );


            droplet.rotation =
                Random.Range(
                    0f,
                    360f
                );


            droplets.Add(
                droplet
            );
        }
    }


    // ==================================================
    // Play
    // ==================================================

    private IEnumerator PlayRoutine()
    {
        float timer =
            0f;


        float safeDuration =
            Mathf.Max(
                effectDuration,
                0.01f
            );


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
            // Flash
            // ======================================

            float flashT =
                Mathf.Clamp01(
                    t / 0.30f
                );


            float flashRadius =
                Mathf.Lerp(
                    explosionRadius * 0.10f,
                    explosionRadius * 0.55f,
                    flashT
                );


            Color flashColor =
                Color.white;


            flashColor.a =
                1f - flashT;


            DrawRing(
                flashRing,
                flashRadius,
                Mathf.Lerp(
                    0.20f,
                    0.02f,
                    flashT
                ),
                flashColor
            );


            // ======================================
            // Inner Ring
            // ======================================

            float innerT =
                EaseOutCubic(
                    t
                );


            float innerRadius =
                Mathf.Lerp(
                    explosionRadius * 0.15f,
                    explosionRadius * 0.75f,
                    innerT
                );


            Color innerColor =
                effectColor;


            innerColor.a =
                1f - t;


            DrawRing(
                innerRing,
                innerRadius,
                Mathf.Lerp(
                    0.14f,
                    0.025f,
                    t
                ),
                innerColor
            );


            // ======================================
            // Outer Ring
            // ======================================

            float outerDelay =
                0.08f;


            float outerT =
                Mathf.InverseLerp(
                    outerDelay,
                    1f,
                    t
                );


            outerT =
                Mathf.Clamp01(
                    outerT
                );


            float easedOuter =
                EaseOutCubic(
                    outerT
                );


            float outerRadius =
                Mathf.Lerp(
                    explosionRadius * 0.18f,
                    explosionRadius,
                    easedOuter
                );


            Color outerColor =
                effectColor;


            outerColor.a =
                (1f - outerT)
                * 0.75f;


            DrawRing(
                outerRing,
                outerRadius,
                Mathf.Lerp(
                    0.09f,
                    0.015f,
                    outerT
                ),
                outerColor
            );


            // ======================================
            // Ink Droplets
            // ======================================

            UpdateDroplets(
                t
            );


            yield return null;
        }


        Destroy(
            gameObject
        );
    }


    // ==================================================
    // Droplets Update
    // ==================================================

    private void UpdateDroplets(
        float progress)
    {
        float eased =
            EaseOutCubic(
                progress
            );


        Vector2 center =
            transform.position;


        foreach (Droplet droplet
                 in droplets)
        {
            if (droplet.line == null)
                continue;


            Vector2 position =
                center
                + droplet.direction
                * droplet.distance
                * eased;


            float size =
                droplet.size
                * Mathf.Lerp(
                    1.3f,
                    0.25f,
                    progress
                );


            Color color =
                effectColor;


            color.a =
                1f - progress;


            DrawDroplet(
                droplet.line,
                position,
                size,
                droplet.rotation
                + progress * 180f,
                color
            );
        }
    }


    // ==================================================
    // Draw Ring
    // ==================================================

    private void DrawRing(
        LineRenderer line,
        float radius,
        float width,
        Color color)
    {
        if (line == null)
            return;


        Vector2 center =
            transform.position;


        line.startWidth =
            width;


        line.endWidth =
            width;


        line.startColor =
            color;


        line.endColor =
            color;


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


            line.SetPosition(
                i,
                point
            );
        }
    }


    // ==================================================
    // Draw Droplet
    // ==================================================

    private void DrawDroplet(
        LineRenderer line,
        Vector2 center,
        float size,
        float rotationDegrees,
        Color color)
    {
        int segments =
            line.positionCount;


        line.startWidth =
            Mathf.Max(
                size * 0.55f,
                0.01f
            );


        line.endWidth =
            line.startWidth;


        line.startColor =
            color;


        line.endColor =
            color;


        float rotation =
            rotationDegrees
            * Mathf.Deg2Rad;


        for (int i = 0;
             i < segments;
             i++)
        {
            float angle =
                Mathf.PI
                * 2f
                * i
                / segments
                + rotation;


            // 완벽한 원보다는
            // 잉크 조각 느낌의 타원
            Vector2 point =
                center
                + new Vector2(
                    Mathf.Cos(angle)
                    * size,
                    Mathf.Sin(angle)
                    * size
                    * 0.65f
                );


            line.SetPosition(
                i,
                point
            );
        }
    }


    // ==================================================
    // Ease
    // ==================================================

    private float EaseOutCubic(
        float t)
    {
        float inverse =
            1f - t;


        return
            1f
            - inverse
            * inverse
            * inverse;
    }
}