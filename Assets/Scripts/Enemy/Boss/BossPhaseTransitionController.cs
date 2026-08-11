using System.Collections;
using UnityEngine;

public class BossPhaseTransitionController : MonoBehaviour
{
    // ==================================================
    // References
    // ==================================================

    [Header("References")]

    public BossHealth bossHealth;

    public BossAttackController attackController;

    [Tooltip("Boss 실제 그래픽 Root")]
    public Transform visualRoot;

    public SpriteRenderer referenceRenderer;


    // ==================================================
    // Phase 2
    // ==================================================

    [Header("Phase 2")]

    public float phase2TransitionDuration =
        0.90f;

    public float phase2Scale =
        1.25f;

    public float phase2InkRadius =
        1.8f;

    public int phase2InkSplatCount =
        24;

    public float phase2ShakeDuration =
        0.22f;

    public float phase2ShakeStrength =
        0.20f;


    // ==================================================
    // Phase 3
    // ==================================================

    [Header("Phase 3")]

    public float phase3TransitionDuration =
        1.05f;

    public float phase3Scale =
        1.38f;

    public float phase3InkRadius =
        2.5f;

    public int phase3InkSplatCount =
        36;

    public float phase3ShakeDuration =
        0.32f;

    public float phase3ShakeStrength =
        0.30f;


    // ==================================================
    // Shockwave
    // ==================================================

    [Header("Shockwave")]

    public int ringSegments =
        48;

    public float ringStartRadius =
        0.25f;

    public float ringStartWidth =
        0.15f;

    public int ringSortingOffset =
        5;


    // ==================================================
    // Runtime
    // ==================================================

    private BossPhaseTransitionUI phaseUI;

    private CameraFollow cameraFollow;


    private LineRenderer shockwaveRing;


    private Vector3 originalVisualScale;


    private Color originalColor;


    private bool transitionRunning =
        false;


    // ==================================================
    // Awake
    // ==================================================

    private void Awake()
    {
        // ==========================================
        // References
        // ==========================================

        if (bossHealth == null)
        {
            bossHealth =
                GetComponent<BossHealth>();
        }


        if (attackController == null)
        {
            attackController =
                GetComponent<BossAttackController>();
        }


        if (visualRoot == null)
        {
            Transform found =
                transform.Find(
                    "VisualRoot"
                );


            visualRoot =
                found != null
                    ? found
                    : transform;
        }


        if (referenceRenderer == null)
        {
            referenceRenderer =
                visualRoot
                    .GetComponentInChildren<SpriteRenderer>(
                        true
                    );
        }


        originalVisualScale =
            visualRoot.localScale;


        if (referenceRenderer != null)
        {
            originalColor =
                referenceRenderer.color;
        }


        // ==========================================
        // Scene UI 찾기
        // ==========================================

        phaseUI =
            FindAnyObjectByType<
                BossPhaseTransitionUI
            >();


        // ==========================================
        // Camera
        // ==========================================

        if (Camera.main != null)
        {
            cameraFollow =
                Camera.main.GetComponent<
                    CameraFollow
                >();
        }


        CreateShockwaveRing();
    }


    // ==================================================
    // Enable / Disable
    // ==================================================

    private void OnEnable()
    {
        if (bossHealth != null)
        {
            bossHealth.PhaseChanged +=
                OnPhaseChanged;
        }
    }


    private void OnDisable()
    {
        if (bossHealth != null)
        {
            bossHealth.PhaseChanged -=
                OnPhaseChanged;
        }
    }


    // ==================================================
    // Phase
    // ==================================================

    private void OnPhaseChanged(
        int phase)
    {
        if (transitionRunning)
            return;


        if (phase < 2)
            return;


        StartCoroutine(
            PhaseTransitionRoutine(
                phase
            )
        );
    }


    // ==================================================
    // Transition
    // ==================================================

