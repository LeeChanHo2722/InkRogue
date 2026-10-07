using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

[InitializeOnLoad]
public static class TowerInputValidation
{
 private const string Active="TowerInputValidation.Active";
 private static int phase,errors,feeds;
 private static double deadline;
 private static float startTime,inkBefore;
 private static Vector3 startPosition;
 private static EnemyHealth shotTarget;
 private static double playerDamageBefore;
 private static double startReal;
 private static float pausedElapsed;
 private static Keyboard keyboard;
 private static Mouse mouse;
 static TowerInputValidation(){EditorApplication.update+=Tick;Application.logMessageReceived+=Log;}
 public static void Start(){EditorSceneManager.OpenScene("Assets/Scenes/BootstrapScene.unity",OpenSceneMode.Single);SessionState.SetBool(Active,true);EditorApplication.isPlaying=true;}
 private static void Log(string s,string stack,LogType t){if(SessionState.GetBool(Active,false)&&(t==LogType.Error||t==LogType.Exception))errors++;}
 private static void Check(bool ok,string message){if(!ok)throw new Exception(message);Debug.Log("INPUT_PASS: "+message);}
 private static void Next(){phase++;startTime=Time.time;startReal=EditorApplication.timeSinceStartup;deadline=EditorApplication.timeSinceStartup+60;}
 private static void Feed()
 {
  if(!SessionState.GetBool(Active,false)||!EditorApplication.isPlaying||keyboard==null||mouse==null)return;
  if(InputState.currentUpdateType!=InputUpdateType.Dynamic)return;
  feeds++;
  keyboard.MakeCurrent();mouse.MakeCurrent();
  InputState.Change(keyboard,new KeyboardState(phase==1?new[]{Key.D}:phase==5?new[]{Key.LeftShift}:phase==7||phase==9?new[]{Key.Escape}:Array.Empty<Key>()));
  var player=UnityEngine.Object.FindAnyObjectByType<PlayerMovement>();
  Vector2 aim=player!=null&&Camera.main!=null?(Vector2)Camera.main.WorldToScreenPoint(player.transform.position+Vector3.right*3):new Vector2(600,400);
  var state=new MouseState{position=aim};
  if(phase==2)state=state.WithButton(MouseButton.Left);
  if(phase==3)state=state.WithButton(MouseButton.Right);
  InputState.Change(mouse,state);
 }
 private static void Tick()
 {
  if(!SessionState.GetBool(Active,false)||!EditorApplication.isPlaying)return;
  try
  {
   if(deadline==0)deadline=EditorApplication.timeSinceStartup+90;
   if(EditorApplication.timeSinceStartup>deadline)throw new Exception("Input timeout phase "+phase);
   var ui=TowerRunUI.Instance;if(ui==null)return;
   if(!ui.HasStarted){ui.StartRun(RunMode.TwentyFloor);return;}
   var player=UnityEngine.Object.FindAnyObjectByType<PlayerMovement>();if(player==null)return;
   player.GetComponent<PlayerShield>().ResetAfterRespawn(120);
   var ink=player.GetComponent<PlayerInkResource>();
   var floor=UnityEngine.Object.FindAnyObjectByType<FloorManager>();
   switch(phase)
   {
    case 0:
     if(floor==null||!floor.IsEncounterActive||!player.enabled||player.transform.localScale.x<0.6f||Time.timeScale<0.99f)return;
     keyboard=InputSystem.AddDevice<Keyboard>();mouse=InputSystem.AddDevice<Mouse>();keyboard.MakeCurrent();mouse.MakeCurrent();
     InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
     InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
     InputSystem.onAfterUpdate-=Feed;InputSystem.onAfterUpdate+=Feed;
     var weapons=player.GetComponentsInChildren<PlayerWeaponBehaviour>(true).Select(b=>b.Definition).Where(w=>w!=null).ToArray();
     player.GetComponent<PlayerWeaponController>().SetWeapon(WeaponSlotSide.Right,weapons.First(w=>w.Type==WeaponType.Shooter));
     player.GetComponent<PlayerWeaponController>().SetWeapon(WeaponSlotSide.Left,weapons.First(w=>w.Type==WeaponType.SplashBomb));
     startPosition=player.transform.position;Next();break;
    case 1:
     if(Time.time-startTime<0.35f)return;
     Debug.Log("INPUT_DEBUG feeds="+feeds+" key="+keyboard.dKey.isPressed+" current="+(Keyboard.current==keyboard)+" enabled="+player.enabled+" velocity="+player.GetComponent<Rigidbody2D>().linearVelocity+" distance="+Vector3.Distance(startPosition,player.transform.position));
     Check(Vector3.Distance(startPosition,player.transform.position)>0.4f,"WASD input moves the player");
     Capture("walk");
     var target=new GameObject("Input QA damage target");target.transform.position=player.transform.position+Vector3.right*2;target.AddComponent<CircleCollider2D>().radius=0.3f;shotTarget=target.AddComponent<EnemyHealth>();shotTarget.ApplyFloorHealthMultiplier(100);Physics2D.SyncTransforms();
     playerDamageBefore=TowerPowers.Instance.PlayerDamageDealt;
     ink.FillInk();inkBefore=ink.CurrentInk;Next();break;
    case 2:
     if(Time.time-startTime<0.7f)return;
     Debug.Log("INPUT_SHOOT_DEBUG mouse="+(Mouse.current==mouse)+" held="+mouse.leftButton.isPressed+" ink="+ink.CurrentInk+" weapon="+player.GetComponent<PlayerWeaponController>().RightWeapon);
     Check(ink.CurrentInk<inkBefore,"left mouse fires right-hand weapon and spends ink");
     Capture("shoot");
     Check(shotTarget.CurrentHealthExact<shotTarget.MaxHealth&&TowerPowers.Instance.PlayerDamageDealt>playerDamageBefore,"actual player projectile collision damages enemy");
     Check(InkMap.Instance.GetDominantInkTeam(player.transform.position+Vector3.right*0.6f,0.25f)==InkTeam.Player,"shot leaves player ink ahead");
     ink.FillInk();inkBefore=ink.CurrentInk;Next();break;
    case 3:
     if(Time.time-startTime<0.4f)return;Next();break;
    case 4:
     if(Time.time-startTime<0.8f)return;
     Check(ink.CurrentInk<inkBefore,"right mouse charges/releases left-hand weapon and spends ink");
     InkMap.Instance.PaintCircle(player.transform.position,1f,InkTeam.Player);
     TowerPowers.Instance.Add(TowerPowerId.DiveCharge);
     ink.SpendInk(60);inkBefore=ink.CurrentInk;Next();break;
    case 5:
     if(Time.time-startTime<0.8f)return;
     Check(player.GetComponent<PlayerDive>().IsDiving,"Shift enters friendly ink");
     Check(ink.CurrentInk>inkBefore+5,"diving restores ink");
     Capture("dive");
     Next();break;
    case 6:
     if(Time.time-startTime<0.2f)return;
     Check(!player.GetComponent<PlayerDive>().IsSwimForm,"releasing Shift restores human form");
     float targetBefore=shotTarget.CurrentHealthExact;shotTarget.TakeDamage(1);Check(Mathf.Abs(targetBefore-shotTarget.CurrentHealthExact-1.8f)<0.001f,"real dive exit empowers first hit");
     ValidateAudio();
     Check(errors==0,"input smoke has no runtime errors");
     Next();break;
    case 7:
     if(Time.timeScale>0)return;Check(true,"Escape pauses gameplay");Capture("pause");pausedElapsed=ui.Run.ElapsedSeconds;Next();break;
    case 8:
     if(EditorApplication.timeSinceStartup-startReal<0.25)return;Check(Mathf.Approximately(pausedElapsed,ui.Run.ElapsedSeconds),"run timer freezes while paused");Next();break;
    case 9:
     if(Time.timeScale<0.99f)return;Check(true,"Escape resumes gameplay");Check(errors==0,"final input QA has no errors");Debug.Log("TOWER_INPUT_VALIDATION_PASSED");Finish(0);break;
   }
  }
  catch(Exception ex){Debug.LogError("TOWER_INPUT_VALIDATION_FAILED phase="+phase+" "+ex);Finish(1);}
 }
 private static void ValidateAudio()
 {
  var a=GameAudioManager.Instance;var clips=new[]{a.shootSFX,a.splashBombSFX,a.enemyDeathSFX,a.cardSelectSFX,a.bossPhaseSFX,a.bossDeathSFX,a.playerHitSFX,a.emergencySFX};
  foreach(var c in clips){Check(c!=null&&c.samples>0,"audio loaded: "+(c!=null?c.name:"missing"));var data=new float[c.samples*c.channels];c.GetData(data,0);float peak=0;foreach(var x in data){CheckFinite(x);peak=Mathf.Max(peak,Mathf.Abs(x));}Check(peak>0.01f&&peak<1,"audio has signal without clipping: "+c.name);}
 }
 private static void CheckFinite(float x){if(float.IsNaN(x)||float.IsInfinity(x))throw new Exception("Invalid audio sample");}
 private static void Capture(string name)
 {
  string folder=Environment.GetEnvironmentVariable("TOWER_INPUT_CAPTURE_DIR");
  if(string.IsNullOrEmpty(folder)||SystemInfo.graphicsDeviceType==UnityEngine.Rendering.GraphicsDeviceType.Null)return;
  Directory.CreateDirectory(folder);var camera=Camera.main;
  var target=new RenderTexture(1280,720,24);var old=camera.targetTexture;var active=RenderTexture.active;
  var overlays=UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None).Where(c=>c.renderMode==RenderMode.ScreenSpaceOverlay).ToArray();
  var cameras=overlays.Select(c=>c.worldCamera).ToArray();var distances=overlays.Select(c=>c.planeDistance).ToArray();
  camera.targetTexture=target;
  foreach(var canvas in overlays){canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;}
  Canvas.ForceUpdateCanvases();camera.Render();RenderTexture.active=target;
  for(int i=0;i<overlays.Length;i++){overlays[i].renderMode=RenderMode.ScreenSpaceOverlay;overlays[i].worldCamera=cameras[i];overlays[i].planeDistance=distances[i];}
  var pixels=new Texture2D(1280,720,TextureFormat.RGB24,false);pixels.ReadPixels(new Rect(0,0,1280,720),0,0);pixels.Apply();
  File.WriteAllBytes(Path.Combine(folder,name+".png"),pixels.EncodeToPNG());
  camera.targetTexture=old;RenderTexture.active=active;UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(pixels);
 }
 private static void Finish(int code){SessionState.SetBool(Active,false);if(keyboard!=null)InputSystem.RemoveDevice(keyboard);if(mouse!=null)InputSystem.RemoveDevice(mouse);EditorApplication.Exit(code);}
}
