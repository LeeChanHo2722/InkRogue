using System.Collections;
using UnityEngine;

public class SlowMotionController : MonoBehaviour
{
    // ==================================================
    // Runtime
    // ==================================================

    private float originalFixedDeltaTime;

    private Coroutine restoreRoutine;


    // ==================================================
    // Awake
    // ==================================================

    private void Awake()
    {
        originalFixedDeltaTime =
            Time.fixedDeltaTime;
    }


    // ==================================================
    // Immediate Slow
    // ==================================================

    public void SetTimeScale(
        float timeScale)
    {
        if (restoreRoutine != null)
        {
            StopCoroutine(
                restoreRoutine
            );

            restoreRoutine =
                null;
        }


        float safeScale =
            Mathf.Clamp(
                timeScale,
                0.01f,
                1f
            );


        Time.timeScale =
            safeScale;


        Time.fixedDeltaTime =
            originalFixedDeltaTime
            * safeScale;
    }


    // ==================================================
    // Restore Immediately
    // ==================================================

    public void RestoreImmediate()
    {
        if (restoreRoutine != null)
        {
            StopCoroutine(
                restoreRoutine
            );

            restoreRoutine =
                null;
        }


        Time.timeScale =
            1f;


        Time.fixedDeltaTime =
            originalFixedDeltaTime;
    }


    // ==================================================
    // Smooth Restore
    // ==================================================

    public IEnumerator RestoreSmooth(
        float duration)
    {
        if (restoreRoutine != null)
        {
            StopCoroutine(
                restoreRoutine
            );

            restoreRoutine =
                null;
        }


        float startScale =
            Time.timeScale;


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


            t =
                Mathf.SmoothStep(
                    0f,
                    1f,
                    t
                );


            float scale =
                Mathf.Lerp(
                    startScale,
                    1f,
                    t
                );


            Time.timeScale =
                scale;


            Time.fixedDeltaTime =
                originalFixedDeltaTime
                * scale;


            yield return null;
        }


        RestoreImmediate();
    }


    // ==================================================
    // Safety
    // ==================================================

    private void OnDestroy()
    {
        Time.timeScale =
            1f;


        Time.fixedDeltaTime =
            originalFixedDeltaTime;
    }
}