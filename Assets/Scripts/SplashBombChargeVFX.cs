using System.Collections.Generic;
using UnityEngine;

public class SplashBombChargeVFX : MonoBehaviour
{
    // ==================================================
    // References
    // ==================================================

    [Header("References")]

    public SplashBombWeaponBehaviour
        splashBombWeaponBehaviour;


    [Tooltip("입자가 모일 중심점")]
    public Transform chargePoint;


    [Tooltip("Material / Sorting 기준")]
    public SpriteRenderer referenceRenderer;


    // ==================================================
    // Gathering Particles
    // ==================================================

    [Header("Gathering Particles")]

    public int particleCount =
        7;


    [Tooltip("입자가 처음 나타나는 거리")]
    public float outerRadius =
        0.65f;


    [Tooltip("중심에서 사라지는 거리")]
    public float innerRadius =
        0.08f;


    public float particleMinSize =
        0.025f;


    public float particleMaxSize =
        0.055f;


    [Tooltip("입자가 중심으로 빨려 들어오는 기본 속도")]
    public float gatherSpeed =
        0.75f;


    [Tooltip("충전 완료에 가까울수록 추가되는 속도")]
    public float chargedSpeedBonus =
        0.55f;


    [Tooltip("입자가 직선이 아니라 살짝 회전하며 들어옴")]
    public float swirlAmount =
        55f;


    // ==================================================
    // Center Ring
    // ==================================================

    [Header("Center Ring")]

    public int ringSegments =
        28;


    public float ringRadius =
        0.16f;


    public float ringMaxRadius =
        0.21f;


    public float ringWidth =
        0.025f;


    public float ringPulseSpeed =
        7f;


    // ==================================================
    // Rendering
    // ==================================================

    [Header("Rendering")]

    public int particleSortingOffset =
        2;


    public int ringSortingOffset =
        1;


    // ==================================================
    // Runtime
    // ==================================================

    private GameObject runtimeRoot;

    private LineRenderer centerRing;


    private readonly List<ChargeParticle>
        particles =
        new List<ChargeParticle>();


    private bool wasCharging =
        false;


    private Color playerColor;

    private Color brightPlayerColor;


    // ==================================================
    // Particle Data
    // ==================================================

    private class ChargeParticle
    {
        public LineRenderer line;

        public float progress;

        public float speedMultiplier;

        public float startAngle;

        public float size;
    }


    // ==================================================
    // Awake
    // ==================================================

    private void Awake()
    {
        if (splashBombWeaponBehaviour == null)
        {
            splashBombWeaponBehaviour =
                transform.root
                    .GetComponentInChildren<
                        SplashBombWeaponBehaviour
                    >(
                        true
                    );
        }


        if (chargePoint == null)
        {
            chargePoint =
                transform;
        }


        if (referenceRenderer == null)
        {
            referenceRenderer =
                GetComponentInChildren<
                    SpriteRenderer
                >(
                    true
                );
        }


        CreateRuntimeVFX();


        SetVFXVisible(
            false
        );
    }


    // ==================================================
    // Start
    // ==================================================

    private void Start()
    {
        UpdateColors();
    }


    // ==================================================
    // Update
    // ==================================================

    private void Update()
    {
        if (splashBombWeaponBehaviour == null ||
            chargePoint == null)
        {
            return;
        }


        bool charging =
            splashBombWeaponBehaviour
                .IsCharging;


        // ==========================================
        // Charge Start
        // ==========================================

        if (charging &&
            !wasCharging)
        {
            BeginChargeVFX();
        }


        // ==========================================
        // Charge End
        // ==========================================

        if (!charging &&
            wasCharging)
        {
            EndChargeVFX();
        }


        wasCharging =
            charging;


        if (!charging)
        {
            return;
        }


        // 실제 SplashBomb의 충전량을 그대로 사용.
        float chargePercent =
            splashBombWeaponBehaviour
                .Charge01;


        UpdateCenterRing(
            chargePercent
        );


        UpdateParticles(
            chargePercent
        );
    }


    // ==================================================
    // Charge Start
    // ==================================================

    private void BeginChargeVFX()
    {
        UpdateColors();


        ResetParticles();


        SetVFXVisible(
            true
        );
    }


