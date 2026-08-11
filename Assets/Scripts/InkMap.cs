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

    [Header("Map")]

    public Tilemap groundTilemap;

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
        CreateInkMap();
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