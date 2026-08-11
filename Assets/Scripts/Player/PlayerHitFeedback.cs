using System.Collections;
using UnityEngine;

public class PlayerHitFeedback : MonoBehaviour
{
    [Header("References")]
    public PlayerShield playerShield;

    [Tooltip("HumanVisual SpriteRenderer")]
    public SpriteRenderer humanRenderer;

    [Tooltip("SwimVisual SpriteRenderer")]
    public SpriteRenderer swimRenderer;

    [Tooltip("Ink 방울 Sprite를 가져올 SpriteRenderer")]
    public SpriteRenderer splatSourceRenderer;

    [Tooltip("Material/Sorting 기준으로 사용할 Renderer")]
    public SpriteRenderer referenceRenderer;


    // ==================================================
    // Character Flash
    // ==================================================

    [Header("Character Flash")]

    public Color hitFlashColor =
        new Color(
            1f,
            0.15f,
            0.32f,
            1f
        );

    [Tooltip("피격색으로 변하는 시간")]
    public float flashInDuration = 0.035f;

    [Tooltip("원래색으로 돌아오는 시간")]
    public float flashOutDuration = 0.10f;


    // ==================================================
    // Ink Splash
    // ==================================================

    [Header("Directional Ink Splash")]

    public Color hitInkColor =
        new Color(
            1f,
            0.12f,
            0.28f,
            0.90f
        );


    [Tooltip("Hit 때 생성할 Ink 방울 개수")]
    public int splatCount = 10;


    [Tooltip("Player 중심에서 Hit 위치까지 거리")]
    public float contactOffset = 0.32f;


    public float splatMinSpeed = 1.3f;
    public float splatMaxSpeed = 2.8f;


    public float splatMinScale = 0.055f;
    public float splatMaxScale = 0.15f;


    public float splatDuration = 0.32f;


    [Range(0f, 90f)]
    [Tooltip("맞은 방향을 중심으로 퍼지는 각도")]
    public float spreadAngle = 55f;


    [Tooltip("Hit Effect의 Sorting Order")]
    public int splatSortingOrder = 10;


    // ==================================================
    // Runtime
    // ==================================================

    private Color humanBaseColor;
    private Color swimBaseColor;


    private Coroutine flashRoutine;

    private Transform splatRoot;


    // ==================================================
    // Awake
    // ==================================================

    private void Awake()
    {
        if (playerShield == null)
        {
            playerShield =
                GetComponent<PlayerShield>();
        }


        if (humanRenderer != null)
        {
            humanBaseColor =
                humanRenderer.color;
        }


        if (swimRenderer != null)
        {
            swimBaseColor =
                swimRenderer.color;
        }


        GameObject rootObject =
            new GameObject(
                "Runtime_PlayerHitSplats"
            );


        splatRoot =
            rootObject.transform;


        splatRoot.position =
            Vector3.zero;
    }


    // ==================================================
    // Events
    // ==================================================

    private void OnEnable()
    {
        if (playerShield == null)
        {
            playerShield =
                GetComponent<PlayerShield>();
        }


        if (playerShield == null)
            return;


        playerShield.ShieldHitDirectional +=
            OnPlayerHit;
    }


    private void OnDisable()
    {
        if (playerShield == null)
            return;


        playerShield.ShieldHitDirectional -=
            OnPlayerHit;
    }


    // ==================================================
    // Hit
    // ==================================================

    private void OnPlayerHit(
        Vector2 hitSourcePosition)
    {
        // ==========================================
        // Character Flash
        // ==========================================

        if (flashRoutine != null)
        {
            StopCoroutine(
                flashRoutine
            );
        }


        flashRoutine =
            StartCoroutine(
                FlashRoutine()
            );


        // ==========================================
        // Directional Ink
        // ==========================================

        SpawnDirectionalSplats(
            hitSourcePosition
        );
    }


    // ==================================================
    // Flash
    // ==================================================

    private IEnumerator FlashRoutine()
    {
        float timer =
            0f;


        // ==========================================
        // 빠르게 Hit Color
        // ==========================================

        while (timer <
               flashInDuration)
        {
            timer +=
                Time.deltaTime;


            float t =
                Mathf.Clamp01(
                    timer
                    /
                    Mathf.Max(
                        flashInDuration,
                        0.001f
                    )
                );


            ApplyFlashColor(
                t
            );


            yield return null;
        }


        // ==========================================
        // 원래 색으로 복귀
        // ==========================================

        timer =
            0f;


        while (timer <
               flashOutDuration)
        {
            timer +=
                Time.deltaTime;


            float t =
                Mathf.Clamp01(
                    timer
                    /
                    Mathf.Max(
                        flashOutDuration,
                        0.001f
                    )
                );


            ApplyFlashColor(
                1f - t
            );


            yield return null;
        }


        RestoreBodyColors();


        flashRoutine =
            null;
    }


    // ==================================================
    // Apply Flash
    // ==================================================

    private void ApplyFlashColor(
        float amount)
    {
        amount =
            Mathf.Clamp01(
                amount
            );


        if (humanRenderer != null)
        {
            humanRenderer.color =
                Color.Lerp(
                    humanBaseColor,
                    hitFlashColor,
                    amount
                );
        }


        if (swimRenderer != null)
        {
            swimRenderer.color =
                Color.Lerp(
                    swimBaseColor,
                    hitFlashColor,
                    amount
                );
        }
    }


    private void RestoreBodyColors()
    {
        if (humanRenderer != null)
        {
            humanRenderer.color =
                humanBaseColor;
        }


        if (swimRenderer != null)
        {
            swimRenderer.color =
                swimBaseColor;
        }
    }


