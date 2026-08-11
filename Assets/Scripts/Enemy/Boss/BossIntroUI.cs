using System.Collections;
using TMPro;
using UnityEngine;

public class BossIntroUI : MonoBehaviour
{
    // ==================================================
    // References
    // ==================================================

    [Header("References")]

    public CanvasGroup canvasGroup;

    public TMP_Text warningText;

    public TMP_Text bossTitleText;


    // ==================================================
    // Timing
    // ==================================================

    [Header("Timing")]

    public float appearDuration = 0.18f;

    public float holdDuration = 0.65f;

    public float disappearDuration = 0.25f;


    // ==================================================
    // Animation
    // ==================================================

    [Header("Animation")]

    public float startScale = 1.35f;


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

    public IEnumerator PlayIntro(
        string bossName)
    {
        if (warningText != null)
        {
            warningText.text =
                "WARNING";
        }


        if (bossTitleText != null)
        {
            bossTitleText.text =
                bossName;
        }


        canvasGroup.alpha =
            0f;


        rectTransform.localScale =
            originalScale
            * startScale;


        // ==========================================
        // Appear
        // ==========================================

        float timer =
            0f;


        while (timer <
               appearDuration)
        {
            timer +=
                Time.unscaledDeltaTime;


            float t =
                Mathf.Clamp01(
                    timer
                    / Mathf.Max(
                        appearDuration,
                        0.01f
                    )
                );


            float eased =
                EaseOutCubic(
                    t
                );


            canvasGroup.alpha =
                eased;


            rectTransform.localScale =
                originalScale
                * Mathf.Lerp(
                    startScale,
                    1f,
                    eased
                );


            yield return null;
        }


        canvasGroup.alpha =
            1f;


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
        // Hide
        // ==========================================

        timer =
            0f;


        while (timer <
               disappearDuration)
        {
            timer +=
                Time.unscaledDeltaTime;


            float t =
                Mathf.Clamp01(
                    timer
                    / Mathf.Max(
                        disappearDuration,
                        0.01f
                    )
                );


            canvasGroup.alpha =
                1f - t;


            yield return null;
        }


        HideImmediate();
    }


    // ==================================================
    // Hide
    // ==================================================

    private void HideImmediate()
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