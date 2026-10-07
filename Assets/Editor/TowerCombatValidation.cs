using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

// Runs actual weapon input, projectile collisions, enemy AI and ink reloads.
// Only setup skips floors; combat never applies test damage or refills ink.
[InitializeOnLoad]
public static class TowerCombatValidation
{
    private const string Active="TowerCombatValidation.Active";
    private sealed class Case
    {
        public int floor, points, split;
        public bool boss, protect, endless, idle, checkRestart, robotOnly;
        public string Name => (boss?"boss":"wave")+"-"+floor+"-"+new[]{"player","robot","mixed"}[split]+(protect?"-protected":"-unprotected")+(idle?"-idle":"")+(checkRestart?"-restart":"")+(robotOnly?"-robot-only":"");
    }
    private static List<Case> cases;
    private static int index,phase,errors,failures,hits,deaths,reloads;
    private static double deadline;
    private static double playerDamageAtStart,robotDamageAtStart;
    private static float started,nextReport,divingTime,sampleAt;
    private static bool reloading;
    private static PlayerMovement player;
    private static BossHealth boss;
    private static Keyboard keyboard;
    private static Mouse mouse;
    private static Vector2 aim,move;
    private static bool fire,dive;
    private static readonly Queue<Vector2> route=new();
    private static Transform routeTarget;
    private static Case Current=>cases[index];
    static TowerCombatValidation(){EditorApplication.update+=Tick;Application.logMessageReceived+=Log;}
    public static void Start()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/BootstrapScene.unity",OpenSceneMode.Single);
        SessionState.SetBool(Active,true);EditorApplication.isPlaying=true;
    }
    private static void Log(string s,string stack,LogType t){if(SessionState.GetBool(Active,false)&&(t==LogType.Error||t==LogType.Exception))errors++;}
    private static void Init()
    {
        cases=new List<Case>();
        string suite=Environment.GetEnvironmentVariable("TOWER_COMBAT_SUITE")??"boss";
        foreach(int floor in suite=="boss"?new[]{5,10,20,100}:new[]{1,10,20,100})
        for(int split=0;split<3;split++)cases.Add(new Case{floor=floor,points=Math.Min(floor-1,30),split=split,boss=suite=="boss",protect=true,endless=floor>20});
        if(suite=="survival")cases= new List<Case>{new Case{floor=1,points=0,split=0},new Case{floor=10,points=9,split=1},new Case{floor=20,points=19,split=2,boss=true},new Case{floor=100,points=30,split=1,endless=true,boss=true}};
        if(suite=="death")cases=new List<Case>{new Case{floor=1,idle=true},new Case{floor=1,checkRestart=true},new Case{floor=15,points=14,split=2,boss=true,protect=true},new Case{floor=20,points=19,split=1,protect=true,robotOnly=true}};
        if(Environment.GetEnvironmentVariable("TOWER_COMBAT_FOCUS")=="reload")cases=new List<Case>{new Case{floor=10,points=9,split=0,boss=true,protect=true}};
        if(Environment.GetEnvironmentVariable("TOWER_COMBAT_FOCUS")=="deep-wave")cases=cases.Where(c=>c.floor==100).ToList();
        if(Environment.GetEnvironmentVariable("TOWER_COMBAT_FOCUS")=="robot-control")cases=new List<Case>{new Case{floor=20,points=19,split=1,boss=true,robotOnly=true}};
        keyboard=InputSystem.AddDevice<Keyboard>();mouse=InputSystem.AddDevice<Mouse>();
        InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
        InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        InputSystem.onAfterUpdate+=Feed;
    }
    private static void Feed()
    {
        if(!SessionState.GetBool(Active,false)||!EditorApplication.isPlaying||keyboard==null||InputState.currentUpdateType!=InputUpdateType.Dynamic)return;
        keyboard.MakeCurrent();mouse.MakeCurrent();var keys=new List<Key>();
        if(phase==3){if(move.x>0.25f)keys.Add(Key.D);if(move.x<-.25f)keys.Add(Key.A);if(move.y>.25f)keys.Add(Key.W);if(move.y<-.25f)keys.Add(Key.S);if(dive)keys.Add(Key.LeftShift);}
        InputState.Change(keyboard,new KeyboardState(keys.ToArray()));
        var screen=Camera.main!=null?(Vector2)Camera.main.WorldToScreenPoint(aim):Vector2.zero;
        var state=new MouseState{position=screen};if(phase==3&&fire)state=state.WithButton(MouseButton.Left);InputState.Change(mouse,state);
    }
    private static void Set(object obj,string name,object value)=>obj.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(obj,value);
    private static void Tick()
    {
        if(!SessionState.GetBool(Active,false)||!EditorApplication.isPlaying)return;
        try
        {
            if(cases==null){Init();deadline=EditorApplication.timeSinceStartup+120;}
            if(EditorApplication.timeSinceStartup>deadline)throw new Exception("setup/time limit "+Current.Name+" phase="+phase+" scale="+Time.timeScale+" finished="+TowerRunUI.Instance?.HasFinished+" playerEnabled="+player?.enabled);
            var ui=TowerRunUI.Instance;if(ui==null)return;
            if(phase==0)
            {
                if(ui.HasStarted)return;
                UnityEngine.Random.InitState(20261006);ui.StartRun(Current.endless?RunMode.Endless:RunMode.TwentyFloor);

                var powers=TowerPowers.Instance;
                int pp=Current.split==0?Current.points:Current.split==1?0:(Current.points+1)/2;
                int rp=Current.points-pp;
                for(int i=0;i<pp;i++)powers.Add(TowerPowerId.Nozzle);
                for(int i=0;i<rp;i++)powers.Add(TowerPowerId.RobotCore);
                phase=1;return;
            }
            player=UnityEngine.Object.FindAnyObjectByType<PlayerMovement>();if(player==null)return; if(phase<3)player.GetComponent<PlayerShield>().ResetAfterRespawn(1200);
            var floor=UnityEngine.Object.FindAnyObjectByType<FloorManager>();
            if(phase==1)
            {
                if(floor==null||!floor.IsEncounterActive||!player.enabled||player.transform.localScale.x<.6f||Time.timeScale<.99f)return;
                Set(ui.Run,"currentFloor",Current.floor);Set(ui.Run,"firstFloorEncounterSeed",20261006); if(Current.boss){typeof(FloorManager).GetMethod("FloorClear",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(floor,null);phase=2;return;}
                // Rebuild the same first-map elimination plan at the requested depth.
                typeof(FloorManager).GetMethod("ResetEncounterRuntime",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(floor,null);
                foreach(var enemy in UnityEngine.Object.FindObjectsByType<EnemyHealth>(FindObjectsSortMode.None))UnityEngine.Object.DestroyImmediate(enemy.gameObject);
                Set(ui.Run,"selectedCandidate",new FloorCandidate(floor.FloorDefinitions[0],FloorDifficulty.Easy,20261006));
                UnityEngine.Random.InitState(20261006);floor.SpawnCurrentFloor();
                Begin();return;
            }
            if(phase==2)
            {
                boss=UnityEngine.Object.FindAnyObjectByType<BossHealth>();
                if(boss==null||boss.IsInvulnerable||!player.enabled)return;
                Begin();return;
            }
            if(phase==3)
            {
                if(Current.checkRestart)
                {
                    var powers=TowerPowers.Instance;
                    if(!player.enabled||ui.Run.CurrentFloor!=1||powers.PlayerLevels!=0||powers.RobotLevels!=0||Time.timeScale<.99f||player.GetComponent<PlayerInkResource>().CurrentInkPercent<.99f)throw new Exception("defeat restart did not restore a playable clean run");
                    Result("restart-ready");return;
                }
                bool bossDead=Current.boss&&(boss==null||boss.IsDead);
                bool waveCleared=!Current.boss&&(bool)typeof(FloorManager).GetField("floorCleared",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(floor);
                if(ui.HasFinished||ui.ChoosingPower||bossDead||waveCleared)
                {Result(ui.HasFinished&&!bossDead?"defeat":"clear");return;}
                float elapsed=Time.time-started;
                if(elapsed>180){Result("combat-timeout");return;}
                if(Time.time>=nextReport){nextReport=Time.time+15;Debug.Log("COMBAT_PROGRESS "+Current.Name+" elapsed="+elapsed.ToString("F1")+" damage="+(TowerPowers.Instance.PlayerDamageDealt+TowerPowers.Instance.RobotDamageDealt).ToString("F1")+" remaining="+floor.RemainingEnemies+" bossHP="+(boss!=null?boss.CurrentHealth:0)+" ink="+player.GetComponent<PlayerInkResource>().CurrentInk+" dive="+player.GetComponent<PlayerDive>().IsDiving+" move="+move+" position="+player.transform.position);}
                if(dive)divingTime+=Mathf.Max(0,Time.time-sampleAt);sampleAt=Time.time;Think();
                return;
            }
            if(phase==4)
            {
                if(ui.HasStarted)return;
                phase=0;deadline=EditorApplication.timeSinceStartup+120;
            }
        }
        catch(Exception ex){Debug.LogError("TOWER_COMBAT_VALIDATION_FAILED "+ex);Finish(1);}
    }
    private static void Begin()
    {
        var definitions=player.GetComponentsInChildren<PlayerWeaponBehaviour>(true).Select(w=>w.Definition).Where(w=>w!=null);
        player.GetComponent<PlayerWeaponController>().SetWeapon(WeaponSlotSide.Right,definitions.First(w=>w.Type==WeaponType.Shooter));
        var shield=player.GetComponent<PlayerShield>();shield.ResetAfterRespawn(Current.protect?1200:0);
        var lives=UnityEngine.Object.FindAnyObjectByType<PlayerLifeManager>();
        if(lives==null||lives.CurrentLives!=lives.MaxLives)throw new Exception("new scenario did not reset lives");
        shield.ShieldHit+=()=>hits++;shield.PlayerDefeated+=()=>deaths++;
        hits=deaths=reloads=0;divingTime=0;reloading=false;started=Time.time;sampleAt=started;nextReport=started+15;phase=3;
        route.Clear();routeTarget=null;
        playerDamageAtStart=TowerPowers.Instance.PlayerDamageDealt;robotDamageAtStart=TowerPowers.Instance.RobotDamageDealt;
        // Keep physics and enemy telegraphs at their shipped rate.
        deadline=EditorApplication.timeSinceStartup+240;
        Debug.Log("COMBAT_BEGIN "+Current.Name+" points="+Current.points+" enemyHPx="+UnityEngine.Object.FindAnyObjectByType<FloorManager>().CurrentEnemyHealthMultiplier+" bossHP="+(boss!=null?boss.MaxHealth:0));
    }
    private static void Think()
    {
        if(Current.idle){fire=dive=false;move=Vector2.zero;return;}
        Vector2 pos=player.transform.position;var ink=player.GetComponent<PlayerInkResource>();
        if(!reloading&&ink.CurrentInk<25){reloading=true;reloads++;}
        if(reloading&&ink.CurrentInk>70)reloading=false;
        var targets=UnityEngine.Object.FindObjectsByType<EnemyHealth>(FindObjectsSortMode.None).Where(e=>e.CurrentHealthExact>0).Select(e=>e.transform).ToList();
        if(boss!=null&&!boss.IsDead)targets.Add(boss.transform);
        var target=targets.OrderBy(t=>Vector2.Distance(pos,t.position)+(TowerPowers.ClearLine(pos,t.position)?0:100)).FirstOrDefault();
        fire=false;dive=false;move=Vector2.zero;if(target==null)return;
        aim=target.position;Vector2 delta=aim-pos;float distance=delta.magnitude;
        bool ownInk=InkMap.Instance!=null&&InkMap.Instance.GetDominantInkTeam(pos,.22f)==InkTeam.Player;
        if(reloading&&ownInk){dive=true;return;}
        if(reloading)
        {
            // Walk back onto ink produced by our shots; never paint or refill from QA.
            for(float r=.4f;r<=6;r+=.4f)for(int a=0;a<8;a++)
            {
                Vector2 spot=pos+new Vector2(Mathf.Cos(a*Mathf.PI/4),Mathf.Sin(a*Mathf.PI/4))*r;
                if(InkMap.Instance.GetDominantInkTeam(spot,.22f)!=InkTeam.Player||!TowerPowers.ClearLine(pos,spot))continue;
                if(Physics2D.OverlapCircleAll(spot,.35f).Any(c=>!c.isTrigger&&(c.CompareTag("Obstacle")||c.GetComponentInParent<BossHealth>()!=null||c.GetComponentInParent<EnemyHealth>()!=null)))continue;
                move=(spot-pos).normalized;return;
            }
            // Reserve ink creates a reachable retreat path if enemies have covered our old ink.
            aim=pos-delta.normalized*.8f;fire=ink.CurrentInk>2;move=-delta.normalized;return;
        }
        fire=!Current.robotOnly&&!reloading&&distance<8&&TowerPowers.ClearLine(pos,aim);
        if(distance>2.6f)move=delta.normalized;
        else if(distance<1.5f)move=-delta.normalized;
        else if(!Current.protect)move=new Vector2(-delta.y,delta.x).normalized;
        if(routeTarget!=target){route.Clear();routeTarget=target;}
        if(distance>2.6f&&route.Count==0&&!CanWalk(pos,pos+move*.8f))FindRoute(pos,aim);
        while(route.Count>0&&Vector2.Distance(pos,route.Peek())<.35f)route.Dequeue();
        if(route.Count>0)move=(route.Peek()-pos).normalized;
        else if(!CanWalk(pos,pos+move*.8f))
        {var side=new Vector2(-move.y,move.x);move=CanWalk(pos,pos+side*.8f)?side:-side;}
    }
    private static bool CanWalk(Vector2 from,Vector2 to)
    {
        Vector2 delta=to-from;
        Vector2 size=player!=null?(Vector2)player.GetComponent<Collider2D>().bounds.size*.95f:new Vector2(.5f,.5f);
        return !Physics2D.BoxCastAll(from,size,0,delta.normalized,delta.magnitude).Any(h=>h.collider!=null&&!h.collider.isTrigger&&h.collider.CompareTag("Obstacle"));
    }
    // Bounded test-only navigation: a ray can clear a corner while the player's body cannot.
    private static void FindRoute(Vector2 origin,Vector2 goal)
    {
        var open=new List<Vector2Int>{Vector2Int.zero};var costs=new Dictionary<Vector2Int,float>{{Vector2Int.zero,0}};
        var parents=new Dictionary<Vector2Int,Vector2Int>();var closed=new HashSet<Vector2Int>();
        const float step=.8f;
        for(int n=0;n<1200&&open.Count>0;n++)
        {
            var cell=open.OrderBy(c=>costs[c]+Vector2.Distance(origin+(Vector2)c*step,goal)).First();open.Remove(cell);closed.Add(cell);
            Vector2 point=origin+(Vector2)cell*step;
            if(Vector2.Distance(point,goal)<2.3f&&TowerPowers.ClearLine(point,goal))
            {
                var path=new List<Vector2>();while(cell!=Vector2Int.zero){path.Add(origin+(Vector2)cell*step);cell=parents[cell];}path.Reverse();foreach(var p in path)route.Enqueue(p);return;
            }
            for(int x=-1;x<=1;x++)for(int y=-1;y<=1;y++)
            {
                if(x==0&&y==0)continue;var next=cell+new Vector2Int(x,y);if(closed.Contains(next)||Mathf.Abs(next.x)>32||Mathf.Abs(next.y)>32)continue;
                Vector2 nextPoint=origin+(Vector2)next*step;if(!CanWalk(point,nextPoint))continue;
                float cost=costs[cell]+Vector2.Distance(point,nextPoint);if(costs.TryGetValue(next,out float old)&&old<=cost)continue;
                costs[next]=cost;parents[next]=cell;if(!open.Contains(next))open.Add(next);
            }
        }
    }
    private static void Result(string outcome)
    {
        if(Current.protect&&outcome!="clear")failures++;
        if(Current.idle&&(outcome!="defeat"||deaths!=3))failures++;
        var p=TowerPowers.Instance;double pd=p.PlayerDamageDealt-playerDamageAtStart,rd=p.RobotDamageDealt-robotDamageAtStart,total=pd+rd;
        if(Current.robotOnly&&pd>0)failures++;
        Debug.Log("COMBAT_RESULT case="+Current.Name+" outcome="+outcome+" seconds="+(Time.time-started).ToString("F2")+" player="+pd.ToString("F2")+" robot="+rd.ToString("F2")+" playerShare="+(total>0?100*pd/total:0).ToString("F2")+" hits="+hits+" deaths="+deaths+" reloads="+reloads+" diveSeconds="+divingTime.ToString("F2"));
        fire=dive=false;move=Vector2.zero;boss=null;index++;
        if(index>=cases.Count){Debug.Log("TOWER_COMBAT_VALIDATION_COMPLETED cases="+cases.Count+" errors="+errors+" failures="+failures);Finish(errors==0&&failures==0?0:1);return;}
        RunRestart.Restart();phase=4;deadline=EditorApplication.timeSinceStartup+120;
    }
    private static void Finish(int code){SessionState.SetBool(Active,false);InputSystem.onAfterUpdate-=Feed;if(keyboard!=null)InputSystem.RemoveDevice(keyboard);if(mouse!=null)InputSystem.RemoveDevice(mouse);EditorApplication.Exit(code);}
}
