using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class TowerValidation
{
    private static int checks;
    private static void Check(bool condition, string description)
    {
        if (!condition) throw new Exception("FAIL: " + description);
        checks++;
        Debug.Log("PASS: " + description);
    }

    public static void Run()
    {
        checks = 0;
        System.IO.Directory.CreateDirectory("Assets/Resources/TowerCleaning/Bosses");
        for (int i=0;i<4;i++)
        {
            string path="Assets/Resources/TowerCleaning/Bosses/Boss_"+i+".asset";
            if (AssetDatabase.LoadAssetAtPath<TowerBossDefinition>(path)==null)
                AssetDatabase.CreateAsset(TowerBossDefinition.CreateDefault(i),path);
        }
        AssetDatabase.SaveAssets();
        Check(TowerBossDefinition.IndexFor(RunMode.TwentyFloor,20)==3,"campaign final boss is fixed");
        Check(Enumerable.Range(1,4).Select(i=>TowerBossDefinition.IndexFor(RunMode.Endless,i*10)).Distinct().Count()==4,"four endless bosses rotate");
        Check(TowerBossDefinition.IndexFor(RunMode.Endless,50)==0,"endless boss roster repeats safely");
        Check(TowerEnemyScaling.Health(101)>TowerEnemyScaling.Health(21),"endless health continues growing beyond 20");
        Check(TowerEnemyScaling.Damage(101)>TowerEnemyScaling.Damage(21),"endless damage continues growing beyond 20");
        Check(TowerEnemyScaling.Speed(101)>TowerEnemyScaling.Speed(21) && TowerEnemyScaling.Speed(10000)<=1.61f,"movement grows with dodgeable upper bound");
        var obj = new GameObject("Run regression test");
        var run = obj.AddComponent<RunManager>();
        var weapon = ScriptableObject.CreateInstance<WeaponDefinition>();
        run.InitializeRun(RunMode.TwentyFloor, new[] { weapon });
        Check(run.CurrentFloor == 1 && !run.IsLastFloor, "20-floor run begins at floor 1");
        for (int i = 1; i < 20; i++) run.AdvanceFloor();
        Check(run.IsLastFloor && run.IsBossFloor, "20th floor is final boss floor");
        run.InitializeRun(RunMode.Endless, new[] { weapon });
        Check(run.CurrentFloor == 1 && run.ElapsedSeconds == 0, "new run resets floor and timer");
        for (int i = 1; i < 101; i++) run.AdvanceFloor();
        Check(!run.IsLastFloor, "endless does not finish above 20 floors");
        Check(new[] {5,10,15,20}.All(f => RunManager.IsBossFloorFor(RunMode.TwentyFloor,f)), "all campaign boss floors");
        Check(!RunManager.IsBossFloorFor(RunMode.Endless,5) && RunManager.IsBossFloorFor(RunMode.Endless,30), "endless boss interval is ten floors");
        run.GrantFloorReward(); run.GrantFloorReward();
        Check(run.RewardedFloors==1,"floor reward cannot be granted twice");
        var floors = AssetDatabase.FindAssets("t:FloorDefinition").Select(g => UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<FloorDefinition>(AssetDatabase.GUIDToAssetPath(g)))).Take(3).ToArray();
        Check(run.TryPrepareFloorCandidates(floors), "three floor candidates generated");
        Check(run.FloorCandidates.Select(c => c.Difficulty).Distinct().Count() == 3, "floor choices contain three distinct difficulties");
        try { run.InitializeRun(RunMode.TwentyFloor, null); Check(false,"null roster rejected"); }
        catch (ArgumentNullException) { Check(run.CurrentMode == RunMode.Endless && run.CurrentFloor == 101, "invalid initialization preserves run"); }
        var director = new EliminationSpawnDirector();
        var plan = new EncounterWavePlan { maxAlive = 2, refillDelay = 0, spawnBag = new WaveEnemyType[8] };
        Check(director.TryBeginWave(plan, 2, out _), "spawn director initializes");
        for (int i=0;i<4;i++) Check(director.TryTakeNext(out _) && director.NotifySpawned(), "floor bonus permits spawn " + (i+1));
        Check(!director.TryTakeNext(out _), "scaled alive cap still enforced");
        director.NotifyDefeated();
        Check(director.TryTakeNext(out _) && director.NotifySpawned(), "defeated enemy frees capacity");
        UnityEngine.Object.DestroyImmediate(obj);
        UnityEngine.Object.DestroyImmediate(weapon);
        foreach (var floor in floors) UnityEngine.Object.DestroyImmediate(floor);
        EditorSceneManager.OpenScene("Assets/Scenes/BootstrapScene.unity", OpenSceneMode.Single);
        var map = EditorSceneManager.OpenScene("Assets/Scenes/Map_Prototype.unity", OpenSceneMode.Additive);
        SceneManager.SetActiveScene(map);
        Check(RunRestart.ResolveScenePath() == "Assets/Scenes/BootstrapScene.unity", "restart resolves bootstrap while map is active");
        Debug.Log("TOWER_VALIDATION_PASSED checks=" + checks);
    }
}