    // ==================================================
    // Directional Splats
    // ==================================================

    private void SpawnDirectionalSplats(
        Vector2 hitSourcePosition)
    {
        if (splatSourceRenderer == null ||
            splatSourceRenderer.sprite == null)
        {
            return;
        }


        Vector2 playerPosition =
            transform.position;


        // Player -> 공격자 방향
        Vector2 towardSource =
            hitSourcePosition
            - playerPosition;


        // 방향 정보를 얻을 수 없는 공격이라면
        // 랜덤 방향 사용
        if (towardSource.sqrMagnitude <
            0.0001f)
        {
            towardSource =
                Random.insideUnitCircle
                    .normalized;
        }
        else
        {
            towardSource.Normalize();
        }


        // ==========================================
        // 공격과 접촉한 Player 가장자리
        // ==========================================

        Vector2 contactPosition =
            playerPosition
            + towardSource
            * contactOffset;


        // ==========================================
        // 공격이 들어온 반대 방향으로
        // Ink가 튀도록 설정
        // ==========================================

        Vector2 mainSprayDirection =
            -towardSource;


        int count =
            Mathf.Max(
                1,
                splatCount
            );


        for (int i = 0;
             i < count;
             i++)
        {
            float randomAngle =
                Random.Range(
                    -spreadAngle,
                    spreadAngle
                );


            Vector2 direction =
                RotateVector(
                    mainSprayDirection,
                    randomAngle
                );


            // 완전히 똑같은 방향 방지
            direction +=
                Random.insideUnitCircle
                * 0.18f;


            direction.Normalize();


            float speed =
                Random.Range(
                    splatMinSpeed,
                    splatMaxSpeed
                );


            float scale =
                Random.Range(
                    splatMinScale,
                    splatMaxScale
                );


            CreateSplat(
                contactPosition,
                direction,
                speed,
                scale
            );
        }
    }


    // ==================================================
    // Splat Object
    // ==================================================

    private void CreateSplat(
        Vector2 position,
        Vector2 direction,
        float speed,
        float scale)
    {
        GameObject splatObject =
            new GameObject(
                "HitInkSplat"
            );


        splatObject.transform.SetParent(
            splatRoot,
            true
        );


        splatObject.transform.position =
            new Vector3(
                position.x,
                position.y,
                0f
            );


        SpriteRenderer renderer =
            splatObject
                .AddComponent<SpriteRenderer>();


        renderer.sprite =
            splatSourceRenderer.sprite;


        renderer.color =
            hitInkColor;


        if (referenceRenderer != null)
        {
            renderer.sharedMaterial =
                referenceRenderer.sharedMaterial;


            renderer.sortingLayerID =
                referenceRenderer.sortingLayerID;
        }


        renderer.sortingOrder =
            splatSortingOrder;


        splatObject.transform.localScale =
            new Vector3(
                scale
                * Random.Range(
                    0.7f,
                    1.6f
                ),

                scale
                * Random.Range(
                    0.7f,
                    1.6f
                ),

                1f
            );


        StartCoroutine(
            AnimateSplat(
                splatObject.transform,
                renderer,
                direction,
                speed
            )
        );
    }


    // ==================================================
    // Splat Animation
    // ==================================================

    private IEnumerator AnimateSplat(
        Transform splatTransform,
        SpriteRenderer renderer,
        Vector2 direction,
        float speed)
    {
        float timer =
            0f;


        Vector3 originalScale =
            splatTransform.localScale;


        Color originalColor =
            renderer.color;


        float rotationSpeed =
            Random.Range(
                -360f,
                360f
            );


        while (timer <
               splatDuration)
        {
            if (splatTransform == null ||
                renderer == null)
            {
                yield break;
            }


            timer +=
                Time.deltaTime;


            float t =
                Mathf.Clamp01(
                    timer
                    /
                    Mathf.Max(
                        splatDuration,
                        0.001f
                    )
                );


            // ======================================
            // 처음 빠르게 → 점점 느려짐
            // ======================================

            float moveMultiplier =
                1f - t;


            Vector2 movement =
                direction
                * speed
                * moveMultiplier
                * Time.deltaTime;


            splatTransform.position +=
                (Vector3)movement;


            // ======================================
            // 회전
            // ======================================

            splatTransform.Rotate(
                0f,
                0f,
                rotationSpeed
                * Time.deltaTime
            );


            // ======================================
            // 조금 커졌다가 작아짐
            // ======================================

            float sizeMultiplier =
                Mathf.Lerp(
                    1f,
                    0.30f,
                    t
                );


            splatTransform.localScale =
                originalScale
                * sizeMultiplier;


            // ======================================
            // Fade
            // ======================================

            Color color =
                originalColor;


            color.a =
                originalColor.a
                * (1f - t);


            renderer.color =
                color;


            yield return null;
        }


        if (splatTransform != null)
        {
            Destroy(
                splatTransform.gameObject
            );
        }
    }


    // ==================================================
    // Vector Rotation
    // ==================================================

    private Vector2 RotateVector(
        Vector2 vector,
        float degrees)
    {
        float radians =
            degrees
            * Mathf.Deg2Rad;


        float cos =
            Mathf.Cos(
                radians
            );


        float sin =
            Mathf.Sin(
                radians
            );


        return new Vector2(
            vector.x * cos
            - vector.y * sin,

            vector.x * sin
            + vector.y * cos
        );
    }


    // ==================================================
    // Cleanup
    // ==================================================

    private void OnDestroy()
    {
        if (splatRoot != null)
        {
            Destroy(
                splatRoot.gameObject
            );
        }
    }
}