    private IEnumerator PhaseTransitionRoutine(
        int phase)
    {
        transitionRunning =
            true;


        // ==========================================
        // 1. Boss 무적
        // ==========================================

        if (bossHealth != null)
        {
            bossHealth.SetInvulnerable(
                true
            );
        }


        // ==========================================
        // 2. 현재 공격 즉시 종료
        // ==========================================

        if (attackController != null)
        {
            attackController.StopCombat();
        }


        // ==========================================
        // Phase별 Parameters
        // ==========================================

        bool finalPhase =
            phase >= 3;


        float duration =
            finalPhase
                ? phase3TransitionDuration
                : phase2TransitionDuration;


        float maxScale =
            finalPhase
                ? phase3Scale
                : phase2Scale;


        float inkRadius =
            finalPhase
                ? phase3InkRadius
                : phase2InkRadius;


        int splatCount =
            finalPhase
                ? phase3InkSplatCount
                : phase2InkSplatCount;


        float shakeDuration =
            finalPhase
                ? phase3ShakeDuration
                : phase2ShakeDuration;


        float shakeStrength =
            finalPhase
                ? phase3ShakeStrength
                : phase2ShakeStrength;


        // ==========================================
        // 3. Camera Shake
        // ==========================================

        if (cameraFollow != null)
        {
            cameraFollow.StartShake(
                shakeDuration,
                shakeStrength
            );
        }


        // ==========================================
        // 4. UI 동시 시작
        // ==========================================

        if (phaseUI != null)
        {
            StartCoroutine(
                phaseUI.PlayPhaseTransition(
                    phase
                )
            );
        }


        // ==========================================
        // 5. Enemy Ink Burst
        // ==========================================

        if (InkMap.Instance != null)
        {
            InkMap.Instance.PaintExplosion(
                transform.position,
                inkRadius,
                InkTeam.Enemy,
                splatCount
            );
        }


        // ==========================================
        // 6. Shockwave
        // ==========================================

        if (shockwaveRing != null)
        {
            shockwaveRing.enabled =
                true;
        }


        // ==========================================
        // 7. Boss Pulse
        // ==========================================

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


            // ======================================
            // Scale
            //
            // 전반부 커짐
            // 후반부 원래 크기로 복귀
            // ======================================

            float scale;


            if (t < 0.40f)
            {
                float growT =
                    t / 0.40f;


                scale =
                    Mathf.Lerp(
                        1f,
                        maxScale,
                        EaseOutCubic(
                            growT
                        )
                    );
            }
            else
            {
                float settleT =
                    Mathf.InverseLerp(
                        0.40f,
                        1f,
                        t
                    );


                scale =
                    Mathf.Lerp(
                        maxScale,
                        1f,
                        EaseOutCubic(
                            settleT
                        )
                    );
            }


            if (visualRoot != null)
            {
                visualRoot.localScale =
                    originalVisualScale
                    * scale;
            }


            // ======================================
            // Flash
            // ======================================

            if (referenceRenderer != null)
            {
                float flash =
                    Mathf.Abs(
                        Mathf.Sin(
                            t
                            * Mathf.PI
                            * (
                                finalPhase
                                    ? 8f
                                    : 6f
                            )
                        )
                    );


                Color enemyColor =
                    GetEnemyColor();


                referenceRenderer.color =
                    Color.Lerp(
                        enemyColor,
                        Color.white,
                        flash * 0.65f
                    );
            }


            // ======================================
            // Shockwave
            // ======================================

            UpdateShockwave(
                t,
                inkRadius,
                finalPhase
            );


            yield return null;
        }


        // ==========================================
        // 8. 원상복구
        // ==========================================

        if (visualRoot != null)
        {
            visualRoot.localScale =
                originalVisualScale;
        }


        if (referenceRenderer != null)
        {
            referenceRenderer.color =
                originalColor;
        }


        if (shockwaveRing != null)
        {
            shockwaveRing.enabled =
                false;
        }


