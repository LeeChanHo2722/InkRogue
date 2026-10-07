using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using Object = UnityEngine.Object;

[InitializeOnLoad]
public static class TowerUIFlowValidation
{
    private const string Active = "TowerUIFlowValidation.Active";
    private static Stack<IEnumerator> routines;
    private static int checks, errors;
    private static Mouse mouse;
    private static Keyboard keyboard;
    private static Vector2 cursor;
    private static bool down, escape;
    private static InputSettings.BackgroundBehavior background;
    private static InputSettings.EditorInputBehaviorInPlayMode editorInput;
    private static TowerRunUI UI => TowerRunUI.Instance;
    private static FloorSelectionUI Routes => Object.FindAnyObjectByType<FloorSelectionUI>(FindObjectsInactive.Include);
    private static PlayerMovement Player => Object.FindAnyObjectByType<PlayerMovement>();
    private static FloorManager Floor => Object.FindAnyObjectByType<FloorManager>();
    static TowerUIFlowValidation() { EditorApplication.update += Tick; Application.logMessageReceived += Log; }
    public static void Start() { EditorSceneManager.OpenScene("Assets/Scenes/BootstrapScene.unity"); SessionState.SetBool(Active, true); EditorApplication.isPlaying = true; }
    private static void Log(string s, string stack, LogType type)
    { if (SessionState.GetBool(Active, false) && (type == LogType.Error || type == LogType.Exception) && !stack.Contains("UnityEditor.Search.SearchDatabase")) errors++; }
    private static void Check(bool ok, string message) { if (!ok) throw new Exception(message); checks++; Debug.Log("UI_FLOW_PASS " + message); }
    private static object Field(object o, string name) => o.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(o);
    private static void Invoke(object o, string name) => o.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public).Invoke(o, null);
    private static IEnumerator Wait(Func<bool> ready, string reason, double seconds = 60)
    { double end = EditorApplication.timeSinceStartup + seconds; while (!ready()) { if (EditorApplication.timeSinceStartup > end) throw new Exception("Timeout: " + reason); yield return null; } }
    private static IEnumerator Frames(int count = 3) { int start = Time.frameCount; yield return Wait(() => Time.frameCount > start + count, "game frames"); }
    private static bool Playing => Player != null && Player.enabled && Player.GetComponent<PlayerWeaponInputController>().InputEnabled && Floor != null && Floor.IsEncounterActive;
    private static bool Visible(Component c) => c.gameObject.activeInHierarchy && c.GetComponentsInParent<CanvasGroup>().All(g => g.alpha > .01f);
    private static UnityEngine.UI.Button Button(string prefix) => Object.FindObjectsByType<UnityEngine.UI.Button>(FindObjectsSortMode.None)
        .First(b => Visible(b) && b.GetComponentInChildren<TMP_Text>()?.text.StartsWith(prefix) == true);
    private static void Feed()
    {
        if (mouse == null || InputState.currentUpdateType != InputUpdateType.Dynamic) return;
        mouse.MakeCurrent(); keyboard.MakeCurrent();
        InputState.Change(mouse, new MouseState { position = cursor }.WithButton(MouseButton.Left, down));
        InputState.Change(keyboard, new KeyboardState(escape ? new[] { Key.Escape } : Array.Empty<Key>()));
    }
    private static IEnumerator Click(UnityEngine.UI.Button button)
    {
        yield return Frames(); Canvas.ForceUpdateCanvases();
        cursor = RectTransformUtility.WorldToScreenPoint(null, button.transform.position);
        var hits = new List<RaycastResult>(); var es = EventSystem.current;
        es.RaycastAll(new PointerEventData(es) { position = cursor }, hits);
        Check(hits.Count > 0 && hits[0].gameObject.GetComponentInParent<UnityEngine.UI.Button>() == button, "visible button receives raycast: " + button.GetComponentInChildren<TMP_Text>().text);
        down = true; yield return Frames(); down = false; yield return Frames();
    }
    private static IEnumerator Escape() { escape = true; yield return Frames(); escape = false; yield return Frames(); }
    private static Rect ScreenRect(RectTransform rect)
    {
        var points = new Vector3[4]; rect.GetWorldCorners(points);
        var canvas = rect.GetComponentInParent<Canvas>(); var camera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
        var p = points.Select(v => RectTransformUtility.WorldToScreenPoint(camera, v)).ToArray();
        return Rect.MinMaxRect(p.Min(v => v.x), p.Min(v => v.y), p.Max(v => v.x), p.Max(v => v.y));
    }
    private static void Layout(int width, int height)
    {
        foreach (var b in UI.GetComponentsInChildren<UnityEngine.UI.Button>().Where(Visible))
        {
            Rect r = ScreenRect((RectTransform)b.transform);
            Check(r.xMin >= -2 && r.yMin >= -2 && r.xMax <= width + 2 && r.yMax <= height + 2, "button stays inside " + width + "x" + height);
        }
        foreach (var t in UI.GetComponentsInChildren<TMP_Text>().Where(Visible))
            Check(!t.isTextOverflowing, "run text fits: " + t.text.Substring(0, Mathf.Min(22, t.text.Length)));
        var boss = Object.FindAnyObjectByType<BossHealthUI>();
        if (boss != null && boss.IsVisible)
        {
            var status = (TMP_Text)Field(UI, "status");
            Check(!ScreenRect(status.rectTransform).Overlaps(ScreenRect(boss.bossNameText.rectTransform)), "boss name and run summary do not overlap at " + width + "x" + height);
            Check(!ScreenRect(status.rectTransform).Overlaps(ScreenRect(boss.phaseText.rectTransform)), "boss phase and run summary do not overlap");
            Rect bar = ScreenRect((RectTransform)boss.healthFill.transform.parent);
            Rect lives = ScreenRect(Object.FindAnyObjectByType<PlayerLifeManager>().GetComponent<RectTransform>());
            Check(!bar.Overlaps(lives), "boss health bar and life icons do not overlap");
        }
        var intro = Object.FindAnyObjectByType<BossIntroUI>();
        if (intro != null && intro.canvasGroup.alpha > .9f)
        {
            Check(!intro.bossTitleText.isTextOverflowing, "boss intro title and hint fit");
            Check(!ScreenRect(intro.bossTitleText.rectTransform).Overlaps(ScreenRect(intro.warningText.rectTransform)), "boss intro warning and title do not overlap");
        }
    }
    private static void Capture(string name, bool aspects = false)
    {
        FloorRouteValidation.Capture(name + "-1280x720", 1280, 720, Layout);
        if (aspects) { FloorRouteValidation.Capture(name + "-1024x768", 1024, 768, Layout); FloorRouteValidation.Capture(name + "-2560x1080", 2560, 1080, Layout); }
    }
    private static IEnumerator ChooseRoute(int index)
    {
        yield return Wait(() => Routes != null && Routes.IsVisible, "route UI");
        var card = ((Array)Field(Routes, "cards")).GetValue(index);
        yield return Click((UnityEngine.UI.Button)card.GetType().GetField("button").GetValue(card));
        Check(Routes.HighlightedIndex == index, "mouse previews route");
        Capture("route");
        yield return Click((UnityEngine.UI.Button)Field(Routes, "confirm"));
        yield return Wait(() => Playing && !Routes.IsVisible, "next floor");
    }
    private static IEnumerator Run()
    {
        yield return Wait(() => UI != null, "menu");
        mouse = InputSystem.AddDevice<Mouse>(); keyboard = InputSystem.AddDevice<Keyboard>();
        background = InputSystem.settings.backgroundBehavior; editorInput = InputSystem.settings.editorInputBehaviorInPlayMode;
        InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
        InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        InputSystem.onAfterUpdate += Feed;
        Capture("menu", true); yield return Click(Button("20층 모드"));
        yield return Wait(() => Playing, "campaign start");
        Check(UI.Run.CurrentMode == RunMode.TwentyFloor, "menu mouse input starts campaign"); Capture("combat");
        yield return Escape(); Check(Time.timeScale == 0, "Escape pauses combat"); Capture("pause", true);
        yield return Click(Button("청소 계속하기")); Check(Time.timeScale > 0, "pause mouse input resumes combat");
        Invoke(Floor, "FloorClear"); yield return Wait(() => UI.ChoosingPower, "first power choices"); Capture("three-powers", true);
        int total = TowerPowers.Instance.PlayerLevels + TowerPowers.Instance.RobotLevels;
        yield return Click(UI.GetComponentsInChildren<UnityEngine.UI.Button>().First(b => Visible(b)));
        Check(TowerPowers.Instance.PlayerLevels + TowerPowers.Instance.RobotLevels == total + 1, "mouse power selection awards exactly one level");
        yield return ChooseRoute(2); Invoke(Floor, "FloorClear"); yield return Wait(() => UI.ChoosingPower, "hard reward");
        Check(UI.PowerChoices.Length == 4, "hard reward has four choices"); Capture("four-powers", true);
        yield return Click(UI.GetComponentsInChildren<UnityEngine.UI.Button>().First(b => Visible(b)));
        yield return ChooseRoute(0);
        while (UI.Run.CurrentFloor < 20) UI.Run.AdvanceFloor(); Invoke(Floor, "FloorClear");
        var intro = Object.FindAnyObjectByType<BossIntroUI>(FindObjectsInactive.Include);
        yield return Wait(() => intro.canvasGroup.alpha > .95f, "boss intro"); Capture("boss-intro", true);
        string native = System.IO.Path.Combine(Environment.GetEnvironmentVariable("FLOOR_UI_CAPTURE_DIR"), "boss-intro-native.png");
        ScreenCapture.CaptureScreenshot(native); yield return Frames(5);
        Debug.Log("UI_NATIVE_CAPTURE requested=" + native + " written=" + System.IO.File.Exists(native));
        Debug.Log("INTRO_LAYOUT title=" + intro.bossTitleText.rectTransform.rect + " font=" + intro.bossTitleText.fontSize + " overflow=" + intro.bossTitleText.isTextOverflowing);
        var manager = Object.FindAnyObjectByType<BossBattleManager>();
        yield return Wait(() => manager.CurrentBossHealth != null && !manager.CurrentBossHealth.IsInvulnerable, "boss combat"); yield return Frames();
        var boss = manager.CurrentBossHealth; var attack = boss.GetComponent<BossAttackController>(); var hud = Object.FindAnyObjectByType<BossHealthUI>();
        Object.FindAnyObjectByType<TowerRobotCompanion>().enabled = false;
        yield return Wait(() => hud.IsVisible, "visible boss HUD"); yield return Frames();
        Capture("boss-hud", true);
        foreach (TowerBossPattern pattern in Enum.GetValues(typeof(TowerBossPattern)))
        {
            attack.StopCombat(); attack.towerPattern = pattern;
            var definition = TowerBossDefinition.CreateDefault((int)pattern); hud.bossName = definition.displayName; Object.Destroy(definition);
            typeof(BossHealth).GetField("currentPhase", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(boss, 1);
            boss.ConfigureMaxHealth(1000); boss.SetInvulnerable(false); hud.Bind(boss); attack.BeginCombat();
            foreach (int phase in new[] { 1, 2, 3 })
            {
                if (phase > 1)
                {
                    var pending = TowerBossZone.Create(attack.transform, Player.transform.position, Player.transform.position, 1, 10, 1);
                    TowerPowers.Damage(boss.gameObject, phase == 2 ? 400 : 350, TowerDamageOrigin.Robot);
                    Check(boss.CurrentPhase == phase && boss.IsInvulnerable, pattern + " phase " + phase + " starts real transition");
                    Check(pending == null || !pending.gameObject.activeSelf, "phase transition cancels old warning");
                    yield return Wait(() => !boss.IsInvulnerable && attack.CurrentPatternAction != "Stopped", "phase resume");
                }
                yield return Wait(() => attack.GetComponentsInChildren<TowerBossZone>().Any(z => !z.Activated) || attack.GetComponentsInChildren<LineRenderer>().Any(l => l.enabled), "phase warning");
                Check(attack.CurrentPatternAction != "Stopped", pattern + " phase " + phase + " resumes pattern and warning");
                Capture("phase-" + pattern + "-" + phase); yield return Frames();
            }
        }
        TowerPowers.Damage(boss.gameObject, 1000000, TowerDamageOrigin.Robot);
        yield return Wait(() => UI.HasFinished, "victory"); Capture("victory", true);
        yield return Click(Button("다시 도전")); yield return Wait(() => UI != null && !UI.HasStarted, "restart menu");
        yield return Click(Button("무한 모드")); yield return Wait(() => Playing, "endless start");
        Check(UI.Run.CurrentMode == RunMode.Endless, "menu mouse input starts endless");
        var lives = Object.FindAnyObjectByType<PlayerLifeManager>(); while (lives.CurrentLives > 0) lives.TryConsumeLife();
        yield return Wait(() => UI.HasFinished, "defeat"); Capture("defeat", true);
        yield return Click(Button("다시 도전")); yield return Wait(() => UI != null && !UI.HasStarted, "defeat restart");
        Check(!UI.Run.IsInitialized && Object.FindAnyObjectByType<PlayerLifeManager>().CurrentLives == 3, "both result screens restart to clean menu");
        Check(errors == 0, "UI flow and every boss phase have no gameplay errors");
    }
    private static void Tick()
    {
        if (!SessionState.GetBool(Active, false) || !EditorApplication.isPlaying) return;
        try
        {
            EditorApplication.isPaused = false;
            if (Player != null && UI != null && !UI.HasFinished) Player.GetComponent<PlayerShield>().ResetAfterRespawn(1200);
            if (routines == null) { routines = new Stack<IEnumerator>(); routines.Push(Run()); }
            for (int i = 0; i < 100 && routines.Count > 0; i++)
            { var top = routines.Peek(); if (!top.MoveNext()) { routines.Pop(); continue; } if (top.Current is IEnumerator nested) { routines.Push(nested); continue; } return; }
            if (routines.Count == 0) { Debug.Log("TOWER_UI_FLOW_PASSED checks=" + checks); Finish(0); }
        }
        catch (Exception ex) { Debug.LogError("TOWER_UI_FLOW_FAILED " + ex); Finish(1); }
    }
    private static void Finish(int code)
    {
        InputSystem.onAfterUpdate -= Feed;
        if (mouse != null) { InputSystem.RemoveDevice(mouse); InputSystem.RemoveDevice(keyboard); InputSystem.settings.backgroundBehavior = background; InputSystem.settings.editorInputBehaviorInPlayMode = editorInput; }
        SessionState.SetBool(Active, false); EditorApplication.Exit(code);
    }
}
