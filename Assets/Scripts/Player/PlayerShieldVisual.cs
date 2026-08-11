using System.Collections;
using UnityEngine;

public class PlayerShieldVisual : MonoBehaviour
{
    [Header("References")]
    public PlayerShield playerShield;

    public Transform shieldEffect;
    public SpriteRenderer shieldRenderer;


    [Header("Shield Appearance")]
    [Range(0f, 1f)]
    public float shieldMaxAlpha = 0.38f;


    [Header("Shield Break")]
    [Tooltip("Shield가 나타나는 준비 시간")]
    public float breakAppearDuration = 0.08f;

    [Tooltip("Shield가 깨지며 사라지는 시간")]
    public float breakDuration = 0.22f;

    public float breakStartScale = 0.85f;
    public float breakPeakScale = 1.12f;
    public float breakEndScale = 1.45f;


    [Header("Break Fragments")]
    public int breakFragmentCount = 12;

    public float fragmentMinSpeed = 2.0f;
    public float fragmentMaxSpeed = 4.0f;

    public float fragmentDuration = 0.42f;

    public float fragmentMinScale = 0.10f;
    public float fragmentMaxScale = 0.23f;

    public float fragmentRotationSpeed = 360f;


    [Header("Shield Restore")]
    [Tooltip("외곽 조각들이 Player에게 모이는 시간")]
    public float restoreGatherDuration = 0.42f;

    [Tooltip("완성된 Shield가 Pulse하는 시간")]
    public float restorePulseDuration = 0.30f;

    public float restoreSpawnRadiusMin = 1.2f;
    public float restoreSpawnRadiusMax = 2.0f;

    public float restoreStartScale = 0.78f;
    public float restorePulseScale = 1.18f;
    public float restoreEndScale = 1f;


    private Vector3 shieldBaseScale;
    private Color shieldBaseColor;

    private Coroutine currentRoutine;

    private Transform fragmentRoot;


    // ==================================================
    // Fragment Data
    // ==================================================

    private class ShieldFragment
    {
        public Transform transform;
        public SpriteRenderer renderer;

        public Vector3 startPosition;
        public Vector3 targetPosition;

        public Vector2 direction;

        public float speed;
        public float rotationSpeed;

        public Vector3 baseScale;
    }


    private void Awake()
    {
        if (playerShield == null)
        {
            playerShield =
                GetComponent<PlayerShield>();
        }


        if (shieldEffect != null)
        {
            shieldBaseScale =
                shieldEffect.localScale;
        }


        if (shieldRenderer != null)
        {
            shieldBaseColor =
                shieldRenderer.color;
        }


        // 파편을 담을 별도 Root 생성
        GameObject rootObject =
            new GameObject(
                "ShieldFragments"
            );


        fragmentRoot =
            rootObject.transform;


        fragmentRoot.SetParent(
            transform,
            false
        );


        fragmentRoot.localPosition =
            Vector3.zero;


        // 평소 Shield는 보이지 않음
        HideMainShield();
    }


    private void OnEnable()
    {
        if (playerShield == null)
        {
            playerShield =
                GetComponent<PlayerShield>();
        }


        if (playerShield == null)
            return;


        playerShield.ShieldBroken +=
            OnShieldBroken;


        playerShield.ShieldRestored +=
            OnShieldRestored;
    }


    private void OnDisable()
    {
        if (playerShield == null)
            return;


        playerShield.ShieldBroken -=
            OnShieldBroken;


        playerShield.ShieldRestored -=
            OnShieldRestored;
    }


    // ==================================================
    // Events
    // ==================================================

    private void OnShieldBroken()
    {
        StopCurrentEffect();


        currentRoutine =
            StartCoroutine(
                ShieldBreakRoutine()
            );
    }


    private void OnShieldRestored()
    {
        StopCurrentEffect();


        currentRoutine =
            StartCoroutine(
                ShieldRestoreRoutine()
            );
    }


    // ==================================================
    // Shield Break
    // ==================================================

