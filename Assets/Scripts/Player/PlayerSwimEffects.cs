using System.Collections;
using UnityEngine;

public class PlayerSwimEffects : MonoBehaviour
{
    [Header("References")]
    public PlayerDive playerDive;
    public Rigidbody2D playerRb;

    [Tooltip("SwimEffects/WakeTrail의 TrailRenderer")]
    public TrailRenderer wakeTrail;

    [Tooltip("SwimVisual의 SpriteRenderer")]
    public SpriteRenderer swimRenderer;


    // ==================================================
    // Wake Trail
    // ==================================================

    [Header("Wake Trail")]
    [Tooltip("흔적이 화면에 남아있는 시간")]
    public float wakeTime = 0.38f;

    public float wakeStartWidth = 0.32f;
    public float wakeEndWidth = 0.04f;

    [Tooltip("새 Trail 점을 생성하는 최소 이동 거리")]
    public float wakeMinVertexDistance = 0.07f;

    [Range(0f, 1f)]
    public float wakeAlpha = 0.52f;


    // ==================================================
    // Ripple
    // ==================================================

    [Header("Ripple")]
    [Tooltip("물결 사이의 이동 거리")]
    public float rippleSpacing = 0.45f;

    [Tooltip("물결 하나가 사라질 때까지 걸리는 시간")]
    public float rippleDuration = 0.32f;

    public float rippleStartRadius = 0.13f;
    public float rippleEndRadius = 0.55f;

    public float rippleStartWidth = 0.055f;
    public float rippleEndWidth = 0.012f;

    [Range(0f, 1f)]
    public float rippleAlpha = 0.65f;

    [Range(0.3f, 1f)]
    [Tooltip("1이면 원, 낮을수록 납작한 물결")]
    public float rippleEllipseRatio = 0.62f;

    [Range(8, 64)]
    public int rippleSegments = 32;


    // ==================================================
    // Movement
    // ==================================================

    [Header("Activation")]
    [Tooltip("이 속도보다 느리면 흔적/물결 생성 안 함")]
    public float minimumMoveSpeed = 0.25f;

    [Tooltip("한 프레임에 생성 가능한 최대 Ripple")]
    public int maxRipplesPerFrame = 6;


    // ==================================================
    // Rendering
    // ==================================================

    [Header("Rendering")]
    [Tooltip("InkMap(-5)보다 위, Player(5)보다 아래 권장")]
    public int effectSortingOrder = -4;


    private bool wasDiving = false;

    private Vector2 lastRipplePosition;

    private Transform rippleRoot;

    private Color effectColor;


    // ==================================================
    // Awake
    // ==================================================

    private void Awake()
    {
        if (playerDive == null)
        {
            playerDive =
                GetComponent<PlayerDive>();
        }


        if (playerRb == null)
        {
            playerRb =
                GetComponent<Rigidbody2D>();
        }


        if (wakeTrail == null)
        {
            wakeTrail =
                GetComponentInChildren<TrailRenderer>(
                    true
                );
        }


        // ------------------------------------------
        // Ripple들은 Player의 자식으로 만들면
        // Player를 따라 움직여버리므로
        // Scene Root에 별도 Root 생성
        // ------------------------------------------

        GameObject rippleRootObject =
            new GameObject(
                "Runtime_SwimRipples"
            );


        rippleRoot =
            rippleRootObject.transform;


        rippleRoot.position =
            Vector3.zero;


        // ------------------------------------------
        // 현재 SwimVisual 색상 사용
        // ------------------------------------------

        if (swimRenderer != null)
        {
            effectColor =
                swimRenderer.color;
        }
        else
        {
            effectColor =
                new Color(
                    0.1f,
                    0.8f,
                    1f,
                    1f
                );
        }


        ConfigureWakeTrail();
    }


    // ==================================================
    // Update
    // ==================================================

