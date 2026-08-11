using UnityEngine;

public class EnemyBomberTelegraph : MonoBehaviour
{
    // ==================================================
    // Settings
    // ==================================================

    [Header("Telegraph")]

    public int segments = 48;

    public float lineWidth = 0.06f;

    [Tooltip("바깥 원이 처음 시작하는 배율")]
    public float countdownStartScale = 1.7f;

    public int sortingOrderOffset = 4;


    // ==================================================
    // References
    // ==================================================

    [Header("Reference")]

    public SpriteRenderer referenceRenderer;


    // ==================================================
    // Runtime
    // ==================================================

    private GameObject runtimeRoot;

    private LineRenderer dangerRing;

    private LineRenderer countdownRing;


    // ==================================================
    // Awake
    // ==================================================

    private void Awake()
    {
        if (referenceRenderer == null)
        {
            referenceRenderer =
                GetComponentInChildren<SpriteRenderer>(
                    true
                );
        }


        CreateRuntimeVisuals();


        Hide();
    }


    // ==================================================
    // Create
    // ==================================================

    private void CreateRuntimeVisuals()
    {
        runtimeRoot =
            new GameObject(
                "Runtime_BomberTelegraph"
            );


        dangerRing =
            CreateRing(
                "DangerRing"
            );


        countdownRing =
            CreateRing(
                "CountdownRing"
            );
    }


    private LineRenderer CreateRing(
        string objectName)
    {
        GameObject ringObject =
            new GameObject(
                objectName
            );


        ringObject.transform.SetParent(
            runtimeRoot.transform,
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
                12,
                segments
            );


        line.startWidth =
            lineWidth;


        line.endWidth =
            lineWidth;


        line.numCornerVertices =
            4;


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


        return line;
    }


    // ==================================================
    // Show
    // ==================================================

    public void Show(
        Vector2 position,
        float radius,
        float progress,
        bool locked)
    {
        if (dangerRing == null ||
            countdownRing == null)
        {
            return;
        }


        dangerRing.enabled =
            true;


        countdownRing.enabled =
            true;


        Color color =
            GetEnemyColor();


        // ==========================================
        // 실제 폭발 범위
        // ==========================================

        Color dangerColor =
            color;


        dangerColor.a =
            locked
                ? 0.95f
                : 0.60f;


        DrawRing(
            dangerRing,
            position,
            radius,
            lineWidth,
            dangerColor
        );


        // ==========================================
        // Countdown Ring
        //
        // 큰 원 → 실제 폭발 범위로 축소
        // ==========================================

        float countdownRadius =
            Mathf.Lerp(
                radius
                * countdownStartScale,
                radius,
                progress
            );


        Color countdownColor =
            color;


        countdownColor.a =
            Mathf.Lerp(
                0.45f,
                1f,
                progress
            );


        DrawRing(
            countdownRing,
            position,
            countdownRadius,
            lineWidth * 1.25f,
            countdownColor
        );
    }


    // ==================================================
    // Ring
    // ==================================================

    private void DrawRing(
        LineRenderer line,
        Vector2 center,
        float radius,
        float width,
        Color color)
    {
        int safeSegments =
            Mathf.Max(
                12,
                segments
            );


        line.positionCount =
            safeSegments;


        line.startWidth =
            width;


        line.endWidth =
            width;


        line.startColor =
            color;


        line.endColor =
            color;


        for (int i = 0;
             i < safeSegments;
             i++)
        {
            float angle =
                Mathf.PI
                * 2f
                * i
                / safeSegments;


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
    // Color
    // ==================================================

    private Color GetEnemyColor()
    {
        if (InkMap.Instance != null)
        {
            Color color =
                InkMap.Instance.enemyInkColor;


            color.a =
                1f;


            return color;
        }


        return new Color(
            1f,
            0.1f,
            0.35f,
            1f
        );
    }


    // ==================================================
    // Hide
    // ==================================================

    public void Hide()
    {
        if (dangerRing != null)
        {
            dangerRing.enabled =
                false;
        }


        if (countdownRing != null)
        {
            countdownRing.enabled =
                false;
        }
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