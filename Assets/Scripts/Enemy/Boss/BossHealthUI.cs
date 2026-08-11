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


    // ==================================================
    // Awake
    // ==================================================

    private void Awake()
    {
        if (canvasGroup == null)
        {
            canvasGroup =
                GetComponent<CanvasGroup>();
        }


        HideImmediate();
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