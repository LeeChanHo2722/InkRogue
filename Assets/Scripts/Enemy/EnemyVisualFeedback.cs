using System.Collections;
using UnityEngine;

public class EnemyVisualFeedback : MonoBehaviour
{
    // ==================================================
    // References
    // ==================================================

    [Header("References")]
    public Transform visualRoot;
    public SpriteRenderer primaryRenderer;
    public EnemyHealth enemyHealth;


    // ==================================================
    // Health Tint
    // ==================================================

    [Header("Health Color")]

    [Tooltip("플레이어 Ink와 같은 색으로 설정")]
    public Color playerInkColor =
        new Color(
            0.15f,
            0.45f,
            1f,
            1f
        );

    [Range(0f, 1f)]
    [Tooltip("HP가 거의 0일 때 Player Ink색으로 물드는 최대 정도")]
    public float maxHealthTintStrength = 0.90f;

    [Range(0.25f, 3f)]
    [Tooltip("1보다 작으면 초중반부터 색 변화가 잘 보임")]
    public float healthTintPower = 0.80f;


    // ==================================================
    // Hit
    // ==================================================

    [Header("Hit Feedback")]

    public Color hitFlashColor =
        Color.white;

    public float hitDuration =
        0.11f;

    public float hitScaleX =
        1.14f;

    public float hitScaleY =
        0.86f;


    // ==================================================
    // Death
    // ==================================================

    [Header("Death Burst")]

    public int deathFragmentCount =
        12;

    public float deathFragmentMinSpeed =
        1.8f;

    public float deathFragmentMaxSpeed =
        3.8f;

    public float deathFragmentMinScale =
        0.08f;

    public float deathFragmentMaxScale =
        0.18f;

    public float deathEffectDuration =
        0.45f;


    // ==================================================
    // Runtime
    // ==================================================

    private SpriteRenderer[] renderers;

    // 몬스터 원래 색
    private Color[] originalColors;

    // 현재 HP를 반영한 색
    private Color[] healthColors;

    private Vector3 baseVisualScale;

    private Coroutine hitRoutine;

    private bool deathPlayed =
        false;


    // ==================================================
    // Awake
    // ==================================================

    private void Awake()
    {
        if (enemyHealth == null)
        {
            enemyHealth =
                GetComponent<EnemyHealth>();
        }


        if (visualRoot == null)
        {
            visualRoot =
                transform.Find(
                    "VisualRoot"
                );
        }


        if (visualRoot == null)
        {
            Debug.LogWarning(
                name
                + " : VisualRoot가 없습니다."
            );

            return;
        }


        renderers =
            visualRoot
                .GetComponentsInChildren<SpriteRenderer>(
                    true
                );


        if (primaryRenderer == null &&
            renderers.Length > 0)
        {
            primaryRenderer =
                renderers[0];
        }


        originalColors =
            new Color[
                renderers.Length
            ];


        healthColors =
            new Color[
                renderers.Length
            ];


        for (int i = 0;
             i < renderers.Length;
             i++)
        {
            originalColors[i] =
                renderers[i].color;

            healthColors[i] =
                originalColors[i];
        }


        baseVisualScale =
            visualRoot.localScale;


        RefreshHealthTint();
    }


    // ==================================================
    // Update
    // ==================================================

    private void Update()
    {
        if (deathPlayed)
            return;


        RefreshHealthTint();


        // Hit Flash 중이 아닐 때만
        // 현재 HP 색 적용
        if (hitRoutine == null)
        {
            ApplyHealthTint();
        }
    }


    // ==================================================
    // Health Tint
    // ==================================================

    private void RefreshHealthTint()
    {
        if (enemyHealth == null ||
            renderers == null ||
            originalColors == null)
        {
            return;
        }


        float healthPercent =
            enemyHealth
                .CurrentHealthPercent;


        // Full HP = 0
        // Dead = 1
        float damagePercent =
            1f - healthPercent;


        float tintAmount =
            Mathf.Pow(
                damagePercent,
                healthTintPower
            );


        tintAmount *=
            maxHealthTintStrength;


        tintAmount =
            Mathf.Clamp01(
                tintAmount
            );


        for (int i = 0;
             i < renderers.Length;
             i++)
        {
            healthColors[i] =
                Color.Lerp(
                    originalColors[i],
                    playerInkColor,
                    tintAmount
                );


            // 원래 Sprite Alpha 유지
            healthColors[i].a =
                originalColors[i].a;
        }
    }


    private void ApplyHealthTint()
    {
        if (renderers == null ||
            healthColors == null)
        {
            return;
        }


        for (int i = 0;
             i < renderers.Length;
             i++)
        {
            if (renderers[i] == null)
                continue;


            renderers[i].color =
                healthColors[i];
        }
    }


