using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Controlled clears and route fixtures verify all authored maps accept each objective.
// This is an integration smoke test, not an unprotected balance playthrough.
[InitializeOnLoad]
public static class FloorRouteMatrixValidation
{
    private const string Active="FloorRouteMatrixValidation.Active";
    private static int phase,index,checks,errors;
    private static double deadline;
    private static FloorDefinition[] floors;
    private static FloorCandidate expected;
    static FloorRouteMatrixValidation(){EditorApplication.update+=Tick;Application.logMessageReceived+=Log;}
    private static object Field(object o,string name)=>o.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(o);
    private static void Invoke(object o,string name,params object[] args)=>o.GetType().GetMethod(name,BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic).Invoke(o,args);
    private static void Check(bool ok,string message){if(!ok)throw new Exception(message);checks++;Debug.Log("MATRIX_PASS "+message);}
    private static void Log(string s,string stack,LogType t){if(SessionState.GetBool(Active,false)&&(t==LogType.Exception||t==LogType.Error)&&!stack.Contains("UnityEditor.Search.SearchDatabase"))errors++;}
    public static void Start(){EditorSceneManager.OpenScene("Assets/Scenes/BootstrapScene.unity",OpenSceneMode.Single);SessionState.SetBool(Active,true);EditorApplication.isPlaying=true;}
    private static void Next(int n){phase=n;deadline=EditorApplication.timeSinceStartup+100;}
    private static void Tick()
    {
        if(!SessionState.GetBool(Active,false)||!EditorApplication.isPlaying)return;
        try
        {
            EditorApplication.isPaused=false;
            if(deadline==0)deadline=EditorApplication.timeSinceStartup+100;
            if(EditorApplication.timeSinceStartup>deadline)throw new Exception("Matrix timeout phase="+phase+" case="+index);
            var ui=TowerRunUI.Instance;if(ui==null)return;
            var run=ui.Run;var floor=UnityEngine.Object.FindAnyObjectByType<FloorManager>();
            var player=UnityEngine.Object.FindAnyObjectByType<PlayerMovement>();
            if(player!=null)player.GetComponent<PlayerShield>().ResetAfterRespawn(1200);
            bool ready=floor!=null&&floor.IsEncounterActive&&player!=null&&player.enabled&&player.GetComponent<PlayerWeaponInputController>().InputEnabled;
            var view=UnityEngine.Object.FindAnyObjectByType<FloorSelectionUI>(FindObjectsInactive.Include);
            if(phase==0){ui.StartRun(RunMode.TwentyFloor);Next(1);return;}
            if(phase==1)
            {
                if(!ready)return;
                floors=floor.FloorDefinitions.Where(f=>f!=null).ToArray();
                run.InitializeRun(RunMode.TwentyFloor,run.RunInventory.ToArray(),1000+index);
                Invoke(floor,"FloorClear");Next(2);return;
            }
            if(phase==2){if(!ui.ChoosingPower)return;ui.ChoosePower(0);Next(3);return;}
            if(phase==3)
            {
                if(!view.IsVisible)return;
                var choices=(List<FloorCandidate>)Field(run,"floorCandidates");choices.Clear();
                var map=floors[index/3];var mode=(FloorEncounterMode)(index%3);
                expected=new FloorCandidate(map,FloorDifficulty.Normal,4100+index,mode);
                choices.Add(expected);choices.Add(new FloorCandidate(map,FloorDifficulty.Easy,4200+index,(FloorEncounterMode)((index+1)%3)));choices.Add(new FloorCandidate(map,FloorDifficulty.Hard,4300+index,mode));
                var transition=UnityEngine.Object.FindAnyObjectByType<FloorTransitionManager>();
                view.ShowCandidates(choices,c=>Invoke(transition,"FloorCandidateSelected",c));Invoke(view,"SelectCard",0);view.Confirm();Next(4);return;
            }
            if(phase==4)
            {
                if(!ready||run.CurrentFloor!=2)return;
                Check(floor.CurrentEncounterMode==expected.EncounterMode && UnityEngine.SceneManagement.SceneManager.GetSceneByName(expected.Floor.Map.SceneName).isLoaded,expected.Floor.Map.SceneName+" / "+expected.EncounterMode+" starts actual director");
                if(expected.EncounterMode==FloorEncounterMode.Defense)Check(floor.CurrentDefenseTarget!=null,"defense target placed on "+expected.Floor.Map.SceneName);
                var plan=(EncounterPlan)Field(floor,"currentEncounterPlan");
                string preview=floor.DescribeRoute(expected,2);
                Check(plan!=null && (expected.EncounterMode!=FloorEncounterMode.Elimination||preview.Contains("총 "+plan.totalQuota+"체")),"preview quota agrees with actual generated plan");
                index++;if(index<9){Next(1);return;}
                Check(errors==0,"all nine map/objective combinations without runtime errors");Debug.Log("FLOOR_ROUTE_MATRIX_PASSED checks="+checks);Finish(0);
            }
        }
        catch(Exception e){Debug.LogError("FLOOR_ROUTE_MATRIX_FAILED "+e);Finish(1);}
    }
    private static void Finish(int code){SessionState.SetBool(Active,false);EditorApplication.Exit(code);}
}
