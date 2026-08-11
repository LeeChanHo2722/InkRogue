using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BossDeathVFX : MonoBehaviour
{
    // ==================================================
    // References
    // ==================================================

    [Header("References")]

    public Transform visualRoot;

    public SpriteRenderer referenceRenderer;


    // ==================================================
    // Flash
    // ==================================================

    [Header("Flash")]

    public float flashDuration =
        0.45f;

    public float flashFrequency =
        18f;

    public float flashScaleAmount =
        1.18f;


    // ==================================================
    // Explosion Ring
    // ==================================================

    [Header("Explosion Ring")]

    public int ringSegments =
        48;

    public float ringStartRadius =
        0.15f;

    public float ringEndRadius =
        2.8f;

    public float ringStartWidth =
        0.28f;

    public float ringDuration =
        0.55f;


    // ==================================================
    // Fragments
    // ==================================================

    [Header("Fragments")]

    public int fragmentCount =
        18;

    public float fragmentMinSpeed =
        1.6f;

    public float fragmentMaxSpeed =
        4.5f;

    public float fragmentMinSize =
        0.07f;

    public float fragmentMaxSize =
        0.22f;

    [Tooltip(
        "실제 시간 기준 파편 수명"
    )]
    public float fragmentLifetime =
        1.9f;

    public float fragmentRotationSpeed =
        420f;


    // ==================================================
    // Camera
    // ==================================================

    [Header("Camera")]

    public float cameraShakeDuration =
        0.45f;

    public float cameraShakeStrength =
        0.42f;


    // ==================================================
    // Runtime
    // ==================================================

    private LineRenderer explosionRing;

    private CameraFollow cameraFollow;


    private Color enemyColor;

    private Color originalColor;

    private Vector3 originalScale;


    private readonly List<Fragment>
        fragments =
            new List<Fragment>();


    private class Fragment
    {
        public GameObject root;

        public SpriteRenderer renderer;

        public Vector2 velocity;

        public float rotationSpeed;

        public float size;
    }


    // ==================================================
    // Awake
    // ==================================================

    private void Awake()
    {
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
                    .GetComponentInChildren<
                        SpriteRenderer
                    >(
                        true
                    );
        }


        originalScale =
            visualRoot.localScale;


        if (referenceRenderer != null)
        {
            originalColor =
                referenceRenderer.color;
        }


        if (InkMap.Instance != null)
        {
            enemyColor =
                InkMap.Instance.enemyInkColor;
        }
        else
        {
            enemyColor =
                new Color(
                    0.8f,
                    0.05f,
                    0.25f,
                    1f
                );
        }


        enemyColor.a =
            1f;


        if (Camera.main != null)
        {
            cameraFollow =
                Camera.main
                    .GetComponent<
                        CameraFollow
                    >();
        }


        CreateExplosionRing();
    }


    // ==================================================
    // Public
    // ==================================================

    public IEnumerator PlayDeathVFX()
    {
        if (cameraFollow != null)
        {
            cameraFollow.StartShake(
                cameraShakeDuration,
                cameraShakeStrength
            );
        }


        SpawnFragments();


        StartCoroutine(
            FragmentRoutine()
        );


        yield return StartCoroutine(
            FlashRoutine()
        );


        // Boss 본체는 Flash 이후 사라짐
        HideBossVisual();


        StartCoroutine(
            ExplosionRingRoutine()
        );


        // VFX 종료를 여기서 기다리지는 않는다.
        // BossBattleManager가 전체 Cinematic timing 담당.
    }


    // ==================================================
    // Flash
    // ==================================================

    private IEnumerator FlashRoutine()
    {
        float timer =
            0f;


        float safeDuration =
            Mathf.Max(
                flashDuration,
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


            float pulse =
                Mathf.Abs(
                    Mathf.Sin(
                        timer
                        * flashFrequency
                    )
                );


            if (referenceRenderer != null)
            {
                referenceRenderer.color =
                    Color.Lerp(
                        enemyColor,
                        Color.white,
                        pulse
                    );
            }


            if (visualRoot != null)
            {
                float scale =
                    Mathf.Lerp(
                        1f,
                        flashScaleAmount,
                        pulse
                        * (1f - t * 0.5f)
                    );


                visualRoot.localScale =
                    originalScale
                    * scale;
            }


            yield return null;
        }
    }


    // ==================================================
    // Hide Boss
    // ==================================================

    private void HideBossVisual()
    {
        if (visualRoot != null)
        {
            SpriteRenderer[] renderers =
                visualRoot
                    .GetComponentsInChildren<
                        SpriteRenderer
                    >(
                        true
                    );


            foreach (
                SpriteRenderer renderer
                in renderers)
            {
                renderer.enabled =
                    false;
            }
        }


        Collider2D[] colliders =
            GetComponentsInChildren<
                Collider2D
            >(
                true
            );


        foreach (
            Collider2D collider
            in colliders)
        {
            collider.enabled =
                false;
        }
    }


    // ==================================================
    // Ring
    // ==================================================

    private void CreateExplosionRing()
    {
        GameObject ringObject =
            new GameObject(
                "Runtime_BossDeathRing"
            );


        ringObject.transform.SetParent(
            transform,
            false
        );


        explosionRing =
            ringObject.AddComponent<
                LineRenderer
            >();


        explosionRing.useWorldSpace =
            true;


        explosionRing.loop =
            true;


        explosionRing.positionCount =
            Mathf.Max(
                16,
                ringSegments
            );


        explosionRing.numCornerVertices =
            4;


        explosionRing.enabled =
            false;


        if (referenceRenderer != null)
        {
            explosionRing.sharedMaterial =
                referenceRenderer
                    .sharedMaterial;


            explosionRing.sortingLayerID =
                referenceRenderer
                    .sortingLayerID;


            explosionRing.sortingOrder =
                referenceRenderer
                    .sortingOrder
                + 10;
        }
    }


    private IEnumerator ExplosionRingRoutine()
    {
        if (explosionRing == null)
            yield break;


        explosionRing.enabled =
            true;


        float timer =
            0f;


        float safeDuration =
            Mathf.Max(
                ringDuration,
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


            float radius =
                Mathf.Lerp(
                    ringStartRadius,
                    ringEndRadius,
                    EaseOutCubic(t)
                );


            float width =
                Mathf.Lerp(
                    ringStartWidth,
                    0.015f,
                    t
                );


            Color color =
                Color.Lerp(
                    Color.white,
                    enemyColor,
                    t
                );


            color.a =
                1f - t;


            DrawRing(
                radius,
                width,
                color
            );


            yield return null;
        }


        explosionRing.enabled =
            false;
    }


    private void DrawRing(
        float radius,
        float width,
        Color color)
    {
        int segments =
            Mathf.Max(
                16,
                ringSegments
            );


        explosionRing.positionCount =
            segments;


        explosionRing.startWidth =
            width;


        explosionRing.endWidth =
            width;


        explosionRing.startColor =
            color;


        explosionRing.endColor =
            color;


        Vector2 center =
            transform.position;


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


            explosionRing.SetPosition(
                i,
                point
            );
        }
    }


    // ==================================================
    // Fragments
    // ==================================================

    private void SpawnFragments()
    {
        for (int i = 0;
             i < fragmentCount;
             i++)
        {
            GameObject fragmentObject =
                new GameObject(
                    "BossFragment_"
                    + i
                );


            fragmentObject.transform.position =
                transform.position;


            SpriteRenderer renderer =
                fragmentObject
                    .AddComponent<
                        SpriteRenderer
                    >();


            if (referenceRenderer != null)
            {
                renderer.sprite =
                    referenceRenderer.sprite;


                renderer.sharedMaterial =
                    referenceRenderer
                        .sharedMaterial;


                renderer.sortingLayerID =
                    referenceRenderer
                        .sortingLayerID;


                renderer.sortingOrder =
                    referenceRenderer
                        .sortingOrder
                    + 8;
            }


            Color fragmentColor =
                Color.Lerp(
                    enemyColor,
                    Color.white,
                    Random.Range(
                        0f,
                        0.35f
                    )
                );


            renderer.color =
                fragmentColor;


            float size =
                Random.Range(
                    fragmentMinSize,
                    fragmentMaxSize
                );


            fragmentObject.transform.localScale =
                Vector3.one
                * size;


            Vector2 direction =
                Random.insideUnitCircle;


            if (direction.sqrMagnitude <
                0.01f)
            {
                direction =
                    Vector2.right;
            }


            direction.Normalize();


            Fragment fragment =
                new Fragment();


            fragment.root =
                fragmentObject;


            fragment.renderer =
                renderer;


            fragment.velocity =
                direction
                * Random.Range(
                    fragmentMinSpeed,
                    fragmentMaxSpeed
                );


            fragment.rotationSpeed =
                Random.Range(
                    -fragmentRotationSpeed,
                    fragmentRotationSpeed
                );


            fragment.size =
                size;


            fragments.Add(
                fragment
            );
        }
    }


    private IEnumerator FragmentRoutine()
    {
        float timer =
            0f;


        float safeLifetime =
            Mathf.Max(
                fragmentLifetime,
                0.01f
            );


        while (timer <
               safeLifetime)
        {
            // Lifetime / Fade는 실제 시간 기준.
            timer +=
                Time.unscaledDeltaTime;


            float t =
                Mathf.Clamp01(
                    timer
                    / safeLifetime
                );


            foreach (
                Fragment fragment
                in fragments)
            {
                if (fragment.root == null)
                    continue;


                // ==================================
                // 이동은 scaled DeltaTime 사용.
                //
                // 따라서 Slow Motion 때
                // 실제로 파편도 느리게 날아감.
                // ==================================

                fragment.root.transform.position +=
                    (Vector3)(
                        fragment.velocity
                        * Time.deltaTime
                    );


                fragment.root.transform.Rotate(
                    0f,
                    0f,
                    fragment.rotationSpeed
                    * Time.deltaTime
                );


                if (fragment.renderer != null)
                {
                    Color color =
                        fragment.renderer.color;


                    color.a =
                        Mathf.Lerp(
                            1f,
                            0f,
                            Mathf.InverseLerp(
                                0.45f,
                                1f,
                                t
                            )
                        );


                    fragment.renderer.color =
                        color;
                }


                float scale =
                    Mathf.Lerp(
                        fragment.size,
                        fragment.size * 0.25f,
                        t
                    );


                fragment.root.transform.localScale =
                    Vector3.one
                    * scale;
            }


            yield return null;
        }


        foreach (
            Fragment fragment
            in fragments)
        {
            if (fragment.root != null)
            {
                Destroy(
                    fragment.root
                );
            }
        }


        fragments.Clear();
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