using System.Collections;
using UnityEngine;

public partial class FloorTransitionManager
{
    private IEnumerator RestoreSafeStage()
    {
        firstFloorStarted = true;
        transitionRunning = true; LockPlayer();
        var stage = RunPersistence.Instance.ResumeStage;
        RunPersistence.Instance.FinishResume();
        ClearFloorCombatObjects();
        if (screenWipe != null) yield return StartCoroutine(screenWipe.Reveal());
        Time.timeScale = 0; transitionRunning = false; waitingForUpgrade = true;
        if (stage == RunSaveStage.PowerChoice) TowerRunUI.Instance.ShowPowerChoices(UpgradeSelected);
        else if (stage == RunSaveStage.Travel) StartCoroutine(NextFloorRoutine());
        else UpgradeSelected();
    }
}
