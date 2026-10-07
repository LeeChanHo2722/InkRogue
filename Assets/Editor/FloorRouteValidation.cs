using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

[InitializeOnLoad]
public static class FloorRouteValidation
{
    private const string Active="FloorRouteValidation.Active";
    private static int phase, checks, errors, routeIndex;
    private static double deadline,ready;
    private static FloorCandidate chosen;
    private static bool inputVerified;
    private static int inputFrame;
    private static Mouse testMouse;
    private static Keyboard testKeyboard;
    private static Vector2 testCursor;
    private static InputSettings.BackgroundBehavior oldBackground;
    private static InputSettings.EditorInputBehaviorInPlayMode oldEditorInput;
    private static void Feed()
    {
        if(testMouse==null||testKeyboard==null||InputState.currentUpdateType!=InputUpdateType.Dynamic)return;
        testMouse.MakeCurrent();testKeyboard.MakeCurrent();
        InputState.Change(testMouse,new MouseState{position=testCursor}.WithButton(MouseButton.Left,phase==30 || phase==34));
        InputState.Change(testKeyboard,new KeyboardState(phase==32 ? new[]{Key.Escape} : Array.Empty<Key>()));
    }
    private static string Folder=>Environment.GetEnvironmentVariable("FLOOR_UI_CAPTURE_DIR");
    static FloorRouteValidation(){EditorApplication.update+=Tick;Application.logMessageReceived+=Log;}
    private static void Log(string s,string stack,LogType type){if(SessionState.GetBool(Active,false)&&(type==LogType.Error||type==LogType.Exception)&&!stack.Contains("UnityEditor.Search.SearchDatabase"))errors++;}
    private static void Check(bool ok,string message){if(!ok)throw new Exception(message);checks++;Debug.Log("ROUTE_PASS "+message);}
    private static object Field(object o,string name)=>o.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(o);
    private static void Invoke(object o,string name,params object[] args)=>o.GetType().GetMethod(name,BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic).Invoke(o,args);
    private static string Signature(RunManager r)=>string.Join("|",r.FloorCandidates.Select(c=>$"{c.Floor.name}:{c.Difficulty}:{c.EncounterMode}:{c.EncounterSeed}"));
    public static void Pure()
    {
        var floors=AssetDatabase.FindAssets("t:FloorDefinition").Select(g=>AssetDatabase.LoadAssetAtPath<FloorDefinition>(AssetDatabase.GUIDToAssetPath(g))).Where(f=>f.Map!=null).OrderBy(f=>f.name).ToArray();
        var go=new GameObject("Route test");var r=go.AddComponent<RunManager>();
        var b=new GameObject("Replay test").AddComponent<RunManager>();
        var counts=new Dictionary<string,int>();var sets=new HashSet<string>();bool replay=true,diverse=true,offer=true,stable=true,hasDouble=false;int repeats=0,eligible=0,uniformRepeats=0;
        for(int seed=1;seed<=1000;seed++)
        {
            r.InitializeRun(RunMode.TwentyFloor,Array.Empty<WeaponDefinition>(),seed);
            b.InitializeRun(RunMode.TwentyFloor,Array.Empty<WeaponDefinition>(),seed);
            var recent=new Queue<FloorCandidate>();
            for(int floor=1;floor<=19;floor++)
            {
                r.TryPrepareFloorCandidates(floors);UnityEngine.Random.InitState(seed*floor);UnityEngine.Random.Range(0,100);b.TryPrepareFloorCandidates(floors);
                replay &= Signature(r)==Signature(b);
                int modes=r.FloorCandidates.Select(c=>c.EncounterMode).Distinct().Count();hasDouble |= modes==2;
                diverse &= r.FloorCandidates.Select(c=>c.Floor.Map).Distinct().Count()==3 && modes>=2 && r.FloorCandidates.Select(c=>c.Difficulty).Distinct().Count()==3;
                foreach(var old in recent){eligible++;if(r.FloorCandidates.Any(c=>old.Floor==c.Floor&&old.EncounterMode==c.EncounterMode))repeats++;}
                // Counterfactual: identical maps/history, but uniform objective combinations.
                var counterfactual=new System.Random(unchecked(seed*23 ^ floor*317));int a,bMode,cMode;
                do{a=counterfactual.Next(3);bMode=counterfactual.Next(3);cMode=counterfactual.Next(3);}while(a==bMode&&bMode==cMode);
                int[] uniform={a,bMode,cMode};
                foreach(var previous in recent)
                    for(int i=0;i<3;i++)if(r.FloorCandidates[i].Floor==previous.Floor&&uniform[i]==(int)previous.EncounterMode)uniformRepeats++;
                string sig=Signature(r);var randomState=UnityEngine.Random.state;r.TryPrepareFloorCandidates(floors);stable &= sig==Signature(r)&&randomState.Equals(UnityEngine.Random.state);
                sets.Add(string.Join("|",r.FloorCandidates.Select(c=>$"{c.Floor.name}:{c.EncounterMode}")));
                foreach(var c in r.FloorCandidates){string key=$"{c.Floor.name}/{c.EncounterMode}/{c.Difficulty}";counts[key]=counts.GetValueOrDefault(key)+1;offer &= c.PowerOfferCount==2+(int)c.Difficulty;}
                var selected=r.FloorCandidates[(seed+floor)%3];r.TrySelectFloorCandidate(selected);b.TrySelectFloorCandidate(b.FloorCandidates[(seed+floor)%3]);
                recent.Enqueue(selected);if(recent.Count>2)recent.Dequeue();r.AdvanceFloor();b.AdvanceFloor();
            }
        }
        Check(replay,"1000 seeds x 19 transitions replay despite unrelated global RNG");
        Check(diverse&&hasDouble,"three maps and difficulties; duplicate objectives allowed, triple identical forbidden");
        Check(repeats>0 && repeats<(eligible*.29),"recent-pair repetition downweighted, not banned: "+repeats+"/"+eligible);
        Check(repeats<uniformRepeats*.8,"same catalog/maps/history uniform counterfactual repeats "+uniformRepeats+"/"+eligible);
        Check(offer,"difficulty pays 2/3/4 distinct power candidates, one award");
        Check(stable,"reopening offers never rerolls");
        Check(counts.Count==27 && counts.Values.Min()>700 && sets.Count>=30,"all 27 combinations and broad offer distribution");
        r.InitializeRun(RunMode.Endless,Array.Empty<WeaponDefinition>(),42);
        Check(!r.TryPrepareFloorCandidates(null)&&!r.TryPrepareFloorCandidates(Array.Empty<FloorDefinition>()),"no data returns false without stale offers");
        var invalid=ScriptableObject.CreateInstance<FloorDefinition>();
        Check(!r.TryPrepareFloorCandidates(new[]{invalid,null}),"null map/content exhausted pool returns unavailable");UnityEngine.Object.DestroyImmediate(invalid);
        Check(r.TryPrepareFloorCandidates(new[]{floors[0],null,floors[0]})&&r.FloorCandidates.Select(c=>c.EncounterMode).Distinct().Count()>=2,"one-map fallback still has at least two real objectives");
        var stale=r.FloorCandidates[0];r.InitializeRun(RunMode.Endless,Array.Empty<WeaponDefinition>(),43);
        Check(r.CurrentFloor==1 && r.RunSeed==43 && r.FloorCandidates.Count==0 && !r.TrySelectFloorCandidate(stale),"restart clears history, candidates and stale selection");
        Check(r.TryPrepareFloorCandidates(floors.Take(2).ToArray())&&r.FloorCandidates.Count==3,"two-map fallback generates three valid choices");
        var fourth=UnityEngine.Object.Instantiate(floors[0]);fourth.name="Fourth route";
        r.InitializeRun(RunMode.Endless,Array.Empty<WeaponDefinition>(),44);
        Check(r.TryPrepareFloorCandidates(floors.Concat(new[]{fourth}).ToArray())&&r.FloorCandidates.Select(c=>c.Floor).Distinct().Count()==3,"catalog of four samples three distinct definitions");
        UnityEngine.Object.DestroyImmediate(fourth);
        Check(new[]{5,10,15,20}.All(f=>RunManager.IsBossFloorFor(RunMode.TwentyFloor,f)) && !RunManager.IsBossFloorFor(RunMode.Endless,5)&&RunManager.IsBossFloorFor(RunMode.Endless,10),"campaign/endless boss cadence unchanged");
        Check(new[]{4,6,9,11,14,16,19,21}.All(f=>!RunManager.IsBossFloorFor(RunMode.TwentyFloor,f))&&RunManager.IsBossFloorFor(RunMode.Endless,40)&&!RunManager.IsBossFloorFor(RunMode.Endless,41),"boss boundary floors preserve neighboring normal floors");
        for(int available=0;available<=4;available++)
        {
            var powersObject=new GameObject("Filtered rewards");var powers=powersObject.AddComponent<TowerPowers>();
            for(int i=available;i<TowerPowerCatalog.All.Length;i++)
                for(int level=0;level<TowerPowerCatalog.All[i].maxLevel;level++)powers.Add(TowerPowerCatalog.All[i].id);
            for(int requested=2;requested<=4;requested++)
            {
                var offers=powers.Offers(requested,420);
                Check(offers.Length==Math.Min(available,requested)&&offers.Select(x=>x.id).Distinct().Count()==offers.Length&&offers.All(x=>powers.Level(x.id)<x.maxLevel),$"reward pool {available}, limit {requested}: unique eligible candidates only");
                Check(offers.Select(x=>x.id).SequenceEqual(powers.Offers(requested,420).Select(x=>x.id)),"reward seed reproducible after max-level filter");
            }
            UnityEngine.Object.DestroyImmediate(powersObject);
        }
        Directory.CreateDirectory(Folder);File.WriteAllText(Path.Combine(Folder,"distribution.txt"),"Seeds 1..1000, 19 transitions, selection (seed+floor)%3\nRecent pair repeats="+repeats+"/"+eligible+"\nUniform counterfactual="+uniformRepeats+"/"+eligible+"\nUnique ordered offer sets="+sets.Count+"\n"+string.Join("\n",counts.OrderBy(x=>x.Key).Select(x=>x.Key+"="+x.Value)));
        UnityEngine.Object.DestroyImmediate(go);UnityEngine.Object.DestroyImmediate(b.gameObject);
        Debug.Log("FLOOR_ROUTE_PURE_PASSED checks="+checks);
    }
    public static void Start(){EditorSceneManager.OpenScene("Assets/Scenes/BootstrapScene.unity",OpenSceneMode.Single);SessionState.SetBool(Active,true);EditorApplication.isPaused=false;EditorApplication.isPlaying=true;}
    private static void Next(int n){phase=n;deadline=EditorApplication.timeSinceStartup+100;}
    private static void Tick()
    {
        if(!SessionState.GetBool(Active,false)||!EditorApplication.isPlaying)return;
        try
        {
            if(deadline==0)deadline=EditorApplication.timeSinceStartup+100;
            EditorApplication.isPaused=false;
            if(EditorApplication.timeSinceStartup>deadline)throw new Exception("Timeout phase="+phase);
            var ui=TowerRunUI.Instance;if(ui==null)return;
            var run=ui.Run;var floor=UnityEngine.Object.FindAnyObjectByType<FloorManager>();
            var selection=UnityEngine.Object.FindAnyObjectByType<FloorSelectionUI>(FindObjectsInactive.Include);
            var player=UnityEngine.Object.FindAnyObjectByType<PlayerMovement>();
            if(player!=null)player.GetComponent<PlayerShield>().ResetAfterRespawn(1200);
            bool playable=floor!=null&&floor.IsEncounterActive&&player!=null&&player.enabled&&player.GetComponent<PlayerWeaponInputController>().InputEnabled;
            if(phase==0){ui.StartRun(RunMode.TwentyFloor);Next(1);return;}
            if(phase==1)
            {
                if(!playable)return;
                Check(run.CurrentFloor==1,"new run starts first floor");Invoke(floor,"FloorClear");Next(2);return;
            }
            if(phase==2)
            {
                if(!ui.ChoosingPower)return;
                Check(ui.PowerChoices.Length==run.CurrentPowerOfferCount,"real clear reward matches selected route");
                Check(ui.PowerChoices.Select(x=>x.id).Distinct().Count()==ui.PowerChoices.Length && ui.PowerChoices.Select(x=>x.owner).Distinct().Count()==2,"reward options unique and preserve both owners");
                if(routeIndex==2)Capture("05-normal-reward-three",1920,1080);
                int levels=TowerPowers.Instance.PlayerLevels+TowerPowers.Instance.RobotLevels;
                ui.ChoosePower(0);ui.ChoosePower(0);
                Check(TowerPowers.Instance.PlayerLevels+TowerPowers.Instance.RobotLevels==levels+1,"2/3/4 options award exactly one power even after duplicate click");
                Next(3);ready=EditorApplication.timeSinceStartup+.5;return;
            }
            if(phase==3)
            {
                if(!selection.IsVisible||EditorApplication.timeSinceStartup<ready)return;
                if(!inputVerified)
                {
                    var cards=(Array)Field(selection,"cards");var card=cards.GetValue(0);var button=(UnityEngine.UI.Button)card.GetType().GetField("button").GetValue(card);
                    Canvas.ForceUpdateCanvases();var point=RectTransformUtility.WorldToScreenPoint(null,button.transform.position);
                    var es=UnityEngine.EventSystems.EventSystem.current;var hits=new List<UnityEngine.EventSystems.RaycastResult>();es.RaycastAll(new UnityEngine.EventSystems.PointerEventData(es){position=point},hits);
                    Debug.Log("INPUT_DIAG point="+point+" screen="+Screen.width+"x"+Screen.height+" focus="+es.isFocused+" module="+es.currentInputModule+" hits="+string.Join(",",hits.Select(h=>h.gameObject.name+":"+h.sortingOrder)));
                    testMouse=InputSystem.AddDevice<Mouse>();testKeyboard=InputSystem.AddDevice<Keyboard>();
                    oldBackground=InputSystem.settings.backgroundBehavior;oldEditorInput=InputSystem.settings.editorInputBehaviorInPlayMode;
                    InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
                    InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
                    testCursor=point;InputSystem.onAfterUpdate+=Feed;inputFrame=Time.frameCount;Next(30);return;
                }
                int start=run.CurrentFloor;
                selection.Confirm();Check(run.CurrentFloor==start&&!selection.IsConfirming,"confirm without selection does nothing");
                Invoke(selection,"SelectCard",routeIndex%3);
                Check(selection.HighlightedIndex==routeIndex%3&&run.CurrentFloor==start,"card selects without travelling");
                if(routeIndex==0)
                {
                    Capture("01-route-1920x1080",1920,1080);Capture("02-route-1280x720",1280,720);Capture("03-route-1024x768",1024,768);Capture("04-route-2560x1080",2560,1080);
                    selection.Back();Check(selection.HighlightedIndex==-1&&selection.IsVisible,"back cancels selection without reroll");Invoke(selection,"SelectCard",0);
                    var sig=Signature(run);selection.TogglePreparation();Check(selection.IsPreparing,"weapon preparation opens");
                    var beforeLeft=run.LeftFloorLoadout.ToArray();var beforeRight=run.RightFloorLoadout.ToArray();
                    var weapons=run.RunInventory;
                    Invoke(selection,"PickWeapon",new object[]{null});for(int i=0;i<6;i++)selection.AssignSlot(i);
                    Invoke(selection,"PickWeapon",weapons[0]);
                    Check(selection.AssignSlot(0)&&!selection.AssignSlot(1),"same-hand duplicate rejected with feedback");
                    Check(selection.AssignSlot(3),"same weapon allowed in other hand");
                    selection.Confirm();Check(!selection.IsConfirming,"confirm blocked during weapon preparation");
                    Capture("06-weapon-preparation",1920,1080);
                    Check(run.LeftFloorLoadout.SequenceEqual(beforeLeft)&&run.RightFloorLoadout.SequenceEqual(beforeRight),"weapon preview does not mutate run loadout");
                    selection.Back();Check(!selection.IsPreparing&&selection.HighlightedIndex==0&&Signature(run)==sig&&run.LeftFloorLoadout.SequenceEqual(beforeLeft),"back cancels draft, retains route without reroll");
                    selection.TogglePreparation();Invoke(selection,"PickWeapon",new object[]{null});selection.AssignSlot(0);selection.TogglePreparation();Check(run.LeftFloorLoadout[0]==null,"apply commits staged loadout");
                    bool retried=false;selection.ShowUnavailable(()=>retried=true);selection.Confirm();Check(retried&&selection.IsVisible,"empty catalog has retry action without travel");Capture("09-empty-data",1920,1080);
                    var transition=UnityEngine.Object.FindAnyObjectByType<FloorTransitionManager>();selection.ShowCandidates(run.FloorCandidates,c=>Invoke(transition,"FloorCandidateSelected",c));Invoke(selection,"SelectCard",0);
                }
                chosen=run.FloorCandidates[routeIndex%3];
                if(routeIndex==0)
                {
                    // GPU captures temporarily change canvas projection; wait for overlay layout to settle.
                    inputFrame=Time.frameCount;Next(33);return;
                }
                selection.Confirm();selection.Confirm();selection.Back();selection.TogglePreparation();
                Check(!selection.IsVisible&&selection.IsConfirming,"confirmation locks duplicate, back and preparation input");
                Next(4);return;
            }
            if(phase==30 && Time.frameCount>inputFrame+2)
            {inputFrame=Time.frameCount;Next(31);return;}
            if(phase==31 && Time.frameCount>inputFrame+2)
            {
                var module=UnityEngine.EventSystems.EventSystem.current.currentInputModule as UnityEngine.InputSystem.UI.InputSystemUIInputModule;
                Debug.Log("INPUT_DIAG selected="+selection.HighlightedIndex+" runSelected="+(run.SelectedCandidate!=null)+" mouse="+testMouse.position.ReadValue()+" enabled="+testMouse.enabled+" point="+module?.point?.action?.ReadValue<Vector2>()+" click="+module?.leftClick?.action?.ReadValue<float>());
                Check(selection.HighlightedIndex==0&&run.SelectedCandidate==null,"InputSystem mouse click selects card through EventSystem without mutating run");
                inputFrame=Time.frameCount;Next(32);return;
            }
            if(phase==32 && Time.frameCount>inputFrame+2)
            {
                Check(selection.HighlightedIndex==-1&&selection.IsVisible,"InputSystem Escape cancels selected route");
                InputSystem.onAfterUpdate-=Feed;InputSystem.RemoveDevice(testMouse);InputSystem.RemoveDevice(testKeyboard);testMouse=null;testKeyboard=null;
                InputSystem.settings.backgroundBehavior=oldBackground;InputSystem.settings.editorInputBehaviorInPlayMode=oldEditorInput;
                inputVerified=true;Next(3);return;
            }
            if(phase==33 && Time.frameCount>inputFrame+2)
            {
                var button=(UnityEngine.UI.Button)Field(selection,"confirm");Canvas.ForceUpdateCanvases();
                testCursor=RectTransformUtility.WorldToScreenPoint(null,button.transform.position);
                var es=UnityEngine.EventSystems.EventSystem.current;var hits=new List<UnityEngine.EventSystems.RaycastResult>();es.RaycastAll(new UnityEngine.EventSystems.PointerEventData(es){position=testCursor},hits);
                Check(hits.Count>0&&hits[0].gameObject==button.gameObject,"settled overlay raycast hits departure button");
                Debug.Log("CONFIRM_INPUT point="+testCursor+" screen="+Screen.width+"x"+Screen.height+" interactable="+button.interactable);
                testMouse=InputSystem.AddDevice<Mouse>();testKeyboard=InputSystem.AddDevice<Keyboard>();
                oldBackground=InputSystem.settings.backgroundBehavior;oldEditorInput=InputSystem.settings.editorInputBehaviorInPlayMode;
                InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
                InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
                InputSystem.onAfterUpdate+=Feed;inputFrame=Time.frameCount;Next(34);return;
            }
            if(phase==34 && Time.frameCount>inputFrame+2){inputFrame=Time.frameCount;Next(35);return;}
            if(phase==35 && Time.frameCount>inputFrame+2)
            {
                Check(!selection.IsVisible&&selection.IsConfirming&&run.SelectedCandidate==chosen,"InputSystem mouse confirms through actual new uGUI button exactly once");
                selection.Confirm();selection.Back();selection.TogglePreparation();
                InputSystem.onAfterUpdate-=Feed;InputSystem.RemoveDevice(testMouse);InputSystem.RemoveDevice(testKeyboard);testMouse=null;testKeyboard=null;
                InputSystem.settings.backgroundBehavior=oldBackground;InputSystem.settings.editorInputBehaviorInPlayMode=oldEditorInput;
                Next(4);return;
            }
            if(phase==4)
            {
                if(!playable || run.SelectedCandidate!=chosen)return;
                Check(floor.CurrentEncounterMode==chosen.EncounterMode,"chosen objective reaches live encounter");
                Check(run.CurrentPowerOfferCount==chosen.PowerOfferCount,"live floor owns its displayed reward");
                Capture("live-floor-"+run.CurrentFloor,1280,720);
                if(routeIndex++<2){Invoke(floor,"FloorClear");Next(2);return;}
                // Exercise every mode/map combination separately through the existing transition path.
                Invoke(floor,"FloorClear");Next(5);return;
            }
            if(phase==5)
            {
                if(!ui.ChoosingPower)return;
                Check(ui.PowerChoices.Length==4,"hard clear presents four actual options");Capture("07-hard-reward",1920,1080);ui.ChoosePower(0);Next(6);return;
            }
            if(phase==6)
            {
                if(!selection.IsVisible)return;
                Check(run.CurrentFloor==4,"next route is boss floor five");Capture("08-boss-route",1920,1080);
                Invoke(selection,"SelectCard",1);chosen=run.FloorCandidates[1];selection.Confirm();Next(7);return;
            }
            if(phase==7)
            {if(!playable||run.CurrentFloor!=5)return;Invoke(floor,"FloorClear");Next(8);return;}
            if(phase==8)
            {
                var boss=UnityEngine.Object.FindAnyObjectByType<BossBattleManager>();if(boss.CurrentBossHealth==null||boss.CurrentBossHealth.IsInvulnerable)return;
                Check(run.IsBossFloor,"selected boss floor still enters boss arena");boss.CurrentBossHealth.TakeDamage(1000000);Next(9);return;
            }
            if(phase==9)
            {
                if(!ui.ChoosingPower)return;Check(run.BossesDefeated==1&&ui.PowerChoices.Length==3,"boss completion retains normal-route reward");
                RunRestart.Restart();Next(10);return;
            }
            if(phase==10)
            {
                if(ui.HasStarted)return;Check(!run.IsInitialized&&run.FloorCandidates.Count==0,"no-save restart returns fresh menu");ui.StartRun(RunMode.Endless);Next(11);return;
            }
            if(phase==11)
            {
                if(!playable)return;
                Check(run.CurrentMode==RunMode.Endless&&run.CurrentFloor==1,"new endless run resets selection and progress");
                Check(errors==0,"no runtime errors");Debug.Log("FLOOR_ROUTE_PLAY_PASSED checks="+checks);Finish(0);
            }
        }
        catch(Exception e){Debug.LogError("FLOOR_ROUTE_PLAY_FAILED "+e);Finish(1);}
    }
    public static void Capture(string name,int width,int height,Action<int,int> validate=null)
    {
        Directory.CreateDirectory(Folder);var camera=Camera.main;var render=new RenderTexture(width,height,24);var old=camera.targetTexture;var active=RenderTexture.active;
        var overlays=UnityEngine.Object.FindObjectsByType<Canvas>().Where(c=>c.renderMode==RenderMode.ScreenSpaceOverlay).ToArray();var cameras=overlays.Select(c=>c.worldCamera).ToArray();var distances=overlays.Select(c=>c.planeDistance).ToArray();
        var orders=overlays.Select(c=>c.sortingOrder).ToArray();var layers=overlays.Select(c=>c.sortingLayerID).ToArray();
        var sorted=overlays.OrderBy(c=>c.sortingOrder).ToArray();int topLayer=SortingLayer.layers.OrderBy(l=>l.value).Last().id;
        // Overlay canvases render after the world. Preserve that ordering during camera captures.
        camera.targetTexture=render;foreach(var c in overlays){c.renderMode=RenderMode.ScreenSpaceCamera;c.worldCamera=camera;c.planeDistance=1;c.sortingLayerID=topLayer;c.sortingOrder=30000+Array.IndexOf(sorted,c);}
        // Rebuild retained TMP meshes after switching capture aspect ratios in one frame.
        foreach(var graphic in UnityEngine.Object.FindObjectsByType<UnityEngine.UI.Graphic>())graphic.SetAllDirty();
        Canvas.ForceUpdateCanvases();
        foreach(var text in UnityEngine.Object.FindObjectsByType<TMP_Text>())text.ForceMeshUpdate();
        Canvas.ForceUpdateCanvases();camera.Render();RenderTexture.active=render;
        var pixels=new Texture2D(width,height,TextureFormat.RGB24,false);pixels.ReadPixels(new Rect(0,0,width,height),0,0);pixels.Apply();File.WriteAllBytes(Path.Combine(Folder,name+".png"),pixels.EncodeToPNG());
        Exception validationError=null;
        try{validate?.Invoke(width,height);}catch(Exception e){validationError=e;}
        for(int i=0;i<overlays.Length;i++){overlays[i].renderMode=RenderMode.ScreenSpaceOverlay;overlays[i].worldCamera=cameras[i];overlays[i].planeDistance=distances[i];overlays[i].sortingLayerID=layers[i];overlays[i].sortingOrder=orders[i];}
        camera.targetTexture=old;RenderTexture.active=active;UnityEngine.Object.DestroyImmediate(render);UnityEngine.Object.DestroyImmediate(pixels);
        if(validationError!=null)throw validationError;
    }
    private static void Finish(int code){SessionState.SetBool(Active,false);EditorApplication.Exit(code);}
}
