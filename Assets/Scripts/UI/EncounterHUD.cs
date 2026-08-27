using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Reads authoritative encounter state from FloorManager and shows the part
// that matches the current mode. It owns no gameplay state: no timers, no
// wave counting, no completion checks, and it never calls back into the
// encounter. FloorManager must never hold a reference to this.
public class EncounterHUD : MonoBehaviour
{
    [Header("Source")]

    [SerializeField]
    private FloorManager floorManager;

    [Header("Root")]

    [SerializeField]
    private GameObject hudRoot;

    [Header("Elimination")]

    [SerializeField]
    private GameObject eliminationSection;

    [SerializeField]
    private TMP_Text eliminationWaveText;

    [SerializeField]
    private TMP_Text eliminationEnemiesText;

    [Header("Rush")]

    [SerializeField]
    private GameObject rushSection;

    [SerializeField]
    private TMP_Text rushTimerText;

    [SerializeField]
    private TMP_Text rushNextAssaultText;

    [Header("Defense")]

    [SerializeField]
    private GameObject defenseSection;

    [SerializeField]
    private TMP_Text defensePhaseText;

    [SerializeField]
    private TMP_Text defenseTimerText;

    [SerializeField]
    private Image defenseHealthFill;

    [Header("Attention")]

    [Tooltip("Shared by the Rush and Defense final countdowns.")]
    [SerializeField]
    private TMP_Text centerCountdownText;

    [SerializeField]
    private WaveStartUI waveStartUI;

    [SerializeField]
    private EncounterGuideLines guideLines;

    [Min(1)]
    [SerializeField]
    private int lastStandEnemyCount = 3;

    [Min(0f)]
    [SerializeField]
    private float centerCountdownSeconds = 10f;

    [SerializeField]
    private string lastStandMessage = "LAST 3";

    [SerializeField]
    private string lastStandSubMessage = "FINISH THEM";

    [Range(1f, 2f)]
    [SerializeField]
    private float countdownPulseScale = 1.15f;

    [Min(0.01f)]
    [SerializeField]
    private float countdownPulseDuration = 0.15f;


    private bool sectionsApplied;

    private FloorEncounterMode appliedMode;

    private bool hudVisible;

    private DefenseTarget boundDefenseTarget;

    private Transform playerTransform;

    private readonly List<Transform> guideTargets =
        new List<Transform>();

    private int lastStandWaveIndex = -1;

    private bool lastStandActive;

    private int shownCountdownValue = -1;

    private float countdownPulseTimer;


    private void Awake()
    {
        GameObject player =
            GameObject.FindGameObjectWithTag(
                "Player"
            );


        if (player != null)
        {
            playerTransform = player.transform;
        }


        // Sync the cache with what the Scene actually authored, otherwise a
        // hudRoot left active in the Scene stays visible: SetHudVisible would
        // see a matching cached false and return without touching it.
        hudVisible =
            hudRoot != null && hudRoot.activeSelf;


        SetHudVisible(false);


        // Same cache/Scene mismatch as hudRoot: a countdown text left active
        // in the Scene would keep its authored placeholder, because the first
        // hide sees an already-hidden cache and returns.
        if (centerCountdownText != null)
        {
            shownCountdownValue = -1;
            countdownPulseTimer = 0f;


            centerCountdownText.rectTransform
                .localScale = Vector3.one;


            if (centerCountdownText.gameObject
                    .activeSelf)
            {
                centerCountdownText.gameObject
                    .SetActive(false);
            }
        }
    }


    private void OnDisable()
    {
        UnbindDefenseTarget();
        ClearAttentionEffects();
    }


    private void OnDestroy()
    {
        UnbindDefenseTarget();
    }


    private void Update()
    {
        if (floorManager == null ||
            !floorManager.IsEncounterActive)
        {
            UnbindDefenseTarget();
            ClearAttentionEffects();
            SetHudVisible(false);
            return;
        }


        SetHudVisible(true);


        FloorEncounterMode mode =
            floorManager.CurrentEncounterMode;


        ApplySections(mode);


        switch (mode)
        {
            case FloorEncounterMode.Rush:

                ClearLastStand();
                UpdateRush();
                UpdateCenterCountdown(
                    RushCountdownRemaining()
                );

                break;


            case FloorEncounterMode.Defense:

                ClearLastStand();
                UpdateDefense();
                UpdateCenterCountdown(
                    DefenseCountdownRemaining()
                );

                break;


            default:

                UnbindDefenseTarget();
                UpdateElimination();
                UpdateLastStand();
                UpdateCenterCountdown(-1f);

                break;
        }
    }


