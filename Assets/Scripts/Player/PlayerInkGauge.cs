using UnityEngine;

public class PlayerInkGauge : MonoBehaviour
{
    [Header("References")]
    public PlayerInkResource inkResource;
    public PlayerDive playerDive;

    public SpriteRenderer backgroundRenderer;
    public SpriteRenderer fillRenderer;


    // ==================================================
    // Visibility
    // ==================================================

    [Header("Visibility")]

    [Range(0f, 1f)]
    public float swimFormAlpha = 0.90f;

    [Range(0f, 1f)]
    public float humanFormAlpha = 0.70f;

    [Range(0.9f, 1f)]
    public float fullInkThreshold = 0.995f;

    public float visibilitySpeed = 10f;


    // ==================================================
    // Fill
    // ==================================================

    [Header("Fill")]

    [Tooltip("표시되는 Ink가 실제 Ink량을 따라가는 속도")]
    public float fillFollowSpeed = 14f;


    // ==================================================
    // Low Ink
    // ==================================================

    [Header("Low Ink")]

    [Range(0f, 1f)]
    public float lowInkThreshold = 0.20f;

    public float lowInkPulseSpeed = 6f;

    [Range(0f, 0.6f)]
    public float lowInkPulseStrength = 0.28f;


    // ==================================================
    // Color
    // ==================================================

    [Header("Color")]

    public Color normalInkColor =
        new Color(
            0.1f,
            0.8f,
            1f,
            1f
        );

    public Color lowInkColor =
        new Color(
            1f,
            0.25f,
            0.15f,
            1f
        );


    // ==================================================
    // Runtime
    // ==================================================

    private Transform fillTransform;

    private Vector3 fillFullScale;

    private Vector3 fillFullPosition;

    private float fillSpriteHeight = 1f;

    private float displayedInkPercent = 1f;

    private float currentVisibility = 0f;

    private Color backgroundBaseColor;


    // ==================================================
    // Awake
    // ==================================================

    private void Awake()
    {
        if (inkResource == null)
        {
            inkResource =
                GetComponentInParent<PlayerInkResource>();
        }


        if (playerDive == null)
        {
            playerDive =
                GetComponentInParent<PlayerDive>();
        }


        if (fillRenderer != null)
        {
            fillTransform =
                fillRenderer.transform;


            fillFullScale =
                fillTransform.localScale;


            fillFullPosition =
                fillTransform.localPosition;


            // Sprite 크기가 1x1이 아니어도
            // 아래쪽 고정 계산이 정확하도록 저장
            if (fillRenderer.sprite != null)
            {
                fillSpriteHeight =
                    fillRenderer.sprite.bounds.size.y;
            }
        }


        if (backgroundRenderer != null)
        {
            backgroundBaseColor =
                backgroundRenderer.color;
        }


        if (inkResource != null)
        {
            displayedInkPercent =
                inkResource.CurrentInkPercent;
        }


        ApplyVisibility(
            0f
        );


        UpdateFillVisual();
    }


    // ==================================================
    // Update
    // ==================================================

    private void Update()
    {
        if (inkResource == null)
            return;


        UpdateInkPercent();

        UpdateVisibility();

        UpdateFillVisual();
    }


    // ==================================================
    // Ink Amount
    // ==================================================

    private void UpdateInkPercent()
    {
        float targetPercent =
            Mathf.Clamp01(
                inkResource.CurrentInkPercent
            );


        float follow =
            1f
            - Mathf.Exp(
                -fillFollowSpeed
                * Time.deltaTime
            );


        displayedInkPercent =
            Mathf.Lerp(
                displayedInkPercent,
                targetPercent,
                follow
            );


        if (targetPercent <= 0.001f &&
            displayedInkPercent < 0.002f)
        {
            displayedInkPercent =
                0f;
        }


        if (targetPercent >= 0.999f &&
            displayedInkPercent > 0.998f)
        {
            displayedInkPercent =
                1f;
        }
    }


    // ==================================================
    // Visibility
    // ==================================================

