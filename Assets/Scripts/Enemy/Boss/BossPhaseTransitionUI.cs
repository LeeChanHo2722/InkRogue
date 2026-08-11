using System.Collections;
using TMPro;
using UnityEngine;

public class BossPhaseTransitionUI : MonoBehaviour
{
    // ==================================================
    // References
    // ==================================================

    [Header("References")]

    public CanvasGroup canvasGroup;

    public TMP_Text mainText;

    public TMP_Text subText;


    // ==================================================
    // Timing
    // ==================================================

    [Header("Timing")]

    public float appearDuration = 0.18f;

    public float holdDuration = 0.55f;

    public float disappearDuration = 0.25f;


    // ==================================================
    // Animation
    // ==================================================

    [Header("Animation")]

    public float startScale = 1.35f;

    public float overshootScale = 1.08f;


    // ==================================================
    // Runtime
    // ==================================================

    private RectTransform rectTransform;

    private Vector3 originalScale;


    // ==================================================
    // Awake
    // ==================================================

    private void Awake()
    {
        rectTransform =
            GetComponent<RectTransform>();


        if (canvasGroup == null)
        {
            canvasGroup =
                GetComponent<CanvasGroup>();
        }


        originalScale =
            rectTransform.localScale;


        HideImmediate();
    }


    // ==================================================
    // Play
    // ==================================================

    public IEnumerator PlayPhaseTransition(
        int phase)
    {
        // ==========================================
        // Text
        // ==========================================

        if (phase >= 3)
        {
            if (mainText != null)
            {
                mainText.text =
                    "FINAL PHASE";
            }


            if (subText != null)
            {
                subText.text =
                    "CORE UNSTABLE";
            }
        }
        else
        {
            if (mainText != null)
            {
                mainText.text =
                    "PHASE 2";
            }


            if (subText != null)
            {
                subText.text =
                    "INK OVERLOAD";
            }
        }


        // ==========================================
        // Initial
        // ==========================================

        canvasGroup.alpha =
            0f;


        canvasGroup.blocksRaycasts =
            false;


        rectTransform.localScale =
            originalScale
            * startScale;


        // ==========================================
        // Appear
        // ==========================================

        float timer =
            0f;


        float safeAppear =
            Mathf.Max(
                appearDuration,
                0.01f
            );


        while (timer <
               safeAppear)
        {
            timer +=
                Time.unscaledDeltaTime;


            float t =
                Mathf.Clamp01(
                    timer
                    / safeAppear
                );


            float eased =
                EaseOutBack(
                    t
                );


            canvasGroup.alpha =
                Mathf.Clamp01(
                    t * 1.4f
                );


            float scale =
                Mathf.Lerp(
                    startScale,
                    overshootScale,
                    eased
                );


            rectTransform.localScale =
                originalScale
                * scale;


            yield return null;
        }


        rectTransform.localScale =
            originalScale
            * overshootScale;


        canvasGroup.alpha =
            1f;


        // ==========================================
        // Settle
        // ==========================================

        timer =
            0f;


        const float settleDuration =
            0.10f;


        while (timer <
               settleDuration)
        {
            timer +=
                Time.unscaledDeltaTime;


            float t =
                Mathf.Clamp01(
                    timer
                    / settleDuration
                );


            rectTransform.localScale =
                originalScale
                * Mathf.Lerp(
                    overshootScale,
                    1f,
                    t
                );


            yield return null;
        }


        rectTransform.localScale =
            originalScale;


        // ==========================================
        // Hold
        // ==========================================

        yield return
            new WaitForSecondsRealtime(
                holdDuration
            );


        // ==========================================
        // Disappear
        // ==========================================

        timer =
            0f;


        float safeDisappear =
            Mathf.Max(
                disappearDuration,
                0.01f
            );


        while (timer <
               safeDisappear)
        {
            timer +=
                Time.unscaledDeltaTime;


            float t =
                Mathf.Clamp01(
                    timer
                    / safeDisappear
                );


            canvasGroup.alpha =
                1f - t;


            rectTransform.localScale =
                originalScale
                * Mathf.Lerp(
                    1f,
                    0.92f,
                    t
                );


            yield return null;
        }


        HideImmediate();
    }


    // ==================================================
    // Hide
    // ==================================================

    public void HideImmediate()
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha =
                0f;


            canvasGroup.blocksRaycasts =
                false;
        }


        if (rectTransform != null)
        {
            rectTransform.localScale =
                originalScale;
        }
    }


    // ==================================================
    // Ease
    // ==================================================

    private float EaseOutBack(
        float t)
    {
        const float c1 =
            1.70158f;


        const float c3 =
            c1 + 1f;


        float x =
            t - 1f;


        return
            1f
            + c3
            * x
            * x
            * x
            + c1
            * x
            * x;
    }
}