    private IEnumerator ShieldBreakRoutine()
    {
        if (shieldEffect == null ||
            shieldRenderer == null)
        {
            yield break;
        }


        ClearFragments();


        // ==========================================
        // STEP 1
        // Shield가 순간적으로 모습을 드러냄
        // ==========================================

        shieldEffect.gameObject
            .SetActive(true);


        float timer =
            0f;


        while (timer <
               breakAppearDuration)
        {
            timer +=
                Time.unscaledDeltaTime;


            float t =
                Mathf.Clamp01(
                    timer
                    /
                    Mathf.Max(
                        breakAppearDuration,
                        0.001f
                    )
                );


            float scale =
                Mathf.Lerp(
                    breakStartScale,
                    breakPeakScale,
                    EaseOutBack(t)
                );


            shieldEffect.localScale =
                shieldBaseScale
                * scale;


            SetShieldAlpha(
                Mathf.Lerp(
                    0f,
                    shieldMaxAlpha,
                    t
                )
            );


            yield return null;
        }


        // ==========================================
        // STEP 2
        // 파편 생성
        // ==========================================

        ShieldFragment[] fragments =
            CreateBreakFragments();


        // ==========================================
        // STEP 3
        // Shield 본체가 터지면서 사라짐
        // ==========================================

        timer = 0f;


        float totalDuration =
            Mathf.Max(
                breakDuration,
                fragmentDuration
            );


        while (timer <
               totalDuration)
        {
            timer +=
                Time.unscaledDeltaTime;


            // --------------------------------------
            // Main Shield
            // --------------------------------------

            float shieldT =
                Mathf.Clamp01(
                    timer
                    /
                    Mathf.Max(
                        breakDuration,
                        0.001f
                    )
                );


            float shieldScale =
                Mathf.Lerp(
                    breakPeakScale,
                    breakEndScale,
                    shieldT
                );


            shieldEffect.localScale =
                shieldBaseScale
                * shieldScale;


            SetShieldAlpha(
                shieldMaxAlpha
                * (1f - shieldT)
            );


            // --------------------------------------
            // Fragments
            // --------------------------------------

            float fragmentT =
                Mathf.Clamp01(
                    timer
                    /
                    Mathf.Max(
                        fragmentDuration,
                        0.001f
                    )
                );


            UpdateBreakFragments(
                fragments,
                fragmentT
            );


            yield return null;
        }


        HideMainShield();

        DestroyFragments(
            fragments
        );


        currentRoutine =
            null;
    }


    // ==================================================
    // Break Fragment 생성
    // ==================================================

    private ShieldFragment[]
        CreateBreakFragments()
    {
        int count =
            Mathf.Max(
                1,
                breakFragmentCount
            );


        ShieldFragment[] fragments =
            new ShieldFragment[count];


        for (int i = 0;
             i < count;
             i++)
        {
            float angle =
                360f
                / count
                * i;


            // 약간 랜덤하게 틀어줌
            angle +=
                Random.Range(
                    -12f,
                    12f
                );


            float radians =
                angle
                * Mathf.Deg2Rad;


            Vector2 direction =
                new Vector2(
                    Mathf.Cos(radians),
                    Mathf.Sin(radians)
                );


            ShieldFragment fragment =
                CreateFragmentObject(
                    "BreakFragment_"
                    + i
                );


            fragment.direction =
                direction;


            fragment.speed =
                Random.Range(
                    fragmentMinSpeed,
                    fragmentMaxSpeed
                );


            fragment.rotationSpeed =
                Random.Range(
                    -fragmentRotationSpeed,
                    fragmentRotationSpeed
                );


            fragment.startPosition =
                direction
                * Random.Range(
                    0.30f,
                    0.55f
                );


            fragment.transform.localPosition =
                fragment.startPosition;


            float scale =
                Random.Range(
                    fragmentMinScale,
                    fragmentMaxScale
                );


            // 원 Sprite를 길쭉하게 만들어
            // 에너지 파편처럼 보이게 함
            fragment.baseScale =
                new Vector3(
                    scale
                    * Random.Range(
                        1.4f,
                        2.2f
                    ),

                    scale
                    * Random.Range(
                        0.45f,
                        0.8f
                    ),

                    1f
                );


            fragment.transform.localScale =
                fragment.baseScale;


            fragments[i] =
                fragment;
        }


        return fragments;
    }


