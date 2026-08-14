using System.Collections;
using UnityEngine;

public class PlayerFloorSpawnVisual : MonoBehaviour
{
    // ==================================================
    // References
    // ==================================================

    [Header("References")]

    [Tooltip(
        "HumanVisual의 SpriteRenderer 추천"
    )]
    public SpriteRenderer referenceRenderer;


    // ==================================================
    // Spawn Ink
    // ==================================================

    [Header("Spawn Ink")]

    public bool paintSpawnInk = true;

    public float spawnInkRadius = 0.85f;

    public int spawnInkSplatCount = 18;


    // ==================================================
    // Player Pop
    // ==================================================

    [Header("Player Pop")]

    public float emergeDuration = 0.22f;

    public float popOvershoot = 1.15f;

    public float settleDuration = 0.12f;

    // ==================================================
    // Death VFX
    // ==================================================

    [Header("Death VFX")]

    [Tooltip("죽는 순간 살짝 커지는 크기")]
    public float deathPopScale = 1.18f;

    [Tooltip("죽는 순간 팽창 시간")]
    public float deathPopDuration = 0.07f;

    [Tooltip("팽창 후 완전히 사라지는 시간")]
    public float deathCollapseDuration = 0.18f;

    [Tooltip("사망 시 좌우 흔들림 각도")]
    public float deathShakeAngle = 7f;

    [Tooltip("사망 Primary Ring 최종 크기")]
    public float deathPrimaryEndRadius = 1.15f;

    [Tooltip("사망 Secondary Ring 최종 크기")]
    public float deathSecondaryEndRadius = 0.85f;

    [Tooltip("두 번째 Ring이 조금 늦게 발생하는 시간")]
    public float deathSecondaryDelay = 0.035f;


    // ==================================================
    // Ring
    // ==================================================

    [Header("Spawn Rings")]

    public float primaryStartRadius = 0.10f;

    public float primaryEndRadius = 0.90f;

    public float primaryStartWidth = 0.12f;


    public float secondaryStartRadius = 0.08f;

    public float secondaryEndRadius = 0.65f;

    public float secondaryStartWidth = 0.075f;

    public float secondaryDelay = 0.06f;


    // ==================================================
    // Rendering
    // ==================================================

    [Header("Rendering")]

    public int ringSegments = 36;

    public int sortingOrderOffset = 3;


    // ==================================================
    // Runtime
    // ==================================================

    private Vector3 originalScale;

    private Quaternion originalRotation;

    private bool prepared = false;


    private GameObject runtimeRoot;

    private LineRenderer primaryRing;

    private LineRenderer secondaryRing;




    // ==================================================
    // Awake
    // ==================================================

    private void Awake()
    {
        originalScale =
            transform.localScale;

        originalRotation =
            transform.localRotation;


        if (referenceRenderer == null)
        {
            referenceRenderer =
                GetComponentInChildren<SpriteRenderer>(
                    true
                );
        }


        CreateRuntimeVFX();
    }


    // ==================================================
    // Prepare
    // ==================================================

    public void PrepareHidden()
    {
        prepared =
            true;

        transform.localScale =
            Vector3.zero;

        transform.localRotation =
            originalRotation;

        HideRings();
    }


    // ==================================================
    // Spawn
    // ==================================================

