using System;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RawImage))]
public class DamageInkOverlay : MonoBehaviour
{
    [Header("References")]
    public PlayerShield playerShield;


    [Header("Texture")]
    public int textureWidth = 256;
    public int textureHeight = 144;

    [Tooltip("초당 Texture 갱신 횟수")]
    public float updateRate = 20f;


    [Header("Enemy Ink")]
    public Color enemyInkColor =
        new Color(
            1f,
            0.12f,
            0.30f,
            0.75f
        );


    [Header("Ink Growth")]
    [Range(0.05f, 0.35f)]
    [Tooltip("Shield가 완전히 깨졌을 때 화면 안쪽으로 들어오는 최대 깊이")]
    public float maxDepth = 0.20f;

    [Range(0.5f, 3f)]
    [Tooltip("높을수록 초반 피해에서는 Ink가 적고 후반에 빠르게 증가")]
    public float damageDepthCurve = 1.35f;


    [Header("Irregular Edge")]
    [Range(0f, 0.8f)]
    [Tooltip("큰 굴곡")]
    public float primaryRoughness = 0.45f;

    [Range(0f, 0.4f)]
    [Tooltip("작은 굴곡")]
    public float secondaryRoughness = 0.16f;

    public float primaryNoiseScale = 4f;
    public float secondaryNoiseScale = 11f;

    [Range(0.001f, 0.04f)]
    public float feather = 0.012f;

    public int seed = 17;


    [Header("Detached Splats")]
    [Tooltip("경계보다 안쪽에 따로 나타나는 Ink 방울")]
    public int splatCount = 16;

    public float splatMinRadius = 0.012f;
    public float splatMaxRadius = 0.040f;

    [Range(0f, 1f)]
    public float splatMinDamage = 0.18f;

    [Range(0f, 1f)]
    public float splatMaxDamage = 0.75f;


    [Header("Animation")]
    public float followSpeed = 7f;

    [Range(0f, 0.08f)]
    public float hitPulseAmount = 0.025f;

    public float hitPulseDuration = 0.18f;

    public float restoreSpeed = 18f;


    private RawImage rawImage;

    private Texture2D texture;
    private Color32[] pixels;

    private float[] noiseMap;
    private float[] splatThresholdMap;


    private float visualDamage = 0f;

    private float hitPulseTimer = 0f;
    private float fastRestoreTimer = 0f;

    private float textureUpdateTimer = 0f;


    private void Awake()
    {
        rawImage =
            GetComponent<RawImage>();


        if (playerShield == null)
        {
            playerShield =
                FindFirstObjectByType<PlayerShield>();
        }


        CreateTexture();

        GenerateNoiseMap();

        GenerateSplatMap();


        rawImage.texture =
            texture;

        rawImage.color =
            Color.white;

        rawImage.raycastTarget =
            false;

        rawImage.enabled =
            false;


        RenderTexture(0f);
    }


    private void OnEnable()
    {
        if (playerShield == null)
        {
            playerShield =
                FindFirstObjectByType<PlayerShield>();
        }


        if (playerShield == null)
            return;


        playerShield.ShieldHit +=
            OnShieldHit;

        playerShield.ShieldRestored +=
            OnShieldRestored;
    }


    private void OnDisable()
    {
        if (playerShield == null)
            return;


        playerShield.ShieldHit -=
            OnShieldHit;

        playerShield.ShieldRestored -=
            OnShieldRestored;
    }


    private void Update()
    {
        if (playerShield == null)
            return;


        UpdateDamageValue();


        bool shouldShow =
            playerShield.IsEmergency
            ||
            visualDamage > 0.002f
            ||
            hitPulseTimer > 0f;


        if (!shouldShow)
        {
            visualDamage =
                0f;

            rawImage.enabled =
                false;

            return;
        }


        rawImage.enabled =
            true;


        UpdateTextureWhenNeeded();
    }


    // ==================================================
    // Damage Value
    // ==================================================

