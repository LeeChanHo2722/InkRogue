using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class InkScreenWipe : MonoBehaviour
{
    // ==================================================
    // References
    // ==================================================

    [Header("References")]
    public RawImage wipeImage;


    // ==================================================
    // Wipe
    // ==================================================

    [Header("Wipe")]

    [Tooltip("화면을 덮거나 걷어내는 시간")]
    public float duration = 0.45f;

    [Tooltip("Player Ink와 비슷한 색 추천")]
    public Color inkColor =
        new Color(
            0.1f,
            0.75f,
            1f,
            1f
        );


    // ==================================================
    // Ink Edge
    // ==================================================

    [Header("Ink Edge")]

    [Tooltip("잉크 경계의 울퉁불퉁함")]
    [Range(0f, 0.25f)]
    public float edgeRoughness = 0.08f;

    public float primaryNoiseScale = 3.5f;

    public float secondaryNoiseScale = 11f;

    [Tooltip("잉크 경계의 부드러운 정도")]
    public float feather = 0.012f;

    public float seed = 17f;


    // ==================================================
    // Texture
    // ==================================================

    [Header("Texture")]

    public int textureWidth = 256;

    public int textureHeight = 144;


    // ==================================================
    // Runtime
    // ==================================================

    private Texture2D wipeTexture;

    private Color32[] pixels;

    private float[] rowNoise;


    public float Duration =>
        duration;


    // ==================================================
    // Awake
    // ==================================================

    private void Awake()
    {
        if (wipeImage == null)
        {
            wipeImage =
                GetComponent<RawImage>();
        }


        CreateTexture();


        // ==========================================
        // 게임 실행 순간에는
        // 화면 전체를 Ink로 덮어 둔다.
        // ==========================================

        SetCoveredImmediate(
            true
        );
    }


    // ==================================================
    // Create Texture
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
                36,
                textureHeight
            );


        wipeTexture =
            new Texture2D(
                textureWidth,
                textureHeight,
                TextureFormat.RGBA32,
                false
            );


        wipeTexture.filterMode =
            FilterMode.Bilinear;


        wipeTexture.wrapMode =
            TextureWrapMode.Clamp;


        if (wipeImage != null)
        {
            wipeImage.texture =
                wipeTexture;
        }


        pixels =
            new Color32[
                textureWidth
                * textureHeight
            ];


        rowNoise =
            new float[
                textureHeight
            ];


        GenerateRowNoise();
    }


    // ==================================================
    // Generate Noise
    // ==================================================

    private void GenerateRowNoise()
    {
        for (int y = 0;
             y < textureHeight;
             y++)
        {
            float normalizedY =
                textureHeight <= 1
                    ? 0f
                    : (float)y
                    / (textureHeight - 1);


            float primary =
                Mathf.PerlinNoise(
                    seed,
                    normalizedY
                    * primaryNoiseScale
                )
                * 2f
                - 1f;


            float secondary =
                Mathf.PerlinNoise(
                    seed + 31.7f,
                    normalizedY
                    * secondaryNoiseScale
                )
                * 2f
                - 1f;


            float wave =
                Mathf.Sin(
                    normalizedY
                    * Mathf.PI
                    * 10f
                    + seed
                );


            rowNoise[y] =
                primary * 0.60f
                + secondary * 0.25f
                + wave * 0.15f;
        }
    }


    // ==================================================
    // Cover
    // ==================================================

    public IEnumerator Cover()
    {
        if (wipeImage == null)
            yield break;


        wipeImage.enabled =
            true;


        wipeImage.raycastTarget =
            true;


        float timer =
            0f;


        float safeDuration =
            Mathf.Max(
                duration,
                0.01f
            );


        while (timer <
               safeDuration)
        {
            timer +=
                Time.unscaledDeltaTime;


            float t =
                Mathf.Clamp01(
                    timer
                    / safeDuration
                );


            float eased =
                EaseInOutCubic(
                    t
                );


            RenderWipe(
                eased,
                true
            );


            yield return null;
        }


        RenderWipe(
            1f,
            true
        );
    }


    // ==================================================
    // Reveal
    // ==================================================

    public IEnumerator Reveal()
    {
        if (wipeImage == null)
            yield break;


        wipeImage.enabled =
            true;


        wipeImage.raycastTarget =
            true;


        float safeDuration =
            Mathf.Max(
                duration,
                0.01f
            );


        // ==========================================
        // 중요:
        // Reveal의 시작 상태를 명시적으로 그림
        //
        // progress = 0
        // → 화면 전체 Ink
        // ==========================================

        RenderWipe(
            0f,
            false
        );


        // ==========================================
        // 반드시 완전히 덮인 화면을
        // 실제 한 프레임 렌더링
        // ==========================================

        yield return new WaitForEndOfFrame();


        float timer =
            0f;


        while (timer <
               safeDuration)
        {
            float t =
                Mathf.Clamp01(
                    timer
                    / safeDuration
                );


            float eased =
                EaseInOutCubic(
                    t
                );


            RenderWipe(
                eased,
                false
            );


            yield return null;


            // ==========================================
            // Play 직후 첫 Delta가 비정상적으로
            // 커도 애니메이션을 건너뛰지 않음
            //
            // 최대 0.033초씩만 진행
            // ≒ 최소 약 30fps 기준
            // ==========================================

            float frameDelta =
                Mathf.Min(
                    Time.unscaledDeltaTime,
                    0.033f
                );


            timer +=
                frameDelta;
        }


        // ==========================================
        // 완전히 Reveal
        // ==========================================

        RenderWipe(
            1f,
            false
        );


        wipeImage.raycastTarget =
            false;


        wipeImage.enabled =
            false;
    }

    // ==================================================
    // Immediate
    // ==================================================

    public void SetCoveredImmediate(
        bool covered)
    {
        if (wipeImage == null ||
            wipeTexture == null)
        {
            return;
        }


        wipeImage.enabled =
            covered;


        wipeImage.raycastTarget =
            covered;


        if (covered)
        {
            RenderSolid(
                1f
            );
        }
        else
        {
            RenderSolid(
                0f
            );
        }
    }


    // ==================================================
    // Render Wipe
    // ==================================================

    private void RenderWipe(
        float progress,
        bool covering)
    {
        if (wipeTexture == null ||
            pixels == null ||
            rowNoise == null)
        {
            return;
        }


        float margin =
            edgeRoughness
            + 0.06f;


        float mainEdge =
            Mathf.Lerp(
                -margin,
                1f + margin,
                progress
            );


        Color32 baseColor =
            inkColor;


        for (int y = 0;
             y < textureHeight;
             y++)
        {
            float edge =
                mainEdge
                + rowNoise[y]
                * edgeRoughness;


            for (int x = 0;
                 x < textureWidth;
                 x++)
            {
                float normalizedX =
                    textureWidth <= 1
                        ? 0f
                        : (float)x
                        / (textureWidth - 1);


                float smooth =
                    Mathf.SmoothStep(
                        0f,
                        1f,
                        Mathf.InverseLerp(
                            edge - feather,
                            edge + feather,
                            normalizedX
                        )
                    );


                float alpha;


                // ======================================
                // Cover
                //
                // 왼쪽 → 오른쪽으로 Ink가 덮는다.
                // ======================================

                if (covering)
                {
                    alpha =
                        1f - smooth;
                }

                // ======================================
                // Reveal
                //
                // 왼쪽 → 오른쪽으로 Ink가 걷힌다.
                // ======================================

                else
                {
                    alpha =
                        smooth;
                }


                Color32 pixel =
                    baseColor;


                pixel.a =
                    (byte)Mathf.RoundToInt(
                        Mathf.Clamp01(
                            alpha
                        )
                        * 255f
                    );


                pixels[
                    y * textureWidth
                    + x
                ] =
                    pixel;
            }
        }


        wipeTexture.SetPixels32(
            pixels
        );


        wipeTexture.Apply(
            false
        );
    }


    // ==================================================
    // Solid
    // ==================================================

    private void RenderSolid(
        float alpha)
    {
        if (wipeTexture == null ||
            pixels == null)
        {
            return;
        }


        Color32 color =
            inkColor;


        color.a =
            (byte)Mathf.RoundToInt(
                Mathf.Clamp01(
                    alpha
                )
                * 255f
            );


        for (int i = 0;
             i < pixels.Length;
             i++)
        {
            pixels[i] =
                color;
        }


        wipeTexture.SetPixels32(
            pixels
        );


        wipeTexture.Apply(
            false
        );
    }


    // ==================================================
    // Ease
    // ==================================================

    private float EaseInOutCubic(
        float t)
    {
        if (t < 0.5f)
        {
            return
                4f
                * t
                * t
                * t;
        }


        float f =
            -2f * t
            + 2f;


        return
            1f
            - f
            * f
            * f
            / 2f;
    }


    // ==================================================
    // Cleanup
    // ==================================================

    private void OnDestroy()
    {
        if (wipeTexture != null)
        {
            Destroy(
                wipeTexture
            );
        }
    }
}