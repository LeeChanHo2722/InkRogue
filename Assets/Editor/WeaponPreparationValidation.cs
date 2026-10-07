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
public static class WeaponPreparationValidation
{
    private const string Active = "WeaponPreparationValidation.Active";
    private static Stack<IEnumerator> routines;
    private static Mouse mouse;
    private static Keyboard keyboard;
    private static Vector2 cursor;
    private static Key[] keys = Array.Empty<Key>();
    private static bool down;
    private static int checks, errors;
    private static InputSettings.BackgroundBehavior background;
    private static InputSettings.EditorInputBehaviorInPlayMode editorInput;
    private static TowerRunUI UI => TowerRunUI.Instance;
    private static FloorSelectionUI Routes => Object.FindAnyObjectByType<FloorSelectionUI>();
    private static PlayerWeaponInputController Input => Object.FindAnyObjectByType<PlayerWeaponInputController>();
    private static object Field(object o, string name) => o.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance).GetValue(o);
    private static object Invoke(object o, string name, params object[] args) => o.GetType().GetMethod(name, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance).Invoke(o, args);
    private static UnityEngine.UI.Button Control(string field) => (UnityEngine.UI.Button)Field(Routes, field);
    private static UnityEngine.UI.Button Slot(int index) => ((UnityEngine.UI.Button[])Field(Routes, "slots"))[index];
    private static UnityEngine.UI.Button Choice(WeaponType type) => ((List<UnityEngine.UI.Button>)Field(Routes, "inventory")).Single(b => b.name == "Choose" + type);
    static WeaponPreparationValidation() { EditorApplication.update += Tick; Application.logMessageReceived += Log; }
    public static void Start() { EditorSceneManager.OpenScene("Assets/Scenes/BootstrapScene.unity"); SessionState.SetBool(Active, true); EditorApplication.isPlaying = true; }
    private static void Log(string text, string stack, LogType type)
    { if (SessionState.GetBool(Active, false) && (type == LogType.Error || type == LogType.Exception) && !stack.Contains("UnityEditor.Search.SearchDatabase")) errors++; }
    private static void Check(bool ok, string message) { if (!ok) throw new Exception(message); checks++; Debug.Log("PREPARATION_PASS " + message); }
    private static IEnumerator Wait(Func<bool> ready, string reason, double seconds = 50)
    { double end = EditorApplication.timeSinceStartup + seconds; while (!ready()) { if (EditorApplication.timeSinceStartup > end) throw new Exception("Timeout: " + reason); yield return null; } }
    private static IEnumerator Frames(int n = 3) { int start = Time.frameCount; yield return Wait(() => Time.frameCount > start + n, "frames"); }
    private static IEnumerator Delay(double seconds) { double end = EditorApplication.timeSinceStartup + seconds; yield return Wait(() => EditorApplication.timeSinceStartup > end, "delay"); }
    private static void Feed()
    {
        if (mouse == null || InputState.currentUpdateType != InputUpdateType.Dynamic) return;
        mouse.MakeCurrent(); keyboard.MakeCurrent();
        InputState.Change(mouse, new MouseState { position = cursor }.WithButton(MouseButton.Left, down));
        InputState.Change(keyboard, new KeyboardState(keys));
    }
    private static IEnumerator Click(UnityEngine.UI.Button button)
    {
        yield return Frames(); Canvas.ForceUpdateCanvases();
        cursor = RectTransformUtility.WorldToScreenPoint(null, button.transform.position);
        var hits = new List<RaycastResult>(); var system = EventSystem.current;
        system.RaycastAll(new PointerEventData(system) { position = cursor }, hits);
        Check(button.interactable && hits.Count > 0 && hits[0].gameObject.GetComponentInParent<UnityEngine.UI.Button>() == button, "actual pointer reaches " + button.name);
        down = true; yield return Frames(); down = false; yield return Frames();
    }
    private static string Saved() => string.Join("|", UI.Run.LeftFloorLoadout.Concat(UI.Run.RightFloorLoadout).Select(w => w == null ? "empty" : w.WeaponId));
    private static string Draft() => string.Join("|", ((WeaponDefinition[])Field(Routes, "draftLeft")).Concat((WeaponDefinition[])Field(Routes, "draftRight")).Select(w => w == null ? "empty" : w.WeaponId));
    private static Rect ScreenRect(RectTransform rect)
    {
        var points = new Vector3[4]; rect.GetWorldCorners(points); var canvas = rect.GetComponentInParent<Canvas>();
        var camera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
        var p = points.Select(v => RectTransformUtility.WorldToScreenPoint(camera, v)).ToArray();
        return Rect.MinMaxRect(p.Min(v => v.x), p.Min(v => v.y), p.Max(v => v.x), p.Max(v => v.y));
    }
    private static void Layout(int width, int height)
    {
        var root = (GameObject)Field(Routes, "panelRoot");
        foreach (var text in root.GetComponentsInChildren<TMP_Text>())
        {
            Check(!text.isTextOverflowing, "text fits: " + text.text);
            Check(!text.text.Contains("12시") && !text.text.Contains("4시") && !text.text.Contains("8시"), "no clock direction instructions");
        }
        var buttons = root.GetComponentsInChildren<UnityEngine.UI.Button>();
        for (int i = 0; i < buttons.Length; i++)
        {
            Rect rect = ScreenRect((RectTransform)buttons[i].transform);
            Check(rect.xMin >= -1 && rect.yMin >= -1 && rect.xMax <= width + 1 && rect.yMax <= height + 1, "button inside " + width + "x" + height);
            for (int j = i + 1; j < buttons.Length; j++) Check(!rect.Overlaps(ScreenRect((RectTransform)buttons[j].transform)), "click areas do not overlap");
        }
        if (!Routes.IsPreparing) return;
        for (int hand = 0; hand < 2; hand++)
        {
            Vector2 top = Slot(hand * 3).transform.localPosition;
            Vector2 right = Slot(hand * 3 + 1).transform.localPosition;
            Vector2 left = Slot(hand * 3 + 2).transform.localPosition;
            Check(top.y > left.y && top.y > right.y && right.x > top.x && left.x < top.x, "loadout matches combat wheel direction order");
        }
    }
    private static void Capture(string name)
    {
        foreach (var size in new[] { new Vector2Int(1280, 720), new Vector2Int(1024, 768), new Vector2Int(2560, 1080) })
            FloorRouteValidation.Capture(name + "-" + size.x + "x" + size.y, size.x, size.y, Layout);
    }
    private static IEnumerator Compose()
    {
        for (int i = 0; i < 6; i++) { yield return Click(Slot(i)); if (Control("clearSlot").interactable) yield return Click(Control("clearSlot")); }
        var types = new[] { WeaponType.Blade, WeaponType.Breach, WeaponType.SplashBomb, WeaponType.MachineGun, WeaponType.Shooter, WeaponType.Breach };
        for (int i = 0; i < 6; i++) { yield return Click(Slot(i)); yield return Click(Choice(types[i])); }
    }
    private static IEnumerator Run()
    {
        yield return Wait(() => UI != null, "menu");
        mouse = InputSystem.AddDevice<Mouse>(); keyboard = InputSystem.AddDevice<Keyboard>();
        background = InputSystem.settings.backgroundBehavior; editorInput = InputSystem.settings.editorInputBehaviorInPlayMode;
        InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
        InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        InputSystem.onAfterUpdate += Feed;
        UI.StartRun(RunMode.TwentyFloor);
        yield return Wait(() => Input != null && Input.InputEnabled && Object.FindAnyObjectByType<FloorManager>()?.IsEncounterActive == true, "combat");
        var weapons = UI.Run.RunInventory;
        Check(weapons.Count == 5 && weapons.All(w => w.Icon != null) && weapons.Select(w => w.Icon).Distinct().Count() == 5, "all five actual weapons have distinct sprite assets");
        Check(weapons.Select(w => System.IO.File.ReadAllBytes(AssetDatabase.GetAssetPath(w.Icon))).Select(b => Convert.ToBase64String(b)).Distinct().Count() == 5, "all five icon pixels differ");
        Invoke(Object.FindAnyObjectByType<FloorManager>(), "FloorClear"); yield return Wait(() => UI.ChoosingPower, "reward"); UI.ChoosePower(0);
        yield return Wait(() => Routes != null && Routes.IsVisible, "routes");
        var candidates = UI.Run.FloorCandidates.ToArray();
        var card = ((Array)Field(Routes, "cards")).GetValue(1);
        yield return Click((UnityEngine.UI.Button)card.GetType().GetField("button").GetValue(card));
        Capture("route-readable"); string original = Saved();
        yield return Click(Control("prepare")); Check(Routes.IsPreparing, "entry opens loadout");
        Check(((TMP_Text)Field(Routes, "subheading")).text.Contains(candidates[1].MapName), "selected route stays visible while editing");
        string draft = Draft(); yield return Click(Slot(4)); Check(Draft() == draft && Saved() == original, "slot focus changes no equipment");
        if (Control("clearSlot").interactable) yield return Click(Control("clearSlot"));
        Check(Saved() == original, "draft does not leak into saved loadout");
        keys = new[] { Key.Escape }; yield return Frames(); keys = Array.Empty<Key>(); yield return Frames();
        Check(!Routes.IsPreparing && Saved() == original && Routes.HighlightedIndex == 1, "Escape discards draft and preserves route");
        yield return Click(Control("prepare")); yield return Click(Control("clearSlot")); yield return Click(Control("cancel"));
        Check(!Routes.IsPreparing && Saved() == original, "cancel button discards changes");
        yield return Click(Control("prepare")); yield return Compose();
        Check(Saved() == original, "full staged composition stays uncommitted");
        yield return Click(Slot(0)); Check(!Choice(WeaponType.Breach).interactable, "same-hand duplicate is visibly unavailable");
        var breach = weapons.Single(w => w.Type == WeaponType.Breach);
        Invoke(Routes, "PickWeapon", breach); Check(!(bool)Invoke(Routes, "AssignSlot", 0), "same-hand duplicate cannot overwrite selected slot");
        yield return Click(Slot(5)); Check(Choice(WeaponType.Breach).interactable, "same weapon remains valid in opposite hand");
        Capture("weapon-preparation"); string expected = Draft();
        yield return Click(Control("confirm")); Check(!Routes.IsPreparing && Saved() == expected, "apply button commits exactly the visible composition");
        Invoke(Routes, "ApplyPreparation"); Check(Saved() == expected && !Routes.IsPreparing, "repeated apply is inert");
        Check(UI.Run.FloorCandidates.SequenceEqual(candidates) && Routes.HighlightedIndex == 1, "editing does not regenerate route candidates or selection");
        Capture("route-applied"); yield return Delay(.4); yield return Click(Control("confirm"));
        yield return Wait(() => !Routes.IsVisible && Input != null && Input.InputEnabled && Object.FindAnyObjectByType<FloorManager>()?.IsEncounterActive == true, "departed");
        Check(Input.LeftCombatLoadout.SequenceEqual(UI.Run.LeftFloorLoadout) && Input.RightCombatLoadout.SequenceEqual(UI.Run.RightFloorLoadout), "departure copies exact left/right slot arrays into combat");
        Invoke(Input, "OnApplicationFocus", true);
        foreach (var key in new[] { Key.E, Key.Q })
        {
            keys = new[] { key }; yield return Wait(() => Input.IsWeaponWheelOpen, "wheel key");
            var side = key == Key.E ? WeaponSlotSide.Left : WeaponSlotSide.Right;
            Check(Input.ActiveWeaponWheelSide == side, key + " opens correct hand");
            var loadout = side == WeaponSlotSide.Left ? Input.LeftCombatLoadout : Input.RightCombatLoadout;
            var wheel = Object.FindAnyObjectByType<WeaponWheelUI>(); yield return Frames();
            var visuals = (Array)Field(wheel, "slots");
            for (int i = 0; i < 3; i++)
            {
                var visual = visuals.GetValue(i);
                var icon = (UnityEngine.UI.Image)visual.GetType().GetField("icon").GetValue(visual);
                Check(icon.sprite == loadout[i].Icon, "combat wheel icon matches assigned slot " + side + i);
            }
            int active = side == WeaponSlotSide.Left ? Input.ActiveLeftLoadoutIndex : Input.ActiveRightLoadoutIndex;
            int slot = (active + 1) % 3;
            Vector2[] directions = { Vector2.up, new(.8660254f, -.5f), new(-.8660254f, -.5f) };
            cursor = Input.WeaponWheelOrigin + directions[slot] * 180;
            yield return Wait(() => Input.HighlightedWeaponSlotIndex == slot, "wheel pointer direction");
            FloorRouteValidation.Capture("combat-wheel-" + key, 1280, 720);
            keys = Array.Empty<Key>(); yield return Wait(() => !Input.IsWeaponWheelOpen, "wheel release");
            Check((side == WeaponSlotSide.Left ? Input.ActiveLeftLoadoutIndex : Input.ActiveRightLoadoutIndex) == slot, "wheel release equips matching visual slot");
        }
        Check(errors == 0, "no gameplay errors");
    }
    private static void Tick()
    {
        if (!SessionState.GetBool(Active, false) || !EditorApplication.isPlaying) return;
        try
        {
            EditorApplication.isPaused = false;
            var shield = Object.FindAnyObjectByType<PlayerShield>(); if (shield != null) shield.ResetAfterRespawn(1200);
            if (routines == null) { routines = new Stack<IEnumerator>(); routines.Push(Run()); }
            for (int i = 0; i < 100 && routines.Count > 0; i++)
            { var top = routines.Peek(); if (!top.MoveNext()) { routines.Pop(); continue; } if (top.Current is IEnumerator nested) { routines.Push(nested); continue; } return; }
            if (routines.Count == 0) { Debug.Log("WEAPON_PREPARATION_PASSED checks=" + checks); Finish(0); }
        }
        catch (Exception ex) { Debug.LogError("WEAPON_PREPARATION_FAILED checks=" + checks + " " + ex); Finish(1); }
    }
    private static void Finish(int code)
    {
        InputSystem.onAfterUpdate -= Feed; PlayerWeaponInputController.CancelActiveWeaponWheel();
        if (mouse != null) { InputSystem.RemoveDevice(mouse); InputSystem.RemoveDevice(keyboard); InputSystem.settings.backgroundBehavior = background; InputSystem.settings.editorInputBehaviorInPlayMode = editorInput; }
        SessionState.SetBool(Active, false); EditorApplication.Exit(code);
    }
}
