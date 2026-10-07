using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using TMPro;

[InitializeOnLoad]
public static class FloorBoundaryValidation
{
    private const string Active="FloorBoundaryValidation.Active";
    private static int phase,index,checks,errors;
    private static double deadline;
    private static int StartFloor=>index==1?39:19;
    private static RunMode Mode=>index==2?RunMode.TwentyFloor:RunMode.Endless;
    static FloorBoundaryValidation(){EditorApplication.update+=Tick;Application.logMessageReceived+=Log;}
    private static void Log(string s,string stack,LogType t){if(SessionState.GetBool(Active,false)&&(t==LogType.Exception||t==LogType.Error)&&!stack.Contains("UnityEditor.Search.SearchDatabase"))errors++;}
    private static void Check(bool ok,string message){if(!ok)throw new Exception(message);checks++;Debug.Log("BOUNDARY_PASS "+message);}
    private static object Field(object o,string name)=>o.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(o);
    private static void Invoke(object o,string name,params object[] args)=>o.GetType().GetMethod(name,BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public).Invoke(o,args);
    public static void Start(){EditorSceneManager.OpenScene("Assets/Scenes/BootstrapScene.unity",OpenSceneMode.Single);SessionState.SetBool(Active,true);EditorApplication.isPlaying=true;}
    private static void Next(int n){phase=n;deadline=EditorApplication.timeSinceStartup+100;}
    private static void Tick()
    {
        if(!SessionState.GetBool(Active,false)||!EditorApplication.isPlaying)return;
        try
        {
            EditorApplication.isPaused=false;if(deadline==0)deadline=EditorApplication.timeSinceStartup+100;
            if(EditorApplication.timeSinceStartup>deadline)throw new Exception("Boundary timeout case="+index+" phase="+phase);
            var ui=TowerRunUI.Instance;if(ui==null)return;var run=ui.Run;
            var floor=UnityEngine.Object.FindAnyObjectByType<FloorManager>();var player=UnityEngine.Object.FindAnyObjectByType<PlayerMovement>();
            if(player!=null)player.GetComponent<PlayerShield>().ResetAfterRespawn(1200);
            bool ready=floor!=null&&floor.IsEncounterActive&&player!=null&&player.enabled&&player.GetComponent<PlayerWeaponInputController>().InputEnabled;
            var view=UnityEngine.Object.FindAnyObjectByType<FloorSelectionUI>(FindObjectsInactive.Include);
            if(phase==0){if(ui.HasStarted)return;ui.StartRun(Mode);Next(1);return;}
            if(phase==1)
            {
                if(!ready)return;typeof(RunManager).GetField("currentFloor",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(run,StartFloor);
                Invoke(floor,"FloorClear");Next(2);return;
            }
            if(phase==2){if(!ui.ChoosingPower)return;ui.ChoosePower(0);Next(3);return;}
            if(phase==3)
            {
                if(!view.IsVisible)return;
                var root=(GameObject)Field(view,"panelRoot");bool finalLabel=root.GetComponentsInChildren<TMP_Text>().Any(t=>t.text.Contains("타워 정화 완료"));
                Check(finalLabel==(Mode==RunMode.TwentyFloor),Mode+" "+StartFloor+"→"+(StartFloor+1)+" final reward label matches mode");
                FloorRouteValidation.Capture("boundary-"+Mode+"-"+(StartFloor+1),1920,1080);
                Invoke(view,"SelectCard",1);view.Confirm();Next(4);return;
            }
            if(phase==4)
            {
                if(!ready||run.CurrentFloor!=StartFloor+1)return;
                Check(run.IsBossFloor,Mode+" floor "+run.CurrentFloor+" is boss floor");Invoke(floor,"FloorClear");Next(5);return;
            }
            if(phase==5)
            {
                var boss=UnityEngine.Object.FindAnyObjectByType<BossBattleManager>();if(boss.CurrentBossHealth==null||boss.CurrentBossHealth.IsInvulnerable)return;
                boss.CurrentBossHealth.TakeDamage(1000000);Next(6);return;
            }
            if(phase==6)
            {
                if(Mode==RunMode.TwentyFloor)
                {
                    if(!ui.HasFinished)return;Check(run.CurrentFloor==20&&run.IsLastFloor,"campaign floor 20 ends run");
                    Check(errors==0,"boundary transitions have no runtime errors");Debug.Log("FLOOR_BOUNDARY_PASSED checks="+checks);Finish(0);return;
                }
                if(!ui.ChoosingPower)return;Check(!ui.HasFinished&&!run.IsLastFloor,"endless boss has reward instead of ending");ui.ChoosePower(0);Next(7);return;
            }
            if(phase==7){if(!view.IsVisible)return;Invoke(view,"SelectCard",1);view.Confirm();Next(8);return;}
            if(phase==8)
            {
                if(!ready||run.CurrentFloor!=StartFloor+2)return;
                Check(!ui.HasFinished&&!run.IsLastFloor&&!run.IsBossFloor,"endless continues into floor "+run.CurrentFloor);
                index++;RunRestart.Restart();Next(0);
            }
        }
        catch(Exception e){Debug.LogError("FLOOR_BOUNDARY_FAILED "+e);Finish(1);}
    }
    private static void Finish(int code){SessionState.SetBool(Active,false);EditorApplication.Exit(code);}
}
