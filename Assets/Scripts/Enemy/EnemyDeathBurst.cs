using UnityEngine;

public class EnemyDeathBurst : MonoBehaviour
{
    private class Fragment
    {
        public Transform transform;
        public SpriteRenderer renderer;

        public Vector2 direction;

        public float speed;
        public float rotationSpeed;

        public Vector3 startScale;
        public Color startColor;
    }


    private Fragment[] fragments;

    private Transform coreTransform;
    private SpriteRenderer coreRenderer;

    private Vector3 coreStartScale;
    private Color coreStartColor;


    private float duration;
    private bool initialized = false;
    private float timer;


    // ==================================================
    // Initialize
    // ==================================================

    public void Initialize(
        Sprite sprite,
        Material material,
        int sortingLayerID,
        int sortingOrder,
        Color enemyColor,
        Vector3 worldScale,

        int fragmentCount,
        float fragmentMinSpeed,
        float fragmentMaxSpeed,
        float fragmentMinScale,
        float fragmentMaxScale,
        float effectDuration)
    {
        duration =
            Mathf.Max(
                effectDuration,
                0.05f
            );


        CreateCore(
            sprite,
            material,
            sortingLayerID,
            sortingOrder,
            enemyColor,
            worldScale
        );


        CreateFragments(
            sprite,
            material,
            sortingLayerID,
            sortingOrder + 1,
            enemyColor,
            fragmentCount,
            fragmentMinSpeed,
            fragmentMaxSpeed,
            fragmentMinScale,
            fragmentMaxScale
        );
        initialized = true;
    }


    // ==================================================
    // Core
    // ==================================================

    private void CreateCore(
        Sprite sprite,
        Material material,
        int sortingLayerID,
        int sortingOrder,
        Color enemyColor,
        Vector3 worldScale)
    {
        GameObject coreObject =
            new GameObject(
                "DeathCore"
            );


        coreObject.transform.SetParent(
            transform,
            false
        );


        coreObject.transform.localPosition =
            Vector3.zero;


        coreTransform =
            coreObject.transform;


        coreRenderer =
            coreObject
                .AddComponent<SpriteRenderer>();


        coreRenderer.sprite =
            sprite;


        coreRenderer.sharedMaterial =
            material;


        coreRenderer.sortingLayerID =
            sortingLayerID;


        coreRenderer.sortingOrder =
            sortingOrder;


        // 처음에는 밝게 터지는 느낌
        Color coreColor =
            Color.Lerp(
                enemyColor,
                Color.white,
                0.65f
            );


        coreColor.a =
            enemyColor.a;


        coreRenderer.color =
            coreColor;


        coreTransform.localScale =
            worldScale;


        coreStartScale =
            coreTransform.localScale;


        coreStartColor =
            coreRenderer.color;
    }


    // ==================================================
    // Fragments
    // ==================================================

