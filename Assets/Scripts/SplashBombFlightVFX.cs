using UnityEngine;

public class SplashBombFlightVFX : MonoBehaviour
{
    // ==================================================
    // References
    // ==================================================

    [Header("References")]

    [Tooltip(
        "실제로 공중에서 움직이는 Bomb Visual. "
        + "비워두면 이 GameObject를 사용합니다."
    )]
    public Transform visualTransform;


    [Tooltip(
        "Bomb SpriteRenderer. "
        + "Material / Sorting 정보를 가져옵니다."
    )]
    public SpriteRenderer referenceRenderer;


    // ==================================================
    // Trail
    // ==================================================

    [Header("Ink Trail")]

    public float trailTime = 0.24f;

    public float trailStartWidth = 0.18f;

    public float trailEndWidth = 0.025f;

    public float minVertexDistance = 0.035f;

    public int trailSortingOffset = -1;


    // ==================================================
    // Pulse Ring
    // ==================================================

    [Header("Pulse Ring")]

    public bool usePulseRing = true;

    public int ringSegments = 24;

    public float ringBaseRadius = 0.20f;

    public float ringPulseAmount = 0.045f;

    public float ringPulseSpeed = 10f;

    public float ringWidth = 0.035f;

    public int ringSortingOffset = 1;


    // ==================================================
    // Rotation Accent
    // ==================================================

    [Header("Rotation Accent")]

    [Tooltip(
        "Visual이 기존 SplashBomb 코드에서 "
        + "이미 회전한다면 0으로 둡니다."
    )]
    public float additionalRotationSpeed = 0f;


    // ==================================================
    // Runtime
    // ==================================================

    private TrailRenderer inkTrail;

    private LineRenderer pulseRing;


    private Color playerColor;

    private Color brightColor;


    // ==================================================
    // Awake
    // ==================================================

    private void Awake()
    {
        // ==========================================
        // Visual 자동 검색
        // ==========================================

        if (visualTransform == null)
        {
            Transform found =
                transform.Find(
                    "Visual"
                );


            visualTransform =
                found != null
                    ? found
                    : transform;
        }


        // ==========================================
        // Renderer 자동 검색
        // ==========================================

        if (referenceRenderer == null)
        {
            referenceRenderer =
                visualTransform
                    .GetComponentInChildren<SpriteRenderer>(
                        true
                    );
        }


        CreateTrail();


        if (usePulseRing)
        {
            CreatePulseRing();
        }
    }


    // ==================================================
    // Start
    // ==================================================

    private void Start()
    {
        UpdateColors();


        ApplyTrailColor();
    }


    // ==================================================
    // Update
    // ==================================================

    private void Update()
    {
        if (visualTransform == null)
            return;


        // ==========================================
        // 추가 회전
        // ==========================================

        if (Mathf.Abs(
                additionalRotationSpeed)
            > 0.01f)
        {
            visualTransform.Rotate(
                0f,
                0f,
                additionalRotationSpeed
                * Time.deltaTime
            );
        }


        // ==========================================
        // Pulse Ring
        // ==========================================

        if (usePulseRing &&
            pulseRing != null)
        {
            UpdatePulseRing();
        }
    }


    // ==================================================
    // Create Trail
    // ==================================================

    private void CreateTrail()
    {
        if (visualTransform == null)
            return;


        GameObject trailObject =
            new GameObject(
                "Runtime_SplashBombInkTrail"
            );


        trailObject.transform.SetParent(
            visualTransform,
            false
        );


        trailObject.transform.localPosition =
            Vector3.zero;


        inkTrail =
            trailObject.AddComponent<
                TrailRenderer
            >();


        inkTrail.time =
            trailTime;


        inkTrail.startWidth =
            trailStartWidth;


        inkTrail.endWidth =
            trailEndWidth;


        inkTrail.minVertexDistance =
            minVertexDistance;


        inkTrail.numCornerVertices =
            3;


        inkTrail.numCapVertices =
            3;


        inkTrail.textureMode =
            LineTextureMode.Stretch;


        inkTrail.emitting =
            true;


        // ==========================================
        // Material / Sorting
        // ==========================================

        if (referenceRenderer != null)
        {
            inkTrail.sharedMaterial =
                referenceRenderer.sharedMaterial;


            inkTrail.sortingLayerID =
                referenceRenderer.sortingLayerID;


            inkTrail.sortingOrder =
                referenceRenderer.sortingOrder
                + trailSortingOffset;
        }
    }