    // ==================================================
    // Last Stand (Elimination)
    // ==================================================

    // Fires once per Wave, the first frame the Wave's remaining enemies
    // drop to the threshold. Uses <= so a multi-kill cannot skip past it.
    private void UpdateLastStand()
    {
        int waveIndex =
            floorManager.CurrentWave;


        if (lastStandWaveIndex != waveIndex)
        {
            lastStandWaveIndex = waveIndex;
            lastStandActive = false;
            guideLines?.Hide();
        }


        int remaining =
            floorManager.RemainingWaveEnemies;


        if (!lastStandActive &&
            remaining > 0 &&
            remaining <= lastStandEnemyCount)
        {
            lastStandActive = true;


            if (waveStartUI != null)
            {
                waveStartUI.PlayMessage(
                    lastStandMessage,
                    lastStandSubMessage
                );
            }
        }


        if (!lastStandActive || remaining <= 0)
        {
            guideLines?.Hide();
            return;
        }


        DrawGuideLines();
    }


    private void DrawGuideLines()
    {
        if (guideLines == null ||
            playerTransform == null)
        {
            return;
        }


        guideTargets.Clear();


        IReadOnlyList<EnemyWaveMember> enemies =
            floorManager.ActiveEncounterEnemies;


        if (enemies != null)
        {
            for (int i = 0; i < enemies.Count; i++)
            {
                EnemyWaveMember member = enemies[i];


                if (member == null)
                    continue;


                guideTargets.Add(member.transform);


                if (guideTargets.Count >=
                    lastStandEnemyCount)
                {
                    break;
                }
            }
        }


        guideLines.UpdateLines(
            playerTransform,
            guideTargets,
            lastStandEnemyCount
        );
    }


    private void ClearLastStand()
    {
        if (lastStandWaveIndex != -1 ||
            lastStandActive)
        {
            lastStandWaveIndex = -1;
            lastStandActive = false;
        }


        guideLines?.Hide();
    }


    private void ClearAttentionEffects()
    {
        ClearLastStand();
        UpdateCenterCountdown(-1f);
    }


    // ==================================================
    // Center Countdown (Rush / Defense)
    // ==================================================

    private float RushCountdownRemaining()
    {
        return floorManager.RushRemainingTime;
    }


    // Only the final Assault, and never during Rest.
    private float DefenseCountdownRemaining()
    {
        if (floorManager.IsDefenseRestPhase)
            return -1f;


        if (floorManager.DefenseAssaultIndex !=
            floorManager.EncounterWaveCount - 1)
        {
            return -1f;
        }


        return floorManager.DefensePhaseRemaining;
    }


    private void UpdateCenterCountdown(
        float remaining)
    {
        if (centerCountdownText == null)
            return;


        bool show =
            remaining > 0f &&
            remaining <= centerCountdownSeconds;


        if (!show)
        {
            if (shownCountdownValue != -1)
            {
                shownCountdownValue = -1;
                centerCountdownText.gameObject
                    .SetActive(false);
            }


            return;
        }


        int value =
            Mathf.CeilToInt(remaining);


        if (value != shownCountdownValue)
        {
            shownCountdownValue = value;
            countdownPulseTimer =
                countdownPulseDuration;


            centerCountdownText.text =
                value.ToString();


            if (!centerCountdownText.gameObject
                    .activeSelf)
            {
                centerCountdownText.gameObject
                    .SetActive(true);
            }
        }


        if (countdownPulseTimer > 0f)
        {
            countdownPulseTimer -=
                Time.deltaTime;


            float t =
                Mathf.Clamp01(
                    countdownPulseTimer
                    / countdownPulseDuration
                );


            float scale =
                Mathf.Lerp(
                    1f,
                    countdownPulseScale,
                    t
                );


            centerCountdownText.rectTransform
                .localScale =
                    Vector3.one * scale;
        }
        else
        {
            centerCountdownText.rectTransform
                .localScale = Vector3.one;
        }
    }


