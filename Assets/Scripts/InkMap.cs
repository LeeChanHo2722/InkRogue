using UnityEngine;
using UnityEngine.Tilemaps;

public enum InkTeam
{
    Neutral = 0,
    Player = 1,
    Enemy = 2
}


[System.Serializable]
public class InkTrailSettings
{
    [Header("Main Trail")]
    public float trailSpacing = 0.4f;

    [Range(0f, 1f)]
    public float paintChance = 0.65f;

    public float sideJitter = 0.32f;

    public float mainRadiusMin = 0.20f;
    public float mainRadiusMax = 0.38f;


    [Header("Splatter")]

    [Range(0f, 1f)]
    public float splatterChance = 0.35f;

    public float splatterRadiusMin = 0.07f;
    public float splatterRadiusMax = 0.16f;

    public float splatterDistanceMin = 0.25f;
    public float splatterDistanceMax = 0.65f;
}


public class InkMap : MonoBehaviour
{
    // ==================================================
    // Singleton
    // ==================================================

    public static InkMap Instance
    {
        get;
        private set;
    }


    // ==================================================
    // Map
    // ==================================================

    private Tilemap groundTilemap;


    [Header("Map")]

    [Tooltip("월드 1칸당 Ink Texture 픽셀 수")]
    public int pixelsPerUnit = 16;


    // ==================================================
    // Ink Colors
    // ==================================================

    [Header("Ink Colors")]

    public Color playerInkColor =
        new Color(
            0.1f,
            0.5f,
            1f,
            0.85f
        );


    public Color enemyInkColor =
        new Color(
            1f,
            0.1f,
            0.35f,
            0.85f
        );


    // ==================================================
    // Trail
    // ==================================================

    [Header("Player Trail")]

    public InkTrailSettings playerTrail =
        new InkTrailSettings();


    [Header("Enemy Shooter Trail")]

    public InkTrailSettings enemyShooterTrail =
        new InkTrailSettings();


    // ==================================================
    // Runtime
    // ==================================================

    private Texture2D inkTexture;

    private Sprite runtimeInkSprite;

    private SpriteRenderer spriteRenderer;


    private Color32[] pixelColors;

    private byte[] inkOwners;


    private Bounds worldBounds;


    private int textureWidth;

    private int textureHeight;


    private bool textureDirty = false;


    // ==================================================
    // Public State
    // ==================================================

    public bool IsReady =>
        inkTexture != null &&
        pixelColors != null &&
        inkOwners != null;


    public Vector2 WorldCenter =>
        new Vector2(
            worldBounds.center.x,
            worldBounds.center.y
        );


    // ==================================================
    // Awake
    // ==================================================

    private void Awake()
    {
        Instance =
            this;


        spriteRenderer =
            GetComponent<SpriteRenderer>();


        if (spriteRenderer == null)
        {
            spriteRenderer =
                gameObject
                    .AddComponent<SpriteRenderer>();
        }
    }


    // ==================================================
    // Start
    // ==================================================

    private void Start()
    {
        if (groundTilemap != null &&
            !IsReady)
        {
            CreateInkMap();
        }
    }


    // ==================================================
    // Late Update
    // ==================================================

    private void LateUpdate()
    {
        if (!textureDirty)
            return;


        if (!IsReady)
            return;


        inkTexture.SetPixels32(
            pixelColors
        );


        inkTexture.Apply(
            false
        );


        textureDirty =
            false;
    }


