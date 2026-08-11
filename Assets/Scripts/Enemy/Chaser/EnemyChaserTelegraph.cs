using System.Collections;
using UnityEngine;

public class EnemyChaserTelegraph : MonoBehaviour
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

    public Color telegraphColor =
        new Color(
            1f,
            0.12f,
            0.28f,
            0.90f
        );


    // ==================================================
    // Danger Ring
    // ==================================================

    [Header("Danger Ring")]

    public float ringStartRadius =
        0.75f;


    public float ringEndRadius =
        0.35f;


    public float ringWidth =
        0.065f;


    public int ringSegments =
        36;


    // ==================================================
    // Dash Direction
    // ==================================================

    [Header("Dash Direction")]

    public float directionLineStart =
        0.30f;


    public float directionLineLength =
        1.50f;


    public float directionLineWidth =
        0.07f;


    public float lockedLineWidth =
        0.11f;


    // ==================================================
    // Dash Flash
    // ==================================================

    [Header("Dash Flash")]

    public float dashFlashDuration =
        0.14f;


    public float dashFlashStartRadius =
        0.25f;


    public float dashFlashEndRadius =
        0.75f;


    // ==================================================
    // Rendering
    // ==================================================

    [Header("Rendering")]

    public int sortingOrderOffset =
        2;


    // ==================================================
    // Runtime
    // ==================================================

    private Transform effectRoot;

    private LineRenderer dangerRing;

    private LineRenderer directionLine;

    private LineRenderer dashFlashRing;

    private Coroutine flashRoutine;


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


        if (dashFlashRing != null)
        {
            dashFlashRing.enabled =
                false;
        }
    }


    // ==================================================
    // Create Objects
    // ==================================================

    private void CreateRuntimeObjects()
    {
        GameObject rootObject =
            new GameObject(
                "Runtime_ChaserTelegraph"
            );


        rootObject.transform.SetParent(
            transform,
            false
        );


        rootObject.transform.localPosition =
            Vector3.zero;


        effectRoot =
            rootObject.transform;


        dangerRing =
            CreateLineRenderer(
                "DangerRing",
                true,
                false,
                Mathf.Max(
                    8,
                    ringSegments
                )
            );


        directionLine =
            CreateLineRenderer(
                "DashDirection",
                false,
                true,
                2
            );


        dashFlashRing =
            CreateLineRenderer(
                "DashFlash",
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
        if (dangerRing != null)
        {
            dangerRing.enabled =
                true;
        }


        if (directionLine != null)
        {
            directionLine.enabled =
                true;
        }
    }


    // ==================================================
    // Update Telegraph
    // ==================================================

    public void UpdateTelegraph(
        Vector2 origin,
        Vector2 direction,
        float progress,
        bool locked)
    {
        if (dangerRing == null ||
            directionLine == null)
        {
            return;
        }


        progress =
            Mathf.Clamp01(
                progress
            );


        if (direction.sqrMagnitude <
            0.0001f)
        {
            direction =
                Vector2.right;
        }


        direction.Normalize();


        // ==========================================
        // Lock ½Ã ºü¸¥ Pulse
        // ==========================================

        float pulse =
            1f;


        if (locked)
        {
            pulse =
                0.72f
                + Mathf.Sin(
                    Time.time
                    * 45f
                )
                * 0.28f;
        }


        // ==========================================
        // Danger Ring
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
                0.35f,
                1f,
                progress
            )
            * pulse;


        dangerRing.startColor =
            ringColor;


        dangerRing.endColor =
            ringColor;


        dangerRing.startWidth =
            ringWidth;


        dangerRing.endWidth =
            ringWidth;


        SetCircleLocal(
            dangerRing,
            radius
        );


        // ==========================================
        // Dash Direction
        // ==========================================

        Vector2 start =
            origin
            + direction
            * directionLineStart;


        Vector2 end =
            origin
            + direction
            * directionLineLength;


        directionLine.SetPosition(
            0,
            new Vector3(
                start.x,
                start.y,
                0f
            )
        );


        directionLine.SetPosition(
            1,
            new Vector3(
                end.x,
                end.y,
                0f
            )
        );


        Color lineColor =
            telegraphColor;


        lineColor.a *=
            Mathf.Lerp(
                0.30f,
                1f,
                progress
            )
            * pulse;


        directionLine.startColor =
            lineColor;


        directionLine.endColor =
            lineColor;


        float width =
            locked
                ? lockedLineWidth
                : directionLineWidth;


        directionLine.startWidth =
            width;


        directionLine.endWidth =
            width * 0.45f;
    }


    // ==================================================
    // Hide
    // ==================================================

    public void HideTelegraph()
    {
        if (dangerRing != null)
        {
            dangerRing.enabled =
                false;
        }


        if (directionLine != null)
        {
            directionLine.enabled =
                false;
        }
    }


    // ==================================================
    // Dash Flash
    // ==================================================

    public void PlayDashFlash(
        Vector2 position)
    {
        if (dashFlashRing == null)
            return;


        if (flashRoutine != null)
        {
            StopCoroutine(
                flashRoutine
            );
        }


        flashRoutine =
            StartCoroutine(
                DashFlashRoutine(
                    position
                )
            );
    }


    private IEnumerator DashFlashRoutine(
        Vector2 position)
    {
        dashFlashRing.enabled =
            true;


        float timer =
            0f;


        while (timer <
               dashFlashDuration)
        {
            timer +=
                Time.deltaTime;


            float t =
                Mathf.Clamp01(
                    timer
                    /
                    Mathf.Max(
                        dashFlashDuration,
                        0.001f
                    )
                );


            float radius =
                Mathf.Lerp(
                    dashFlashStartRadius,
                    dashFlashEndRadius,
                    t
                );


            Color color =
                telegraphColor;


            color.a *=
                1f - t;


            dashFlashRing.startColor =
                color;


            dashFlashRing.endColor =
                color;


            float width =
                Mathf.Lerp(
                    0.11f,
                    0.015f,
                    t
                );


            dashFlashRing.startWidth =
                width;


            dashFlashRing.endWidth =
                width;


            SetCircleWorld(
                dashFlashRing,
                position,
                radius
            );


            yield return null;
        }


        dashFlashRing.enabled =
            false;


        flashRoutine =
            null;
    }


    // ==================================================
    // Circle Local
    // ==================================================

    private void SetCircleLocal(
        LineRenderer line,
        float radius)
    {
        int segments =
            Mathf.Max(
                8,
                ringSegments
            );


        line.positionCount =
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


            line.SetPosition(
                i,
                new Vector3(
                    Mathf.Cos(angle)
                    * radius,

                    Mathf.Sin(angle)
                    * radius,

                    0f
                )
            );
        }
    }


    // ==================================================
    // Circle World
    // ==================================================

    private void SetCircleWorld(
        LineRenderer line,
        Vector2 center,
        float radius)
    {
        int segments =
            Mathf.Max(
                8,
                ringSegments
            );


        line.positionCount =
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


            Vector2 position =
                center
                + new Vector2(
                    Mathf.Cos(angle),
                    Mathf.Sin(angle)
                )
                * radius;


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


        if (dashFlashRing != null)
        {
            dashFlashRing.enabled =
                false;
        }
    }
}