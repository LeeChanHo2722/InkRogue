using System.Collections.Generic;
using UnityEngine;

// World-space dashed lines from the Player to a handful of targets. Purely
// presentational: it owns no gameplay state and never searches the scene,
// the caller hands it the targets it already tracks.
public class EncounterGuideLines : MonoBehaviour
{
    [Header("Style")]

    [Min(0.005f)]
    [SerializeField]
    private float lineWidth = 0.05f;

    [Range(0f, 1f)]
    [SerializeField]
    private float lineAlpha = 0.4f;

    [Tooltip("Dash repeats per world unit.")]
    [Min(0.1f)]
    [SerializeField]
    private float dashTiling = 3f;

    [SerializeField]
    private int sortingOrder = 50;


    private readonly List<LineRenderer> lines =
        new List<LineRenderer>();

    private Material dashMaterial;

    private int visibleCount;


    private void OnDestroy()
    {
        if (dashMaterial != null)
        {
            Destroy(dashMaterial);
        }
    }


    // Draws one line per non-null target, up to maxLines. Targets whose
    // GameObject is already destroyed are skipped, so a dead enemy's line
    // disappears on the next call.
    public void UpdateLines(
        Transform origin,
        IReadOnlyList<Transform> targets,
        int maxLines)
    {
        if (origin == null || targets == null)
        {
            Hide();
            return;
        }


        int used = 0;


        for (int i = 0;
             i < targets.Count && used < maxLines;
             i++)
        {
            Transform target = targets[i];


            if (target == null)
                continue;


            LineRenderer line = GetLine(used);


            line.SetPosition(0, origin.position);
            line.SetPosition(1, target.position);


            if (!line.enabled)
            {
                line.enabled = true;
            }


            used++;
        }


        for (int i = used; i < lines.Count; i++)
        {
            if (lines[i].enabled)
            {
                lines[i].enabled = false;
            }
        }


        visibleCount = used;
    }


    public void Hide()
    {
        if (visibleCount == 0)
            return;


        for (int i = 0; i < lines.Count; i++)
        {
            if (lines[i] != null && lines[i].enabled)
            {
                lines[i].enabled = false;
            }
        }


        visibleCount = 0;
    }


    private LineRenderer GetLine(
        int index)
    {
        while (lines.Count <= index)
        {
            lines.Add(CreateLine(lines.Count));
        }


        return lines[index];
    }


    private LineRenderer CreateLine(
        int index)
    {
        GameObject lineObject =
            new GameObject(
                "GuideLine_" + index
            );


        lineObject.transform.SetParent(
            transform,
            false
        );


        LineRenderer line =
            lineObject.AddComponent<LineRenderer>();


        line.useWorldSpace = true;
        line.positionCount = 2;
        line.startWidth = lineWidth;
        line.endWidth = lineWidth;
        line.numCapVertices = 0;
        line.numCornerVertices = 0;
        line.shadowCastingMode =
            UnityEngine.Rendering.ShadowCastingMode.Off;
        line.receiveShadows = false;
        line.sortingOrder = sortingOrder;


        Color color =
            new Color(1f, 1f, 1f, lineAlpha);


        line.startColor = color;
        line.endColor = color;


        line.sharedMaterial = GetDashMaterial();
        line.textureMode = LineTextureMode.Tile;
        line.textureScale =
            new Vector2(dashTiling, 1f);


        line.enabled = false;


        return line;
    }


    // Two lit texels then two clear ones, tiled along the line. Avoids
    // needing a dash material asset or a custom shader.
    private Material GetDashMaterial()
    {
        if (dashMaterial != null)
        {
            return dashMaterial;
        }


        Texture2D texture =
            new Texture2D(
                4,
                1,
                TextureFormat.RGBA32,
                false
            );


        texture.wrapMode = TextureWrapMode.Repeat;
        texture.filterMode = FilterMode.Point;


        texture.SetPixels(
            new[]
            {
                Color.white,
                Color.white,
                Color.clear,
                Color.clear
            }
        );


        texture.Apply();


        dashMaterial =
            new Material(
                Shader.Find("Sprites/Default")
            );


        dashMaterial.mainTexture = texture;


        return dashMaterial;
    }
}