    private void Update()
    {
        if (playerDive == null)
            return;


        bool isDiving =
            playerDive.IsDiving;


        float moveSpeed =
            0f;


        if (playerRb != null)
        {
            moveSpeed =
                playerRb
                    .linearVelocity
                    .magnitude;
        }


        // ==========================================
        // Player Ink에 실제 진입
        // ==========================================

        if (isDiving &&
            !wasDiving)
        {
            BeginDiveEffects();
        }


        // ==========================================
        // Player Ink에서 빠져나옴
        //
        // Swim Form 자체는 유지 가능
        // ==========================================

        if (!isDiving &&
            wasDiving)
        {
            EndDiveEffects();
        }


        // ==========================================
        // 실제 잠수 중
        // ==========================================

        if (isDiving)
        {
            bool isMoving =
                moveSpeed >=
                minimumMoveSpeed;


            // 이동할 때만 Trail 생성
            if (wakeTrail != null)
            {
                wakeTrail.emitting =
                    isMoving;
            }


            if (isMoving)
            {
                UpdateRipples();
            }
        }
        else
        {
            if (wakeTrail != null)
            {
                wakeTrail.emitting =
                    false;
            }
        }


        wasDiving =
            isDiving;
    }


    // ==================================================
    // Trail 설정
    // ==================================================

    private void ConfigureWakeTrail()
    {
        if (wakeTrail == null)
            return;


        wakeTrail.time =
            wakeTime;


        wakeTrail.startWidth =
            wakeStartWidth;


        wakeTrail.endWidth =
            wakeEndWidth;


        wakeTrail.minVertexDistance =
            wakeMinVertexDistance;


        wakeTrail.numCapVertices =
            4;


        wakeTrail.numCornerVertices =
            3;


        wakeTrail.alignment =
            LineAlignment.View;


        wakeTrail.emitting =
            false;


        // ------------------------------------------
        // SwimVisual과 동일한 Material 사용
        // ------------------------------------------

        if (swimRenderer != null &&
            swimRenderer.sharedMaterial != null)
        {
            wakeTrail.sharedMaterial =
                swimRenderer.sharedMaterial;


            wakeTrail.sortingLayerID =
                swimRenderer.sortingLayerID;
        }


        wakeTrail.sortingOrder =
            effectSortingOrder;


        // ------------------------------------------
        // Trail 색상
        //
        // 앞쪽 진함 → 뒤쪽 투명
        // ------------------------------------------

        Gradient gradient =
            new Gradient();


        Color rgbColor =
            effectColor;


        rgbColor.a =
            1f;


        gradient.SetKeys(
            new GradientColorKey[]
            {
                new GradientColorKey(
                    rgbColor,
                    0f
                ),

                new GradientColorKey(
                    rgbColor,
                    1f
                )
            },

            new GradientAlphaKey[]
            {
                new GradientAlphaKey(
                    wakeAlpha,
                    0f
                ),

                new GradientAlphaKey(
                    0f,
                    1f
                )
            }
        );


        wakeTrail.colorGradient =
            gradient;


        wakeTrail.Clear();
    }


    // ==================================================
    // Dive 시작
    // ==================================================

    private void BeginDiveEffects()
    {
        lastRipplePosition =
            transform.position;


        if (wakeTrail != null)
        {
            // 이전 잠수 Trail과
            // 새 잠수 Trail이 연결되지 않도록 제거
            wakeTrail.Clear();


            wakeTrail.emitting =
                false;
        }


        // 잠수 진입 순간 작은 물결
        SpawnRipple(
            transform.position,
            0.75f
        );
    }


    // ==================================================
    // Dive 종료
    // ==================================================

    private void EndDiveEffects()
    {
        if (wakeTrail != null)
        {
            // 기존 흔적은 자연스럽게 사라지고
            // 새 흔적만 더 이상 생성하지 않음
            wakeTrail.emitting =
                false;
        }
    }


    // ==================================================
    // Ripple 거리 판정
    // ==================================================

    private void UpdateRipples()
    {
        Vector2 currentPosition =
            transform.position;


        Vector2 difference =
            currentPosition
            - lastRipplePosition;


        float distance =
            difference.magnitude;


        if (distance <
            rippleSpacing)
        {
            return;
        }


        Vector2 direction =
            difference.normalized;


        int createdCount =
            0;


        // ------------------------------------------
        // 빠르게 이동해도 Ripple 간격이
        // 일정하게 보이도록 중간 지점 생성
        // ------------------------------------------

        while (distance >=
               rippleSpacing)
        {
            lastRipplePosition +=
                direction
                * rippleSpacing;


            SpawnRipple(
                lastRipplePosition,
                1f
            );


            createdCount++;


            if (createdCount >=
                maxRipplesPerFrame)
            {
                lastRipplePosition =
                    currentPosition;

                break;
            }


            difference =
                currentPosition
                - lastRipplePosition;


            distance =
                difference.magnitude;


            if (distance >
                0.001f)
            {
                direction =
                    difference.normalized;
            }
        }
    }