    // ==================================================
    // Trail Color
    // ==================================================

    private void ApplyTrailColor()
    {
        if (inkTrail == null)
            return;


        Gradient gradient =
            new Gradient();


        Color startColor =
            brightColor;


        Color endColor =
            playerColor;


        startColor.a =
            0.95f;


        endColor.a =
            0f;


        gradient.SetKeys(
            new GradientColorKey[]
            {
                new GradientColorKey(
                    startColor,
                    0f
                ),

                new GradientColorKey(
                    playerColor,
                    0.45f
                ),

                new GradientColorKey(
                    endColor,
                    1f
                )
            },
            new GradientAlphaKey[]
            {
                new GradientAlphaKey(
                    0.95f,
                    0f
                ),

                new GradientAlphaKey(
                    0.65f,
                    0.45f
                ),

                new GradientAlphaKey(
                    0f,
                    1f
                )
            }
        );


        inkTrail.colorGradient =
            gradient;
    }


    // ==================================================
    // Pulse Ring
    // ==================================================

    private void CreatePulseRing()
    {
        if (visualTransform == null)
            return;


        GameObject ringObject =
            new GameObject(
                "Runtime_SplashBombPulseRing"
            );


        ringObject.transform.SetParent(
            visualTransform,
            false
        );


        ringObject.transform.localPosition =
            Vector3.zero;


        pulseRing =
            ringObject.AddComponent<
                LineRenderer
            >();


        pulseRing.useWorldSpace =
            false;


        pulseRing.loop =
            true;


        pulseRing.positionCount =
            Mathf.Max(
                12,
                ringSegments
            );


        pulseRing.startWidth =
            ringWidth;


        pulseRing.endWidth =
            ringWidth;


        pulseRing.numCornerVertices =
            3;


        if (referenceRenderer != null)
        {
            pulseRing.sharedMaterial =
                referenceRenderer.sharedMaterial;


            pulseRing.sortingLayerID =
                referenceRenderer.sortingLayerID;


            pulseRing.sortingOrder =
                referenceRenderer.sortingOrder
                + ringSortingOffset;
        }
    }


    // ==================================================
    // Update Pulse Ring
    // ==================================================

    private void UpdatePulseRing()
    {
        float pulse =
            Mathf.Sin(
                Time.time
                * ringPulseSpeed
            );


        pulse =
            pulse * 0.5f
            + 0.5f;


        float radius =
            ringBaseRadius
            + pulse
            * ringPulseAmount;


        Color color =
            brightColor;


        color.a =
            Mathf.Lerp(
                0.35f,
                0.80f,
                pulse
            );


        pulseRing.startColor =
            color;


        pulseRing.endColor =
            color;


        int segments =
            Mathf.Max(
                12,
                ringSegments
            );


        pulseRing.positionCount =
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


            Vector3 point =
                new Vector3(
                    Mathf.Cos(angle)
                    * radius,

                    Mathf.Sin(angle)
                    * radius,

                    0f
                );


            pulseRing.SetPosition(
                i,
                point
            );
        }
    }


    // ==================================================
    // Color
    // ==================================================

    private void UpdateColors()
    {
        if (InkMap.Instance != null)
        {
            playerColor =
                InkMap.Instance
                    .playerInkColor;
        }
        else
        {
            playerColor =
                new Color(
                    0.1f,
                    0.55f,
                    1f,
                    1f
                );
        }


        playerColor.a =
            1f;


        // Trail 앞부분만 조금 밝게
        brightColor =
            Color.Lerp(
                playerColor,
                Color.white,
                0.30f
            );


        brightColor.a =
            1f;
    }
}