    // ==================================================
    // Charge End
    // ==================================================

    private void EndChargeVFX()
    {
        SetVFXVisible(
            false
        );
    }


    // ==================================================
    // Create Runtime VFX
    // ==================================================

    private void CreateRuntimeVFX()
    {
        runtimeRoot =
            new GameObject(
                "Runtime_SplashBombChargeVFX"
            );


        runtimeRoot.transform.SetParent(
            chargePoint,
            false
        );


        runtimeRoot.transform.localPosition =
            Vector3.zero;


        CreateCenterRing();

        CreateParticles();
    }


    // ==================================================
    // Center Ring
    // ==================================================

    private void CreateCenterRing()
    {
        GameObject ringObject =
            new GameObject(
                "ChargeRing"
            );


        ringObject.transform.SetParent(
            runtimeRoot.transform,
            false
        );


        centerRing =
            ringObject.AddComponent<
                LineRenderer
            >();


        centerRing.useWorldSpace =
            false;


        centerRing.loop =
            true;


        centerRing.positionCount =
            Mathf.Max(
                12,
                ringSegments
            );


        centerRing.numCornerVertices =
            3;


        centerRing.startWidth =
            ringWidth;


        centerRing.endWidth =
            ringWidth;


        if (referenceRenderer != null)
        {
            centerRing.sharedMaterial =
                referenceRenderer
                    .sharedMaterial;


            centerRing.sortingLayerID =
                referenceRenderer
                    .sortingLayerID;


            centerRing.sortingOrder =
                referenceRenderer
                    .sortingOrder
                +
                ringSortingOffset;
        }
    }


    // ==================================================
    // Create Particles
    // ==================================================

    private void CreateParticles()
    {
        int safeCount =
            Mathf.Max(
                1,
                particleCount
            );


        for (int i = 0;
             i < safeCount;
             i++)
        {
            GameObject particleObject =
                new GameObject(
                    "GatherParticle_"
                    +
                    i
                );


            particleObject.transform.SetParent(
                runtimeRoot.transform,
                false
            );


            LineRenderer line =
                particleObject.AddComponent<
                    LineRenderer
                >();


            line.useWorldSpace =
                false;


            line.loop =
                true;


            line.positionCount =
                7;


            line.numCornerVertices =
                2;


            if (referenceRenderer != null)
            {
                line.sharedMaterial =
                    referenceRenderer
                        .sharedMaterial;


                line.sortingLayerID =
                    referenceRenderer
                        .sortingLayerID;


                line.sortingOrder =
                    referenceRenderer
                        .sortingOrder
                    +
                    particleSortingOffset;
            }


            ChargeParticle particle =
                new ChargeParticle();


            particle.line =
                line;


            particle.progress =
                Random.value;


            particle.speedMultiplier =
                Random.Range(
                    0.75f,
                    1.25f
                );


            particle.startAngle =
                Random.Range(
                    0f,
                    360f
                );


            particle.size =
                Random.Range(
                    particleMinSize,
                    particleMaxSize
                );


            particles.Add(
                particle
            );
        }
    }


    // ==================================================
    // Reset Particles
    // ==================================================

    private void ResetParticles()
    {
        foreach (
            ChargeParticle particle
            in particles
        )
        {
            particle.progress =
                Random.Range(
                    0f,
                    0.8f
                );


            particle.startAngle =
                Random.Range(
                    0f,
                    360f
                );
        }
    }


    // ==================================================
    // Update Particles
    // ==================================================