    // ==================================================
    // Ripple 생성
    // ==================================================

    private void SpawnRipple(
        Vector2 worldPosition,
        float sizeMultiplier)
    {
        if (rippleRoot == null)
            return;


        GameObject rippleObject =
            new GameObject(
                "SwimRipple"
            );


        rippleObject.transform.SetParent(
            rippleRoot,
            true
        );


        rippleObject.transform.position =
            new Vector3(
                worldPosition.x,
                worldPosition.y,
                0f
            );


        LineRenderer line =
            rippleObject
                .AddComponent<LineRenderer>();


        ConfigureRippleLine(
            line
        );


        CreateRippleCircle(
            line
        );


        StartCoroutine(
            AnimateRipple(
                rippleObject.transform,
                line,
                sizeMultiplier
            )
        );
    }


    // ==================================================
    // Ripple LineRenderer
    // ==================================================

    private void ConfigureRippleLine(
        LineRenderer line)
    {
        line.useWorldSpace =
            false;


        line.loop =
            true;


        line.positionCount =
            Mathf.Max(
                8,
                rippleSegments
            );


        line.alignment =
            LineAlignment.View;


        line.numCornerVertices =
            2;


        if (swimRenderer != null)
        {
            line.sharedMaterial =
                swimRenderer.sharedMaterial;


            line.sortingLayerID =
                swimRenderer.sortingLayerID;
        }
        else if (wakeTrail != null)
        {
            line.sharedMaterial =
                wakeTrail.sharedMaterial;


            line.sortingLayerID =
                wakeTrail.sortingLayerID;
        }


        line.sortingOrder =
            effectSortingOrder;


        line.widthMultiplier =
            rippleStartWidth;


        Color startColor =
            effectColor;


        startColor.a =
            rippleAlpha;


        line.startColor =
            startColor;


        line.endColor =
            startColor;
    }


    // ==================================================
    // Ripple 원 형태
    // ==================================================

    private void CreateRippleCircle(
        LineRenderer line)
    {
        int count =
            line.positionCount;


        for (int i = 0;
             i < count;
             i++)
        {
            float angle =
                (
                    (float)i
                    / count
                )
                * Mathf.PI
                * 2f;


            float x =
                Mathf.Cos(
                    angle
                );


            float y =
                Mathf.Sin(
                    angle
                )
                * rippleEllipseRatio;


            line.SetPosition(
                i,
                new Vector3(
                    x,
                    y,
                    0f
                )
            );
        }
    }


    // ==================================================
    // Ripple Animation
    // ==================================================

    private IEnumerator AnimateRipple(
        Transform rippleTransform,
        LineRenderer line,
        float sizeMultiplier)
    {
        float timer =
            0f;


        while (timer <
               rippleDuration)
        {
            if (rippleTransform == null ||
                line == null)
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
                        rippleDuration,
                        0.001f
                    )
                );


            // ======================================
            // 빠르게 퍼졌다가 느려지는 Ease
            // ======================================

            float inverse =
                1f - t;


            float easeOut =
                1f
                - inverse
                * inverse
                * inverse;


            float radius =
                Mathf.Lerp(
                    rippleStartRadius,
                    rippleEndRadius,
                    easeOut
                )
                * sizeMultiplier;


            rippleTransform.localScale =
                new Vector3(
                    radius,
                    radius,
                    1f
                );


            // ======================================
            // 선 두께 감소
            // ======================================

            line.widthMultiplier =
                Mathf.Lerp(
                    rippleStartWidth,
                    rippleEndWidth,
                    t
                );


            // ======================================
            // Fade Out
            // ======================================

            float alpha =
                rippleAlpha
                * (
                    1f - t
                );


            Color color =
                effectColor;


            color.a =
                alpha;


            line.startColor =
                color;


            line.endColor =
                color;


            yield return null;
        }


        if (rippleTransform != null)
        {
            Destroy(
                rippleTransform.gameObject
            );
        }
    }


    // ==================================================
    // Cleanup
    // ==================================================

    private void OnDestroy()
    {
        if (rippleRoot != null)
        {
            Destroy(
                rippleRoot.gameObject
            );
        }
    }
}