using System.Collections;
using UnityEngine;

public class EnemyShooterTelegraph : MonoBehaviour
{
    // ==================================================
    // References
    // ==================================================

    [Header("References")]

    [Tooltip("VisualRoot의 SpriteRenderer")]
    public SpriteRenderer referenceRenderer;


    // ==================================================
    // Color
    // ==================================================

    [Header("Color")]

    [Tooltip("Enemy Ink와 같은 색으로 설정")]
    public Color telegraphColor =
        new Color(
            1f,
            0.12f,
            0.28f,
            0.85f
        );


    // ==================================================
    // Aim Line
    // ==================================================

    [Header("Aim Line")]

    public float aimLineWidth =
        0.045f;

    public float lockedLineWidth =
        0.075f;


    // ==================================================
    // Charge Ring
    // ==================================================

    [Header("Charge Ring")]

    public float ringStartRadius =
        0.60f;

    public float ringEndRadius =
        0.20f;

    public float ringWidth =
        0.055f;

    public int ringSegments =
        36;


    // ==================================================
    // Fire Flash
    // ==================================================

    [Header("Fire Flash")]

    public float fireFlashDuration =
        0.12f;

    public float fireFlashStartRadius =
        0.10f;

    public float fireFlashEndRadius =
        0.55f;

    public float fireFlashStartWidth =
        0.10f;


    // ==================================================
    // Sorting
    // ==================================================

    [Header("Rendering")]

    public int sortingOrderOffset =
        2;


    // ==================================================
    // Runtime
    // ==================================================

    private Transform effectRoot;

    private LineRenderer aimLine;

    private LineRenderer chargeRing;

    private LineRenderer fireRing;

    private Coroutine fireFlashRoutine;


    // ==================================================
    // Awake
    // ==================================================

    private void Awake()
    {
        if (referenceRenderer == null)
        {
            referenceRenderer =
                GetComponentInChildren<SpriteRenderer>();
        }


        CreateRuntimeObjects();


        HideTelegraph();


        if (fireRing != null)
        {
            fireRing.enabled =
                false;
        }
    }


    // ==================================================
    // Runtime Objects
    // ==================================================

    private void CreateRuntimeObjects()
    {
        GameObject rootObject =
            new GameObject(
                "Runtime_ShooterTelegraph"
            );


        rootObject.transform.SetParent(
            transform,
            false
        );


        rootObject.transform.localPosition =
            Vector3.zero;


        effectRoot =
            rootObject.transform;


        // ==========================================
        // Aim Line
        // ==========================================

        aimLine =
            CreateLineRenderer(
                "AimLine",
                false,
                true,
                2
            );


        // ==========================================
        // Charge Ring
        // ==========================================

        chargeRing =
            CreateLineRenderer(
                "ChargeRing",
                true,
                false,
                Mathf.Max(
                    8,
                    ringSegments
                )
            );


        // ==========================================
        // Fire Ring
        // ==========================================

        fireRing =
            CreateLineRenderer(
                "FireFlash",
                true,
                true,
                Mathf.Max(
                    8,
                    ringSegments
                )
            );
    }


    // ==================================================
    // Create Line Renderer
    // ==================================================

    private LineRenderer CreateLineRenderer(
        string objectName,
        bool loop,
        bool useWorldSpace,
        int positionCount)
    {
        GameObject lineObject =
            new GameObject(
                objectName
            );


        lineObject.transform.SetParent(
            effectRoot,
            false
        );


        LineRenderer line =
            lineObject
                .AddComponent<LineRenderer>();


        line.loop =
            loop;


        line.useWorldSpace =
            useWorldSpace;


        line.positionCount =
            positionCount;


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


        line.enabled =
            false;


        return line;
    }


    // ==================================================
    // Begin
    // ==================================================

    public void BeginTelegraph()
    {
        if (aimLine != null)
        {
            aimLine.enabled =
                true;
        }


        if (chargeRing != null)
        {
            chargeRing.enabled =
                true;
        }
    }


    // ==================================================
    // Update Telegraph
    // ==================================================

