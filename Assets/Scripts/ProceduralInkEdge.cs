using System;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class ProceduralInkEdge : MonoBehaviour
{
    public enum EdgeSide
    {
        Top,
        Bottom,
        Left,
        Right
    }


    [Header("Direction")]
    public EdgeSide side = EdgeSide.Top;


    [Header("Texture")]
    public int longResolution = 320;
    public int shortResolution = 128;


    [Header("Main Ink Shape")]
    [Range(0.25f, 0.8f)]
    [Tooltip("기본 잉크 경계 위치")]
    public float baseDepth = 0.50f;

    [Range(0f, 0.3f)]
    public float primaryRoughness = 0.18f;

    [Range(0f, 0.2f)]
    public float secondaryRoughness = 0.06f;


    [Header("Ink Bulges")]
    [Tooltip("안쪽으로 크게 튀어나오는 잉크 덩어리 수")]
    public int splatterCount = 12;

    [Range(0f, 0.4f)]
    public float splatterDepth = 0.24f;

    public float splatterMinWidth = 0.015f;
    public float splatterMaxWidth = 0.065f;


    [Header("Ink Notches")]
    [Tooltip("잉크 경계가 움푹 들어가는 부분")]
    public int notchCount = 8;

    [Range(0f, 0.25f)]
    public float notchDepth = 0.12f;


    [Header("Detached Droplets")]
    [Tooltip("본체에서 떨어져 나온 작은 잉크 방울")]
    public int dropletCount = 9;


    [Header("Noise")]
    public float primaryNoiseScale = 3.2f;
    public float secondaryNoiseScale = 9f;

    public int seed = 11;


    [Header("Edge")]
    [Range(0.002f, 0.08f)]
    public float feather = 0.012f;


    private Image targetImage;

    private Texture2D generatedTexture;
    private Sprite generatedSprite;


    // 큰 잉크 돌출
    private float[] splatCenters;
    private float[] splatWidths;
    private float[] splatStrengths;

    // 움푹 들어가는 곳
    private float[] notchCenters;
    private float[] notchWidths;
    private float[] notchStrengths;

    // 떨어진 잉크 방울
    private float[] dropletX;
    private float[] dropletY;
    private float[] dropletRadiusX;
    private float[] dropletRadiusY;


    private void Awake()
    {
        targetImage =
            GetComponent<Image>();

        GenerateInkSprite();
    }


    private void OnEnable()
    {
        if (targetImage == null)
        {
            targetImage =
                GetComponent<Image>();
        }

        if (generatedSprite == null)
        {
            GenerateInkSprite();
        }
    }


    // ==================================================
    // Generate
    // ==================================================

    private void GenerateInkSprite()
    {
        CleanupGeneratedResources();

        CreateRandomShapeData();


        int width;
        int height;


        if (side == EdgeSide.Top ||
            side == EdgeSide.Bottom)
        {
            width =
                Mathf.Max(
                    64,
                    longResolution
                );

            height =
                Mathf.Max(
                    32,
                    shortResolution
                );
        }
        else
        {
            width =
                Mathf.Max(
                    32,
                    shortResolution
                );

            height =
                Mathf.Max(
                    64,
                    longResolution
                );
        }


        generatedTexture =
            new Texture2D(
                width,
                height,
                TextureFormat.RGBA32,
                false
            );


        generatedTexture.name =
            "GeneratedInkEdge_"
            + side;


        generatedTexture.wrapMode =
            TextureWrapMode.Clamp;


        generatedTexture.filterMode =
            FilterMode.Bilinear;


        Color32[] pixels =
            new Color32[
                width * height
            ];


        for (int y = 0;
             y < height;
             y++)
        {
            for (int x = 0;
                 x < width;
                 x++)
            {
                float u =
                    (float)x /
                    (width - 1);


                float v =
                    (float)y /
                    (height - 1);


                float tangent =
                    GetTangent(
                        u,
                        v
                    );


                float inward =
                    GetInwardDistance(
                        u,
                        v
                    );


                // ==================================
                // 기본 울퉁불퉁함
                // ==================================

                float primary =
                    Mathf.PerlinNoise(
                        tangent
                        * primaryNoiseScale
                        + seed * 0.17f,

                        seed * 0.31f
                    );


                primary =
                    (primary - 0.5f)
                    * 2f;


                float secondary =
                    Mathf.PerlinNoise(
                        tangent
                        * secondaryNoiseScale
                        + seed * 0.71f,

                        seed * 0.91f
                    );


                secondary =
                    (secondary - 0.5f)
                    * 2f;


                float edgeDepth =
                    baseDepth

                    + primary
                    * primaryRoughness

                    + secondary
                    * secondaryRoughness;


                // ==================================
                // 큰 잉크 덩어리
                // ==================================

                for (int i = 0;
                     i < splatterCount;
                     i++)
                {
                    float distance =
                        tangent
                        - splatCenters[i];


                    float widthValue =
                        Mathf.Max(
                            splatWidths[i],
                            0.001f
                        );


                    float gaussian =
                        Mathf.Exp(
                            -(
                                distance
                                * distance
                            )
                            /
                            (
                                2f
                                * widthValue
                                * widthValue
                            )
                        );


                    edgeDepth +=
                        gaussian
                        * splatStrengths[i];
                }


                // ==================================
                // 움푹 파인 곳
                // ==================================

                for (int i = 0;
                     i < notchCount;
                     i++)
                {
                    float distance =
                        tangent
                        - notchCenters[i];


                    float widthValue =
                        Mathf.Max(
                            notchWidths[i],
                            0.001f
                        );


                    float gaussian =
                        Mathf.Exp(
                            -(
                                distance
                                * distance
                            )
                            /
                            (
                                2f
                                * widthValue
                                * widthValue
                            )
                        );


                    edgeDepth -=
                        gaussian
                        * notchStrengths[i];
                }


                edgeDepth =
                    Mathf.Clamp(
                        edgeDepth,
                        0.18f,
                        0.93f
                    );


                // ==================================
                // 메인 Ink Alpha
                // ==================================

                float alpha =
                    1f
                    - Mathf.SmoothStep(
                        edgeDepth
                        - feather,
                        edgeDepth
                        + feather,
                        inward
                    );


                // ==================================
                // 분리된 Ink 방울
                // ==================================

                for (int i = 0;
                     i < dropletCount;
                     i++)
                {
                    float dx =
                        (
                            tangent
                            - dropletX[i]
                        )
                        /
                        dropletRadiusX[i];


                    float dy =
                        (
                            inward
                            - dropletY[i]
                        )
                        /
                        dropletRadiusY[i];


                    float ellipseDistance =
                        dx * dx
                        + dy * dy;


                    if (ellipseDistance < 1f)
                    {
                        float dropletAlpha =
                            1f
                            - Mathf.SmoothStep(
                                0.72f,
                                1f,
                                ellipseDistance
                            );


                        alpha =
                            Mathf.Max(
                                alpha,
                                dropletAlpha
                            );
                    }
                }


                byte alphaByte =
                    (byte)Mathf.RoundToInt(
                        Mathf.Clamp01(alpha)
                        * 255f
                    );


                pixels[
                    y * width + x
                ] =
                    new Color32(
                        255,
                        255,
                        255,
                        alphaByte
                    );
            }
        }


        generatedTexture.SetPixels32(
            pixels
        );


        // 이번에는 CPU 데이터도 유지
        // 작은 UI 텍스처라 부담이 매우 작음
        generatedTexture.Apply(
            false,
            false
        );


        generatedSprite =
            Sprite.Create(
                generatedTexture,

                new Rect(
                    0,
                    0,
                    width,
                    height
                ),

                new Vector2(
                    0.5f,
                    0.5f
                ),

                100f
            );


        generatedSprite.name =
            "GeneratedInkSprite_"
            + side;


        // ==========================================
        // Image에 강제로 적용
        // ==========================================

        targetImage.sprite =
            generatedSprite;


        targetImage.overrideSprite =
            generatedSprite;


        targetImage.type =
            Image.Type.Simple;


        targetImage.preserveAspect =
            false;


        targetImage.raycastTarget =
            false;


        targetImage.SetVerticesDirty();
        targetImage.SetMaterialDirty();
    }


    // ==================================================
    // Random Shape
    // ==================================================

    private void CreateRandomShapeData()
    {
        System.Random random =
            new System.Random(seed);


        // ------------------------------------------
        // Splat
        // ------------------------------------------

        splatCenters =
            new float[splatterCount];

        splatWidths =
            new float[splatterCount];

        splatStrengths =
            new float[splatterCount];


        for (int i = 0;
             i < splatterCount;
             i++)
        {
            splatCenters[i] =
                RandomRange(
                    random,
                    0.03f,
                    0.97f
                );


            splatWidths[i] =
                RandomRange(
                    random,
                    splatterMinWidth,
                    splatterMaxWidth
                );


            splatStrengths[i] =
                RandomRange(
                    random,
                    splatterDepth * 0.45f,
                    splatterDepth
                );
        }


        // ------------------------------------------
        // Notch
        // ------------------------------------------

        notchCenters =
            new float[notchCount];

        notchWidths =
            new float[notchCount];

        notchStrengths =
            new float[notchCount];


        for (int i = 0;
             i < notchCount;
             i++)
        {
            notchCenters[i] =
                RandomRange(
                    random,
                    0.05f,
                    0.95f
                );


            notchWidths[i] =
                RandomRange(
                    random,
                    0.02f,
                    0.07f
                );


            notchStrengths[i] =
                RandomRange(
                    random,
                    notchDepth * 0.4f,
                    notchDepth
                );
        }


        // ------------------------------------------
        // Droplet
        // ------------------------------------------

        dropletX =
            new float[dropletCount];

        dropletY =
            new float[dropletCount];

        dropletRadiusX =
            new float[dropletCount];

        dropletRadiusY =
            new float[dropletCount];


        for (int i = 0;
             i < dropletCount;
             i++)
        {
            dropletX[i] =
                RandomRange(
                    random,
                    0.04f,
                    0.96f
                );


            // 메인 잉크보다 더 안쪽
            dropletY[i] =
                RandomRange(
                    random,
                    0.62f,
                    0.92f
                );


            dropletRadiusX[i] =
                RandomRange(
                    random,
                    0.012f,
                    0.035f
                );


            dropletRadiusY[i] =
                RandomRange(
                    random,
                    0.025f,
                    0.075f
                );
        }
    }


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


    // ==================================================
    // Coordinate
    // ==================================================

    private float GetTangent(
        float u,
        float v)
    {
        if (side == EdgeSide.Top ||
            side == EdgeSide.Bottom)
        {
            return u;
        }

        return v;
    }


    private float GetInwardDistance(
        float u,
        float v)
    {
        switch (side)
        {
            case EdgeSide.Top:
                return 1f - v;

            case EdgeSide.Bottom:
                return v;

            case EdgeSide.Left:
                return u;

            case EdgeSide.Right:
                return 1f - u;
        }


        return 0f;
    }


    // ==================================================
    // Cleanup
    // ==================================================

    private void CleanupGeneratedResources()
    {
        if (generatedSprite != null)
        {
            Destroy(
                generatedSprite
            );

            generatedSprite =
                null;
        }


        if (generatedTexture != null)
        {
            Destroy(
                generatedTexture
            );

            generatedTexture =
                null;
        }
    }


    private void OnDestroy()
    {
        CleanupGeneratedResources();
    }
}