        // UI가 끝날 시간을 약간 확보
        yield return
            new WaitForSecondsRealtime(
                0.12f
            );


        // ==========================================
        // 9. Boss 무적 종료
        // ==========================================

        if (bossHealth != null &&
            !bossHealth.IsDead)
        {
            bossHealth.SetInvulnerable(
                false
            );
        }


        // ==========================================
        // 10. 새 Phase 공격 시작
        // ==========================================

        if (attackController != null &&
            bossHealth != null &&
            !bossHealth.IsDead)
        {
            attackController.BeginCombat();
        }


        transitionRunning =
            false;


        Debug.Log(
            "PHASE "
            + phase
            + " TRANSITION COMPLETE"
        );
    }


    // ==================================================
    // Shockwave Creation
    // ==================================================

    private void CreateShockwaveRing()
    {
        GameObject ringObject =
            new GameObject(
                "Runtime_BossPhaseShockwave"
            );


        ringObject.transform.SetParent(
            transform,
            false
        );


        shockwaveRing =
            ringObject.AddComponent<
                LineRenderer
            >();


        shockwaveRing.useWorldSpace =
            true;


        shockwaveRing.loop =
            true;


        shockwaveRing.positionCount =
            Mathf.Max(
                12,
                ringSegments
            );


        shockwaveRing.numCornerVertices =
            4;


        shockwaveRing.startWidth =
            ringStartWidth;


        shockwaveRing.endWidth =
            ringStartWidth;


        shockwaveRing.enabled =
            false;


        if (referenceRenderer != null)
        {
            shockwaveRing.sharedMaterial =
                referenceRenderer
                    .sharedMaterial;


            shockwaveRing.sortingLayerID =
                referenceRenderer
                    .sortingLayerID;


            shockwaveRing.sortingOrder =
                referenceRenderer
                    .sortingOrder
                + ringSortingOffset;
        }
    }


    // ==================================================
    // Shockwave Update
    // ==================================================

    private void UpdateShockwave(
        float progress,
        float maxRadius,
        bool finalPhase)
    {
        if (shockwaveRing == null)
            return;


        float eased =
            EaseOutCubic(
                progress
            );


        float radius =
            Mathf.Lerp(
                ringStartRadius,
                maxRadius,
                eased
            );


        float widthMultiplier =
            finalPhase
                ? 1.30f
                : 1f;


        float width =
            Mathf.Lerp(
                ringStartWidth
                * widthMultiplier,
                0.015f,
                progress
            );


        Color color =
            GetEnemyColor();


        color =
            Color.Lerp(
                color,
                Color.white,
                finalPhase
                    ? 0.15f
                    : 0.05f
            );


        color.a =
            (1f - progress)
            * 0.95f;


        shockwaveRing.startWidth =
            width;


        shockwaveRing.endWidth =
            width;


        shockwaveRing.startColor =
            color;


        shockwaveRing.endColor =
            color;


        Vector2 center =
            transform.position;


        int segments =
            Mathf.Max(
                12,
                ringSegments
            );


        shockwaveRing.positionCount =
            segments;


        for (int i = 0;
             i < segments;
             i++)
        {
            float angle =
                Mathf.PI
                * 2f
                * i
                / segments;


            Vector2 point =
                center
                + new Vector2(
                    Mathf.Cos(angle),
                    Mathf.Sin(angle)
                )
                * radius;


            shockwaveRing.SetPosition(
                i,
                point
            );
        }
    }


    // ==================================================
    // Color
    // ==================================================

    private Color GetEnemyColor()
    {
        if (InkMap.Instance != null)
        {
            Color color =
                InkMap.Instance
                    .enemyInkColor;


            color =
                Color.Lerp(
                    color,
                    Color.black,
                    0.20f
                );


            color.a =
                1f;


            return color;
        }


        return new Color(
            0.8f,
            0.05f,
            0.2f,
            1f
        );
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