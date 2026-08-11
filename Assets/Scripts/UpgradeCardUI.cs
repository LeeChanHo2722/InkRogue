using TMPro;
using UnityEngine;

public class UpgradeCardUI : MonoBehaviour
{
    // ==================================================
    // Upgrade Type
    // ==================================================

    public enum UpgradeType
    {
        Power,
        Rapid,
        Speed
    }


    [Header("Upgrade")]

    public UpgradeType upgradeType;


    // ==================================================
    // Text References
    // ==================================================

    [Header("Text")]

    public TMP_Text titleText;

    public TMP_Text effectText;

    public TMP_Text levelText;


    // ==================================================
    // Refresh
    // ==================================================

    public void Refresh(
        UpgradeManager manager)
    {
        if (manager == null)
            return;


        int currentLevel = 0;


        switch (upgradeType)
        {
            // ==========================================
            // POWER
            // ==========================================

            case UpgradeType.Power:

                currentLevel =
                    manager.PowerLevel;


                if (titleText != null)
                {
                    titleText.text =
                        "POWER";
                }


                if (effectText != null)
                {
                    effectText.text =
                        "DAMAGE +1";
                }

                break;


            // ==========================================
            // RAPID
            // ==========================================

            case UpgradeType.Rapid:

                currentLevel =
                    manager.RapidLevel;


                if (titleText != null)
                {
                    titleText.text =
                        "RAPID";
                }


                if (effectText != null)
                {
                    effectText.text =
                        "FIRE RATE +25%";
                }

                break;


            // ==========================================
            // SPEED
            // ==========================================

            case UpgradeType.Speed:

                currentLevel =
                    manager.SpeedLevel;


                if (titleText != null)
                {
                    titleText.text =
                        "SPEED";
                }


                if (effectText != null)
                {
                    effectText.text =
                        "MOVE SPEED +20%";
                }

                break;
        }


        // ==========================================
        // Level
        // ==========================================

        if (levelText != null)
        {
            levelText.text =
                "LV."
                + currentLevel
                + "  ¡æ  LV."
                + (currentLevel + 1);
        }
    }
}