    private void UpdateDamageValue()
    {
        float maxShield =
            Mathf.Max(
                playerShield.MaxShield,
                0.01f
            );


        float targetDamage =
            1f -
            Mathf.Clamp01(
                playerShield.CurrentShield
                / maxShield
            );


        if (playerShield.IsEmergency)
        {
            targetDamage =
                1f;
        }


        // ==========================================
        // Hit Pulse
        // ==========================================

        float pulse =
            0f;


        if (hitPulseTimer > 0f)
        {
            hitPulseTimer -=
                Time.unscaledDeltaTime;


            float remaining01 =
                Mathf.Clamp01(
                    hitPulseTimer
                    /
                    Mathf.Max(
                        hitPulseDuration,
                        0.01f
                    )
                );


            float progress =
                1f - remaining01;


            pulse =
                Mathf.Sin(
                    progress * Mathf.PI
                )
                * hitPulseAmount;
        }


        targetDamage =
            Mathf.Clamp01(
                targetDamage
                + pulse
            );


        // ==========================================
        // Smooth Follow
        // ==========================================

        float speed =
            followSpeed;


        if (fastRestoreTimer > 0f)
        {
            fastRestoreTimer -=
                Time.unscaledDeltaTime;

            speed =
                restoreSpeed;
        }


        float follow =
            1f -
            Mathf.Exp(
                -speed
                * Time.unscaledDeltaTime
            );


        visualDamage =
            Mathf.Lerp(
                visualDamage,
                targetDamage,
                follow
            );
    }


    // ==================================================
    // Texture Update
    // ==================================================

    private void UpdateTextureWhenNeeded()
    {
        textureUpdateTimer -=
            Time.unscaledDeltaTime;


        if (textureUpdateTimer > 0f)
            return;


        textureUpdateTimer =
            1f /
            Mathf.Max(
                1f,
                updateRate
            );


        RenderTexture(
            visualDamage
        );
    }


    // ==================================================
    // Texture 생성
    // ==================================================

    private void CreateTexture()
    {
        textureWidth =
            Mathf.Max(
                64,
                textureWidth
            );


        textureHeight =
            Mathf.Max(
                64,
                textureHeight
            );


        texture =
            new Texture2D(
                textureWidth,
                textureHeight,
                TextureFormat.RGBA32,
                false
            );


        texture.name =
            "DamageInkFullScreenMask";


        texture.wrapMode =
            TextureWrapMode.Clamp;


        texture.filterMode =
            FilterMode.Bilinear;


        int pixelCount =
            textureWidth
            * textureHeight;


        pixels =
            new Color32[
                pixelCount
            ];


        noiseMap =
            new float[
                pixelCount
            ];


        splatThresholdMap =
            new float[
                pixelCount
            ];
    }


    // ==================================================
    // Noise
    // ==================================================

    private void GenerateNoiseMap()
    {
        for (int y = 0;
             y < textureHeight;
             y++)
        {
            for (int x = 0;
                 x < textureWidth;
                 x++)
            {
                float u =
                    (float)x /
                    (textureWidth - 1);


                float v =
                    (float)y /
                    (textureHeight - 1);


                float primary =
                    Mathf.PerlinNoise(
                        u
                        * primaryNoiseScale
                        + seed * 0.17f,

                        v
                        * primaryNoiseScale
                        + seed * 0.31f
                    );


                primary =
                    (primary - 0.5f)
                    * 2f;


                float secondary =
                    Mathf.PerlinNoise(
                        u
                        * secondaryNoiseScale
                        + seed * 0.71f,

                        v
                        * secondaryNoiseScale
                        + seed * 0.93f
                    );


                secondary =
                    (secondary - 0.5f)
                    * 2f;


                float noise =
                    primary
                    * primaryRoughness

                    + secondary
                    * secondaryRoughness;


                noiseMap[
                    y * textureWidth + x
                ] =
                    noise;
            }
        }
    }


    // ==================================================
    // Detached Splats
    // ==================================================

    private void GenerateSplatMap()
    {
        // 기본적으로 등장하지 않음
        for (int i = 0;
             i < splatThresholdMap.Length;
             i++)
        {
            splatThresholdMap[i] =
                2f;
        }


        System.Random random =
            new System.Random(
                seed + 583
            );


        for (int i = 0;
             i < splatCount;
             i++)
        {
            CreateSplat(
                random
            );
        }
    }