    private void UpdateBreakFragments(
        ShieldFragment[] fragments,
        float t)
    {
        float movementT =
            EaseOutCubic(t);


        foreach (ShieldFragment fragment
                 in fragments)
        {
            if (fragment == null ||
                fragment.transform == null)
            {
                continue;
            }


            float distance =
                fragment.speed
                * fragmentDuration
                * movementT;


            fragment.transform
                .localPosition =
                fragment.startPosition

                + (Vector3)(
                    fragment.direction
                    * distance
                );


            fragment.transform.Rotate(
                0f,
                0f,
                fragment.rotationSpeed
                * Time.unscaledDeltaTime
            );


            fragment.transform.localScale =
                fragment.baseScale
                * Mathf.Lerp(
                    1f,
                    0.35f,
                    t
                );


            SetRendererAlpha(
                fragment.renderer,
                shieldMaxAlpha
                * (1f - t)
            );
        }
    }


    // ==================================================
    // Shield Restore
    // ==================================================

    private IEnumerator ShieldRestoreRoutine()
    {
        if (shieldEffect == null ||
            shieldRenderer == null)
        {
            yield break;
        }


        ClearFragments();


        // ==========================================
        // STEP 1
        // 외곽에서 조각 생성
        // ==========================================

        ShieldFragment[] fragments =
            CreateRestoreFragments();


        float timer =
            0f;


        // ==========================================
        // STEP 2
        // 조각들이 Player에게 모임
        // ==========================================

        while (timer <
               restoreGatherDuration)
        {
            timer +=
                Time.unscaledDeltaTime;


            float t =
                Mathf.Clamp01(
                    timer
                    /
                    Mathf.Max(
                        restoreGatherDuration,
                        0.001f
                    )
                );


            float moveT =
                EaseInCubic(t);


            foreach (ShieldFragment fragment
                     in fragments)
            {
                if (fragment == null ||
                    fragment.transform == null)
                {
                    continue;
                }


                fragment.transform
                    .localPosition =
                    Vector3.Lerp(
                        fragment.startPosition,
                        fragment.targetPosition,
                        moveT
                    );


                fragment.transform.Rotate(
                    0f,
                    0f,
                    fragment.rotationSpeed
                    * Time.unscaledDeltaTime
                );


                // 처음 나타나면서 밝아짐
                float alpha =
                    Mathf.SmoothStep(
                        0f,
                        shieldMaxAlpha,
                        Mathf.Clamp01(
                            t * 2f
                        )
                    );


                SetRendererAlpha(
                    fragment.renderer,
                    alpha
                );


                // 중심으로 갈수록 조금 작아짐
                fragment.transform
                    .localScale =
                    fragment.baseScale
                    * Mathf.Lerp(
                        1f,
                        0.45f,
                        t
                    );
            }


            yield return null;
        }


        DestroyFragments(
            fragments
        );


        // ==========================================
        // STEP 3
        // 완성 Shield 등장 + Pulse
        // ==========================================

        shieldEffect.gameObject
            .SetActive(true);


        timer =
            0f;


        while (timer <
               restorePulseDuration)
        {
            timer +=
                Time.unscaledDeltaTime;


            float t =
                Mathf.Clamp01(
                    timer
                    /
                    Mathf.Max(
                        restorePulseDuration,
                        0.001f
                    )
                );


            // --------------------------------------
            // 0 → Pulse → 1
            // --------------------------------------

            float pulse =
                Mathf.Sin(
                    t
                    * Mathf.PI
                );


            float scale;


            if (t < 0.5f)
            {
                scale =
                    Mathf.Lerp(
                        restoreStartScale,
                        restorePulseScale,
                        t * 2f
                    );
            }
            else
            {
                scale =
                    Mathf.Lerp(
                        restorePulseScale,
                        restoreEndScale,
                        (t - 0.5f) * 2f
                    );
            }


            shieldEffect.localScale =
                shieldBaseScale
                * scale;


            // 중간에 가장 강하고
            // 끝에서 다시 사라짐
            SetShieldAlpha(
                shieldMaxAlpha
                * pulse
            );


            yield return null;
        }


        HideMainShield();


        currentRoutine =
            null;
    }


    // ==================================================
    // Restore Fragment 생성
    // ==================================================