    public IEnumerator PlaySpawn()
    {
        if (!prepared)
        {
            PrepareHidden();
        }


        Vector2 spawnPosition =
            transform.position;


        // ==========================================
        // 바닥 Player Ink 폭발
        // ==========================================

        if (paintSpawnInk &&
            InkMap.Instance != null &&
            InkMap.Instance.IsReady)
        {
            InkMap.Instance.PaintExplosion(
                spawnPosition,
                spawnInkRadius,
                InkTeam.Player,
                spawnInkSplatCount
            );
        }


        Color effectColor =
            GetPlayerInkColor();


        // ==========================================
        // Ring ON
        // ==========================================

        if (primaryRing != null)
        {
            primaryRing.enabled =
                true;
        }


        if (secondaryRing != null)
        {
            secondaryRing.enabled =
                true;
        }


        // ==========================================
        // Player 등장
        // 0 → Overshoot
        // ==========================================

        float timer =
            0f;


        float safeEmergeDuration =
            Mathf.Max(
                emergeDuration,
                0.01f
            );


        while (timer <
               safeEmergeDuration)
        {
            timer +=
                Time.unscaledDeltaTime;


            float t =
                Mathf.Clamp01(
                    timer
                    / safeEmergeDuration
                );


            float eased =
                EaseOutCubic(
                    t
                );


            float playerScale =
                Mathf.Lerp(
                    0f,
                    popOvershoot,
                    eased
                );


            transform.localScale =
                originalScale
                * playerScale;


            // ======================================
            // Primary Ring
            // ======================================

            float primaryRadius =
                Mathf.Lerp(
                    primaryStartRadius,
                    primaryEndRadius,
                    eased
                );


            float primaryWidth =
                Mathf.Lerp(
                    primaryStartWidth,
                    0.015f,
                    t
                );


            Color primaryColor =
                effectColor;


            primaryColor.a =
                1f - t;


            UpdateRing(
                primaryRing,
                spawnPosition,
                primaryRadius,
                primaryWidth,
                primaryColor
            );


            // ======================================
            // Secondary Ring
            // ======================================

            float secondaryT =
                Mathf.InverseLerp(
                    secondaryDelay,
                    safeEmergeDuration,
                    timer
                );


            secondaryT =
                Mathf.Clamp01(
                    secondaryT
                );


            float secondaryRadius =
                Mathf.Lerp(
                    secondaryStartRadius,
                    secondaryEndRadius,
                    secondaryT
                );


            float secondaryWidth =
                Mathf.Lerp(
                    secondaryStartWidth,
                    0.01f,
                    secondaryT
                );


            Color secondaryColor =
                effectColor;


            secondaryColor.a =
                (1f - secondaryT)
                * 0.75f;


            UpdateRing(
                secondaryRing,
                spawnPosition,
                secondaryRadius,
                secondaryWidth,
                secondaryColor
            );


            yield return null;
        }


        // ==========================================
        // Overshoot → 1.0
        // ==========================================

        timer =
            0f;


        float safeSettleDuration =
            Mathf.Max(
                settleDuration,
                0.01f
            );


        while (timer <
               safeSettleDuration)
        {
            timer +=
                Time.unscaledDeltaTime;


            float t =
                Mathf.Clamp01(
                    timer
                    / safeSettleDuration
                );


            float eased =
                EaseOutCubic(
                    t
                );


            float playerScale =
                Mathf.Lerp(
                    popOvershoot,
                    1f,
                    eased
                );


            transform.localScale =
                originalScale
                * playerScale;


            yield return null;
        }


        // ==========================================
        // 최종 복구
        // ==========================================

        transform.localScale =
            originalScale;


        HideRings();


        prepared =
            false;
    }

    // ==================================================
    // Death
    // ==================================================

    public IEnumerator PlayDeath()
    {
        Vector2 deathPosition =
            transform.position;


        Color effectColor =
            GetPlayerInkColor();


        prepared =
            false;


        transform.localRotation =
            originalRotation;


        // ==========================================
        // Primary Ring 시작
        // ==========================================

        if (primaryRing != null)
        {
            primaryRing.enabled =
                true;
        }


        // Secondary는 조금 늦게 시작
        if (secondaryRing != null)
        {
            secondaryRing.enabled =
                false;
        }


        // ==========================================
        // 1. 순간 팽창 + 떨림
        //
        // 1.0 → deathPopScale
        // ==========================================

        float timer =
            0f;


        float safePopDuration =
            Mathf.Max(
                deathPopDuration,
                0.01f
            );


        while (timer <
               safePopDuration)
        {
            timer +=
                Time.deltaTime;


            float t =
                Mathf.Clamp01(
                    timer
                    / safePopDuration
                );


            float eased =
                EaseOutCubic(
                    t
                );


            float playerScale =
                Mathf.Lerp(
                    1f,
                    deathPopScale,
                    eased
                );


            transform.localScale =
                originalScale
                * playerScale;


            // ======================================
            // 좌우 떨림
            // ======================================

            float shake =
                Mathf.Sin(
                    t
                    * Mathf.PI
                    * 3f
                )
                * deathShakeAngle;


            transform.localRotation =
                originalRotation
                * Quaternion.Euler(
                    0f,
                    0f,
                    shake
                );


            // ======================================
            // Primary Ring 초기 폭발
            // ======================================

            float primaryRadius =
                Mathf.Lerp(
                    primaryStartRadius,
                    deathPrimaryEndRadius
                        * 0.35f,
                    eased
                );


            Color primaryColor =
                effectColor;


            primaryColor.a =
                1f;


            UpdateRing(
                primaryRing,
                deathPosition,
                primaryRadius,
                primaryStartWidth,
                primaryColor
            );


            yield return null;
        }


        // ==========================================
        // 2. 빠른 축소 + Ring 폭발
        //
        // deathPopScale → 0
        // ==========================================

        timer =
            0f;


        float safeCollapseDuration =
            Mathf.Max(
                deathCollapseDuration,
                0.01f
            );


        while (timer <
               safeCollapseDuration)
        {
            timer +=
                Time.deltaTime;


            float t =
                Mathf.Clamp01(
                    timer
                    / safeCollapseDuration
                );


            // 처음에는 버티다가
            // 마지막에 빠르게 작아지는 느낌
            float collapseEase =
                t * t * t;


            float playerScale =
                Mathf.Lerp(
                    deathPopScale,
                    0f,
                    collapseEase
                );


            transform.localScale =
                originalScale
                * playerScale;


            // ======================================
            // 죽으면서 빠르게 떨린 후 안정
            // ======================================

            float shake =
                Mathf.Sin(
                    t
                    * Mathf.PI
                    * 5f
                )
                * deathShakeAngle
                * (1f - t);


            transform.localRotation =
                originalRotation
                * Quaternion.Euler(
                    0f,
                    0f,
                    shake
                );


            // ======================================
            // Primary Ring
            // ======================================

            float primaryRadius =
                Mathf.Lerp(
                    deathPrimaryEndRadius
                        * 0.35f,
                    deathPrimaryEndRadius,
                    EaseOutCubic(t)
                );


            float primaryWidth =
                Mathf.Lerp(
                    primaryStartWidth,
                    0.01f,
                    t
                );


            Color primaryColor =
                effectColor;


            primaryColor.a =
                1f - t;


            UpdateRing(
                primaryRing,
                deathPosition,
                primaryRadius,
                primaryWidth,
                primaryColor
            );


            // ======================================
            // Secondary Ring
            // ======================================

            float secondaryT =
                Mathf.InverseLerp(
                    deathSecondaryDelay,
                    safeCollapseDuration,
                    timer
                );


            secondaryT =
                Mathf.Clamp01(
                    secondaryT
                );


            if (secondaryRing != null &&
                secondaryT > 0f)
            {
                secondaryRing.enabled =
                    true;


                float secondaryRadius =
                    Mathf.Lerp(
                        secondaryStartRadius,
                        deathSecondaryEndRadius,
                        EaseOutCubic(
                            secondaryT
                        )
                    );


                float secondaryWidth =
                    Mathf.Lerp(
                        secondaryStartWidth,
                        0.01f,
                        secondaryT
                    );


                Color secondaryColor =
                    effectColor;


                secondaryColor.a =
                    (1f - secondaryT)
                    * 0.8f;


                UpdateRing(
                    secondaryRing,
                    deathPosition,
                    secondaryRadius,
                    secondaryWidth,
                    secondaryColor
                );
            }


            yield return null;
        }


        // ==========================================
        // Player 완전히 사라짐
        // ==========================================

        transform.localScale =
            Vector3.zero;


        transform.localRotation =
            originalRotation;


        HideRings();


        // 이후 PlaySpawn이
        // 0 Scale에서 정상적으로 시작하게 함
        prepared =
            true;
    }