    private void CreateSplat(
        System.Random random)
    {
        int side =
            random.Next(
                0,
                4
            );


        float along =
            RandomRange(
                random,
                0.05f,
                0.95f
            );


        float inward =
            RandomRange(
                random,
                maxDepth * 0.45f,
                maxDepth * 1.15f
            );


        Vector2 center;


        switch (side)
        {
            case 0:
                center =
                    new Vector2(
                        along,
                        1f - inward
                    );
                break;


            case 1:
                center =
                    new Vector2(
                        along,
                        inward
                    );
                break;


            case 2:
                center =
                    new Vector2(
                        inward,
                        along
                    );
                break;


            default:
                center =
                    new Vector2(
                        1f - inward,
                        along
                    );
                break;
        }


        float radiusX =
            RandomRange(
                random,
                splatMinRadius,
                splatMaxRadius
            );


        float radiusY =
            RandomRange(
                random,
                splatMinRadius,
                splatMaxRadius
            );


        float activationDamage =
            RandomRange(
                random,
                splatMinDamage,
                splatMaxDamage
            );


        for (int y = 0;
             y < textureHeight;
             y++)
        {
            for (int x = 0;
                 x < textureWidth;
                 x++)
            {
                float u =
                    (float)x /
                    (textureWidth - 1);


                float v =
                    (float)y /
                    (textureHeight - 1);


                float dx =
                    (
                        u - center.x
                    )
                    /
                    Mathf.Max(
                        radiusX,
                        0.001f
                    );


                float dy =
                    (
                        v - center.y
                    )
                    /
                    Mathf.Max(
                        radiusY,
                        0.001f
                    );


                float distance =
                    dx * dx
                    + dy * dy;


                if (distance > 1f)
                    continue;


                float requiredDamage =
                    activationDamage
                    + distance * 0.12f;


                int index =
                    y * textureWidth + x;


                splatThresholdMap[index] =
                    Mathf.Min(
                        splatThresholdMap[index],
                        requiredDamage
                    );
            }
        }
    }


    // ==================================================
    // Render
    // ==================================================

    private void RenderTexture(
        float damage)
    {
        damage =
            Mathf.Clamp01(
                damage
            );


        float growth =
            Mathf.Pow(
                damage,
                damageDepthCurve
            );


        float baseCurrentDepth =
            maxDepth
            * growth;


        for (int y = 0;
             y < textureHeight;
             y++)
        {
            for (int x = 0;
                 x < textureWidth;
                 x++)
            {
                int index =
                    y * textureWidth + x;


                float u =
                    (float)x /
                    (textureWidth - 1);


                float v =
                    (float)y /
                    (textureHeight - 1);


                // ==================================
                // 화면 끝까지의 최소 거리
                // ==================================

                float edgeDistance =
                    Mathf.Min(
                        Mathf.Min(
                            u,
                            1f - u
                        ),

                        Mathf.Min(
                            v,
                            1f - v
                        )
                    );


                // ==================================
                // Ink 경계의 불규칙성
                // ==================================

                float noise =
                    noiseMap[index];


                float localDepth =
                    baseCurrentDepth
                    * (
                        1f + noise
                    );


                localDepth =
                    Mathf.Clamp(
                        localDepth,
                        0f,
                        0.48f
                    );


                // ==================================
                // Main Ink
                // ==================================

                float mainAlpha =
                    0f;


                if (damage > 0.0001f)
                {
                    mainAlpha =
                        1f -
                        SmoothThreshold(
                            localDepth
                            - feather,

                            localDepth
                            + feather,

                            edgeDistance
                        );
                }


                // ==================================
                // Detached Splat
                // ==================================

                float splatAlpha =
                    0f;


                float splatThreshold =
                    splatThresholdMap[index];


                if (splatThreshold <= 1f)
                {
                    splatAlpha =
                        SmoothThreshold(
                            splatThreshold
                            - 0.035f,

                            splatThreshold
                            + 0.035f,

                            damage
                        );
                }


                float finalAlpha =
                    Mathf.Max(
                        mainAlpha,
                        splatAlpha
                    );


                Color finalColor =
                    enemyInkColor;


                finalColor.a *=
                    finalAlpha;


                pixels[index] =
                    finalColor;
            }
        }


        texture.SetPixels32(
            pixels
        );


        texture.Apply(
            false,
            false
        );
    }


    // ==================================================
    // 진짜 Threshold SmoothStep
    //
    // Mathf.SmoothStep과 목적이 다름
    // ==================================================

    private float SmoothThreshold(
        float edge0,
        float edge1,
        float value)
    {
        float range =
            edge1 - edge0;


        if (Mathf.Abs(range) <
            0.00001f)
        {
            return value >= edge1
                ? 1f
                : 0f;
        }


        float t =
            Mathf.Clamp01(
                (
                    value - edge0
                )
                / range
            );


        // Hermite SmoothStep
        return
            t * t
            * (
                3f
                - 2f * t
            );
    }


    // ==================================================
    // Events
    // ==================================================

    private void OnShieldHit()
    {
        hitPulseTimer =
            hitPulseDuration;
    }


    private void OnShieldRestored()
    {
        fastRestoreTimer =
            0.5f;
    }


    // ==================================================
    // Helpers
    // ==================================================

    private float RandomRange(
        System.Random random,
        float min,
        float max)
    {
        return Mathf.Lerp(
            min,
            max,
            (float)random.NextDouble()
        );
    }


    private void OnDestroy()
    {
        if (texture != null)
        {
            Destroy(
                texture
            );
        }
    }
}