    // ==================================================
    // Hit
    // ==================================================

    public void PlayHit()
    {
        if (deathPlayed)
            return;


        if (visualRoot == null ||
            renderers == null)
        {
            return;
        }


        // Damage 반영 직후의 새 HP색 계산
        RefreshHealthTint();


        if (hitRoutine != null)
        {
            StopCoroutine(
                hitRoutine
            );


            hitRoutine =
                null;


            RestoreVisual();
        }


        hitRoutine =
            StartCoroutine(
                HitRoutine()
            );
    }


    private IEnumerator HitRoutine()
    {
        float timer =
            0f;


        float firstHalf =
            hitDuration
            * 0.35f;


        // ==================================================
        // Flash In
        // ==================================================

        while (timer <
               firstHalf)
        {
            timer +=
                Time.deltaTime;


            float t =
                Mathf.Clamp01(
                    timer
                    /
                    Mathf.Max(
                        firstHalf,
                        0.001f
                    )
                );


            ApplyFlash(
                t
            );


            Vector3 targetScale =
                Vector3.Scale(
                    baseVisualScale,
                    new Vector3(
                        hitScaleX,
                        hitScaleY,
                        1f
                    )
                );


            visualRoot.localScale =
                Vector3.Lerp(
                    baseVisualScale,
                    targetScale,
                    t
                );


            yield return null;
        }


        // ==================================================
        // Flash Out
        // ==================================================

        timer =
            0f;


        float secondHalf =
            hitDuration
            - firstHalf;


        Vector3 hitScale =
            visualRoot.localScale;


        while (timer <
               secondHalf)
        {
            timer +=
                Time.deltaTime;


            float t =
                Mathf.Clamp01(
                    timer
                    /
                    Mathf.Max(
                        secondHalf,
                        0.001f
                    )
                );


            ApplyFlash(
                1f - t
            );


            visualRoot.localScale =
                Vector3.Lerp(
                    hitScale,
                    baseVisualScale,
                    t
                );


            yield return null;
        }


        RestoreVisual();


        hitRoutine =
            null;
    }


    // ==================================================
    // Flash Color
    // ==================================================

    private void ApplyFlash(
        float amount)
    {
        amount =
            Mathf.Clamp01(
                amount
            );


        if (renderers == null ||
            healthColors == null)
        {
            return;
        }


        for (int i = 0;
             i < renderers.Length;
             i++)
        {
            if (renderers[i] == null)
                continue;


            // 원래 몬스터색 기준이 아니라
            // 현재 HP색에서 White로 Flash
            renderers[i].color =
                Color.Lerp(
                    healthColors[i],
                    hitFlashColor,
                    amount
                );
        }
    }


    // ==================================================
    // Death
    // ==================================================

    public void PlayDeath()
    {
        if (deathPlayed)
            return;


        deathPlayed =
            true;


        if (hitRoutine != null)
        {
            StopCoroutine(
                hitRoutine
            );


            hitRoutine =
                null;
        }


        if (primaryRenderer == null)
            return;


        RefreshHealthTint();


        GameObject effectObject =
            new GameObject(
                "EnemyDeathBurst"
            );


        effectObject.transform.position =
            primaryRenderer
                .transform
                .position;


        EnemyDeathBurst effect =
            effectObject
                .AddComponent<EnemyDeathBurst>();


        // 죽을 때도 현재 HP색 사용
        Color deathColor =
            primaryRenderer.color;


        if (healthColors != null &&
            healthColors.Length > 0)
        {
            deathColor =
                healthColors[0];
        }


        effect.Initialize(
            primaryRenderer.sprite,
            primaryRenderer.sharedMaterial,

            primaryRenderer.sortingLayerID,
            primaryRenderer.sortingOrder,

            deathColor,

            primaryRenderer
                .transform
                .lossyScale,

            deathFragmentCount,

            deathFragmentMinSpeed,
            deathFragmentMaxSpeed,

            deathFragmentMinScale,
            deathFragmentMaxScale,

            deathEffectDuration
        );


        // 적 본체는 즉시 숨김
        if (renderers != null)
        {
            foreach (
                SpriteRenderer renderer
                in renderers)
            {
                if (renderer != null)
                {
                    renderer.enabled =
                        false;
                }
            }
        }
    }


    // ==================================================
    // Restore
    // ==================================================

    private void RestoreVisual()
    {
        if (visualRoot != null)
        {
            visualRoot.localScale =
                baseVisualScale;
        }


        // 원래색이 아니라 현재 HP색으로 복귀
        ApplyHealthTint();
    }
}