using System;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class TowerPlayValidation
{
    private const string Active="TowerPlayValidation.Active";
    private static int phase;
    private static double deadline;
    private static int failures;
    static TowerPlayValidation()
    {
        EditorApplication.update += Tick;
        Application.logMessageReceived += Log;
    }
    public static void Start()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/BootstrapScene.unity",OpenSceneMode.Single);
        SessionState.SetBool(Active,true);
        EditorApplication.isPlaying=true;
    }
    private static void Log(string text,string stack,LogType type)
    {
        if(SessionState.GetBool(Active,false) && (type==LogType.Error || type==LogType.Exception || type==LogType.Assert) && !stack.Contains("UnityEditor.Search.SearchDatabase"))
            failures++;
    }
    private static void Check(bool value,string text)
    { if(!value) throw new Exception(text); Debug.Log("PLAY_PASS: "+text); }
    private static void Invoke(object target,string method,params object[] args)
    { target.GetType().GetMethod(method,BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public).Invoke(target,args); }
    private static void Next(int value) { phase=value; deadline=EditorApplication.timeSinceStartup+90; }
    private static void Tick()
    {
        if(!SessionState.GetBool(Active,false)||!EditorApplication.isPlaying) return;
        try
        {
            if(EditorApplication.isPaused) EditorApplication.isPaused=false;
            if(deadline==0) deadline=EditorApplication.timeSinceStartup+90;
            if(EditorApplication.timeSinceStartup>deadline) throw new Exception("Timeout at play phase "+phase);
            var shield=UnityEngine.Object.FindAnyObjectByType<PlayerShield>();
            if(shield!=null && phase!=14 && phase!=15)shield.ResetAfterRespawn(120);
            var ui=TowerRunUI.Instance;
            var run=UnityEngine.Object.FindAnyObjectByType<RunManager>();
            var floor=UnityEngine.Object.FindAnyObjectByType<FloorManager>();
            var boss=UnityEngine.Object.FindAnyObjectByType<BossBattleManager>();
            var transition=UnityEngine.Object.FindAnyObjectByType<FloorTransitionManager>();
            if(ui==null||run==null) return;
            var lives=UnityEngine.Object.FindAnyObjectByType<PlayerLifeManager>();
            bool playerReady=shield!=null && shield.GetComponent<PlayerMovement>().enabled;
            if(ui.ChoosingPower){Check(ui.PowerChoices.Length==run.CurrentPowerOfferCount,"floor offers selected difficulty reward");ui.ChoosePower(0);return;}
            switch(phase)
            {
                case 0:
                    Check(!run.IsInitialized && Time.timeScale==0,"menu waits for mode selection");
                    ui.StartRun(RunMode.TwentyFloor); Next(1); break;
                case 1:
                    if(floor==null||!floor.IsEncounterActive||floor.ActiveEncounterEnemies.Count==0||!playerReady||!shield.GetComponent<PlayerWeaponInputController>().InputEnabled) return;
                    Check(run.CurrentFloor==1 && run.maxFloor==20,"campaign first floor spawns with 20-floor limit");
                    Check(lives.CurrentLives==3,"new run starts with three lives");
                    var powers=TowerPowers.Instance;
                    powers.Add(TowerPowerId.Recycle);
                    Check(Mathf.Approximately(powers.PlayerDamageMultiplier,1f),"utility power does not automatically increase base damage");
                    powers.Add(TowerPowerId.Nozzle);
                    Check(powers.PlayerDamageMultiplier>1f,"explicit nozzle choice increases player base damage");
                    float playerDamage=powers.PlayerDamageMultiplier;
                    powers.Add(TowerPowerId.Brush);
                    Check(powers.Level(TowerPowerId.RobotCore)==0 && Mathf.Approximately(playerDamage,powers.PlayerDamageMultiplier),"robot utility does not grant motor levels or player damage");
                    powers.Add(TowerPowerId.RobotCore);
                    Check(TowerPowers.RobotDamage(powers.Level(TowerPowerId.RobotCore))>TowerPowers.RobotDamage(0),"explicit motor choice increases robot base damage");
                    shield.ResetAfterRespawn(0);shield.TakeDamage(shield.MaxShield+1);
                    Next(14);break;
                case 14:
                    if(shield.IsEmergency && lives.CurrentLives==3)shield.TakeDamage(1);
                    if(lives.CurrentLives!=2 || (bool)typeof(PlayerLifeManager).GetField("isResolvingDeath",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(lives) || !playerReady)return;
                    Check(true,"player defeat spends one life and respawns");
                    Invoke(floor,"FloorClear"); Next(2); break;
                case 2:
                    if(run.FloorCandidates.Count!=3 || Time.timeScale!=0) return;
                    Check(run.RewardedFloors==1,"clear grants power and presents three choices");
                    Invoke(transition,"FloorCandidateSelected",run.FloorCandidates[0]); Next(3); break;
                case 3:
                    if(run.CurrentFloor!=2||!floor.IsEncounterActive||!playerReady) return;
                    Check(lives.CurrentLives==2,"selected map loads and retains spent run life");
                    while(run.CurrentFloor<5) run.AdvanceFloor();
                    Invoke(floor,"FloorClear"); Next(4); break;
                case 4:
                    if(boss.CurrentBossHealth==null||boss.CurrentBossHealth.IsInvulnerable) return;
                    Check(boss.CurrentBossHealth.MaxHealth>0 && lives.CurrentLives==2,"floor five boss enters without refilling lives");
                    boss.CurrentBossHealth.TakeDamage(1000000f); Next(5); break;
                case 5:
                    if(run.BossesDefeated!=1||run.RewardedFloors!=2||Time.timeScale!=0) return;
                    Check(!ui.HasFinished && lives.CurrentLives==3,"intermediate boss continues run and restores one life");
                    lives.TryConsumeLife();run.RecordBossDefeated();
                    Check(lives.CurrentLives==2 && run.BossesDefeated==1,"duplicate boss callback cannot grant another life");
                    Invoke(transition,"FloorCandidateSelected",run.FloorCandidates[0]); Next(6); break;
                case 6:
                    if(run.CurrentFloor!=6||!floor.IsEncounterActive||!playerReady) return;
                    while(run.CurrentFloor<20) run.AdvanceFloor();
                    Invoke(floor,"FloorClear"); Next(7); break;
                case 7:
                    if(boss.CurrentBossHealth==null||boss.CurrentBossHealth.IsInvulnerable) return;
                    Check(boss.CurrentBossHealth.gameObject.GetComponent<BossAttackController>().towerPattern==TowerBossPattern.Core,"floor twenty uses final boss pattern");
                    boss.CurrentBossHealth.TakeDamage(1000000f); Next(8); break;
                case 8:
                    if(!ui.HasFinished) return;
                    Check(Time.timeScale==0 && run.CurrentFloor==20,"final boss opens victory screen");
                    RunRestart.Restart(); Next(9); break;
                case 9:
                    if(ui.HasStarted) return;
                    Check(!run.IsInitialized && lives.CurrentLives==3,"restart returns to bootstrap menu with fresh lives");
                    ui.StartRun(RunMode.Endless); Next(10); break;
                case 10:
                    if(!floor.IsEncounterActive||floor.ActiveEncounterEnemies.Count==0) return;
                    while(run.CurrentFloor<20)run.AdvanceFloor();
                    Invoke(floor,"FloorClear");Next(11);break;
                case 11:
                    if(boss.CurrentBossHealth==null||boss.CurrentBossHealth.IsInvulnerable)return;
                    boss.CurrentBossHealth.TakeDamage(1000000f);Next(12);break;
                case 12:
                    if(run.BossesDefeated!=1||run.RewardedFloors!=1||Time.timeScale!=0)return;
                    Check(!ui.HasFinished && lives.CurrentLives==3,"endless boss continues and life recovery caps at three");
                    Invoke(transition,"FloorCandidateSelected",run.FloorCandidates[0]);Next(13);break;
                case 13:
                    if(run.CurrentFloor!=21||!floor.IsEncounterActive||floor.ActiveEncounterEnemies.Count==0)return;
                    Check(floor.CurrentEnemyHealthMultiplier>1.5f,"endless floor twenty-one uses scaled enemy health");
                    Check(!run.IsLastFloor,"endless remains open ended");
                    while(lives.CurrentLives>0)lives.TryConsumeLife();
                    Next(15);break;
                case 15:
                    if(!ui.HasFinished)return;
                    Check(lives.CurrentLives==0,"last shared life loss ends the run");
                    lives.RestoreBossLife();Check(lives.CurrentLives==0,"late boss reward cannot revive an ended run");
                    RunRestart.Restart();Next(16);break;
                case 16:
                    if(ui.HasStarted)return;
                    Check(lives.CurrentLives==3 && TowerPowers.Instance==null,"game-over restart resets lives and clears powers");
                    Check(failures==0,"no runtime errors during play smoke");
                    Debug.Log("TOWER_PLAY_VALIDATION_PASSED");
                    SessionState.SetBool(Active,false);EditorApplication.Exit(0);break;
            }
        }
        catch(Exception ex)
        {
            var run=UnityEngine.Object.FindAnyObjectByType<RunManager>();
            var tr=UnityEngine.Object.FindAnyObjectByType<FloorTransitionManager>();
            Debug.Log("FLOW_STATE floor="+run?.CurrentFloor+" rewards="+run?.RewardedFloors+" bosses="+run?.BossesDefeated+" time="+Time.timeScale+" transition="+(tr!=null?typeof(FloorTransitionManager).GetField("transitionRunning",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(tr):null)+" waiting="+(tr!=null?typeof(FloorTransitionManager).GetField("waitingForUpgrade",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(tr):null));
            SessionState.SetBool(Active,false);
            Debug.LogError("TOWER_PLAY_VALIDATION_FAILED phase="+phase+" "+ex);
            EditorApplication.Exit(1);
        }
    }
}
