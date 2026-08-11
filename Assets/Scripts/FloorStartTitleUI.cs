using System.Collections;
using TMPro;
using UnityEngine;

public class FloorStartTitleUI : MonoBehaviour
{
    // ==================================================
    // References
    // ==================================================

    [Header("References")]
    public TMP_Text titleText;


    // ==================================================
    // Text
    // ==================================================

    [Header("Text")]

    public string floorPrefix = "FLOOR";


    // ==================================================
    // Timing
    // ==================================================

    [Header("Timing")]

    [Tooltip("처음 나타나는 시간")]
    public float appearDuration = 0.18f;

    [Tooltip("화면에 유지되는 시간")]
    public float holdDuration = 0.50f;

    [Tooltip("사라지는 시간")]
    public float disappearDuration = 0.22f;


    // ==================================================
    // Scale
    // ==================================================

    [Header("Scale")]

    [Tooltip("등장 시작 크기")]
    public float startScale = 1.35f;

    [Tooltip("최종 크기")]
    public float normalScale = 1f;


    // ==================================================
    // Runtime
    // ==================================================

    private RectTransform rectTransform;

    private Color originalColor;

    private Vector3 originalScale;


    // ==================================================
    // Awake
    // ==================================================

    private void Awake()
    {
        if (titleText == null)
        {
            titleText =
                GetComponent<TMP_Text>();
        }


        rectTransform =
            GetComponent<RectTransform>();


        if (titleText != null)
        {
            originalColor =
                titleText.color;
        }


        if (rectTransform != null)
        {
            originalScale =
                rectTransform.localScale;
        }


        HideImmediate();
    }


    // ==================================================
    // Show
    // ==================================================

    public IEnumerator ShowFloorTitle(
        int floorNumber)
    {
        if (titleText == null ||
            rectTransform == null)
        {
            yield break;
        }


        // ==========================================
        // Text
        // ==========================================

        titleText.text =
            floorPrefix
            + " "
            + floorNumber;


        titleText.gameObject
            .SetActive(true);


        // ==========================================
        // 등장 시작 상태
        // ==========================================

        Color color =
            originalColor;


        color.a =
            0f;


        titleText.color =
            color;


        rectTransform.localScale =
            originalScale
            * startScale;


        // ==========================================
        // Appear
        //
        // 커다랗고 투명한 상태
        // →
        // 정상 크기 + 불투명
        // ==========================================

        float timer =
            0f;


        float safeAppearDuration =
            Mathf.Max(
                appearDuration,
                0.01f
            );


        while (timer <
               safeAppearDuration)
        {
            timer +=
                Time.unscaledDeltaTime;


            float t =
                Mathf.Clamp01(
                    timer
                    / safeAppearDuration
                );


            float eased =
                EaseOutCubic(
                    t
                );


            color =
                originalColor;


            color.a =
                eased;


            titleText.color =
                color;


            float scale =
                Mathf.Lerp(
                    startScale,
                    normalScale,
                    eased
                );


            rectTransform.localScale =
                originalScale
                * scale;


            yield return null;
        }


        color =
            originalColor;


        color.a =
            1f;


        titleText.color =
            color;


        rectTransform.localScale =
            originalScale
            * normalScale;


        // ==========================================
        // Hold
        // ==========================================

        if (holdDuration > 0f)
        {
            yield return new WaitForSecondsRealtime(
                holdDuration
            );
        }


        // ==========================================
        // Disappear
        // ==========================================

        timer =
            0f;


        float safeDisappearDuration =
            Mathf.Max(
                disappearDuration,
                0.01f
            );


        while (timer <
               safeDisappearDuration)
        {
            timer +=
                Time.unscaledDeltaTime;


            float t =
                Mathf.Clamp01(
                    timer
                    / safeDisappearDuration
                );


            float eased =
                Mathf.SmoothStep(
                    0f,
                    1f,
                    t
                );


            color =
                originalColor;


            color.a =
                1f - eased;


            titleText.color =
                color;


            yield return null;
        }


        HideImmediate();
    }


    // ==================================================
    // Hide
    // ==================================================

    private void HideImmediate()
    {
        if (titleText == null)
            return;


        Color color =
            titleText.color;


        color.a =
            0f;


        titleText.color =
            color;


        if (rectTransform != null)
        {
            rectTransform.localScale =
                originalScale;
        }


        titleText.gameObject
            .SetActive(false);
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