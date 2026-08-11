using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemySpawnVisual : MonoBehaviour
{
    // ==================================================
    // References
    // ==================================================

    [Header("References")]

    [Tooltip("Enemy의 실제 Sprite가 들어있는 VisualRoot")]
    public Transform visualRoot;

    [Tooltip("VisualRoot의 대표 SpriteRenderer")]
    public SpriteRenderer referenceRenderer;


    // ==================================================
    // Warning
    // ==================================================

    [Header("Spawn Warning")]

    [Tooltip("Enemy Ink와 같은 색")]
    public Color warningColor =
        new Color(
            1f,
            0.12f,
            0.28f,
            0.90f
        );

    [Tooltip("적이 등장하기 전 경고 시간")]
    public float warningDuration =
        0.40f;

    public float warningStartRadius =
        0.80f;

    public float warningEndRadius =
        0.35f;

    public float warningWidth =
        0.065f;


    // ==================================================
    // Spawn Ink
    // ==================================================

    [Header("Spawn Ink")]

    public bool paintSpawnInk =
        true;

    [Tooltip("등장 순간 바닥에 생기는 Enemy Ink 크기")]
    public float spawnInkRadius =
        0.70f;

    public int spawnInkSplatCount =
        12;


    // ==================================================
    // Pop Animation
    // ==================================================

    [Header("Pop Animation")]

    [Tooltip("0 → 큰 크기로 튀어나오는 시간")]
    public float emergeDuration =
        0.22f;

    [Tooltip("잠깐 커지는 최대 배율")]
    public float popOvershoot =
        1.15f;

    [Tooltip("큰 크기 → 원래 크기로 돌아오는 시간")]
    public float settleDuration =
        0.12f;


    // ==================================================
    // Burst Ring
    // ==================================================

    [Header("Burst Ring")]

    public float burstStartRadius =
        0.15f;

    public float burstEndRadius =
        0.90f;

    public float burstStartWidth =
        0.12f;


    // ==================================================
    // Rendering
    // ==================================================

    [Header("Rendering")]

    public int ringSegments =
        36;

    [Tooltip("Enemy Sprite보다 아래에 표시")]
    public int sortingOrderOffset =
        -1;


    // ==================================================
    // Runtime
    // ==================================================

    private Rigidbody2D rb;

    private bool originalSimulated;

    private Vector3 originalVisualScale;


    private LineRenderer warningRing;

    private LineRenderer burstRing;


    // Spawn 중 잠글 AI들
    private readonly List<Behaviour>
        lockedBehaviours =
            new List<Behaviour>();


    private readonly List<bool>
        lockedBehaviourStates =
            new List<bool>();


    // ==================================================
    // 첫 프레임 Sprite 깜빡임 방지
    // ==================================================

    private SpriteRenderer[] visualRenderers;

    private bool[] visualRendererStates;


    private bool spawnFinished =
        false;


    public bool IsSpawnFinished =>
        spawnFinished;


    // ==================================================
    // Awake
    // ==================================================

    private void Awake()
    {
        rb =
            GetComponent<Rigidbody2D>();


        // ==========================================
        // VisualRoot 자동 검색
        // ==========================================

        if (visualRoot == null)
        {
            Transform found =
                transform.Find(
                    "VisualRoot"
                );


            if (found != null)
            {
                visualRoot =
                    found;
            }
        }


        // ==========================================
        // 대표 Renderer 자동 검색
        // ==========================================

        if (referenceRenderer == null)
        {
            if (visualRoot != null)
            {
                referenceRenderer =
                    visualRoot
                        .GetComponentInChildren<SpriteRenderer>(
                            true
                        );
            }
            else
            {
                referenceRenderer =
                    GetComponentInChildren<SpriteRenderer>(
                        true
                    );
            }
        }


        // ==========================================
        // 첫 프레임 적 Sprite 깜빡임 방지
        // ==========================================

        if (visualRoot != null)
        {
            visualRenderers =
                visualRoot
                    .GetComponentsInChildren<SpriteRenderer>(
                        true
                    );


            visualRendererStates =
                new bool[
                    visualRenderers.Length
                ];


            for (int i = 0;
                 i < visualRenderers.Length;
                 i++)
            {
                visualRendererStates[i] =
                    visualRenderers[i].enabled;


                visualRenderers[i].enabled =
                    false;
            }
        }


        // ==========================================
        // Spawn 중 Physics 정지
        // ==========================================

        if (rb != null)
        {
            originalSimulated =
                rb.simulated;

            rb.linearVelocity =
                Vector2.zero;

            rb.simulated =
                false;
        }


        // ==========================================
        // 생성되는 즉시 AI 잠금
        //
        // Start까지 기다리지 않는다.
        // ==========================================

        LockEnemyBehaviour();


        CreateRuntimeVFX();
    }


    // ==================================================
    // Start
    // ==================================================

    private void Start()
    {
        // 모든 Awake가 끝난 뒤
        // VisualRoot 원래 Scale 저장
        if (visualRoot != null)
        {
            originalVisualScale =
                visualRoot.localScale;


            visualRoot.localScale =
                Vector3.zero;
        }
        else
        {
            originalVisualScale =
                Vector3.one;
        }


        // Sprite 다시 활성화
        // Scale = 0이라 아직 보이지 않음
        RestoreVisualRenderers();


        StartCoroutine(
            SpawnRoutine()
        );
    }


    // ==================================================
    // Lock AI
    // ==================================================

    private void LockEnemyBehaviour()
    {
        // ==========================================
        // 공격 Script
        // ==========================================

        CacheAndDisable(
            GetComponent<EnemyShooterAttack>()
        );


        CacheAndDisable(
            GetComponent<EnemyChaserAttack>()
        );


        CacheAndDisable(
            GetComponent<EnemyTankAttack>()
        );


        // ==========================================
        // Bomber Attack
        // ==========================================

        CacheAndDisable(
            GetComponent<EnemyBomberAttack>()
        );

        CacheAndDisable(
            GetComponent<EnemySprinklerAttack>()
        );


        // ==========================================
        // 접촉 Damage
        // ==========================================

        CacheAndDisable(
            GetComponent<EnemyContactDamage>()
        );


        // ==========================================
        // Ink Trail
        // ==========================================

        CacheAndDisable(
            GetComponent<EnemyInkTrail>()
        );


        // ==========================================
        // 이동 Script
        // ==========================================

        CacheAndDisable(
            GetComponent<EnemyShooterMovement>()
        );


        CacheAndDisable(
            GetComponent<EnemyMovement>()
        );


        // ==========================================
        // Bomber Movement
        // ==========================================

        CacheAndDisable(
            GetComponent<EnemyBomberMovement>()
        );

        CacheAndDisable(
            GetComponent<EnemySprinklerMovement>()
        );
    }


    // ==================================================
    // Cache Behaviour
    // ==================================================

    private void CacheAndDisable(
        Behaviour behaviour)
    {
        if (behaviour == null)
            return;


        if (lockedBehaviours.Contains(
            behaviour))
        {
            return;
        }


        lockedBehaviours.Add(
            behaviour
        );


        lockedBehaviourStates.Add(
            behaviour.enabled
        );


        behaviour.enabled =
            false;
    }


    // ==================================================
    // Unlock AI
    // ==================================================

    private void UnlockEnemyBehaviour()
    {
        // ==========================================
        // Physics 먼저 복구
        // ==========================================

        if (rb != null)
        {
            rb.simulated =
                originalSimulated;


            rb.linearVelocity =
                Vector2.zero;
        }


        // ==========================================
        // 역순으로 활성화
        //
        // Movement가 Attack보다 먼저 돌아오도록 함.
        // ==========================================

        for (int i =
                 lockedBehaviours.Count - 1;
             i >= 0;
             i--)
        {
            Behaviour behaviour =
                lockedBehaviours[i];


            if (behaviour == null)
                continue;


            behaviour.enabled =
                lockedBehaviourStates[i];
        }


        spawnFinished =
            true;
    }


    // ==================================================
    // Spawn Routine
    // ==================================================

    private IEnumerator SpawnRoutine()
    {
        // ==========================================
        // 1. Warning
        // ==========================================

        if (warningRing != null)
        {
            warningRing.enabled =
                true;
        }


        float timer =
            0f;


        float safeWarningDuration =
            Mathf.Max(
                warningDuration,
                0.01f
            );


        while (timer <
               safeWarningDuration)
        {
            timer +=
                Time.deltaTime;


            float t =
                Mathf.Clamp01(
                    timer
                    / safeWarningDuration
                );


            float eased =
                EaseInCubic(
                    t
                );


            float radius =
                Mathf.Lerp(
                    warningStartRadius,
                    warningEndRadius,
                    eased
                );


            float pulse =
                0.75f
                + Mathf.Sin(
                    Time.time * 28f
                )
                * 0.25f;


            Color color =
                warningColor;


            color.a *=
                Mathf.Lerp(
                    0.45f,
                    1f,
                    t
                )
                * pulse;


            UpdateRing(
                warningRing,
                transform.position,
                radius,
                warningWidth,
                color
            );


            yield return null;
        }


        if (warningRing != null)
        {
            warningRing.enabled =
                false;
        }


        // ==========================================
        // 2. Spawn Ink
        // ==========================================

        if (paintSpawnInk &&
            InkMap.Instance != null)
        {
            InkMap.Instance.PaintExplosion(
                transform.position,
                spawnInkRadius,
                InkTeam.Enemy,
                spawnInkSplatCount
            );
        }


        // ==========================================
        // 3. Burst + Enemy 등장
        // ==========================================

        if (burstRing != null)
        {
            burstRing.enabled =
                true;
        }


        timer =
            0f;


        float safeEmergeDuration =
            Mathf.Max(
                emergeDuration,
                0.01f
            );


        while (timer <
               safeEmergeDuration)
        {
            timer +=
                Time.deltaTime;


            float t =
                Mathf.Clamp01(
                    timer
                    / safeEmergeDuration
                );


            float eased =
                EaseOutCubic(
                    t
                );


            // ======================================
            // Enemy Scale
            // 0 → Overshoot
            // ======================================

            if (visualRoot != null)
            {
                float scale =
                    Mathf.Lerp(
                        0f,
                        popOvershoot,
                        eased
                    );


                visualRoot.localScale =
                    originalVisualScale
                    * scale;
            }


            // ======================================
            // Burst Ring
            // ======================================

            if (burstRing != null)
            {
                float radius =
                    Mathf.Lerp(
                        burstStartRadius,
                        burstEndRadius,
                        eased
                    );


                float width =
                    Mathf.Lerp(
                        burstStartWidth,
                        0.015f,
                        t
                    );


                Color color =
                    warningColor;


                color.a *=
                    1f - t;


                UpdateRing(
                    burstRing,
                    transform.position,
                    radius,
                    width,
                    color
                );
            }


            yield return null;
        }


        if (burstRing != null)
        {
            burstRing.enabled =
                false;
        }


        // ==========================================
        // 4. Overshoot → 원래 크기
        // ==========================================

        timer =
            0f;


        float safeSettleDuration =
            Mathf.Max(
                settleDuration,
                0.01f
            );


        while (timer <
               safeSettleDuration)
        {
            timer +=
                Time.deltaTime;


            float t =
                Mathf.Clamp01(
                    timer
                    / safeSettleDuration
                );


            float eased =
                EaseOutCubic(
                    t
                );


            float scale =
                Mathf.Lerp(
                    popOvershoot,
                    1f,
                    eased
                );


            if (visualRoot != null)
            {
                visualRoot.localScale =
                    originalVisualScale
                    * scale;
            }


            yield return null;
        }


        // ==========================================
        // 5. 최종 복구
        // ==========================================

        if (visualRoot != null)
        {
            visualRoot.localScale =
                originalVisualScale;
        }


        UnlockEnemyBehaviour();
    }


    // ==================================================
    // Create Runtime VFX
    // ==================================================

    private void CreateRuntimeVFX()
    {
        GameObject rootObject =
            new GameObject(
                "Runtime_EnemySpawnVFX"
            );


        rootObject.transform.SetParent(
            transform,
            false
        );


        rootObject.transform.localPosition =
            Vector3.zero;


        warningRing =
            CreateRing(
                rootObject.transform,
                "WarningRing"
            );


        burstRing =
            CreateRing(
                rootObject.transform,
                "BurstRing"
            );


        warningRing.enabled =
            false;


        burstRing.enabled =
            false;
    }


    // ==================================================
    // Create Ring
    // ==================================================

    private LineRenderer CreateRing(
        Transform parent,
        string objectName)
    {
        GameObject ringObject =
            new GameObject(
                objectName
            );


        ringObject.transform.SetParent(
            parent,
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
                8,
                ringSegments
            );


        line.numCornerVertices =
            4;


        line.numCapVertices =
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
    // Update Ring
    // ==================================================

    private void UpdateRing(
        LineRenderer line,
        Vector2 center,
        float radius,
        float width,
        Color color)
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


        line.startWidth =
            width;


        line.endWidth =
            width;


        line.startColor =
            color;


        line.endColor =
            color;


        for (int i = 0;
             i < segments;
             i++)
        {
            float angle =
                Mathf.PI
                * 2f
                * i
                / segments;


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
    // Restore Renderer
    // ==================================================

    private void RestoreVisualRenderers()
    {
        if (visualRenderers == null ||
            visualRendererStates == null)
        {
            return;
        }


        for (int i = 0;
             i < visualRenderers.Length;
             i++)
        {
            if (visualRenderers[i] != null)
            {
                visualRenderers[i].enabled =
                    visualRendererStates[i];
            }
        }
    }


    // ==================================================
    // Ease
    // ==================================================

    private float EaseInCubic(
        float t)
    {
        return
            t * t * t;
    }


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
}