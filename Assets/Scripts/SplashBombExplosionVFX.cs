using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SplashBombExplosionVFX : MonoBehaviour
{
    // ==================================================
    // Settings
    // ==================================================

    [Header("Duration")]

    public float effectDuration = 0.38f;


    [Header("Rings")]

    public int ringSegments = 40;

    public float outerRingStartWidth = 0.15f;

    public float innerRingStartWidth = 0.20f;


    [Header("Burst Rays")]

    public int burstRayCount = 10;

    public float burstRayMinLength = 0.35f;

    public float burstRayMaxLength = 1.0f;

    public float burstRayWidth = 0.07f;


    [Header("Ink Droplets")]

    public int dropletCount = 14;

    public float dropletMinSize = 0.06f;

    public float dropletMaxSize = 0.14f;


    [Header("Camera Shake")]

    public float shakeDuration = 0.16f;

    public float shakeStrength = 0.16f;


    // ==================================================
    // Runtime
    // ==================================================

    private float explosionRadius;

    private Color playerColor;

    private Color darkPlayerColor;

    private Material sharedMaterial;


    private LineRenderer flashRing;

    private LineRenderer innerRing;

    private LineRenderer outerRing;


    private readonly List<BurstRay>
        burstRays =
            new List<BurstRay>();


    private readonly List<InkDroplet>
        droplets =
            new List<InkDroplet>();


    // ==================================================
    // Runtime Classes
    // ==================================================

    private class BurstRay
    {
        public LineRenderer line;

        public Vector2 direction;

        public float length;
    }


    private class InkDroplet
    {
        public LineRenderer line;

        public Vector2 direction;

        public float travelDistance;

        public float size;

        public float rotation;
    }


    // ==================================================
    // Initialize
    // ==================================================

    public void Initialize(
        float radius,
        Material material)
    {
        explosionRadius =
            Mathf.Max(
                radius,
                0.1f
            );


        sharedMaterial =
            material;


        // ==========================================
        // Player Ink Color
        // ==========================================

        if (InkMap.Instance != null)
        {
            playerColor =
                InkMap.Instance
                    .playerInkColor;
        }
        else
        {
            playerColor =
                new Color(
                    0.1f,
                    0.55f,
                    1f,
                    1f
                );
        }


        playerColor.a =
            1f;


        // 바닥 Ink와 구분되는
        // 더 진한 파란색
        darkPlayerColor =
            Color.Lerp(
                playerColor,
                Color.black,
                0.25f
            );


        darkPlayerColor.a =
            1f;


        CreateRings();

        CreateBurstRays();

        CreateDroplets();

        StartCameraShake();


        StartCoroutine(
            PlayRoutine()
        );
    }


    // ==================================================
    // Rings
    // ==================================================

    private void CreateRings()
    {
        flashRing =
            CreateRing(
                "FlashRing",
                13
            );


        innerRing =
            CreateRing(
                "InnerShockwave",
                12
            );


        outerRing =
            CreateRing(
                "OuterShockwave",
                11
            );
    }


    private LineRenderer CreateRing(
        string objectName,
        int sortingOrder)
    {
        GameObject objectRoot =
            new GameObject(
                objectName
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
            Mathf.Max(
                12,
                ringSegments
            );


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
    // Burst Rays
    // ==================================================

    private void CreateBurstRays()
    {
        int safeCount =
            Mathf.Max(
                1,
                burstRayCount
            );


        float randomOffset =
            Random.Range(
                0f,
                360f
            );


        for (int i = 0;
             i < safeCount;
             i++)
        {
            float angle =
                randomOffset
                + 360f
                * i
                / safeCount;


            float radians =
                angle
                * Mathf.Deg2Rad;


            Vector2 direction =
                new Vector2(
                    Mathf.Cos(radians),
                    Mathf.Sin(radians)
                );


            GameObject rayObject =
                new GameObject(
                    "BurstRay_"
                    + i
                );


            rayObject.transform.SetParent(
                transform,
                false
            );


            LineRenderer line =
                rayObject
                    .AddComponent<LineRenderer>();


            line.useWorldSpace =
                true;


            line.positionCount =
                2;


            line.numCapVertices =
                4;


            line.sortingOrder =
                14;


            if (sharedMaterial != null)
            {
                line.sharedMaterial =
                    sharedMaterial;
            }


            BurstRay ray =
                new BurstRay();


            ray.line =
                line;


            ray.direction =
                direction;


            ray.length =
                Random.Range(
                    burstRayMinLength,
                    burstRayMaxLength
                )
                * explosionRadius;


            burstRays.Add(
                ray
            );
        }
    }


    // ==================================================
    // Droplets
    // ==================================================

    private void CreateDroplets()
    {
        for (int i = 0;
             i < dropletCount;
             i++)
        {
            GameObject dropletObject =
                new GameObject(
                    "InkDroplet_"
                    + i
                );


            dropletObject.transform.SetParent(
                transform,
                false
            );


            LineRenderer line =
                dropletObject
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
                15;


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


            InkDroplet droplet =
                new InkDroplet();


            droplet.line =
                line;


            droplet.direction =
                direction;


            droplet.travelDistance =
                Random.Range(
                    explosionRadius * 0.45f,
                    explosionRadius * 1.15f
                );


            droplet.size =
                Random.Range(
                    dropletMinSize,
                    dropletMaxSize
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


            UpdateFlash(
                t
            );


            UpdateShockwaves(
                t
            );


            UpdateBurstRays(
                t
            );


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
    // Flash
    // ==================================================

    private void UpdateFlash(
        float progress)
    {
        float flashProgress =
            Mathf.Clamp01(
                progress
                / 0.25f
            );


        float radius =
            Mathf.Lerp(
                explosionRadius * 0.05f,
                explosionRadius * 0.55f,
                EaseOutCubic(
                    flashProgress
                )
            );


        Color color =
            Color.white;


        color.a =
            1f
            - flashProgress;


        DrawRing(
            flashRing,
            radius,
            Mathf.Lerp(
                0.28f,
                0.015f,
                flashProgress
            ),
            color
        );
    }


    // ==================================================
    // Shockwaves
    // ==================================================

    private void UpdateShockwaves(
        float progress)
    {
        // ==========================================
        // Inner
        // ==========================================

        float innerProgress =
            EaseOutCubic(
                progress
            );


        float innerRadius =
            Mathf.Lerp(
                explosionRadius * 0.10f,
                explosionRadius * 0.72f,
                innerProgress
            );


        Color innerColor =
            playerColor;


        innerColor.a =
            (1f - progress)
            * 0.95f;


        DrawRing(
            innerRing,
            innerRadius,
            Mathf.Lerp(
                innerRingStartWidth,
                0.02f,
                progress
            ),
            innerColor
        );


        // ==========================================
        // Outer
        // ==========================================

        float delayedProgress =
            Mathf.InverseLerp(
                0.08f,
                1f,
                progress
            );


        delayedProgress =
            Mathf.Clamp01(
                delayedProgress
            );


        float outerRadius =
            Mathf.Lerp(
                explosionRadius * 0.18f,
                explosionRadius,
                EaseOutCubic(
                    delayedProgress
                )
            );


        Color outerColor =
            darkPlayerColor;


        outerColor.a =
            (1f - delayedProgress)
            * 0.85f;


        DrawRing(
            outerRing,
            outerRadius,
            Mathf.Lerp(
                outerRingStartWidth,
                0.015f,
                delayedProgress
            ),
            outerColor
        );
    }


    // ==================================================
    // Burst Rays
    // ==================================================

    private void UpdateBurstRays(
        float progress)
    {
        // Burst는 초반에만 강하게
        float rayProgress =
            Mathf.Clamp01(
                progress
                / 0.55f
            );


        Vector2 center =
            transform.position;


        foreach (
            BurstRay ray
            in burstRays)
        {
            if (ray.line == null)
                continue;


            float length =
                ray.length
                * EaseOutCubic(
                    rayProgress
                );


            Vector2 start =
                center
                + ray.direction
                * explosionRadius
                * 0.08f;


            Vector2 end =
                center
                + ray.direction
                * length;


            Color color =
                Color.white;


            color =
                Color.Lerp(
                    color,
                    playerColor,
                    rayProgress
                );


            color.a =
                1f
                - rayProgress;


            ray.line.startWidth =
                Mathf.Lerp(
                    burstRayWidth,
                    0.01f,
                    rayProgress
                );


            ray.line.endWidth =
                ray.line.startWidth
                * 0.25f;


            ray.line.startColor =
                color;


            ray.line.endColor =
                color;


            ray.line.SetPosition(
                0,
                start
            );


            ray.line.SetPosition(
                1,
                end
            );
        }
    }


    // ==================================================
    // Droplets
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


        foreach (
            InkDroplet droplet
            in droplets)
        {
            if (droplet.line == null)
                continue;


            Vector2 position =
                center
                + droplet.direction
                * droplet.travelDistance
                * eased;


            float size =
                droplet.size
                * Mathf.Lerp(
                    1.3f,
                    0.15f,
                    progress
                );


            Color color =
                darkPlayerColor;


            color.a =
                1f
                - progress;


            DrawDroplet(
                droplet.line,
                position,
                size,
                droplet.rotation
                + progress
                * 220f,
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


        int safeSegments =
            Mathf.Max(
                12,
                ringSegments
            );


        line.positionCount =
            safeSegments;


        line.startWidth =
            width;


        line.endWidth =
            width;


        line.startColor =
            color;


        line.endColor =
            color;


        Vector2 center =
            transform.position;


        for (int i = 0;
             i < safeSegments;
             i++)
        {
            float angle =
                Mathf.PI
                * 2f
                * i
                / safeSegments;


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
                size * 0.5f,
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


            Vector2 point =
                center
                + new Vector2(
                    Mathf.Cos(angle)
                    * size,

                    Mathf.Sin(angle)
                    * size
                    * 0.60f
                );


            line.SetPosition(
                i,
                point
            );
        }
    }


    // ==================================================
    // Camera Shake
    // ==================================================

    private void StartCameraShake()
    {
        Camera mainCamera =
            Camera.main;


        if (mainCamera == null)
            return;


        CameraFollow cameraFollow =
            mainCamera.GetComponent<
                CameraFollow
            >();


        if (cameraFollow == null)
            return;


        cameraFollow.StartShake(
            shakeDuration,
            shakeStrength
        );
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