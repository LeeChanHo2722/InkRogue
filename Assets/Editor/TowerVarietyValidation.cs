using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

[InitializeOnLoad]
public static class TowerVarietyValidation
{
    private const string Active = "TowerVarietyValidation.Active";
    private static int phase, pattern, cycles, checks, errors;
    private static double deadline;
    private static Vector3 bossOrigin;
    private static bool moved, warned, fired;
    static TowerVarietyValidation() { EditorApplication.update += Tick; Application.logMessageReceived += Log; }
    public static void Start() { EditorSceneManager.OpenScene("Assets/Scenes/BootstrapScene.unity"); SessionState.SetBool(Active, true); EditorApplication.isPlaying = true; }
    private static void Log(string s, string stack, LogType type)
    { if (SessionState.GetBool(Active, false) && (type == LogType.Error || type == LogType.Exception) && !stack.Contains("UnityEditor.Search.SearchDatabase")) errors++; }
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); checks++; Debug.Log("VARIETY_PASS " + message); }
    private static object Invoke(object o, string name, params object[] args) => o.GetType().GetMethod(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).Invoke(o, args);
    private static void Next(int value) { phase = value; deadline = EditorApplication.timeSinceStartup + 90; }
    private static EnemyHealth Target(Vector2 position)
    { var go = new GameObject("Variety target"); go.transform.position = position; go.AddComponent<CircleCollider2D>().radius = .2f; return go.AddComponent<EnemyHealth>(); }
    private static void Powers()
    {
        var powers = TowerPowers.Instance; var player = powers.Player;
        var shield = player.GetComponent<PlayerShield>(); var robot = Object.FindAnyObjectByType<TowerRobotCompanion>();
        robot.enabled = false;
        Vector3 original = player.position; player.position = new Vector3(100, 100);
        var target = Target(new Vector2(101, 100)); Physics2D.SyncTransforms();
        Check(TowerPowerCatalog.All.Length == 20 && TowerPowerCatalog.All.Select(p => p.id).Distinct().Count() == 20, "20 unique manual powers");
        powers.Add(TowerPowerId.Chain); powers.Add(TowerPowerId.DiveWake); powers.Add(TowerPowerId.Backwash);
        float hp = target.CurrentHealthExact; double damage = powers.PlayerDamageDealt;
        Invoke(powers, "DiveWake", 1f); Invoke(powers, "DiveWake", 1f);
        Check(Mathf.Approximately(hp - target.CurrentHealthExact, 2) && powers.PlayerDamageDealt - damage == 2, "surface wake deals one owned hit and respects cooldown");
        Check((int)typeof(TowerPowers).GetField("hits", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(powers) == 0, "secondary player effects cannot recursively count direct hits");
        hp = target.CurrentHealthExact; Vector3 before = target.transform.position;
        shield.ResetAfterRespawn(0); shield.TakeDamage(shield.MaxShield + 1);
        Check(Mathf.Approximately(hp - target.CurrentHealthExact, 3) && Vector3.Distance(before, target.transform.position) > .1f, "actual shield break triggers backwash and knockback");
        Invoke(powers, "Backwash"); Check(Mathf.Approximately(hp - target.CurrentHealthExact, 3), "backwash cannot repeat during cooldown");
        Object.DestroyImmediate(target.gameObject); player.position = original; shield.ResetAfterRespawn(1200);
        robot.transform.position = original + Vector3.right;
        powers.Add(TowerPowerId.CleaningTrail); powers.Add(TowerPowerId.ReserveTank); powers.Add(TowerPowerId.Intercept);
        var ink = player.GetComponent<PlayerInkResource>(); ink.SpendInk(50); float inkBefore = ink.CurrentInk;
        var shot = new GameObject("Hostile interception fixture"); shot.transform.position = robot.transform.position + Vector3.up * .5f;
        shot.AddComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic; shot.AddComponent<CircleCollider2D>().isTrigger = true; shot.AddComponent<BossProjectile>();
        var friendly = new GameObject("Friendly collider fixture"); friendly.transform.position = shot.transform.position; friendly.AddComponent<CircleCollider2D>().isTrigger = true;
        Physics2D.SyncTransforms(); Invoke(robot, "SupportPowers");
        Check(!shot.activeSelf && friendly.activeSelf, "filter removes hostile projectile without erasing unrelated friendly object");
        Check(Mathf.Approximately(ink.CurrentInk - inkBefore, 3), "reserve tank transfers ink near the player");
        Invoke(robot, "SupportPowers"); Check(Mathf.Approximately(ink.CurrentInk - inkBefore, 3), "reserve tank cooldown prevents repeated transfer");
        Check(InkMap.Instance.GetDominantInkTeam(robot.transform.position, .1f) == InkTeam.Player, "moving robot trail creates rechargeable friendly area");
        Check(Mathf.Approximately(powers.PlayerDamageMultiplier, 1) && powers.Level(TowerPowerId.RobotCore) == 0, "new utility choices cannot inflate base damage or mix owners");
        Object.DestroyImmediate(friendly); if (shot != null) Object.DestroyImmediate(shot);
        foreach (var p in TowerPowerCatalog.All) if (p.id != TowerPowerId.Intercept) while (powers.Add(p.id)) { }
        Check(powers.Offers().Length == 1 && powers.Offers()[0].id == TowerPowerId.Intercept, "maxed new and old powers are excluded from offers");
        var ui = TowerRunUI.Instance; int level = powers.Level(TowerPowerId.Intercept), callbacks = 0;
        ui.ShowPowerChoices(() => callbacks++); ui.ChoosePower(0); ui.ChoosePower(0);
        Check(powers.Level(TowerPowerId.Intercept) == level + 1 && callbacks == 1, "new power uses manual selection with one reward despite duplicate input");
        Time.timeScale = 1;
    }
    private static void Tick()
    {
        if (!SessionState.GetBool(Active, false) || !EditorApplication.isPlaying) return;
        try
        {
            EditorApplication.isPaused = false;
            if (deadline == 0) deadline = EditorApplication.timeSinceStartup + 90;
            if (EditorApplication.timeSinceStartup > deadline) throw new Exception("Timeout phase=" + phase + " pattern=" + pattern);
            var ui = TowerRunUI.Instance; if (ui == null) return;
            var player = Object.FindAnyObjectByType<PlayerMovement>(); var floor = Object.FindAnyObjectByType<FloorManager>();
            if (player != null) player.GetComponent<PlayerShield>().ResetAfterRespawn(1200);
            if (phase == 0) { ui.StartRun(RunMode.TwentyFloor); Next(1); return; }
            if (phase == 1)
            {
                if (floor == null || !floor.IsEncounterActive || player == null || !player.enabled || !player.GetComponent<PlayerWeaponInputController>().InputEnabled) return;
                Powers(); while (ui.Run.CurrentFloor < 5) ui.Run.AdvanceFloor(); Invoke(floor, "FloorClear"); Next(2); return;
            }
            var boss = Object.FindAnyObjectByType<BossBattleManager>()?.CurrentBossHealth;
            if (boss == null || boss.IsInvulnerable) return;
            var attack = boss.GetComponent<BossAttackController>();
            if (phase == 2) { attack.StopCombat(); bossOrigin = boss.transform.position; Next(3); }
            if (phase == 3)
            {
                foreach (var shot in Object.FindObjectsByType<BossProjectile>(FindObjectsSortMode.None)) Object.Destroy(shot.gameObject);
                boss.transform.position = bossOrigin; player.transform.position = bossOrigin + new Vector3(3, 2);
                attack.towerPattern = (TowerBossPattern)pattern; cycles = attack.CompletedPatternCycles;
                var hud = Object.FindAnyObjectByType<BossHealthUI>();
                var definition = TowerBossDefinition.CreateDefault(pattern);
                if (hud != null && hud.bossNameText != null) hud.bossNameText.text = definition.displayName;
                Object.Destroy(definition);
                moved = warned = fired = false; attack.BeginCombat(); Next(4); return;
            }
            if (phase == 4)
            {
                moved |= Vector3.Distance(boss.transform.position, bossOrigin) > .2f;
                bool visibleWarning = attack.GetComponentsInChildren<TowerBossZone>().Any(z => !z.Activated)
                    || attack.GetComponentsInChildren<LineRenderer>().Any(l => l.enabled);
                if (!warned && visibleWarning) FloorRouteValidation.Capture("warning-" + attack.towerPattern, 1280, 720);
                warned |= visibleWarning;
                fired |= Object.FindObjectsByType<BossProjectile>(FindObjectsSortMode.None).Length > 0;
                if (attack.CompletedPatternCycles <= cycles) return;
                Check(warned, attack.towerPattern + " shows a visible warning before attack");
                Check(pattern == 3 ? !moved : moved, attack.towerPattern + " has its expected mobile/stationary behavior");
                if (pattern == 1 || pattern == 3) Check(fired, attack.towerPattern + " emits projectiles");
                FloorRouteValidation.Capture("variety-" + attack.towerPattern, 1280, 720);
                attack.StopCombat(); TowerBossZone.Create(attack.transform, player.transform.position, player.transform.position, 1, 5, 1);
                attack.StopCombat(); Check(attack.GetComponentsInChildren<TowerBossZone>().Length == 0, attack.towerPattern + " stop cancels pending zones immediately");
                pattern++; if (pattern < 4) { Next(3); return; }
                Check(errors == 0, "no gameplay errors during variety smoke");
                Debug.Log("TOWER_VARIETY_VALIDATION_PASSED checks=" + checks); Finish(0);
            }
        }
        catch (Exception ex) { Debug.LogError("TOWER_VARIETY_VALIDATION_FAILED " + ex); Finish(1); }
    }
    private static void Finish(int code) { SessionState.SetBool(Active, false); EditorApplication.Exit(code); }
}
