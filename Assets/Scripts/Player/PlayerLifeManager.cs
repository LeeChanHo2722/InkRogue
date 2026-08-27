using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class PlayerLifeManager : MonoBehaviour
{
    [Header("Lives")]
    [SerializeField] private int maxLives = 3;
    [SerializeField] private int currentLives;

    [Header("References")]
    [SerializeField] private PlayerShield playerShield;
    [SerializeField] private FloorTransitionManager floorTransitionManager;
    [SerializeField] private PlayerFloorSpawnVisual playerSpawnVisual;

    [Header("HUD")]
    [SerializeField] private Image[] lifeImages;
    [SerializeField] private Sprite normalLifeIcon;
    [SerializeField] private Sprite emptyLifeIcon;

    [Header("HUD Visibility")]
    [SerializeField] private CanvasGroup lifeCanvasGroup;

    [Header("Life Animation")]
    [SerializeField] private float activeLifeScale = 1.08f;
    [SerializeField] private float normalLifeScale = 1f;
    [SerializeField] private float emptyLifeScale = 0.85f;

    [SerializeField] private float deathGrowScale = 1.18f;
    [SerializeField] private float deathShrinkScale = 0.45f;
    [SerializeField] private float deathReboundScale = 0.95f;

    [SerializeField] private float shakeAngle = 8f;

    [Header("Death Slow Motion")]

    [SerializeField]
    [Range(0.05f, 1f)]
    private float deathTimeScale = 0.25f;

    [Header("Respawn")]
    [SerializeField] private float respawnDelay = 0.4f;
    [SerializeField] private float respawnInvulnerability = 2f;

    private bool isResolvingDeath;

    public int CurrentLives => currentLives;
    public int MaxLives => maxLives;

    private void Awake()
    {
        if (lifeCanvasGroup == null)
        {
            lifeCanvasGroup =
                GetComponent<CanvasGroup>();
        }

        if (playerShield == null)
        {
            playerShield =
                FindAnyObjectByType<PlayerShield>();
        }

        if (floorTransitionManager == null)
        {
            floorTransitionManager =
                FindAnyObjectByType<FloorTransitionManager>();
        }
        if (playerSpawnVisual == null)
        {
            playerSpawnVisual =
                FindAnyObjectByType<
                    PlayerFloorSpawnVisual
                >();
        }

        currentLives = maxLives;

        UpdateLifeHUD();

        SetHUDVisible(false);
    }

    public void SetHUDVisible(bool visible)
    {
        if (lifeCanvasGroup == null)
            return;

        lifeCanvasGroup.alpha =
            visible ? 1f : 0f;

        lifeCanvasGroup.interactable =
            false;

        lifeCanvasGroup.blocksRaycasts =
            false;
    }

    private void OnEnable()
    {
        if (playerShield != null)
        {
            playerShield.PlayerDefeated +=
                HandlePlayerDefeated;
        }
    }

    private void OnDisable()
    {
        if (playerShield != null)
        {
            playerShield.PlayerDefeated -=
                HandlePlayerDefeated;
        }
    }

    private void HandlePlayerDefeated()
    {
        if (isResolvingDeath)
            return;

        StartCoroutine(
            ResolveDeathRoutine()
        );
    }

    private IEnumerator ResolveDeathRoutine()
    {
        isResolvingDeath = true;

        // ==========================================
        // 사망 판정 즉시 Player 조작 차단
        // ==========================================

        if (floorTransitionManager != null)
        {
            floorTransitionManager
                .LockPlayerForDeath();
        }

        // ==========================================
        // Death Slow Motion
        // ==========================================

        if (floorTransitionManager != null &&
            floorTransitionManager.slowMotion != null)
        {
            floorTransitionManager
                .slowMotion
                .SetTimeScale(
                    deathTimeScale
                );
        }
        else
        {
            Time.timeScale =
                deathTimeScale;
        }


        // ==========================================
        // Player Death VFX
        // ==========================================

        if (playerSpawnVisual != null)
        {
            yield return StartCoroutine(
                playerSpawnVisual
                    .PlayDeath()
            );
        }

        // ==========================================
        // Death VFX 종료 후 정상 속도 복구
        // ==========================================

        if (floorTransitionManager != null &&
            floorTransitionManager.slowMotion != null)
        {
            floorTransitionManager
                .slowMotion
                .RestoreImmediate();
        }
        else
        {
            Time.timeScale = 1f;
        }


        // ==========================================
        // Life HUD 사망 연출
        // ==========================================

        // Defense spends a life on the DefenseTarget, not on the Player.
        // Dying there still costs the full death presentation and time.
        if (!IsDefenseFloorPlayerDeath())
        {
            int lostLifeIndex =
                currentLives - 1;

            if (lostLifeIndex >= 0 &&
                lostLifeIndex < lifeImages.Length &&
                lifeImages[lostLifeIndex] != null)
            {
                yield return StartCoroutine(
                    PlayLifeDeathAnimation(
                        lifeImages[lostLifeIndex]
                    )
                );
            }

            currentLives =
                Mathf.Max(
                    0,
                    currentLives - 1
                );

            UpdateLifeHUD();

            if (currentLives <= 0)
            {
                if (playerShield != null)
                {
                    playerShield.TriggerGameOver();
                }

                yield break;
            }
        }


        // 목숨이 남아 있으면 현재 Wave 유지 후 부활
        if (floorTransitionManager != null)
        {
            yield return StartCoroutine(
                floorTransitionManager
                    .RespawnPlayerRoutine(
                        respawnDelay,
                        respawnInvulnerability
                    )
            );
        }
        else
        {
            Debug.LogError(
                "PlayerLifeManager: " +
                "FloorTransitionManager가 없습니다."
            );
        }

        isResolvingDeath = false;
    }

    private IEnumerator PlayLifeDeathAnimation(
        Image image
    )
    {
        RectTransform rect =
            image.rectTransform;

        Vector3 startScale =
            rect.localScale;

        // 1단계
        // 살짝 커지면서 첫 떨림
        yield return AnimateLifeTransform(
            rect,
            startScale,
            Vector3.one * deathGrowScale,
            0f,
            -shakeAngle,
            0.06f
        );

        // 2단계
        // 반대쪽으로 흔들림
        yield return AnimateLifeTransform(
            rect,
            Vector3.one * deathGrowScale,
            Vector3.one * 1.08f,
            -shakeAngle,
            shakeAngle,
            0.05f
        );

        // 3단계
        // 빠르게 작아지면서 다시 떨림
        yield return AnimateLifeTransform(
            rect,
            Vector3.one * 1.08f,
            Vector3.one * deathShrinkScale,
            shakeAngle,
            -shakeAngle * 0.6f,
            0.10f
        );

        // 가장 작아진 순간
        // Empty 아이콘으로 교체
        image.sprite = emptyLifeIcon;

        rect.localRotation =
            Quaternion.identity;

        // 뿅 VFX
        StartCoroutine(
            PlayPopVFX(image)
        );

        // 4단계
        // Empty 아이콘이 다시 튀어나옴
        yield return AnimateLifeTransform(
            rect,
            Vector3.one * deathShrinkScale,
            Vector3.one * deathReboundScale,
            0f,
            3f,
            0.09f
        );

        // 5단계
        // 최종 Empty 크기로 안정
        yield return AnimateLifeTransform(
            rect,
            Vector3.one * deathReboundScale,
            Vector3.one * emptyLifeScale,
            3f,
            0f,
            0.08f
        );

        rect.localScale =
            Vector3.one * emptyLifeScale;

        rect.localRotation =
            Quaternion.identity;
    }

    private IEnumerator AnimateLifeTransform(
        RectTransform rect,
        Vector3 fromScale,
        Vector3 toScale,
        float fromRotation,
        float toRotation,
        float duration
    )
    {
        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.unscaledDeltaTime;

            float t =
                Mathf.Clamp01(
                    timer / duration
                );

            // 조금 더 부드러운 움직임
            t =
                t * t *
                (3f - 2f * t);

            rect.localScale =
                Vector3.Lerp(
                    fromScale,
                    toScale,
                    t
                );

            float angle =
                Mathf.Lerp(
                    fromRotation,
                    toRotation,
                    t
                );

            rect.localRotation =
                Quaternion.Euler(
                    0f,
                    0f,
                    angle
                );

            yield return null;
        }

        rect.localScale = toScale;

        rect.localRotation =
            Quaternion.Euler(
                0f,
                0f,
                toRotation
            );
    }

    private IEnumerator PlayPopVFX(Image sourceImage)
    {
        GameObject vfxObject =
            new GameObject(
                "LifePopVFX",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image)
            );

        RectTransform vfxRect =
            vfxObject.GetComponent<RectTransform>();

        Image vfxImage =
            vfxObject.GetComponent<Image>();

        RectTransform sourceRect =
            sourceImage.rectTransform;

        // 핵심:
        // LifeHUD가 아니라 해당 Life 아이콘 자체의 자식으로 생성
        vfxRect.SetParent(
            sourceRect,
            false
        );

        // 아이콘 정중앙
        vfxRect.anchorMin =
            new Vector2(0.5f, 0.5f);

        vfxRect.anchorMax =
            new Vector2(0.5f, 0.5f);

        vfxRect.pivot =
            new Vector2(0.5f, 0.5f);

        vfxRect.anchoredPosition =
            Vector2.zero;

        // 부모 Life 아이콘과 동일한 크기
        vfxRect.sizeDelta =
            sourceRect.rect.size;

        // 회전 초기화
        vfxRect.localRotation =
            Quaternion.identity;

        // Normal 아이콘 잔상
        vfxImage.sprite =
            normalLifeIcon;

        vfxImage.raycastTarget =
            false;

        Color color =
            Color.white;

        color.a = 0.65f;

        vfxImage.color =
            color;

        // 처음에는 작게
        vfxRect.localScale =
            Vector3.one * 0.65f;

        // 부모 이미지보다 위에 표시
        vfxRect.SetAsLastSibling();

        float duration = 0.22f;
        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.unscaledDeltaTime;

            float t =
                Mathf.Clamp01(
                    timer / duration
                );

            // 뿅 하고 바깥으로 퍼짐
            float scale =
                Mathf.Lerp(
                    0.65f,
                    1.55f,
                    t
                );

            vfxRect.localScale =
                Vector3.one * scale;

            // 동시에 사라짐
            Color newColor =
                color;

            newColor.a =
                Mathf.Lerp(
                    0.65f,
                    0f,
                    t
                );

            vfxImage.color =
                newColor;

            yield return null;
        }

        Destroy(vfxObject);
    }

    // True only while a Defense Floor is running. The DefenseTarget owns
    // the life cost there, so a plain Player death must not spend one.
    private bool IsDefenseFloorPlayerDeath()
    {
        FloorManager floorManager =
            floorTransitionManager != null
                ? floorTransitionManager.floorManager
                : null;

        return floorManager != null
            && floorManager.IsDefenseEncounterActive;
    }

    // Floor failure that is not a Player death (Defense Target lost).
    // Returns true when a life remains and the Floor can be retried.
    public bool TryConsumeLife()
    {
        if (currentLives <= 0)
        {
            return false;
        }

        currentLives = Mathf.Max(
            0,
            currentLives - 1
        );

        UpdateLifeHUD();

        if (currentLives <= 0)
        {
            if (playerShield != null)
            {
                playerShield.TriggerGameOver();
            }

            return false;
        }

        return true;
    }

    public void ResetForNewFloor()
    {
        StopAllCoroutines();

        currentLives = maxLives;
        isResolvingDeath = false;

        UpdateLifeHUD();
    }

    private void UpdateLifeHUD()
    {
        if (lifeImages == null)
            return;

        for (
            int i = 0;
            i < lifeImages.Length;
            i++
        )
        {
            Image image =
                lifeImages[i];

            if (image == null)
                continue;

            RectTransform rect =
                image.rectTransform;

            rect.localRotation =
                Quaternion.identity;

            // 이미 소진된 목숨
            if (i >= currentLives)
            {
                image.sprite =
                    emptyLifeIcon;

                rect.localScale =
                    Vector3.one *
                    emptyLifeScale;

                continue;
            }

            // 살아있는 목숨
            image.sprite =
                normalLifeIcon;

            // 현재 사용 중인 라이프
            if (i == currentLives - 1)
            {
                rect.localScale =
                    Vector3.one *
                    activeLifeScale;
            }
            else
            {
                rect.localScale =
                    Vector3.one *
                    normalLifeScale;
            }
        }
    }
}