    // ==================================================
    // Sections
    // ==================================================

    private void SetHudVisible(
        bool visible)
    {
        if (hudVisible == visible)
            return;


        hudVisible = visible;


        if (hudRoot != null)
        {
            hudRoot.SetActive(visible);
        }


        if (!visible)
        {
            sectionsApplied = false;
        }
    }


    private void ApplySections(
        FloorEncounterMode mode)
    {
        if (sectionsApplied &&
            appliedMode == mode)
        {
            return;
        }


        sectionsApplied = true;
        appliedMode = mode;


        SetSectionActive(
            eliminationSection,
            mode == FloorEncounterMode.Elimination
        );


        SetSectionActive(
            rushSection,
            mode == FloorEncounterMode.Rush
        );


        SetSectionActive(
            defenseSection,
            mode == FloorEncounterMode.Defense
        );
    }


    private static void SetSectionActive(
        GameObject section,
        bool active)
    {
        if (section == null)
            return;


        if (section.activeSelf != active)
        {
            section.SetActive(active);
        }
    }


    // ==================================================
    // Elimination
    // ==================================================

    private void UpdateElimination()
    {
        if (eliminationWaveText != null)
        {
            eliminationWaveText.text =
                "WAVE "
                + floorManager.CurrentWave
                + " / "
                + floorManager.EncounterWaveCount;
        }


        if (eliminationEnemiesText != null)
        {
            eliminationEnemiesText.text =
                "ENEMIES "
                + floorManager.RemainingWaveEnemies;
        }
    }


    // ==================================================
    // Rush
    // ==================================================

    private void UpdateRush()
    {
        if (rushTimerText != null)
        {
            rushTimerText.text =
                FormatClock(
                    floorManager.RushRemainingTime
                );
        }


        if (rushNextAssaultText != null)
        {
            rushNextAssaultText.text =
                "NEXT ASSAULT "
                + FormatSeconds(
                    floorManager.RushNextAssaultRemaining
                );
        }
    }


    // ==================================================
    // Defense
    // ==================================================

    private void UpdateDefense()
    {
        bool resting =
            floorManager.IsDefenseRestPhase;


        if (defensePhaseText != null)
        {
            defensePhaseText.text =
                resting
                    ? "REST"
                    : "ASSAULT "
                        + (floorManager.DefenseAssaultIndex + 1)
                        + " / "
                        + floorManager.EncounterWaveCount;
        }


        if (defenseTimerText != null)
        {
            defenseTimerText.text =
                FormatSeconds(
                    floorManager.DefensePhaseRemaining
                );
        }


        BindDefenseTarget(
            floorManager.CurrentDefenseTarget
        );
    }


    private void BindDefenseTarget(
        DefenseTarget target)
    {
        if (ReferenceEquals(
                boundDefenseTarget,
                target))
        {
            return;
        }


        UnbindDefenseTarget();


        if (target == null)
            return;


        boundDefenseTarget = target;


        boundDefenseTarget.HealthChanged +=
            HandleDefenseHealthChanged;


        HandleDefenseHealthChanged(
            boundDefenseTarget.CurrentHealth,
            boundDefenseTarget.MaxHealth
        );
    }


    private void UnbindDefenseTarget()
    {
        if (boundDefenseTarget == null)
            return;


        boundDefenseTarget.HealthChanged -=
            HandleDefenseHealthChanged;


        boundDefenseTarget = null;
    }


    private void HandleDefenseHealthChanged(
        float current,
        float max)
    {
        if (defenseHealthFill == null)
            return;


        defenseHealthFill.fillAmount =
            max > 0f
                ? Mathf.Clamp01(current / max)
                : 0f;
    }


    // ==================================================
    // Format
    // ==================================================

    private static string FormatClock(
        float seconds)
    {
        float safe =
            Mathf.Max(0f, seconds);


        int total =
            Mathf.CeilToInt(safe);


        return (total / 60).ToString("00")
            + ":"
            + (total % 60).ToString("00");
    }


    private static string FormatSeconds(
        float seconds)
    {
        return Mathf.Max(0f, seconds)
            .ToString("0.0");
    }
}