    // ==================================================
    // Player Ink Color
    // ==================================================

    private Color GetPlayerInkColor()
    {
        if (InkMap.Instance != null)
        {
            Color color =
                InkMap.Instance.playerInkColor;


            // Ring은 조금 더 선명하게
            color.a = 1f;


            return color;
        }


        return new Color(
            0.1f,
            0.5f,
            1f,
            1f
        );
    }


    // ==================================================
    // Runtime VFX
    // ==================================================

    private void CreateRuntimeVFX()
    {
        // ==========================================
        // Player 자식으로 만들지 않는다.
        //
        // Player Scale = 0이어도
        // Spawn Effect가 보이게 하기 위함.
        // ==========================================

        runtimeRoot =
            new GameObject(
                "Runtime_PlayerFloorSpawnVFX"
            );


        primaryRing =
            CreateRing(
                runtimeRoot.transform,
                "PrimaryRing"
            );


        secondaryRing =
            CreateRing(
                runtimeRoot.transform,
                "SecondaryRing"
            );


        HideRings();
    }


    // ==================================================
    // Create Ring
    // ==================================================

    private LineRenderer CreateRing(
        Transform parent,
        string objectName)
    {
        GameObject ringObject =
            new GameObject(
                objectName
            );


        ringObject.transform.SetParent(
            parent,
            false
        );


        LineRenderer line =
            ringObject
                .AddComponent<LineRenderer>();


        line.useWorldSpace =
            true;


        line.loop =
            true;


        line.positionCount =
            Mathf.Max(
                8,
                ringSegments
            );


        line.numCornerVertices =
            4;


        line.numCapVertices =
            4;


        if (referenceRenderer != null)
        {
            line.sharedMaterial =
                referenceRenderer
                    .sharedMaterial;


            line.sortingLayerID =
                referenceRenderer
                    .sortingLayerID;


            line.sortingOrder =
                referenceRenderer
                    .sortingOrder
                + sortingOrderOffset;
        }


        return line;
    }


    // ==================================================
    // Update Ring
    // ==================================================

    private void UpdateRing(
        LineRenderer line,
        Vector2 center,
        float radius,
        float width,
        Color color)
    {
        if (line == null)
            return;


        int segments =
            Mathf.Max(
                8,
                ringSegments
            );


        if (line.positionCount !=
            segments)
        {
            line.positionCount =
                segments;
        }


        line.startWidth =
            width;


        line.endWidth =
            width;


        line.startColor =
            color;


        line.endColor =
            color;


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


            line.SetPosition(
                i,
                new Vector3(
                    point.x,
                    point.y,
                    0f
                )
            );
        }
    }


    // ==================================================
    // Hide
    // ==================================================

    private void HideRings()
    {
        if (primaryRing != null)
        {
            primaryRing.enabled =
                false;
        }


        if (secondaryRing != null)
        {
            secondaryRing.enabled =
                false;
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


    // ==================================================
    // Cleanup
    // ==================================================

    private void OnDestroy()
    {
        if (runtimeRoot != null)
        {
            Destroy(
                runtimeRoot
            );
        }
    }
}