    // ==================================================
    // Destroy
    // ==================================================

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance =
                null;
        }


        DestroyRuntimeInkResources();
    }


    // ==================================================
    // Create Ink Map
    // ==================================================

    private void CreateInkMap()
    {
        if (groundTilemap == null)
        {
            Debug.LogError(
                "InkMap: Ground Tilemap이 연결되지 않았습니다."
            );

            return;
        }


        // ==========================================
        // 실제 Tile 영역으로 Bounds 압축
        // ==========================================

        groundTilemap
            .CompressBounds();


        // ==========================================
        // 이전 Ink Sprite / Texture 제거
        // ==========================================

        DestroyRuntimeInkResources();


        // ==========================================
        // Bounds 계산
        // ==========================================

        Bounds localBounds =
            groundTilemap.localBounds;


        Vector3 worldMin =
            groundTilemap.transform
                .TransformPoint(
                    localBounds.min
                );


        Vector3 worldMax =
            groundTilemap.transform
                .TransformPoint(
                    localBounds.max
                );


        Vector3 center =
            (
                worldMin
                + worldMax
            )
            * 0.5f;


        Vector3 size =
            worldMax
            - worldMin;


        worldBounds =
            new Bounds(
                center,
                new Vector3(
                    Mathf.Abs(size.x),
                    Mathf.Abs(size.y),
                    0f
                )
            );


        // ==========================================
        // Texture 크기
        // ==========================================

        textureWidth =
            Mathf.Max(
                1,
                Mathf.RoundToInt(
                    worldBounds.size.x
                    * pixelsPerUnit
                )
            );


        textureHeight =
            Mathf.Max(
                1,
                Mathf.RoundToInt(
                    worldBounds.size.y
                    * pixelsPerUnit
                )
            );


        // ==========================================
        // Texture 생성
        // ==========================================

        inkTexture =
            new Texture2D(
                textureWidth,
                textureHeight,
                TextureFormat.RGBA32,
                false
            );


        inkTexture.filterMode =
            FilterMode.Bilinear;


        inkTexture.wrapMode =
            TextureWrapMode.Clamp;


        // ==========================================
        // CPU Data
        // ==========================================

        pixelColors =
            new Color32[
                textureWidth
                * textureHeight
            ];


        inkOwners =
            new byte[
                textureWidth
                * textureHeight
            ];


        inkTexture.SetPixels32(
            pixelColors
        );


        inkTexture.Apply(
            false
        );


        // ==========================================
        // Sprite 생성
        // ==========================================

        runtimeInkSprite =
            Sprite.Create(
                inkTexture,
                new Rect(
                    0,
                    0,
                    textureWidth,
                    textureHeight
                ),
                new Vector2(
                    0.5f,
                    0.5f
                ),
                pixelsPerUnit
            );


        spriteRenderer.sprite =
            runtimeInkSprite;


        // ==========================================
        // Ground Rendering 설정 상속
        // ==========================================

        TilemapRenderer groundRenderer =
            groundTilemap
                .GetComponent<TilemapRenderer>();


        if (groundRenderer != null)
        {
            spriteRenderer.sortingLayerID =
                groundRenderer.sortingLayerID;


            spriteRenderer.sortingOrder =
                groundRenderer.sortingOrder
                + 5;


            spriteRenderer.sharedMaterial =
                groundRenderer.sharedMaterial;
        }


        // ==========================================
        // InkMap 위치
        // ==========================================

        transform.position =
            new Vector3(
                worldBounds.center.x,
                worldBounds.center.y,
                0f
            );


        textureDirty =
            false;


        Debug.Log(
            "InkMap Created | Ground: "
            + groundTilemap.name
            + " | Size: "
            + textureWidth
            + " x "
            + textureHeight
        );
    }


    // ==================================================
    // Switch Ground Tilemap
    // ==================================================

    public void SwitchGroundTilemap(
        Tilemap newGroundTilemap)
    {
        if (newGroundTilemap == null)
        {
            Debug.LogError(
                "InkMap: 교체할 Ground Tilemap이 없습니다."
            );

            return;
        }


        groundTilemap =
            newGroundTilemap;


        CreateInkMap();


        Debug.Log(
            "InkMap switched to: "
            + groundTilemap.name
        );
    }


    // ==================================================
    // Clear All Ink
    // ==================================================

    public void ClearAllInk()
    {
        if (!IsReady)
            return;


        System.Array.Clear(
            pixelColors,
            0,
            pixelColors.Length
        );


        System.Array.Clear(
            inkOwners,
            0,
            inkOwners.Length
        );


        inkTexture.SetPixels32(
            pixelColors
        );


        inkTexture.Apply(
            false
        );


        textureDirty =
            false;
    }


    // ==================================================
    // Runtime Resource Cleanup
    // ==================================================

    private void DestroyRuntimeInkResources()
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.sprite =
                null;
        }


        if (runtimeInkSprite != null)
        {
            Destroy(
                runtimeInkSprite
            );


            runtimeInkSprite =
                null;
        }


        if (inkTexture != null)
        {
            Destroy(
                inkTexture
            );


            inkTexture =
                null;
        }
    }


    // ==================================================
    // Trail
    // ==================================================

    public void PaintTrail(
        Vector2 from,
        Vector2 to,
        InkTeam team)
    {
        InkTrailSettings settings;


        if (team ==
            InkTeam.Enemy)
        {
            settings =
                enemyShooterTrail;
        }
        else
        {
            settings =
                playerTrail;
        }


        PaintTrail(
            from,
            to,
            team,
            settings
        );
    }


    private void PaintTrail(
        Vector2 from,
        Vector2 to,
        InkTeam team,
        InkTrailSettings settings)
    {
        Vector2 delta =
            to - from;


        float distance =
            delta.magnitude;


        if (distance <= 0.001f)
            return;


        Vector2 direction =
            delta.normalized;


        Vector2 perpendicular =
            new Vector2(
                -direction.y,
                direction.x
            );


        float safeSpacing =
            Mathf.Max(
                0.01f,
                settings.trailSpacing
            );


        int sampleCount =
            Mathf.Max(
                1,
                Mathf.CeilToInt(
                    distance
                    / safeSpacing
                )
            );


        for (int i = 0;
             i <= sampleCount;
             i++)
        {
            if (Random.value >
                settings.paintChance)
            {
                continue;
            }


            float t =
                (float)i
                / sampleCount;


            Vector2 basePosition =
                Vector2.Lerp(
                    from,
                    to,
                    t
                );


            float jitter =
                Random.Range(
                    -settings.sideJitter,
                    settings.sideJitter
                );


            Vector2 paintPosition =
                basePosition
                + perpendicular
                * jitter;


            float radius =
                Random.Range(
                    settings.mainRadiusMin,
                    settings.mainRadiusMax
                );


            PaintCircle(
                paintPosition,
                radius,
                team
            );


            if (Random.value <
                settings.splatterChance)
            {
                PaintSplatter(
                    paintPosition,
                    team,
                    settings
                );
            }
        }
    }


    // ==================================================
    // Splatter
    // ==================================================

    private void PaintSplatter(
        Vector2 center,
        InkTeam team,
        InkTrailSettings settings)
    {
        Vector2 randomDirection =
            Random.insideUnitCircle;


        if (randomDirection.sqrMagnitude <
            0.001f)
        {
            randomDirection =
                Vector2.right;
        }


        randomDirection.Normalize();


        float distance =
            Random.Range(
                settings.splatterDistanceMin,
                settings.splatterDistanceMax
            );


        float radius =
            Random.Range(
                settings.splatterRadiusMin,
                settings.splatterRadiusMax
            );


        Vector2 splatterPosition =
            center
            + randomDirection
            * distance;


        PaintCircle(
            splatterPosition,
            radius,
            team
        );
    }


    // ==================================================
    // Circle Paint
    // ==================================================

    public void PaintCircle(
        Vector2 worldPosition,
        float radius,
        InkTeam team)
    {
        if (!IsReady)
            return;


        if (!WorldToPixel(
                worldPosition,
                out Vector2Int centerPixel))
        {
            return;
        }


        int radiusPixels =
            Mathf.Max(
                1,
                Mathf.RoundToInt(
                    radius
                    * pixelsPerUnit
                )
            );


        Color32 paintColor =
            GetInkColor(
                team
            );


        int radiusSquared =
            radiusPixels
            * radiusPixels;


        for (int y = -radiusPixels;
             y <= radiusPixels;
             y++)
        {
            for (int x = -radiusPixels;
                 x <= radiusPixels;
                 x++)
            {
                if (x * x + y * y >
                    radiusSquared)
                {
                    continue;
                }


                int pixelX =
                    centerPixel.x
                    + x;


                int pixelY =
                    centerPixel.y
                    + y;


                if (pixelX < 0 ||
                    pixelX >= textureWidth ||
                    pixelY < 0 ||
                    pixelY >= textureHeight)
                {
                    continue;
                }


                int index =
                    pixelY
                    * textureWidth
                    + pixelX;


                pixelColors[index] =
                    paintColor;


                inkOwners[index] =
                    (byte)team;
            }
        }


        textureDirty =
            true;
    }


    // ==================================================
    // Get Ink Team
    // ==================================================

    public InkTeam GetInkTeam(
        Vector2 worldPosition)
    {
        if (!WorldToPixel(
                worldPosition,
                out Vector2Int pixel))
        {
            return
                InkTeam.Neutral;
        }


        int index =
            pixel.y
            * textureWidth
            + pixel.x;


        return
            (InkTeam)inkOwners[index];
    }


    // ==================================================
    // Dominant Ink
    // ==================================================

    public InkTeam GetDominantInkTeam(
        Vector2 worldPosition,
        float sampleRadius,
        int minimumSamples = 2)
    {
        int playerCount =
            0;


        int enemyCount =
            0;


        Vector2[] sampleOffsets =
        {
            Vector2.zero,

            new Vector2(
                -sampleRadius,
                0f
            ),

            new Vector2(
                sampleRadius,
                0f
            ),

            new Vector2(
                0f,
                sampleRadius
            ),

            new Vector2(
                0f,
                -sampleRadius
            ),

            new Vector2(
                -sampleRadius,
                sampleRadius
            ),

            new Vector2(
                sampleRadius,
                sampleRadius
            ),

            new Vector2(
                -sampleRadius,
                -sampleRadius
            ),

            new Vector2(
                sampleRadius,
                -sampleRadius
            )
        };


        foreach (
            Vector2 offset
            in sampleOffsets)
        {
            InkTeam team =
                GetInkTeam(
                    worldPosition
                    + offset
                );


            if (team ==
                InkTeam.Player)
            {
                playerCount++;
            }
            else if (team ==
                     InkTeam.Enemy)
            {
                enemyCount++;
            }
        }


        if (playerCount >=
                minimumSamples &&
            playerCount >
                enemyCount)
        {
            return
                InkTeam.Player;
        }


        if (enemyCount >=
                minimumSamples &&
            enemyCount >
                playerCount)
        {
            return
                InkTeam.Enemy;
        }


        if (playerCount ==
                enemyCount &&
            playerCount >=
                minimumSamples)
        {
            return
                GetInkTeam(
                    worldPosition
                );
        }


        return
            InkTeam.Neutral;
    }


    // ==================================================
    // Spin Slash
    //
    // A spinning cut is read from its direction of travel, so the mark it
    // leaves is a spiral, not a splash. Two arms sweep outward half a turn
    // apart, drawn as a continuous brush stroke, and the spin tears ink
    // off the outer edge and flings it clear. The centre stays empty:
    // filling it is what made this look like a stamp.
    //
    // Structure is deterministic, surface is not. The random here only
    // roughens the stroke; it never decides where the arms go.
    // ==================================================

    private const float SpinSlashTurns = 0.95f;

    // Clockwise, every time, so the attack keeps one identity.
    private const float SpinSlashDirection = -1f;


    public void PaintSpinSlash(
        Vector2 center,
        float radius,
        InkTeam team,
        int samplesPerArm = 18,
        int flingCount = 10)
    {
        if (!IsReady)
            return;


        float twoPi =
            Mathf.PI * 2f;


        // Only the orientation of the whole figure varies between swings.
        float startAngle =
            Random.Range(
                0f,
                twoPi
            );


        float sweep =
            SpinSlashDirection
            * twoPi
            * SpinSlashTurns;


        for (int arm = 0;
             arm < 2;
             arm++)
        {
            PaintSpinSlashArm(
                center,
                radius,
                team,
                startAngle + arm * Mathf.PI,
                sweep,
                samplesPerArm
            );
        }


        for (int i = 0;
             i < flingCount;
             i++)
        {
            PaintSpinSlashFling(
                center,
                radius,
                team,
                sweep
            );
        }
    }


    // One arm, drawn as a chain of connected strokes rather than a row of
    // dots. Consecutive samples share their brush radius, so the ribbon
    // swells and thins along its length instead of beading up.
    private void PaintSpinSlashArm(
        Vector2 center,
        float radius,
        InkTeam team,
        float startAngle,
        float sweep,
        int sampleCount)
    {
        if (sampleCount < 2)
            sampleCount = 2;


        float baseBrush =
            radius * 0.07f;


        Vector2 previousPoint =
            Vector2.zero;

        float previousBrush = 0f;


        for (int i = 0;
             i < sampleCount;
             i++)
        {
            float t =
                i / (float)(sampleCount - 1);


            // Archimedean: the angle winds while the radius grows, so the
            // path can never close into a ring.
            float sampleRadius =
                Mathf.Lerp(
                    radius * 0.35f,
                    radius * 1.10f,
                    t
                )
                * Random.Range(
                    0.97f,
                    1.03f
                );


            float angle =
                startAngle
                + sweep * t
                + Random.Range(
                    -0.03f,
                    0.03f
                );


            Vector2 point =
                center
                + new Vector2(
                    Mathf.Cos(angle),
                    Mathf.Sin(angle)
                )
                * sampleRadius;


            float brush =
                baseBrush
                * Random.Range(
                    0.65f,
                    1.35f
                );


            if (i > 0)
            {
                PaintVariableStroke(
                    previousPoint,
                    point,
                    previousBrush,
                    brush,
                    team
                );
            }


            previousPoint = point;
            previousBrush = brush;
        }
    }


    // Ink torn off the outer edge of the swing. Mostly outward, but
    // carrying enough of the spin to curve, so the spray reads as thrown
    // rather than as an explosion going off.
    private void PaintSpinSlashFling(
        Vector2 center,
        float radius,
        InkTeam team,
        float sweep)
    {
        float angle =
            Random.Range(
                0f,
                Mathf.PI * 2f
            );


        Vector2 radial =
            new Vector2(
                Mathf.Cos(angle),
                Mathf.Sin(angle)
            );


        Vector2 tangent =
            new Vector2(
                -radial.y,
                radial.x
            )
            * Mathf.Sign(sweep);


        Vector2 direction =
            (radial * 0.75f
                + tangent * 0.25f)
            .normalized;


        // Starts out at the rim, not at the centre: this is ink leaving
        // the blade, not ink erupting from the Player.
        Vector2 origin =
            center
            + radial
            * Random.Range(
                radius * 0.75f,
                radius * 1.0f
            );


        float length =
            Random.Range(
                radius * 0.18f,
                radius * 0.5f
            );


        // Most of them keep their tail; the rest have already broken off
        // and travel alone, still along the same line of flight.
        if (Random.value < 0.65f)
        {
            PaintFlingDroplet(
                origin,
                direction,
                length,
                radius * 0.055f,
                team
            );


            return;
        }


        PaintCircle(
            origin + direction * length,
            Random.Range(
                radius * 0.022f,
                radius * 0.038f
            ),
            team
        );
    }


    // ==================================================
    // Brush Primitives
    // ==================================================

    // Connects two points with overlapping stamps, tapering from one
    // radius to the other. Deliberately not PaintTrail: that one drops
    // samples and jitters them sideways, which is fine for a bullet
    // streak but dissolves a silhouette.
    private void PaintVariableStroke(
        Vector2 from,
        Vector2 to,
        float startRadius,
        float endRadius,
        InkTeam team)
    {
        float distance =
            (to - from).magnitude;


        if (distance <= 0.0001f)
        {
            PaintCircle(
                from,
                startRadius,
                team
            );


            return;
        }


        // Spaced under the thinner end so the stamps always overlap and
        // the stroke reads as one mark.
        float spacing =
            Mathf.Max(
                0.02f,
                Mathf.Min(
                    startRadius,
                    endRadius
                )
                * 0.6f
            );


        int steps =
            Mathf.Max(
                1,
                Mathf.CeilToInt(
                    distance / spacing
                )
            );


        for (int i = 0;
             i <= steps;
             i++)
        {
            float t =
                i / (float)steps;


            PaintCircle(
                Vector2.Lerp(
                    from,
                    to,
                    t
                ),
                Mathf.Lerp(
                    startRadius,
                    endRadius,
                    t
                ),
                team
            );
        }
    }


    // A tapered comet: fat where it tore loose, thin where it is still
    // travelling, plus the detached bead that ran ahead of it.
    private void PaintFlingDroplet(
        Vector2 origin,
        Vector2 direction,
        float length,
        float startRadius,
        InkTeam team)
    {
        PaintVariableStroke(
            origin,
            origin + direction * length,
            startRadius,
            startRadius * 0.25f,
            team
        );


        PaintCircle(
            origin + direction * (length * 1.12f),
            startRadius * 0.22f,
            team
        );
    }


    // ==================================================
    // Explosion
    // ==================================================

    public void PaintExplosion(
        Vector2 center,
        float radius,
        InkTeam team,
        int splatCount = 28)
    {
        PaintCircle(
            center,
            radius * 0.5f,
            team
        );


        for (int i = 0;
             i < splatCount;
             i++)
        {
            Vector2 randomDirection =
                Random.insideUnitCircle;


            if (randomDirection.sqrMagnitude <
                0.001f)
            {
                randomDirection =
                    Vector2.right;
            }


            randomDirection.Normalize();


            float distance =
                Random.Range(
                    radius * 0.2f,
                    radius * 0.9f
                );


            Vector2 splatPosition =
                center
                + randomDirection
                * distance;


            float splatRadius =
                Random.Range(
                    radius * 0.10f,
                    radius * 0.28f
                );


            PaintCircle(
                splatPosition,
                splatRadius,
                team
            );
        }
    }


    // ==================================================
    // World -> Pixel
    // ==================================================

    private bool WorldToPixel(
        Vector2 worldPosition,
        out Vector2Int pixel)
    {
        pixel =
            Vector2Int.zero;


        if (!IsReady)
            return false;


        if (worldPosition.x <
                worldBounds.min.x ||
            worldPosition.x >
                worldBounds.max.x ||
            worldPosition.y <
                worldBounds.min.y ||
            worldPosition.y >
                worldBounds.max.y)
        {
            return false;
        }


        float u =
            Mathf.InverseLerp(
                worldBounds.min.x,
                worldBounds.max.x,
                worldPosition.x
            );


        float v =
            Mathf.InverseLerp(
                worldBounds.min.y,
                worldBounds.max.y,
                worldPosition.y
            );


        int x =
            Mathf.Clamp(
                Mathf.FloorToInt(
                    u
                    * textureWidth
                ),
                0,
                textureWidth - 1
            );


        int y =
            Mathf.Clamp(
                Mathf.FloorToInt(
                    v
                    * textureHeight
                ),
                0,
                textureHeight - 1
            );


        pixel =
            new Vector2Int(
                x,
                y
            );


        return true;
    }


    // ==================================================
    // Team -> Color
    // ==================================================

    private Color32 GetInkColor(
        InkTeam team)
    {
        switch (team)
        {
            case InkTeam.Player:

                return
                    playerInkColor;


            case InkTeam.Enemy:

                return
                    enemyInkColor;


            default:

                return
                    new Color32(
                        0,
                        0,
                        0,
                        0
                    );
        }
    }
}