    private void CreateFragments(
        Sprite sprite,
        Material material,
        int sortingLayerID,
        int sortingOrder,
        Color enemyColor,
        int fragmentCount,
        float fragmentMinSpeed,
        float fragmentMaxSpeed,
        float fragmentMinScale,
        float fragmentMaxScale)
    {
        int count =
            Mathf.Max(
                1,
                fragmentCount
            );


        fragments =
            new Fragment[count];


        for (int i = 0;
             i < count;
             i++)
        {
            float baseAngle =
                360f
                / count
                * i;


            float angle =
                baseAngle
                + Random.Range(
                    -20f,
                    20f
                );


            float radians =
                angle
                * Mathf.Deg2Rad;


            Vector2 direction =
                new Vector2(
                    Mathf.Cos(radians),
                    Mathf.Sin(radians)
                );


            GameObject fragmentObject =
                new GameObject(
                    "DeathFragment_"
                    + i
                );


            fragmentObject.transform.SetParent(
                transform,
                false
            );


            SpriteRenderer renderer =
                fragmentObject
                    .AddComponent<SpriteRenderer>();


            renderer.sprite =
                sprite;


            renderer.sharedMaterial =
                material;


            renderer.sortingLayerID =
                sortingLayerID;


            renderer.sortingOrder =
                sortingOrder;


            renderer.color =
                enemyColor;


            float scale =
                Random.Range(
                    fragmentMinScale,
                    fragmentMaxScale
                );


            // 길쭉하게 만들어
            // 파편처럼 보이게 한다.
            Vector3 fragmentScale =
                new Vector3(
                    scale
                    * Random.Range(
                        1.2f,
                        2.2f
                    ),

                    scale
                    * Random.Range(
                        0.45f,
                        0.85f
                    ),

                    1f
                );


            fragmentObject.transform.localScale =
                fragmentScale;


            // 중앙에 딱 붙지 않고
            // 몸 주변에서 시작
            fragmentObject.transform.localPosition =
                direction
                * Random.Range(
                    0.05f,
                    0.20f
                );


            Fragment fragment =
                new Fragment();


            fragment.transform =
                fragmentObject.transform;


            fragment.renderer =
                renderer;


            fragment.direction =
                direction;


            fragment.speed =
                Random.Range(
                    fragmentMinSpeed,
                    fragmentMaxSpeed
                );


            fragment.rotationSpeed =
                Random.Range(
                    -540f,
                    540f
                );


            fragment.startScale =
                fragmentScale;


            fragment.startColor =
                enemyColor;


            fragments[i] =
                fragment;
        }
    }


    // ==================================================
    // Update
    // ==================================================

    private void Update()
    {
        if (!initialized)
            return;

        timer +=
            Time.deltaTime;


        float t =
            Mathf.Clamp01(
                timer
                /
                duration
            );


        UpdateCore(
            t
        );


        UpdateFragments(
            t
        );


        if (timer >= duration)
        {
            Destroy(
                gameObject
            );
        }
    }


    // ==================================================
    // Core Animation
    // ==================================================

    private void UpdateCore(
        float t)
    {
        if (coreTransform == null ||
            coreRenderer == null)
        {
            return;
        }


        // ==========================================
        // 처음 순간적으로 커졌다가 사라짐
        // ==========================================

        float scaleMultiplier =
            Mathf.Lerp(
                1f,
                1.65f,
                EaseOutCubic(t)
            );


        coreTransform.localScale =
            coreStartScale
            * scaleMultiplier;


        Color color =
            coreStartColor;


        // Core는 전체 Duration보다
        // 더 빠르게 사라짐
        float coreFade =
            Mathf.Clamp01(
                t / 0.45f
            );


        color.a =
            coreStartColor.a
            * (1f - coreFade);


        coreRenderer.color =
            color;
    }


    // ==================================================
    // Fragment Animation
    // ==================================================

    private void UpdateFragments(
        float t)
    {
        if (fragments == null)
            return;


        float movementEase =
            EaseOutCubic(
                t
            );


        for (int i = 0;
             i < fragments.Length;
             i++)
        {
            Fragment fragment =
                fragments[i];


            if (fragment == null ||
                fragment.transform == null)
            {
                continue;
            }


            // ======================================
            // 밖으로 빠르게 터지고 감속
            // ======================================

            Vector3 targetPosition =
                (Vector3)fragment.direction
                * fragment.speed
                * duration;


            fragment.transform.localPosition =
                Vector3.Lerp(
                    fragment.transform.localPosition,
                    targetPosition,
                    movementEase
                );


            // ======================================
            // 회전
            // ======================================

            fragment.transform.Rotate(
                0f,
                0f,
                fragment.rotationSpeed
                * Time.deltaTime
            );


            // ======================================
            // 점점 작아짐
            // ======================================

            float scaleMultiplier =
                Mathf.Lerp(
                    1f,
                    0.15f,
                    t
                );


            fragment.transform.localScale =
                fragment.startScale
                * scaleMultiplier;


            // ======================================
            // Fade
            // ======================================

            Color color =
                fragment.startColor;


            color.a =
                fragment.startColor.a
                * (1f - t);


            fragment.renderer.color =
                color;
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