    private void UpdateParticles(
        float chargePercent
    )
    {
        foreach (
            ChargeParticle particle
            in particles
        )
        {
            if (particle.line == null)
            {
                continue;
            }


            float currentSpeed =
                gatherSpeed
                +
                chargedSpeedBonus
                *
                chargePercent;


            particle.progress +=
                Time.deltaTime
                *
                currentSpeed
                *
                particle.speedMultiplier;


            if (particle.progress >= 1f)
            {
                particle.progress -=
                    1f;


                particle.startAngle =
                    Random.Range(
                        0f,
                        360f
                    );


                particle.size =
                    Random.Range(
                        particleMinSize,
                        particleMaxSize
                    );
            }


            float easedProgress =
                EaseInCubic(
                    particle.progress
                );


            float radius =
                Mathf.Lerp(
                    outerRadius,
                    innerRadius,
                    easedProgress
                );


            float angle =
                particle.startAngle
                +
                particle.progress
                *
                swirlAmount;


            float radians =
                angle
                *
                Mathf.Deg2Rad;


            Vector2 center =
                new Vector2(
                    Mathf.Cos(
                        radians
                    ),
                    Mathf.Sin(
                        radians
                    )
                )
                *
                radius;


            float size =
                particle.size
                *
                Mathf.Lerp(
                    1f,
                    0.25f,
                    particle.progress
                );


            Color color =
                brightPlayerColor;


            float alpha =
                Mathf.Sin(
                    particle.progress
                    *
                    Mathf.PI
                );


            alpha *=
                Mathf.Lerp(
                    0.35f,
                    0.70f,
                    chargePercent
                );


            color.a =
                alpha;


            DrawParticle(
                particle.line,
                center,
                size,
                color
            );
        }
    }


    // ==================================================
    // Draw Particle
    // ==================================================

    private void DrawParticle(
        LineRenderer line,
        Vector2 center,
        float size,
        Color color
    )
    {
        if (line == null)
        {
            return;
        }


        int segments =
            Mathf.Max(
                6,
                line.positionCount
            );


        line.positionCount =
            segments;


        line.startWidth =
            Mathf.Max(
                size
                *
                0.45f,
                0.008f
            );


        line.endWidth =
            line.startWidth;


        line.startColor =
            color;


        line.endColor =
            color;


        for (int i = 0;
             i < segments;
             i++)
        {
            float angle =
                Mathf.PI
                *
                2f
                *
                i
                /
                segments;


            Vector2 point =
                center
                +
                new Vector2(
                    Mathf.Cos(angle)
                        *
                        size,

                    Mathf.Sin(angle)
                        *
                        size
                        *
                        0.65f
                );


            line.SetPosition(
                i,
                point
            );
        }
    }


    // ==================================================
    // Center Ring Update
    // ==================================================

    private void UpdateCenterRing(
        float chargePercent
    )
    {
        if (centerRing == null)
        {
            return;
        }


        float pulse =
            Mathf.Sin(
                Time.time
                *
                ringPulseSpeed
            );


        pulse =
            pulse
            *
            0.5f
            +
            0.5f;


        float radius =
            Mathf.Lerp(
                ringRadius,
                ringMaxRadius,
                chargePercent
            );


        radius +=
            pulse
            *
            0.015f;


        Color color =
            playerColor;


        color =
            Color.Lerp(
                color,
                Color.white,
                chargePercent
                *
                0.20f
            );


        color.a =
            Mathf.Lerp(
                0.22f,
                0.55f,
                chargePercent
            )
            *
            Mathf.Lerp(
                0.8f,
                1f,
                pulse
            );


        centerRing.startColor =
            color;


        centerRing.endColor =
            color;


        centerRing.startWidth =
            ringWidth;


        centerRing.endWidth =
            ringWidth;


        int segments =
            Mathf.Max(
                12,
                ringSegments
            );


        centerRing.positionCount =
            segments;


        for (int i = 0;
             i < segments;
             i++)
        {
            float angle =
                Mathf.PI
                *
                2f
                *
                i
                /
                segments;


            Vector2 point =
                new Vector2(
                    Mathf.Cos(angle)
                        *
                        radius,

                    Mathf.Sin(angle)
                        *
                        radius
                );


            centerRing.SetPosition(
                i,
                point
            );
        }
    }


    // ==================================================
    // Colors
    // ==================================================

    private void UpdateColors()
    {
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


        brightPlayerColor =
            Color.Lerp(
                playerColor,
                Color.white,
                0.25f
            );


        brightPlayerColor.a =
            1f;
    }


    // ==================================================
    // Visibility
    // ==================================================

    private void SetVFXVisible(
        bool visible
    )
    {
        if (runtimeRoot != null)
        {
            runtimeRoot.SetActive(
                visible
            );
        }
    }


    // ==================================================
    // Ease
    // ==================================================

    private float EaseInCubic(
        float t
    )
    {
        return
            t
            *
            t
            *
            t;
    }
}