using System.Collections;
using UnityEngine;

public class EnemyTankTelegraph : MonoBehaviour
{
    // ==================================================
    // References
    // ==================================================

    [Header("References")]
    public SpriteRenderer referenceRenderer;


    // ==================================================
    // Color
    // ==================================================

    [Header("Color")]

    [Tooltip("Enemy Ink와 같은 색")]
    public Color telegraphColor =
        new Color(
            1f,
            0.12f,
            0.28f,
            0.90f
        );


    // ==================================================
    // Warning Arc
    // ==================================================

    [Header("Warning Arc")]

    [Tooltip("참격의 절반 각도. 90 = 총 180도")]
    public float halfAngle = 90f;

    public float warningStartRadius = 1.75f;

    public float warningEndRadius = 1.55f;

    public float warningWidth = 0.065f;

    public int arcSegments = 32;


    // ==================================================
    // Slash
    // ==================================================

    [Header("Slash Visual")]

    public float slashWidth = 0.16f;

    [Tooltip("휘두를 때 보이는 칼날 꼬리 각도")]
    public float slashTrailAngle = 55f;

    public float slashStartAlpha = 1f;


    // ==================================================
    // Rendering
    // ==================================================

    [Header("Rendering")]

    public int sortingOrderOffset = 3;


    // ==================================================
    // Runtime
    // ==================================================

    private Transform effectRoot;

    private LineRenderer warningArc;

    private LineRenderer leftBoundary;

    private LineRenderer rightBoundary;

    private LineRenderer slashArc;

    private Coroutine slashRoutine;


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

        HideWarning();


