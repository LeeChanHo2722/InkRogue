using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

[InitializeOnLoad]
public static class TowerWheelValidation
{
    private const string Active="TowerWheelValidation.Active";
    private static Stack<IEnumerator> routines;
    private static Keyboard keyboard;
    private static Mouse mouse;
    private static Key[] keys=Array.Empty<Key>();
    private static Vector2 cursor=new Vector2(500,350);
    private static int checks,errors,feeds;
    private static float fixedStep;
    private static TowerRunUI UI=>TowerRunUI.Instance;
    private static PlayerWeaponInputController Input=>UnityEngine.Object.FindAnyObjectByType<PlayerWeaponInputController>();
    private static PlayerShield Shield=>UnityEngine.Object.FindAnyObjectByType<PlayerShield>();
    static TowerWheelValidation(){EditorApplication.update+=Tick;Application.logMessageReceived+=Log;}
    public static void Start()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/BootstrapScene.unity",OpenSceneMode.Single);
        SessionState.SetBool(Active,true);EditorApplication.isPlaying=true;
    }
    private static void Log(string message,string stack,LogType type){if(SessionState.GetBool(Active,false)&&(type==LogType.Error||type==LogType.Exception))errors++;}
    private static void Check(bool ok,string text){if(!ok)throw new Exception(text);checks++;Debug.Log("WHEEL_PASS: "+text);}
    private static object Field(object target,string name)=>target.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(target);
    private static void Invoke(object target,string name,params object[] args)=>target.GetType().GetMethod(name,BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public).Invoke(target,args);
    private static bool Near(float a,float b)=>Mathf.Abs(a-b)<.0001f;
    private static IEnumerator Wait(Func<bool> condition,string context,float seconds=20)
    {
        double deadline=EditorApplication.timeSinceStartup+seconds;
        while(!condition()){if(EditorApplication.timeSinceStartup>deadline)throw new Exception("Timeout: "+context+" scale="+Time.timeScale+" open="+Input?.IsWeaponWheelOpen+" input="+Input?.InputEnabled+" focused="+Field(Input,"applicationFocused")+" q="+keyboard?.qKey.isPressed+" feeds="+feeds);yield return null;}
    }
    private static IEnumerator Delay(float seconds){double until=EditorApplication.timeSinceStartup+seconds;yield return Wait(()=>EditorApplication.timeSinceStartup>=until,"realtime delay");}
    private static void SetKeys(params Key[] value){keys=value;}
    private static void Feed()
    {
        if(!SessionState.GetBool(Active,false)||keyboard==null||InputState.currentUpdateType!=InputUpdateType.Dynamic)return;
        feeds++; if(!keyboard.enabled)InputSystem.EnableDevice(keyboard);if(!mouse.enabled)InputSystem.EnableDevice(mouse); keyboard.MakeCurrent();mouse.MakeCurrent();InputState.Change(keyboard,new KeyboardState(keys));InputState.Change(mouse,new MouseState{position=cursor});
    }
    private static IEnumerator Open(Key key=Key.Q)
    {
        SetKeys(key);yield return Wait(()=>Input!=null&&Input.IsWeaponWheelOpen,"actual wheel key "+key,3);
    }
    private static IEnumerator Close()
    {
        SetKeys();yield return Wait(()=>Input!=null&&!Input.IsWeaponWheelOpen,"release closes wheel",3);
    }
    private static IEnumerator Ready()
    {
        yield return Wait(()=>Input!=null&&Input.InputEnabled&&Shield!=null&&Shield.GetComponent<PlayerMovement>().enabled&&Shield.transform.localScale.x>.6f&&UnityEngine.Object.FindAnyObjectByType<FloorManager>()?.IsEncounterActive==true&&Near(Time.timeScale,1),"run ready");
        Shield.ResetAfterRespawn(1200);Invoke(Input,"OnApplicationFocus",true);
    }
    private static IEnumerator Run()
    {
        yield return Wait(()=>UI!=null,"menu");UI.StartRun(RunMode.TwentyFloor);yield return Ready();
        fixedStep=Time.fixedDeltaTime;
        keyboard=InputSystem.AddDevice<Keyboard>();mouse=InputSystem.AddDevice<Mouse>();
        InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
        InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        InputSystem.onAfterUpdate+=Feed; yield return Delay(.1f);

        yield return Open();
        Check(Input.ActiveWeaponWheelSide==WeaponSlotSide.Right,"Q opens right wheel through its InputAction");
        Check(Near(Time.timeScale,.1f)&&Near(Time.fixedDeltaTime,fixedStep*.1f),"wheel uses 10% game speed and proportional physics step");
        float game=UI.Run.ElapsedSeconds,real=Time.unscaledTime;
        int slot=(Input.ActiveRightLoadoutIndex+1)%3;
        Check(Input.RightCombatLoadout[slot]!=null,"right wheel has selectable alternate slot");
        Vector2[] directions={Vector2.up,new Vector2(.8660254f,-.5f),new Vector2(-.8660254f,-.5f)};
        cursor=Input.WeaponWheelOrigin+directions[slot]*220;
        yield return Wait(()=>Input.HighlightedWeaponSlotIndex==slot,"unscaled cursor selection",1);
        var wheel=UnityEngine.Object.FindAnyObjectByType<WeaponWheelUI>();
        yield return Delay(.06f);
        var pointer=(RectTransform)Field(wheel,"pointerLine");
        Check(pointer.gameObject.activeInHierarchy&&pointer.sizeDelta.x>20,"wheel pointer updates while game is slowed");
        yield return Delay(.5f);
        float ratio=(UI.Run.ElapsedSeconds-game)/(Time.unscaledTime-real);
        Check(ratio>.07f&&ratio<.13f,"run timer follows scaled game time while UI remains responsive");
        yield return Close();Check(Input.ActiveRightLoadoutIndex==slot,"Q release equips highlighted weapon");
        Check(Near(Time.timeScale,1)&&Near(Time.fixedDeltaTime,fixedStep),"last wheel release restores time and physics exactly");

        yield return Open(Key.E);Check(Input.ActiveWeaponWheelSide==WeaponSlotSide.Left,"E opens left wheel");
        yield return Close();Check(Near(Time.timeScale,1),"E release restores normal speed");
        yield return Open();SetKeys(Key.Q,Key.E);
        yield return Wait(()=>Input.ActiveWeaponWheelSide==WeaponSlotSide.Left,"second hand takes over");
        Check(Near(Time.timeScale,.1f),"overlapping Q/E does not multiply slowdown");
        SetKeys(Key.Q);yield return Wait(()=>Input.ActiveWeaponWheelSide==WeaponSlotSide.Right,"held Q resumes after E release");
        Check(Input.IsWeaponWheelOpen&&Near(Time.timeScale,.1f),"other held wheel keeps one slow lease");
        SetKeys(Key.E);yield return Wait(()=>Input.ActiveWeaponWheelSide==WeaponSlotSide.Left,"rapid Q to E switch");
        yield return Close();Check(Near(Time.timeScale,1)&&Near(Time.fixedDeltaTime,fixedStep),"rapid switching leaves no sticky slowdown");

        var slow=UnityEngine.Object.FindAnyObjectByType<SlowMotionController>();slow.SetTimeScale(.4f);
        yield return Open();Check(Near(Time.timeScale,.1f),"wheel caps an existing slower time at 10%");
        yield return Close();Check(Near(Time.timeScale,.4f)&&Near(Time.fixedDeltaTime,fixedStep*.4f),"wheel restores pre-existing 40% time and physics");
        slow.SetTimeScale(.05f);yield return Open();Check(Near(Time.timeScale,.05f),"wheel never accelerates an existing stronger slow effect");
        yield return Close();Check(Near(Time.timeScale,.05f),"5% prior speed is preserved");slow.RestoreImmediate();

        yield return Open();SetKeys(Key.Q,Key.Escape);
        yield return Wait(()=>Near(Time.timeScale,0),"Escape pauses from wheel");
        Check(!Input.IsWeaponWheelOpen&&Near(Time.fixedDeltaTime,fixedStep),"pause cancels wheel and restores owned physics step");
        SetKeys();game=UI.Run.ElapsedSeconds;yield return Delay(.25f);
        Check(Near(Time.timeScale,0)&&Near(game,UI.Run.ElapsedSeconds),"key release cannot unpause or advance paused timer");
        SetKeys(Key.E);yield return Delay(.1f);Check(!Input.IsWeaponWheelOpen&&Near(Time.timeScale,0),"wheel cannot open over pause menu");
        SetKeys();yield return Delay(.05f);SetKeys(Key.Escape);yield return Wait(()=>Near(Time.timeScale,1),"resume from pause");SetKeys();yield return Delay(.05f);
        Check(Near(Time.fixedDeltaTime,fixedStep),"pause resume restores prior physics step");

        slow.SetTimeScale(.4f);yield return Open();SetKeys(Key.Q,Key.Escape);yield return Wait(()=>Near(Time.timeScale,0),"pause at previous 40% speed");
        SetKeys();yield return Delay(.05f);SetKeys(Key.Escape);yield return Wait(()=>Near(Time.timeScale,.4f),"resume prior 40% speed");SetKeys();yield return Delay(.05f);
        Check(Near(Time.fixedDeltaTime,fixedStep*.4f),"pause resumes the pre-wheel speed, not a stale 10%");slow.RestoreImmediate();

        yield return Open();Time.timeScale=0;yield return Close();
        Check(Near(Time.timeScale,0)&&Near(Time.fixedDeltaTime,fixedStep),"external raw pause is not overwritten on wheel release");Time.timeScale=1;
        yield return Open();Time.timeScale=.3f;Time.fixedDeltaTime=.006f;yield return Close();
        Check(Near(Time.timeScale,.3f)&&Near(Time.fixedDeltaTime,.006f),"external time owner survives wheel cancellation");slow.RestoreImmediate();

        yield return Open();Invoke(Input,"OnApplicationFocus",false);SetKeys();yield return Delay(.1f);
        Check(!Input.IsWeaponWheelOpen&&Near(Time.timeScale,1),"focus loss cancels wheel without sticky slow");
        Time.timeScale=0;Invoke(Input,"OnApplicationFocus",false);Check(Near(Time.timeScale,0),"focus loss cannot unpause another owner");Time.timeScale=1;Invoke(Input,"OnApplicationFocus",true);
        yield return Open();Invoke(Input,"OnApplicationPause",true);SetKeys();yield return Delay(.05f);
        Check(!Input.IsWeaponWheelOpen&&Near(Time.timeScale,1),"application pause releases wheel lease");Invoke(Input,"OnApplicationPause",false);
        yield return Open();Input.enabled=false;SetKeys();yield return Delay(.05f);Check(Near(Time.timeScale,1)&&Near(Time.fixedDeltaTime,fixedStep),"component disable restores time");Input.enabled=true;yield return Delay(.05f);
        yield return Open();Input.SetInputEnabled(false);SetKeys();yield return Delay(.05f);
        Check(!Input.IsWeaponWheelOpen&&Near(Time.timeScale,1),"input lock closes wheel synchronously");Input.SetInputEnabled(true);
        yield return Open();slow.SetTimeScale(.25f);SetKeys();yield return Delay(.1f);
        Check(!Input.IsWeaponWheelOpen&&Near(Time.timeScale,.25f)&&Near(Time.fixedDeltaTime,fixedStep*.25f),"cinematic slow supersedes wheel without restoring stale speed");
        bool restoreDone=false; IEnumerator Restore(){yield return slow.RestoreSmooth(.15f);restoreDone=true;} slow.StartCoroutine(Restore());yield return Wait(()=>restoreDone,"cinematic restore completion");
        Check(Near(Time.fixedDeltaTime,fixedStep),"cinematic restore completes with correct physics step");

        yield return Open();Shield.ResetAfterRespawn(0);Shield.TakeDamage(1000);
        typeof(PlayerShield).GetField("emergencyGraceTimer",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(Shield,0f);Shield.TakeDamage(1);
        Check(!Input.IsWeaponWheelOpen&&!Input.InputEnabled,"actual death locks input and cancels active wheel");SetKeys();yield return Ready();
        Check(Near(Time.timeScale,1)&&Near(Time.fixedDeltaTime,fixedStep),"death respawn does not inherit wheel slowdown");

        yield return Open();var floor=UnityEngine.Object.FindAnyObjectByType<FloorManager>();Invoke(floor,"FloorClear");SetKeys();
        yield return Wait(()=>UI.ChoosingPower,"floor reward after wheel open");
        Check(!Input.IsWeaponWheelOpen&&Near(Time.timeScale,0),"floor transition reward owns pause after cancelling wheel");UI.ChoosePower(0);
        yield return Wait(()=>UI.Run.FloorCandidates.Count==3,"floor candidates");
        Invoke(UnityEngine.Object.FindAnyObjectByType<FloorTransitionManager>(),"FloorCandidateSelected",UI.Run.FloorCandidates[0]);yield return Ready();
        Check(UI.Run.CurrentFloor==2&&Near(Time.fixedDeltaTime,fixedStep),"next floor restores normal speed and physics");

        yield return Open();RunRestart.Restart();SetKeys();yield return Wait(()=>UI!=null&&!UI.HasStarted,"restart from open wheel");yield return Delay(.1f);
        Check(Near(Time.timeScale,0)&&Near(Time.fixedDeltaTime,fixedStep),"restart from wheel stays at menu pause, not stale slow");
        UI.StartRun(RunMode.TwentyFloor);yield return Ready();yield return Open();UI.ShowResult(false);SetKeys();yield return Delay(.1f);
        Check(UI.HasFinished&&!Input.IsWeaponWheelOpen&&Near(Time.timeScale,0),"result screen cancels wheel and retains pause");
        Check(Near(Time.fixedDeltaTime,fixedStep),"result screen restores wheel-owned physics step");
        RunRestart.Restart();yield return Wait(()=>UI!=null&&!UI.HasStarted,"restart after result");yield return Delay(.1f);
        Check(Near(Time.timeScale,0)&&Near(Time.fixedDeltaTime,fixedStep),"result restart has no lingering time ownership");
        Check(errors==0,"wheel lifecycle validation has no runtime errors");
    }
    private static void Tick()
    {
        if(!SessionState.GetBool(Active,false)||!EditorApplication.isPlaying)return;
        try
        {
            if(routines==null){routines=new Stack<IEnumerator>();routines.Push(Run());}
            for(int i=0;i<100&&routines.Count>0;i++)
            {
                var top=routines.Peek();if(!top.MoveNext()){routines.Pop();continue;}
                if(top.Current is IEnumerator nested){routines.Push(nested);continue;}return;
            }
            if(routines.Count==0){Debug.Log("TOWER_WHEEL_VALIDATION_PASSED checks="+checks);Finish(0);}
        }
        catch(Exception ex){Debug.LogError("TOWER_WHEEL_VALIDATION_FAILED checks="+checks+" "+ex);Finish(1);}
    }
    private static void Finish(int code){SessionState.SetBool(Active,false);InputSystem.onAfterUpdate-=Feed;SetKeys();PlayerWeaponInputController.CancelActiveWeaponWheel();if(keyboard!=null)InputSystem.RemoveDevice(keyboard);if(mouse!=null)InputSystem.RemoveDevice(mouse);EditorApplication.Exit(code);}
}
