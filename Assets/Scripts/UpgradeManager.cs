using System.Collections;
using UnityEngine;

public class UpgradeManager : MonoBehaviour
{
    // ==================================================
    // References
    // ==================================================

    [Header("References")]

    public GameObject upgradePanel;

    public PlayerMovement playerMovement;

    public PlayerShoot playerShoot;

    public FloorTransitionManager transitionManager;


    // ==================================================
    // Upgrade Cards
    // ==================================================

    [Header("Upgrade Cards")]

    public UpgradeCardUI powerCardUI;

    public UpgradeCardUI rapidCardUI;

    public UpgradeCardUI speedCardUI;


    // ==================================================
    // UI Feedback
    // ==================================================

    [Header("UI Feedback")]

    public UpgradeSelectionFeedback
        selectionFeedback;

    public UpgradePanelIntro
        panelIntro;


    // ==================================================
    // Levels
    // ==================================================

    private int powerLevel = 0;

    private int rapidLevel = 0;

    private int speedLevel = 0;


    public int PowerLevel =>
        powerLevel;


    public int RapidLevel =>
        rapidLevel;


    public int SpeedLevel =>
        speedLevel;


    // ==================================================
    // State
    // ==================================================

    private bool selectionLocked =
        false;


    // ==================================================
    // Start
    // ==================================================

    private void Start()
    {
        if (upgradePanel != null)
        {
            upgradePanel.SetActive(
                false
            );
        }
    }


    // ==================================================
    // Show
    // ==================================================

    public void ShowUpgrades()
    {
        selectionLocked =
            false;


        if (upgradePanel != null)
        {
            upgradePanel.SetActive(
                true
            );
        }


        // ==========================================
        // 이전 선택 상태 제거
        // ==========================================

        if (selectionFeedback != null)
        {
            selectionFeedback
                .ResetCardsImmediate();
        }


        // ==========================================
        // 실제 Level 갱신
        // ==========================================

        RefreshCardUI();


        // ==========================================
        // Wipe 뒤에 숨어있는 동안
        // 등장 준비 상태로 만듦
        // ==========================================

        if (panelIntro != null)
        {
            panelIntro
                .PrepareHidden();
        }
    }


    // ==================================================
    // Open Animation
    // ==================================================

    public IEnumerator PlayOpenAnimation()
    {
        if (panelIntro != null)
        {
            yield return StartCoroutine(
                panelIntro.PlayIntro()
            );
        }
    }


    // ==================================================
    // Hide
    // ==================================================

    public void HideUpgrades()
    {
        if (upgradePanel != null)
        {
            upgradePanel.SetActive(
                false
            );
        }
    }


    // ==================================================
    // POWER
    // ==================================================

    public void ChoosePower()
    {
        if (selectionLocked)
            return;


        selectionLocked =
            true;


        if (playerShoot != null)
        {
            playerShoot.bulletDamage +=
                1;
        }


        powerLevel++;


        Debug.Log(
            "POWER selected! "
            + "Damage: "
            + (
                playerShoot != null
                    ? playerShoot.bulletDamage
                    : 0
            )
            + " | Level: "
            + powerLevel
        );


        StartCoroutine(
            FinishSelectionRoutine(
                UpgradeCardUI
                    .UpgradeType
                    .Power
            )
        );
    }


    // ==================================================
    // RAPID
    // ==================================================

    public void ChooseRapid()
    {
        if (selectionLocked)
            return;


        selectionLocked =
            true;


        if (playerShoot != null)
        {
            playerShoot.fireRate *=
                1.25f;
        }


        rapidLevel++;


        Debug.Log(
            "RAPID selected! "
            + "Fire Rate: "
            + (
                playerShoot != null
                    ? playerShoot.fireRate
                    : 0f
            )
            + " | Level: "
            + rapidLevel
        );


        StartCoroutine(
            FinishSelectionRoutine(
                UpgradeCardUI
                    .UpgradeType
                    .Rapid
            )
        );
    }


    // ==================================================
    // SPEED
    // ==================================================

    public void ChooseSpeed()
    {
        if (selectionLocked)
            return;


        selectionLocked =
            true;


        if (playerMovement != null)
        {
            playerMovement.moveSpeed *=
                1.2f;
        }


        speedLevel++;


        Debug.Log(
            "SPEED selected! "
            + "Move Speed: "
            + (
                playerMovement != null
                    ? playerMovement.moveSpeed
                    : 0f
            )
            + " | Level: "
            + speedLevel
        );


        StartCoroutine(
            FinishSelectionRoutine(
                UpgradeCardUI
                    .UpgradeType
                    .Speed
            )
        );
    }


    // ==================================================
    // Finish Selection
    // ==================================================

    private IEnumerator FinishSelectionRoutine(
        UpgradeCardUI.UpgradeType selectedType)
    {
        if (selectionFeedback != null)
        {
            yield return StartCoroutine(
                selectionFeedback
                    .PlaySelection(
                        selectedType
                    )
            );
        }


        if (transitionManager != null)
        {
            transitionManager
                .UpgradeSelected();
        }
    }


    // ==================================================
    // Refresh
    // ==================================================

    private void RefreshCardUI()
    {
        if (powerCardUI != null)
        {
            powerCardUI.Refresh(
                this
            );
        }


        if (rapidCardUI != null)
        {
            rapidCardUI.Refresh(
                this
            );
        }


        if (speedCardUI != null)
        {
            speedCardUI.Refresh(
                this
            );
        }
    }
}