    private void UpdateVisibility()
    {
        bool isSwimForm =
            playerDive != null
            && playerDive.IsSwimForm;


        bool inkIsNotFull =
            inkResource.CurrentInkPercent
            < fullInkThreshold;


        float targetVisibility;


        // ==========================================
        // Swim Form
        //
        // 항상 표시
        // ==========================================

        if (isSwimForm)
        {
            targetVisibility =
                swimFormAlpha;
        }

        // ==========================================
        // Human Form + Ink 소모
        // ==========================================

        else if (inkIsNotFull)
        {
            targetVisibility =
                humanFormAlpha;
        }

        // ==========================================
        // Human + Full Ink
        // ==========================================

        else
        {
            targetVisibility =
                0f;
        }


        float follow =
            1f
            - Mathf.Exp(
                -visibilitySpeed
                * Time.deltaTime
            );


        currentVisibility =
            Mathf.Lerp(
                currentVisibility,
                targetVisibility,
                follow
            );


        if (targetVisibility <= 0f &&
            currentVisibility < 0.01f)
        {
            currentVisibility =
                0f;
        }


        ApplyVisibility(
            currentVisibility
        );
    }


    // ==================================================
    // Vertical Fill
    // ==================================================

    private void UpdateFillVisual()
    {
        if (fillTransform == null ||
            fillRenderer == null)
        {
            return;
        }


        float percent =
            Mathf.Clamp01(
                displayedInkPercent
            );


        // ==========================================
        // Y 크기만 변경
        //
        // 100% = 원래 높이
        // 50%  = 절반
        // 0%   = 높이 0
        // ==========================================

        Vector3 newScale =
            fillFullScale;


        newScale.y =
            fillFullScale.y
            * percent;


        fillTransform.localScale =
            newScale;


        // ==========================================
        // Bottom Anchor 효과
        //
        // Scale만 줄이면 위/아래가 동시에
        // 줄어들기 때문에 Y Position도 보정한다.
        //
        // 아래쪽 끝은 고정하고
        // 위쪽에서부터 Ink가 줄어듦.
        // ==========================================

        float fullHeight =
            fillSpriteHeight
            * fillFullScale.y;


        float currentHeight =
            fillSpriteHeight
            * newScale.y;


        float bottomEdge =
            fillFullPosition.y
            - fullHeight * 0.5f;


        float currentCenter =
            bottomEdge
            + currentHeight * 0.5f;


        Vector3 newPosition =
            fillFullPosition;


        newPosition.y =
            currentCenter;


        fillTransform.localPosition =
            newPosition;


        // ==========================================
        // Low Ink Color
        // ==========================================

        float lowInkFactor =
            1f
            - Mathf.Clamp01(
                percent
                /
                Mathf.Max(
                    lowInkThreshold,
                    0.001f
                )
            );


        Color targetColor =
            Color.Lerp(
                normalInkColor,
                lowInkColor,
                lowInkFactor
            );


        // ==========================================
        // Low Ink Pulse
        // ==========================================

        float pulseMultiplier =
            1f;


        if (percent <=
            lowInkThreshold)
        {
            float wave =
                (
                    Mathf.Sin(
                        Time.time
                        * lowInkPulseSpeed
                    )
                    + 1f
                )
                * 0.5f;


            pulseMultiplier =
                1f
                - lowInkPulseStrength
                * wave;
        }


        targetColor.a =
            currentVisibility
            * pulseMultiplier;


        fillRenderer.color =
            targetColor;
    }


    // ==================================================
    // Visibility Alpha
    // ==================================================

    private void ApplyVisibility(
        float alpha)
    {
        alpha =
            Mathf.Clamp01(
                alpha
            );


        if (backgroundRenderer != null)
        {
            Color color =
                backgroundBaseColor;


            color.a =
                backgroundBaseColor.a
                * alpha;


            backgroundRenderer.color =
                color;
        }


        if (fillRenderer != null &&
            alpha <= 0f)
        {
            Color color =
                fillRenderer.color;


            color.a =
                0f;


            fillRenderer.color =
                color;
        }
    }
}