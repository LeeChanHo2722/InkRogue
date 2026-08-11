using UnityEngine;

public class EnemySprinklerTelegraph : MonoBehaviour
{
    // ==================================================
    // Settings
    // ==================================================

    [Header("Telegraph")]

    public int segments = 40;

    public float startRadius = 1.15f;

    public float endRadius = 0.45f;

    public float lineWidth = 0.07f;

    public int sortingOrderOffset = 4;


    // ==================================================
    // Reference
    // ==================================================

    [Header("Reference")]

    public SpriteRenderer referenceRenderer;


    // ==================================================
    // Runtime
    // ==================================================

    private GameObject runtimeRoot;

    private LineRenderer warningRing;


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


        CreateRuntimeVisual();


        Hide();
    }


    // ==================================================
    // Create
    // ==================================================

    private void CreateRuntimeVisual()
    {
        runtimeRoot =
            new GameObject(
                "Runtime_SprinklerTelegraph"
            );


        runtimeRoot.transform.SetParent(
            transform,
            false
        );


        GameObject ringObject =
            new GameObject(
                "WarningRing"
            );


        ringObject.transform.SetParent(
            runtimeRoot.transform,
            false
        );


        warningRing =
            ringObject
                .AddComponent<LineRenderer>();


        warningRing.useWorldSpace =
            true;


        warningRing.loop =
            true;


        warningRing.positionCount =
            Mathf.Max(
                12,
                segments
            );


        warningRing.numCornerVertices =
            4;


        if (referenceRenderer != null)
        {
            warningRing.sharedMaterial =
                referenceRenderer.sharedMaterial;


            warningRing.sortingLayerID =
                referenceRenderer.sortingLayerID;


            warningRing.sortingOrder =
                referenceRenderer.sortingOrder
                + sortingOrderOffset;
        }
    }


    // ==================================================
    // Show
    // ==================================================

    public void Show(
        float progress)
    {
        if (warningRing == null)
            return;


        warningRing.enabled =
            true;


        float radius =
            Mathf.Lerp(
                startRadius,
                endRadius,
                progress
            );


        Color color =
            GetEnemyColor();


        float pulse =
            0.75f
            + Mathf.Sin(
                Time.time * 24f
            )
            * 0.25f;


        color.a =
            Mathf.Lerp(
                0.45f,
                1f,
                progress
            )
            * pulse;


        DrawRing(
            transform.position,
            radius,
            color
        );
    }


    // ==================================================
    // Ring
    // ==================================================

    private void DrawRing(
        Vector2 center,
        float radius,
        Color color)
    {
        int safeSegments =
            Mathf.Max(
                12,
                segments
            );


        warningRing.positionCount =
            safeSegments;


        warningRing.startWidth =
            lineWidth;


        warningRing.endWidth =
            lineWidth;


        warningRing.startColor =
            color;


        warningRing.endColor =
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


            warningRing.SetPosition(
                i,
                point
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
                InkMap.Instance
                    .enemyInkColor;


            color =
                Color.Lerp(
                    color,
                    Color.white,
                    0.15f
                );


            color.a =
                1f;


            return color;
        }


        return new Color(
            1f,
            0.15f,
            0.4f,
            1f
        );
    }


    // ==================================================
    // Hide
    // ==================================================

    public void Hide()
    {
        if (warningRing != null)
        {
            warningRing.enabled =
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