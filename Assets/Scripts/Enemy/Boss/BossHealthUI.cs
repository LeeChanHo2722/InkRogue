using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BossHealthUI : MonoBehaviour
{
    // ==================================================
    // References
    // ==================================================

    [Header("References")]

    public CanvasGroup canvasGroup;

    public TMP_Text bossNameText;

    public TMP_Text phaseText;

    public Image healthFill;


    // ==================================================
    // Boss
    // ==================================================

    [Header("Boss")]

    public string bossName =
        "INK CORE";


    // ==================================================
    // Runtime
    // ==================================================

    private BossHealth bossHealth;
    public bool IsVisible => canvasGroup != null && canvasGroup.alpha > .01f && gameObject.activeInHierarchy;
    private RectTransform hudRect, lifeRect, canvasRect;
    private Vector2 authoredPosition;


    // ==================================================
    // Awake
    // ==================================================

    private void Awake()
    {
        hudRect = GetComponent<RectTransform>();
        authoredPosition = hudRect.anchoredPosition;
        var canvas = GetComponentInParent<Canvas>();
        canvasRect = canvas != null ? canvas.transform as RectTransform : null;
        var lives = FindAnyObjectByType<PlayerLifeManager>(FindObjectsInactive.Include);
        lifeRect = lives != null ? lives.GetComponent<RectTransform>() : null;
        if (canvasGroup == null)
        {
            canvasGroup =
                GetComponent<CanvasGroup>();
        }


        HideImmediate();
    }

    private void OnEnable() { Canvas.willRenderCanvases += AvoidLifeHUD; }
    private void OnDisable() { Canvas.willRenderCanvases -= AvoidLifeHUD; }
    private void AvoidLifeHUD()
    {
        if (!IsVisible || hudRect == null || lifeRect == null || canvasRect == null || healthFill == null) return;
        Bounds life = RectTransformUtility.CalculateRelativeRectTransformBounds(canvasRect, lifeRect);
        Bounds bar = RectTransformUtility.CalculateRelativeRectTransformBounds(canvasRect, healthFill.transform.parent);
        Vector2 offset = hudRect.anchoredPosition - authoredPosition;
        float left = bar.min.x - offset.x, right = bar.max.x - offset.x;
        bool sameRow = bar.max.y - offset.y > life.min.y && bar.min.y - offset.y < life.max.y;
        float shift = sameRow ? Mathf.Max(0, life.max.x + 12f - left) : 0;
        Vector2 next = authoredPosition;
        if (right + shift <= canvasRect.rect.xMax - 16f) next.x += shift;
        else next.y -= life.size.y + 16f;
        if (hudRect.anchoredPosition != next) hudRect.anchoredPosition = next;
    }


    // ==================================================
    // Bind
    // ==================================================

    public void Bind(
        BossHealth health)
    {
        Unbind();


        bossHealth =
            health;


        if (bossHealth == null)
            return;


        bossHealth.HealthChanged +=
            OnHealthChanged;


        bossHealth.PhaseChanged +=
            OnPhaseChanged;


        bossHealth.BossDied +=
            OnBossDied;


        if (bossNameText != null)
        {
            bossNameText.text =
                bossName;
        }


        OnHealthChanged(
            bossHealth.CurrentHealth,
            bossHealth.MaxHealth
        );


        OnPhaseChanged(
            bossHealth.CurrentPhase
        );
    }


    // ==================================================
    // Show
    // ==================================================

    public void Show()
    {
        if (canvasGroup == null)
            return;


        canvasGroup.alpha =
            1f;


        canvasGroup.blocksRaycasts =
            false;
    }


    // ==================================================
    // Hide
    // ==================================================

    public void HideImmediate()
    {
        if (canvasGroup == null)
            return;


        canvasGroup.alpha =
            0f;


        canvasGroup.blocksRaycasts =
            false;
    }


    // ==================================================
    // Health
    // ==================================================

    private void OnHealthChanged(
        int current,
        int maximum)
    {
        if (healthFill == null)
            return;


        float percent =
            maximum <= 0
                ? 0f
                : Mathf.Clamp01(
                    (float)current
                    / maximum
                );


        healthFill.fillAmount =
            percent;
    }


    // ==================================================
    // Phase
    // ==================================================

    private void OnPhaseChanged(
        int phase)
    {
        if (phaseText == null)
            return;


        phaseText.text =
            "PHASE "
            + phase;
    }


    // ==================================================
    // Death
    // ==================================================

    private void OnBossDied()
    {
        HideImmediate();
    }


    // ==================================================
    // Unbind
    // ==================================================

    private void Unbind()
    {
        if (bossHealth == null)
            return;


        bossHealth.HealthChanged -=
            OnHealthChanged;


        bossHealth.PhaseChanged -=
            OnPhaseChanged;


        bossHealth.BossDied -=
            OnBossDied;


        bossHealth =
            null;
    }


    private void OnDestroy()
    {
        Unbind();
    }
}
