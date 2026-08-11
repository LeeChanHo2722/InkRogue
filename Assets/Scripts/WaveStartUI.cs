using System.Collections;
using TMPro;
using UnityEngine;

public class WaveStartUI : MonoBehaviour
{
    // ==================================================
    // References
    // ==================================================

    [Header("References")]

    public TMP_Text mainText;

    public TMP_Text subText;

    public CanvasGroup canvasGroup;


    // ==================================================
    // Position
    // ==================================================

    [Header("Movement")]

    [Tooltip("등장 시작 시 왼쪽으로 떨어진 거리")]
    public float enterOffsetX = 180f;

    [Tooltip("퇴장 시 오른쪽으로 이동하는 거리")]
    public float exitOffsetX = 180f;


    // ==================================================
    // Timing
    // ==================================================

    [Header("Timing")]

    public float enterDuration = 0.16f;

    public float holdDuration = 0.55f;

    public float exitDuration = 0.22f;


    // ==================================================
    // Scale
    // ==================================================

    [Header("Scale")]

    public float startScale = 1.18f;

    public float normalScale = 1f;


    // ==================================================
    // Runtime
    // ==================================================

    private RectTransform rectTransform;

    private Vector2 originalPosition;

    private Coroutine currentRoutine;


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


        originalPosition =
            rectTransform.anchoredPosition;


        HideImmediate();
    }


    // ==================================================
    // Public
    // ==================================================

    public void ShowWave(
        int currentWave,
        int totalWaves)
    {
        if (currentRoutine != null)
        {
            StopCoroutine(
                currentRoutine
            );
        }


        currentRoutine =
            StartCoroutine(
                ShowRoutine(
                    currentWave,
                    totalWaves
                )
            );
    }


    // ==================================================
    // Show Routine
    // ==================================================

    private IEnumerator ShowRoutine(
        int currentWave,
        int totalWaves)
    {
        // ==========================================
        // Text
        // ==========================================

        if (mainText != null)
        {
            mainText.text =
                "WAVE "
                + currentWave
                + " / "
                + totalWaves;
        }


        if (subText != null)
        {
            if (currentWave >=
                totalWaves)
            {
                subText.text =
                    "FINAL WAVE";
            }
            else if (currentWave == 1)
            {
                subText.text =
                    "GET READY";
            }
            else
            {
                subText.text =
                    "ENEMIES INCOMING";
            }
        }


        // ==========================================
        // Initial State
        // ==========================================

        canvasGroup.alpha =
            0f;


        canvasGroup.blocksRaycasts =
            false;


        rectTransform.anchoredPosition =
            originalPosition
            + Vector2.left
            * enterOffsetX;


        rectTransform.localScale =
            Vector3.one
            * startScale;


        // ==========================================
        // 1. Enter
        // ==========================================

        float timer =
            0f;


        float safeEnterDuration =
            Mathf.Max(
                enterDuration,
                0.01f
            );


        while (timer <
               safeEnterDuration)
        {
            timer +=
                Time.unscaledDeltaTime;


            float t =
                Mathf.Clamp01(
                    timer
                    / safeEnterDuration
                );


            float eased =
                EaseOutCubic(
                    t
                );


            canvasGroup.alpha =
                eased;


            rectTransform.anchoredPosition =
                Vector2.Lerp(
                    originalPosition
                    + Vector2.left
                    * enterOffsetX,
                    originalPosition,
                    eased
                );


            float scale =
                Mathf.Lerp(
                    startScale,
                    normalScale,
                    eased
                );


            rectTransform.localScale =
                Vector3.one
                * scale;


            yield return null;
        }


        canvasGroup.alpha =
            1f;


        rectTransform.anchoredPosition =
            originalPosition;


        rectTransform.localScale =
            Vector3.one
            * normalScale;


        // ==========================================
        // 2. Hold
        // ==========================================

        timer =
            0f;


        while (timer <
               holdDuration)
        {
            timer +=
                Time.unscaledDeltaTime;


            yield return null;
        }


        // ==========================================
        // 3. Exit
        // ==========================================

        timer =
            0f;


        float safeExitDuration =
            Mathf.Max(
                exitDuration,
                0.01f
            );


        while (timer <
               safeExitDuration)
        {
            timer +=
                Time.unscaledDeltaTime;


            float t =
                Mathf.Clamp01(
                    timer
                    / safeExitDuration
                );


            float eased =
                EaseInCubic(
                    t
                );


            canvasGroup.alpha =
                1f - eased;


            rectTransform.anchoredPosition =
                Vector2.Lerp(
                    originalPosition,
                    originalPosition
                    + Vector2.right
                    * exitOffsetX,
                    eased
                );


            yield return null;
        }


        HideImmediate();


        currentRoutine =
            null;
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
            rectTransform.anchoredPosition =
                originalPosition;


            rectTransform.localScale =
                Vector3.one
                * normalScale;
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


    private float EaseInCubic(
        float t)
    {
        return
            t * t * t;
    }
}