    private ShieldFragment[]
        CreateRestoreFragments()
    {
        int count =
            Mathf.Max(
                1,
                breakFragmentCount
            );


        ShieldFragment[] fragments =
            new ShieldFragment[count];


        for (int i = 0;
             i < count;
             i++)
        {
            float angle =
                360f
                / count
                * i;


            angle +=
                Random.Range(
                    -15f,
                    15f
                );


            float radians =
                angle
                * Mathf.Deg2Rad;


            Vector2 direction =
                new Vector2(
                    Mathf.Cos(radians),
                    Mathf.Sin(radians)
                );


            ShieldFragment fragment =
                CreateFragmentObject(
                    "RestoreFragment_"
                    + i
                );


            float radius =
                Random.Range(
                    restoreSpawnRadiusMin,
                    restoreSpawnRadiusMax
                );


            fragment.startPosition =
                direction
                * radius;


            fragment.targetPosition =
                direction
                * Random.Range(
                    0.20f,
                    0.40f
                );


            fragment.transform
                .localPosition =
                fragment.startPosition;


            fragment.rotationSpeed =
                Random.Range(
                    -fragmentRotationSpeed,
                    fragmentRotationSpeed
                );


            float scale =
                Random.Range(
                    fragmentMinScale,
                    fragmentMaxScale
                );


            fragment.baseScale =
                new Vector3(
                    scale
                    * Random.Range(
                        1.3f,
                        2.0f
                    ),

                    scale
                    * Random.Range(
                        0.45f,
                        0.75f
                    ),

                    1f
                );


            fragment.transform.localScale =
                fragment.baseScale;


            SetRendererAlpha(
                fragment.renderer,
                0f
            );


            fragments[i] =
                fragment;
        }


        return fragments;
    }


    // ==================================================
    // Fragment Object
    // ==================================================

    private ShieldFragment
        CreateFragmentObject(
            string objectName)
    {
        GameObject fragmentObject =
            new GameObject(
                objectName
            );


        fragmentObject.transform
            .SetParent(
                fragmentRoot,
                false
            );


        SpriteRenderer renderer =
            fragmentObject
                .AddComponent<SpriteRenderer>();


        renderer.sprite =
            shieldRenderer.sprite;


        renderer.sharedMaterial =
            shieldRenderer.sharedMaterial;


        renderer.sortingLayerID =
            shieldRenderer.sortingLayerID;


        renderer.sortingOrder =
            shieldRenderer.sortingOrder
            + 1;


        renderer.color =
            shieldBaseColor;


        ShieldFragment fragment =
            new ShieldFragment();


        fragment.transform =
            fragmentObject.transform;


        fragment.renderer =
            renderer;


        return fragment;
    }


    // ==================================================
    // Main Shield Helpers
    // ==================================================

    private void HideMainShield()
    {
        if (shieldEffect == null)
            return;


        shieldEffect.localScale =
            shieldBaseScale;


        SetShieldAlpha(
            0f
        );


        shieldEffect.gameObject
            .SetActive(false);
    }


    private void SetShieldAlpha(
        float alpha)
    {
        SetRendererAlpha(
            shieldRenderer,
            alpha
        );
    }


    private void SetRendererAlpha(
        SpriteRenderer renderer,
        float alpha)
    {
        if (renderer == null)
            return;


        Color color =
            shieldBaseColor;


        color.a =
            Mathf.Clamp01(
                alpha
            );


        renderer.color =
            color;
    }


    // ==================================================
    // Cleanup
    // ==================================================

    private void StopCurrentEffect()
    {
        if (currentRoutine != null)
        {
            StopCoroutine(
                currentRoutine
            );


            currentRoutine =
                null;
        }


        ClearFragments();

        HideMainShield();
    }


    private void ClearFragments()
    {
        if (fragmentRoot == null)
            return;


        for (int i =
                 fragmentRoot.childCount - 1;
             i >= 0;
             i--)
        {
            Destroy(
                fragmentRoot
                    .GetChild(i)
                    .gameObject
            );
        }
    }


    private void DestroyFragments(
        ShieldFragment[] fragments)
    {
        if (fragments == null)
            return;


        foreach (ShieldFragment fragment
                 in fragments)
        {
            if (fragment == null ||
                fragment.transform == null)
            {
                continue;
            }


            Destroy(
                fragment.transform
                    .gameObject
            );
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


    private float EaseOutBack(
        float t)
    {
        const float c1 =
            1.70158f;


        const float c3 =
            c1 + 1f;


        float value =
            t - 1f;


        return
            1f
            + c3
            * value
            * value
            * value
            + c1
            * value
            * value;
    }
}