    public void UpdateTelegraph(
    Vector2 origin,
    Vector2 targetPosition,
    float progress,
    bool locked,
    LayerMask obstacleLayer)
    {
        if (aimLine == null ||
            chargeRing == null)
        {
            return;
        }


        progress =
            Mathf.Clamp01(
                progress
            );


        // ==========================================
        // Shooter → 실제 조준 지점
        // ==========================================

        Vector2 difference =
            targetPosition
            - origin;


        float targetDistance =
            difference.magnitude;


        Vector2 direction;


        if (targetDistance <
            0.001f)
        {
            direction =
                Vector2.right;

            targetDistance =
                0f;
        }
        else
        {
            direction =
                difference.normalized;
        }


        // ==========================================
        // 조준선의 기본 끝점은
        // Player 위치
        // ==========================================

        Vector2 endPosition =
            targetPosition;


        // ==========================================
        // Shooter와 Player 사이에 벽이 있으면
        // 벽에서 먼저 끊음
        // ==========================================

        if (targetDistance >
            0.001f)
        {
            RaycastHit2D hit =
                Physics2D.Raycast(
                    origin,
                    direction,
                    targetDistance,
                    obstacleLayer
                );


            if (hit.collider != null)
            {
                endPosition =
                    hit.point;
            }
        }


        // ==========================================
        // Aim Line
        // ==========================================

        aimLine.SetPosition(
            0,
            new Vector3(
                origin.x,
                origin.y,
                0f
            )
        );


        aimLine.SetPosition(
            1,
            new Vector3(
                endPosition.x,
                endPosition.y,
                0f
            )
        );


        // ==========================================
        // Lock 상태 점멸
        // ==========================================

        float pulse =
            1f;


        if (locked)
        {
            pulse =
                0.75f
                + Mathf.Sin(
                    Time.time
                    * 45f
                )
                * 0.25f;
        }


        Color lineColor =
            telegraphColor;


        lineColor.a *=
            Mathf.Lerp(
                0.25f,
                1f,
                progress
            )
            * pulse;


        aimLine.startColor =
            lineColor;


        aimLine.endColor =
            lineColor;


        float currentWidth =
            locked
                ? lockedLineWidth
                : aimLineWidth;


        aimLine.startWidth =
            currentWidth;


        aimLine.endWidth =
            currentWidth
            * 0.55f;


        // ==========================================
        // Charge Ring
        // ==========================================

        float radius =
            Mathf.Lerp(
                ringStartRadius,
                ringEndRadius,
                progress
            );


        Color ringColor =
            telegraphColor;


        ringColor.a *=
            Mathf.Lerp(
                0.30f,
                1f,
                progress
            );


        if (locked)
        {
            ringColor.a *=
                pulse;
        }


        chargeRing.startColor =
            ringColor;


        chargeRing.endColor =
            ringColor;


        chargeRing.startWidth =
            ringWidth;


        chargeRing.endWidth =
            ringWidth;


        SetCircle(
            chargeRing,
            Vector2.zero,
            radius,
            false
        );
    }


    // ==================================================
    // Hide
    // ==================================================

    public void HideTelegraph()
    {
        if (aimLine != null)
        {
            aimLine.enabled =
                false;
        }


        if (chargeRing != null)
        {
            chargeRing.enabled =
                false;
        }
    }


    // ==================================================
    // Fire Flash
    // ==================================================

    public void PlayFireFlash(
        Vector2 firePosition)
    {
        if (fireRing == null)
            return;


        if (fireFlashRoutine != null)
        {
            StopCoroutine(
                fireFlashRoutine
            );
        }


        fireFlashRoutine =
            StartCoroutine(
                FireFlashRoutine(
                    firePosition
                )
            );
    }


    private IEnumerator FireFlashRoutine(
        Vector2 firePosition)
    {
        fireRing.enabled =
            true;


        float timer =
            0f;


        while (timer <
               fireFlashDuration)
        {
            timer +=
                Time.deltaTime;


            float t =
                Mathf.Clamp01(
                    timer
                    /
                    Mathf.Max(
                        fireFlashDuration,
                        0.001f
                    )
                );


            float radius =
                Mathf.Lerp(
                    fireFlashStartRadius,
                    fireFlashEndRadius,
                    t
                );


            Color color =
                telegraphColor;


            color.a *=
                1f - t;


            fireRing.startColor =
                color;


            fireRing.endColor =
                color;


            float width =
                Mathf.Lerp(
                    fireFlashStartWidth,
                    0.01f,
                    t
                );


            fireRing.startWidth =
                width;


            fireRing.endWidth =
                width;


            SetCircle(
                fireRing,
                firePosition,
                radius,
                true
            );


            yield return null;
        }


        fireRing.enabled =
            false;


        fireFlashRoutine =
            null;
    }


    // ==================================================
    // Circle
    // ==================================================

    private void SetCircle(
        LineRenderer line,
        Vector2 center,
        float radius,
        bool worldSpace)
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


        for (int i = 0;
             i < segments;
             i++)
        {
            float angle =
                Mathf.PI
                * 2f
                * i
                / segments;


            Vector2 offset =
                new Vector2(
                    Mathf.Cos(angle),
                    Mathf.Sin(angle)
                )
                * radius;


            Vector2 position =
                worldSpace
                    ? center + offset
                    : offset;


            line.SetPosition(
                i,
                new Vector3(
                    position.x,
                    position.y,
                    0f
                )
            );
        }
    }


    // ==================================================
    // Disable
    // ==================================================

    private void OnDisable()
    {
        HideTelegraph();


        if (fireRing != null)
        {
            fireRing.enabled =
                false;
        }
    }
}