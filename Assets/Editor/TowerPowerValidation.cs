using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class TowerPowerValidation
{
    private const string Active="TowerPowerValidation.Active";
    private static int phase,checks,errors;
    private static double deadline;
    private static Vector3 original;
    private static EnemyHealth robotTarget;
    private static int attacks;
    static TowerPowerValidation(){EditorApplication.update+=Tick;Application.logMessageReceived+=Log;}
    public static void Start(){EditorSceneManager.OpenScene("Assets/Scenes/BootstrapScene.unity",OpenSceneMode.Single);SessionState.SetBool(Active,true);EditorApplication.isPlaying=true;}
    // Match the other batch validators: exclude only the known Editor search-index exception.
    private static void Log(string s,string stack,LogType type){if(SessionState.GetBool(Active,false)&&(type==LogType.Error||type==LogType.Exception)&&!stack.Contains("UnityEditor.Search.SearchDatabase"))errors++;}
    private static void Check(bool ok,string text){if(!ok)throw new Exception(text);checks++;Debug.Log("POWER_PASS: "+text);}
    private static EnemyHealth Target(Vector2 position,int multiplier=1)
    {var go=new GameObject("Power QA target");go.transform.position=position;go.AddComponent<CircleCollider2D>().radius=0.2f;var hp=go.AddComponent<EnemyHealth>();hp.ApplyFloorHealthMultiplier(multiplier);return hp;}
    private static void Tick()
    {
        if(!SessionState.GetBool(Active,false)||!EditorApplication.isPlaying)return;
        try
        {
            EditorApplication.isPaused=false;
            if(deadline==0)deadline=EditorApplication.timeSinceStartup+120;
            if(EditorApplication.timeSinceStartup>deadline)throw new Exception("Power QA timeout phase "+phase);
            var ui=TowerRunUI.Instance;if(ui==null)return;
            if(!ui.HasStarted&&phase!=3){ui.StartRun(RunMode.TwentyFloor);return;}
            var player=UnityEngine.Object.FindAnyObjectByType<PlayerMovement>();if(player==null)return;
            player.GetComponent<PlayerShield>().ResetAfterRespawn(120);
            var powers=TowerPowers.Instance;var robot=UnityEngine.Object.FindAnyObjectByType<TowerRobotCompanion>();
            if(phase==0)
            {
                var floor=UnityEngine.Object.FindAnyObjectByType<FloorManager>();if(floor==null||!floor.IsEncounterActive||!player.enabled)return;
                Check(powers!=null&&robot!=null,"run creates player powers and robot");
                var offers=powers.Offers();Check(offers.Length==3&&offers.Select(p=>p.id).Distinct().Count()==3&&offers.Select(p=>p.owner).Distinct().Count()==2,"three distinct offers include both owners");
                original=player.transform.position;player.transform.position=new Vector3(100,100,0);player.enabled=false;robot.enabled=false;
                var ink=player.GetComponent<PlayerInkResource>();ink.SpendInk(50);
                var target=Target(new Vector2(101,100),100);var nearby=Target(new Vector2(102,100),100);
                var wall=new GameObject("QA wall");wall.tag="Obstacle";wall.transform.position=new Vector3(100.5f,100,0);wall.AddComponent<BoxCollider2D>();Physics2D.SyncTransforms();
                Check(!TowerPowers.ClearLine(player.transform.position,target.transform.position),"obstacle blocks robot and secondary attack sight");UnityEngine.Object.DestroyImmediate(wall);
                powers.Add(TowerPowerId.Chain);
                for(int i=0;i<4;i++)target.TakeDamage(1);
                Check(nearby.CurrentHealthExact<nearby.MaxHealth,"four direct hits cause secondary chain");
                Check(Mathf.Approximately((float)powers.PlayerDamageDealt,4.35f),"chain is counted once and cannot recursively proc");
                powers.Add(TowerPowerId.Recycle);var victim=Target(new Vector2(101,101));float before=ink.CurrentInk;
                TowerPowers.Damage(victim.gameObject,20,TowerDamageOrigin.Robot);
                Check(Mathf.Approximately(ink.CurrentInk,before),"robot kill cannot trigger player recycling");
                Check(powers.RobotDamageDealt==12,"robot damage has independent ownership and overkill clamp");
                victim=Target(new Vector2(101,101));victim.TakeDamage(20);Check(ink.CurrentInk>before,"direct player kill recycles ink");
                powers.Add(TowerPowerId.Foam);victim=Target(new Vector2(101,100.5f));before=nearby.CurrentHealthExact;victim.TakeDamage(20);
                Check(nearby.CurrentHealthExact<before,"direct kill foam explosion damages nearby target");
                powers.Add(TowerPowerId.Guard);Check(powers.AbsorbHit()&&!powers.AbsorbHit(),"guard blocks exactly one hit");
                powers.Add(TowerPowerId.Alternating);float basic=powers.PlayerDamageMultiplier;
                powers.BeginAttack(WeaponSlotSide.Left);ink.SpendInk(1);powers.EndAttack();
                powers.BeginAttack(WeaponSlotSide.Right);ink.SpendInk(1);powers.EndAttack();
                Check(powers.PlayerDamageMultiplier>basic,"actual alternating ink consumption activates pressure buff");
                powers.Add(TowerPowerId.Chain);powers.Add(TowerPowerId.Chain);Check(!powers.Add(TowerPowerId.Chain)&&powers.Level(TowerPowerId.Chain)==3,"power stacking is capped at three");
                float playerDamage=powers.PlayerDamageMultiplier;powers.Add(TowerPowerId.Brush);
                Check(Mathf.Approximately(playerDamage,powers.PlayerDamageMultiplier),"robot investment cannot raise player damage");
                int robotPoints=powers.RobotLevels;powers.Add(TowerPowerId.CloseClean);Check(powers.RobotLevels==robotPoints,"player investment cannot raise robot level");
                var shield=player.GetComponent<PlayerShield>();shield.ResetAfterRespawn(0);shield.TakeDamage(1);float shieldBefore=shield.CurrentShield;target.TakeDamage(1);Check(shield.CurrentShield>shieldBefore,"close-range direct hit repairs shield");
                powers.Add(TowerPowerId.Sterilize);var inkTarget=Target(original);InkMap.Instance.PaintCircle(original,0.6f,InkTeam.Player);float inkTargetBefore=inkTarget.CurrentHealthExact;inkTarget.TakeDamage(1);Check(Mathf.Approximately(inkTargetBefore-inkTarget.CurrentHealthExact,1.2f),"friendly ink catalyst amplifies direct damage");UnityEngine.Object.Destroy(inkTarget.gameObject);
                var controller=player.GetComponent<PlayerWeaponController>();
                foreach(var weapon in player.GetComponentsInChildren<PlayerWeaponBehaviour>(true).Select(w=>w.Definition).Where(w=>w!=null))
                {
                    controller.SetWeapon(WeaponSlotSide.Left,weapon);controller.SetWeapon(WeaponSlotSide.Right,weapon);
                    Check(Mathf.Approximately(controller.GetDamageMultiplier(WeaponSlotSide.Right),powers.PlayerDamageMultiplier)&&Mathf.Approximately(controller.GetDamageMultiplier(WeaponSlotSide.Left),0.8f*powers.PlayerDamageMultiplier),"newly equipped "+weapon.name+" inherits owner powers in both hands");
                }
                foreach(var p in TowerPowerCatalog.All)while(powers.Add(p.id)){}
                Check(powers.Offers().Length==0,"maxed catalog has safe service fallback");
                ui.ShowPowerChoices(()=>{});ui.ChoosePower(0);Check(!ui.ChoosingPower,"maxed powers service selection completes");Time.timeScale=1;
                UnityEngine.Object.Destroy(target.gameObject);UnityEngine.Object.Destroy(nearby.gameObject);
                // Detailed balance simulation is deferred to the dedicated QA phase.
                player.enabled=true;robot.enabled=true;robotTarget=Target(new Vector2(101.5f,100),1000);attacks=robot.Attacks;phase=1;deadline=EditorApplication.timeSinceStartup+30;
            }
            else if(phase==1)
            {
                if(robot.Attacks<=attacks||robotTarget.CurrentHealthExact>=robotTarget.MaxHealth)return;
                Check(Vector2.Distance(robot.transform.position,player.transform.position)<2,"robot recovers distant player position and follows");
                Check(robotTarget.CurrentHealthExact<robotTarget.MaxHealth,"robot targets and damages nearby enemy");
                UnityEngine.Object.Destroy(robotTarget.gameObject);ui.Run.AdvanceFloor();player.transform.position=original;phase=2;
            }
            else if(phase==2)
            {
                if(Vector2.Distance(robot.transform.position,player.transform.position)>2)return;
                Check(true,"robot follows floor change and survives disappearing target");
                RunRestart.Restart();phase=3;
            }
            else if(phase==3)
            {
                if(ui.HasStarted)return;
                Check(TowerPowers.Instance==null&&UnityEngine.Object.FindAnyObjectByType<TowerRobotCompanion>()==null,"restart clears all powers and companion");
                Check(errors==0,"power and companion QA has no runtime errors");Debug.Log("TOWER_POWER_VALIDATION_PASSED checks="+checks);SessionState.SetBool(Active,false);EditorApplication.Exit(0);
            }
        }
        catch(Exception ex){SessionState.SetBool(Active,false);Debug.LogError("TOWER_POWER_VALIDATION_FAILED phase="+phase+" "+ex);EditorApplication.Exit(1);}
    }
}
