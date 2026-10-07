using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;

// Opt-in local verification harness. Inert unless BOTH an isolated save directory
// and an explicit process-probe argument are supplied. Never uses player save data.
public sealed class RunSaveProcessProbe : MonoBehaviour
{
    private string folder, scenario;
    private int checks;
    private static TowerRunUI UI => TowerRunUI.Instance;
    private static RunManager Run => UI.Run;
    private static FloorManager Floor => FindAnyObjectByType<FloorManager>();
    private static PlayerLifeManager Lives => FindAnyObjectByType<PlayerLifeManager>();
    private static FloorSelectionUI Routes => FindAnyObjectByType<FloorSelectionUI>(FindObjectsInactive.Include);
    private static RunPersistence Persistence => RunPersistence.Instance;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        var args = Environment.GetCommandLineArgs(); int index = Array.IndexOf(args, "-inkrogue-save-probe");
        string path = Environment.GetEnvironmentVariable("INKROGUE_SAVE_TEST_PATH");
        if (index < 0 || index + 1 >= args.Length || string.IsNullOrWhiteSpace(path)) return;
        if (FindAnyObjectByType<RunSaveProcessProbe>() != null) return;
        var probe = new GameObject("RunSaveProcessProbe").AddComponent<RunSaveProcessProbe>();
        DontDestroyOnLoad(probe); probe.folder = Path.GetFullPath(path); probe.scenario = args[index + 1];
        Directory.CreateDirectory(probe.folder); probe.StartCoroutine(probe.Drive());
    }
    private void Check(bool condition, string text)
    { if (!condition) throw new Exception(text); checks++; Debug.Log("SAVE_PROCESS_PASS " + text); }
    private static void Invoke(object target, string method, params object[] args) => target.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Invoke(target, args);
    private static bool Playing => UI != null && UI.HasStarted && Floor != null && Floor.IsEncounterActive && FindAnyObjectByType<PlayerWeaponInputController>().InputEnabled;
    private IEnumerator Wait(Func<bool> condition, string text)
    { float end = Time.realtimeSinceStartup + 90; while (!condition()) { if (Time.realtimeSinceStartup > end) throw new Exception("Timeout: " + text); yield return null; } }
    private IEnumerator Drive()
    {
        var stack = new System.Collections.Generic.Stack<IEnumerator>(); stack.Push(Scenario());
        while (stack.Count > 0)
        {
            object current;
            try
            {
                if (!stack.Peek().MoveNext()) { stack.Pop(); continue; }
                current = stack.Peek().Current;
            }
            catch (Exception e)
            {
                File.WriteAllText(Path.Combine(folder, "failed-" + scenario + ".txt"), e.ToString());
                Debug.LogException(e); Application.Quit(1); yield break;
            }
            if (current is IEnumerator nested) stack.Push(nested); else yield return current;
        }
    }
    private IEnumerator Scenario()
    {
        yield return Wait(() => UI != null, "main menu");
        if (scenario == "read" || scenario == "read-ui")
        {
            var expected = RunSaveJson.FromJson<SavedRun>(File.ReadAllText(Path.Combine(folder, "expected.json")));
            var stored = Persistence.Checkpoint;
            Check(stored != null && RunSaveJson.ToJson(stored) == RunSaveJson.ToJson(expected), "fresh process loads exact committed checkpoint");
            UI.ContinueRun();
            if (expected.stage == RunSaveStage.PowerChoice) yield return Wait(() => UI.ChoosingPower, "restored power screen");
            else if (expected.stage == RunSaveStage.RouteChoice) yield return Wait(() => Routes != null && Routes.IsVisible, "restored route screen");
            else yield return Wait(() => Playing, "restored floor combat");
            int expectedFloor = expected.floor + (expected.stage == RunSaveStage.Travel ? 1 : 0);
            Check(Run.CurrentFloor == expectedFloor && Run.CurrentMode == expected.mode && Run.RunSeed == expected.seed, "mode floor seed restored");
            Check(Lives.CurrentLives == expected.lives && Persistence.Checkpoint.deaths == expected.deaths, "normal death and remaining lives persist without exit penalty");
            Check(Run.LeftFloorLoadout.Select(w => w == null ? "" : w.WeaponId).SequenceEqual(expected.left) && Run.RightFloorLoadout.Select(w => w == null ? "" : w.WeaponId).SequenceEqual(expected.right), "non-default confirmed equipment restored; draft discarded");
            Check(TowerPowerCatalog.All.All(p => TowerPowers.Instance.Level(p.id) == expected.powerLevels[(int)p.id]), "player and robot powers restored exactly");
            Check(Run.BossesDefeated == expected.bosses && Run.RewardedFloors == expected.rewardedFloors, "reward and boss counters do not duplicate");
            Check(Run.ElapsedSeconds >= expected.elapsedSeconds, "elapsed time retained");
            if (expected.stage == RunSaveStage.PowerChoice)
            {
                Check(UI.PowerChoices.Select(p => (int)p.id).SequenceEqual(expected.powerOffers), "same persisted reward offers shown");
                UI.ChoosePower(0); int total = TowerPowerCatalog.All.Sum(p => TowerPowers.Instance.Level(p.id)); UI.ChoosePower(0);
                Check(total == TowerPowerCatalog.All.Sum(p => TowerPowers.Instance.Level(p.id)), "power consumption happens once");
            }
            if (expected.stage == RunSaveStage.RouteChoice)
            {
                Check(RunSaveJson.ToJson(new RoutesWrapper { routes = Run.FloorCandidates.Select(RunManager.SaveRoute).ToArray() }) == RunSaveJson.ToJson(new RoutesWrapper { routes = expected.candidates }), "cached route objectives maps and seeds unchanged");
            }
            if (expected.stage == RunSaveStage.FloorStart || expected.stage == RunSaveStage.Travel)
                Check(Mathf.Approximately(FindAnyObjectByType<PlayerShield>().CurrentShield, expected.checkpointShield), "floor restart uses checkpoint shield");
            if (scenario == "read-ui")
            {
                Persistence.SaveAndLeave();
                yield return Wait(() => UI != null && !UI.HasStarted, "saved return to menu");
                Check(Persistence.Checkpoint.runId == expected.runId, "save-and-menu preserves active run");
                UI.RequestNewRun(RunMode.TwentyFloor);
                yield return null; yield return null;
                Click("취소 · 기존 저장 유지");
                Check(Persistence.Checkpoint.runId == expected.runId && Persistence.Book.history.Count == 0, "new-run cancellation preserves active and history");
                yield return Capture("continue-menu.png");
                UI.RequestNewRun(RunMode.TwentyFloor); yield return null; yield return null; Click("확인 · 새 런 시작");
                yield return Wait(() => Playing, "confirmed replacement starts new campaign");
                Check(Persistence.Checkpoint.runId != expected.runId && Persistence.Book.history.Count(h => h.run.runId == expected.runId && h.result == "Abandoned") == 1, "confirmed replacement records old run once and commits new run");
                string newRunId = Persistence.Checkpoint.runId;
                Persistence.Store.FailureInjection = point => { if (point == "after-temp") throw new IOException("Injected disk write interruption"); };
                Persistence.SaveAndLeave();
                Check(Persistence.Pending && UI.HasStarted && Time.timeScale == 0, "write failure blocks exit and presents retry");
                Check(new RunSaveStore(folder).Load().active.runId == newRunId, "write failure retains previous valid checkpoint");
                yield return Capture("save-error.png");
                Persistence.Store.FailureInjection = null; Click("다시 저장");
                yield return Wait(() => UI != null && !UI.HasStarted, "successful save retry returns to menu");
                Check(Persistence.Checkpoint.runId == newRunId, "retry saves before leaving");
                UI.ShowHistory(0); yield return null;
                yield return Capture("history-list.png");
                Click("중단"); yield return null;
                yield return Capture("history-detail.png");
                Check(FindObjectsByType<TMPro.TMP_Text>(FindObjectsSortMode.None).Any(t => t.gameObject.activeInHierarchy && t.text.Contains("정상 사망")), "history detail shows death and build summary");
            }
            Complete("reader"); yield break;
        }
        UI.StartRun(RunMode.Endless);
        yield return Wait(() => Playing, "first floor");
        var shield = FindAnyObjectByType<PlayerShield>(); shield.ResetAfterRespawn(120);
        TowerPowers.Instance.Add(TowerPowerId.Nozzle); TowerPowers.Instance.Add(TowerPowerId.Nozzle);
        TowerPowers.Instance.Add(TowerPowerId.RobotCore); TowerPowers.Instance.Add(TowerPowerId.CleaningTrail);
        FindAnyObjectByType<FloorTransitionManager>().BeginFloorClearTransition();
        yield return Wait(() => UI.ChoosingPower, "first power screen"); UI.ChoosePower(0);
        yield return Wait(() => Routes.IsVisible, "first route screen");
        // Use the actual equipment commit operation, then the actual route callback.
        Routes.TogglePreparation();
        for (int i = 0; i < 6; i++) { Invoke(Routes, "PickWeapon", (object)null); Routes.AssignSlot(i); }
        var blade = Run.RunInventory.First(w => w.WeaponId == "blade");
        var machine = Run.RunInventory.First(w => w.WeaponId == "machine_gun");
        Invoke(Routes, "PickWeapon", blade); Routes.AssignSlot(0);
        Invoke(Routes, "PickWeapon", machine); Routes.AssignSlot(2); Routes.AssignSlot(4);
        Routes.TogglePreparation();
        Invoke(Routes, "SelectCard", 1); Routes.Confirm();
        yield return Wait(() => Playing && Run.CurrentFloor == 2, "second floor");
        shield.ResetAfterRespawn(120);
        Lives.TryConsumeLife();
        Check(Lives.CurrentLives == 2 && Persistence.Checkpoint.lives == 2 && Persistence.Checkpoint.deaths == 1, "normal life loss immediately checkpoints");
        if (scenario == "write-combat")
        {
            // Uncommitted combat state must disappear on process kill.
            File.WriteAllText(Path.Combine(folder, "expected.json"), RunSaveJson.ToJson(Persistence.Checkpoint, true));
            TowerPowers.Instance.Add(TowerPowerId.Nozzle);
            shield.ResetAfterRespawn(0); shield.TakeDamage(1);
            FindAnyObjectByType<PlayerMovement>().transform.position += Vector3.right * .25f;
            yield return Capture(scenario + ".png"); Ready(); while (true) yield return null; // External test runner kills this exact PID.
        }
        FindAnyObjectByType<FloorTransitionManager>().BeginFloorClearTransition();
        yield return Wait(() => UI.ChoosingPower, "second power screen");
        if (scenario != "write-power")
        {
            UI.ChoosePower(0); yield return Wait(() => Routes.IsVisible, "second route screen");
            if (scenario == "write-travel")
            { Invoke(Routes, "SelectCard", 2); Routes.Confirm(); Check(Persistence.Checkpoint.stage == RunSaveStage.Travel, "route departure commits before animation"); }
            else
            {
                Routes.TogglePreparation(); Invoke(Routes, "PickWeapon", (object)null); Routes.AssignSlot(0);
                Check(Routes.IsPreparing && Run.LeftFloorLoadout[0] == blade, "unconfirmed draft leaves committed equipment intact");
            }
        }
        File.WriteAllText(Path.Combine(folder, "expected.json"), RunSaveJson.ToJson(Persistence.Checkpoint, true));
        yield return Capture(scenario + ".png"); Ready(); Complete("writer");
    }
    private void Click(string prefix)
    {
        var button = FindObjectsByType<UnityEngine.UI.Button>(FindObjectsSortMode.None).First(b => b.gameObject.activeInHierarchy && b.GetComponentInChildren<TMPro.TMP_Text>()?.text.StartsWith(prefix) == true);
        var hits = new System.Collections.Generic.List<UnityEngine.EventSystems.RaycastResult>();
        var system = UnityEngine.EventSystems.EventSystem.current;
        Canvas.ForceUpdateCanvases();
        system.RaycastAll(new UnityEngine.EventSystems.PointerEventData(system) { position = RectTransformUtility.WorldToScreenPoint(null, button.transform.position) }, hits);
        Check(hits.Count > 0 && hits[0].gameObject.GetComponentInParent<UnityEngine.UI.Button>() == button, "visible UI receives pointer: " + prefix);
        button.onClick.Invoke();
    }
    [Serializable] private sealed class RoutesWrapper { public SavedRoute[] routes; }
    private void Ready()
    {
        File.WriteAllText(Path.Combine(folder, "ready.json"), "{\"pid\":" + System.Diagnostics.Process.GetCurrentProcess().Id + ",\"stage\":\"" + Persistence.Checkpoint.stage + "\"}");

    }
    private IEnumerator Capture(string name)
    {
        yield return null;
        // Hidden player windows have no capturable backbuffer. Render the same
        // camera and overlay canvases into an explicit target, then restore them.
        const int width = 1280, height = 720;
        var camera = Camera.main;
        var render = new RenderTexture(width, height, 24);
        var oldTarget = camera.targetTexture; var oldActive = RenderTexture.active;
        var overlays = FindObjectsByType<Canvas>(FindObjectsSortMode.None).Where(c => c.renderMode == RenderMode.ScreenSpaceOverlay).ToArray();
        var cameras = overlays.Select(c => c.worldCamera).ToArray();
        var distances = overlays.Select(c => c.planeDistance).ToArray();
        var orders = overlays.Select(c => c.sortingOrder).ToArray();
        var layers = overlays.Select(c => c.sortingLayerID).ToArray();
        var sorted = overlays.OrderBy(c => c.sortingOrder).ToArray();
        int topLayer = SortingLayer.layers.OrderBy(l => l.value).Last().id;
        Texture2D pixels = null;
        try
        {
            camera.targetTexture = render;
            foreach (var c in overlays) { c.renderMode = RenderMode.ScreenSpaceCamera; c.worldCamera = camera; c.planeDistance = 1; c.sortingLayerID = topLayer; c.sortingOrder = 30000 + Array.IndexOf(sorted, c); }
            foreach (var graphic in FindObjectsByType<UnityEngine.UI.Graphic>(FindObjectsSortMode.None)) graphic.SetAllDirty();
            Canvas.ForceUpdateCanvases();
            foreach (var text in FindObjectsByType<TMPro.TMP_Text>(FindObjectsSortMode.None)) text.ForceMeshUpdate();
            Canvas.ForceUpdateCanvases(); camera.Render(); RenderTexture.active = render;
            pixels = new Texture2D(width, height, TextureFormat.RGB24, false);
            pixels.ReadPixels(new Rect(0, 0, width, height), 0, 0); pixels.Apply();
            File.WriteAllBytes(Path.Combine(folder, name), pixels.EncodeToPNG());
        }
        finally
        {
            for (int i = 0; i < overlays.Length; i++) { overlays[i].renderMode = RenderMode.ScreenSpaceOverlay; overlays[i].worldCamera = cameras[i]; overlays[i].planeDistance = distances[i]; overlays[i].sortingLayerID = layers[i]; overlays[i].sortingOrder = orders[i]; }
            camera.targetTexture = oldTarget; RenderTexture.active = oldActive;
            Destroy(render); if (pixels != null) Destroy(pixels);
        }
    }
    private void Complete(string role)
    {
        File.WriteAllText(Path.Combine(folder, role + "-result.json"), "{\"passed\":true,\"checks\":" + checks + ",\"pid\":" + System.Diagnostics.Process.GetCurrentProcess().Id + "}");
        Debug.Log("RUN_SAVE_PROCESS_VALIDATION_PASSED " + role + " checks=" + checks);
        Application.Quit(0);
    }
}