        if (slashArc != null)
        {
            slashArc.enabled = false;
        }
    }


    // ==================================================
    // Runtime Objects
    // ==================================================

    private void CreateRuntimeObjects()
    {
        GameObject rootObject =
            new GameObject(
                "Runtime_TankTelegraph"
            );


        rootObject.transform.SetParent(
            transform,
            false
        );


        rootObject.transform.localPosition =
            Vector3.zero;


        effectRoot =
            rootObject.transform;


        warningArc =
            CreateLineRenderer(
                "WarningArc",
                Mathf.Max(4, arcSegments)
            );


        leftBoundary =
            CreateLineRenderer(
                "LeftBoundary",
                2
            );


        rightBoundary =
            CreateLineRenderer(
                "RightBoundary",
                2
            );


        slashArc =
            CreateLineRenderer(
                "SlashArc",
                Mathf.Max(4, arcSegments)
            );
    }


    // ==================================================
    // Line Renderer Factory
    // ==================================================

    private LineRenderer CreateLineRenderer(
        string objectName,
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
            lineObject.AddComponent<LineRenderer>();


        line.useWorldSpace =
            true;


        line.loop =
            false;


        line.positionCount =
            positionCount;


        line.numCornerVertices =
            5;


        line.numCapVertices =
            5;


        if (referenceRenderer != null)
        {
            line.sharedMaterial =
                referenceRenderer.sharedMaterial;


            line.sortingLayerID =
                referenceRenderer.sortingLayerID;


            line.sortingOrder =
                referenceRenderer.sortingOrder
                + sortingOrderOffset;
        }


        line.enabled =
            false;


        return line;
    }


    // ==================================================
    // Begin Warning
    // ==================================================

    public void BeginWarning()
    {
        if (warningArc != null)
            warningArc.enabled = true;


        if (leftBoundary != null)
            leftBoundary.enabled = true;


        if (rightBoundary != null)
            rightBoundary.enabled = true;
    }


    // ==================================================
    // Update Warning
    // ==================================================

    public void UpdateWarning(
        Vector2 origin,
        Vector2 direction,
        float progress,
        bool locked,
        float attackRadius)
    {
        if (warningArc == null)
            return;


        progress =
            Mathf.Clamp01(progress);


        if (direction.sqrMagnitude <
            0.0001f)
        {
            direction =
                Vector2.right;
        }


        direction.Normalize();


        // ==========================================
        // 공격 준비가 끝날수록
        // 실제 공격 범위까지 수축
        // ==========================================

        float startRadius =
            Mathf.Max(
                attackRadius,
                warningStartRadius
            );


        float radius =
            Mathf.Lerp(
                startRadius,
                attackRadius,
                progress
            );


        // ==========================================
        // Lock Pulse
        // ==========================================

        float pulse = 1f;


        if (locked)
        {
            pulse =
                0.70f
                + Mathf.Sin(
                    Time.time * 45f
                )
                * 0.30f;
        }


        Color color =
            telegraphColor;


        color.a *=
            Mathf.Lerp(
                0.30f,
                1f,
                progress
            )
            * pulse;


        warningArc.startColor =
            color;


        warningArc.endColor =
            color;


        warningArc.startWidth =
            warningWidth;


        warningArc.endWidth =
            warningWidth;


        DrawArc(
            warningArc,
            origin,
            direction,
            -halfAngle,
            halfAngle,
            radius
        );


        // ==========================================
        // 양쪽 공격 범위 경계선
        // ==========================================

        DrawBoundary(
            leftBoundary,
            origin,
            direction,
            -halfAngle,
            radius,
            color
        );


        DrawBoundary(
            rightBoundary,
            origin,
            direction,
            halfAngle,
            radius,
            color
        );
    }


    // ==================================================
    // Boundary
    // ==================================================

    private void DrawBoundary(
        LineRenderer line,
        Vector2 origin,
        Vector2 direction,
        float angle,
        float radius,
        Color color)
    {
        if (line == null)
            return;


        Vector2 rotated =
            RotateVector(
                direction,
                angle
            );


        line.SetPosition(
            0,
            origin
        );


        line.SetPosition(
            1,
            origin
            + rotated
            * radius
        );


        line.startColor =
            color;


        line.endColor =
            new Color(
                color.r,
                color.g,
                color.b,
                color.a * 0.35f
            );


        line.startWidth =
            warningWidth * 0.7f;


        line.endWidth =
            warningWidth * 0.25f;
    }


    // ==================================================
    // Hide Warning
    // ==================================================

    public void HideWarning()
    {
        if (warningArc != null)
            warningArc.enabled = false;


        if (leftBoundary != null)
            leftBoundary.enabled = false;


        if (rightBoundary != null)
            rightBoundary.enabled = false;
    }


    // ==================================================
    // Slash
    // ==================================================

    public void PlaySlash(
        Vector2 origin,
        Vector2 direction,
        float radius,
        float duration)
    {
        if (slashArc == null)
            return;


        if (slashRoutine != null)
        {
            StopCoroutine(
                slashRoutine
            );
        }


        slashRoutine =
            StartCoroutine(
                SlashRoutine(
                    origin,
                    direction,
                    radius,
                    duration
                )
            );
    }


    private IEnumerator SlashRoutine(
        Vector2 origin,
        Vector2 direction,
        float radius,
        float duration)
    {
        slashArc.enabled =
            true;


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
                Time.deltaTime;


            float t =
                Mathf.Clamp01(
                    timer
                    / safeDuration
                );


            // ==========================================
            // -90도에서 +90도로 휘두름
            // ==========================================

            float bladeAngle =
                Mathf.Lerp(
                    -halfAngle,
                    halfAngle,
                    EaseOutCubic(t)
                );


            float trailStartAngle =
                Mathf.Max(
                    -halfAngle,
                    bladeAngle
                    - slashTrailAngle
                );


            DrawArc(
                slashArc,
                origin,
                direction,
                trailStartAngle,
                bladeAngle,
                radius
            );


            Color slashColor =
                telegraphColor;


            slashColor.a =
                slashStartAlpha
                * (1f - t * 0.35f);


            slashArc.startColor =
                new Color(
                    slashColor.r,
                    slashColor.g,
                    slashColor.b,
                    slashColor.a * 0.15f
                );


            slashArc.endColor =
                slashColor;


            slashArc.startWidth =
                slashWidth * 0.35f;


            slashArc.endWidth =
                slashWidth;


            yield return null;
        }


        // ==========================================
        // 휘두른 뒤 짧게 Fade
        // ==========================================

        timer = 0f;

        float fadeDuration =
            0.08f;


        while (timer <
               fadeDuration)
        {
            timer +=
                Time.deltaTime;


            float t =
                Mathf.Clamp01(
                    timer / fadeDuration
                );


            Color color =
                telegraphColor;


            color.a =
                1f - t;


            slashArc.startColor =
                new Color(
                    color.r,
                    color.g,
                    color.b,
                    color.a * 0.15f
                );


            slashArc.endColor =
                color;


            slashArc.startWidth =
                Mathf.Lerp(
                    slashWidth * 0.35f,
                    0.01f,
                    t
                );


            slashArc.endWidth =
                Mathf.Lerp(
                    slashWidth,
                    0.01f,
                    t
                );


            yield return null;
        }


        slashArc.enabled =
            false;


        slashRoutine =
            null;
    }


    // ==================================================
    // Draw Arc
    // ==================================================

    private void DrawArc(
        LineRenderer line,
        Vector2 origin,
        Vector2 forward,
        float startAngle,
        float endAngle,
        float radius)
    {
        if (line == null)
            return;


        int segments =
            Mathf.Max(
                4,
                arcSegments
            );


        line.positionCount =
            segments;


        for (int i = 0;
             i < segments;
             i++)
        {
            float ratio =
                segments <= 1
                    ? 0f
                    : (float)i
                    / (segments - 1);


            float angle =
                Mathf.Lerp(
                    startAngle,
                    endAngle,
                    ratio
                );


            Vector2 direction =
                RotateVector(
                    forward,
                    angle
                );


            Vector2 point =
                origin
                + direction
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
    // Rotate
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
    // Safety
    // ==================================================

    private void OnDisable()
    {
        HideWarning();


        if (slashArc != null)
        {
            slashArc.enabled =
                false;
        }
    }
}