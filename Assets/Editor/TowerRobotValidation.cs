using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class TowerRobotValidation
{
    private const string Active="TowerRobotValidation.Active";
    private static Stack<IEnumerator> routines;
    private static int checks,errors;
    private static TowerRobotCompanion Robot=>UnityEngine.Object.FindAnyObjectByType<TowerRobotCompanion>();
    private static Transform Player=>TowerPowers.Instance.Player;
    private static TowerPowers Powers=>TowerPowers.Instance;
    private static readonly HashSet<string> seen=new();
    private static readonly List<GameObject> fixtures=new();
    private static Sprite enemySprite;
    static TowerRobotValidation(){EditorApplication.update+=Tick;Application.logMessageReceived+=Log;}
    public static void Start(){EditorSceneManager.OpenScene("Assets/Scenes/BootstrapScene.unity",OpenSceneMode.Single);SessionState.SetBool(Active,true);EditorApplication.isPlaying=true;}
    private static void Log(string message,string stack,LogType type){if(SessionState.GetBool(Active,false)&&(type==LogType.Error||type==LogType.Exception))errors++;}
    private static void Check(bool ok,string text){if(!ok)throw new Exception(text);checks++;Debug.Log("ROBOT_PASS: "+text);}
    private static IEnumerator Wait(Func<bool> condition,string context,float seconds=25)
    {double end=EditorApplication.timeSinceStartup+seconds;while(!condition()){if(EditorApplication.timeSinceStartup>end)throw new Exception("Timeout: "+context+" state="+Robot?.State+" position="+Robot?.transform.position);yield return null;}}
    private static IEnumerator Delay(float seconds){float end=Time.time+seconds;yield return Wait(()=>Time.time>=end,"game delay",Mathf.Max(10,seconds*12));}
    private static bool Has(string kind)=>Robot.GetComponentsInChildren<TowerRobotEffect>().Any(e=>e.Kind==kind&&e.Line.startColor.a>.5f);
    private static EnemyHealth Target(Vector2 position,int multiplier=100)
    {
        var go=new GameObject("Robot QA target");fixtures.Add(go);go.transform.position=position;
        go.AddComponent<CircleCollider2D>().radius=.2f;var hp=go.AddComponent<EnemyHealth>();hp.ApplyFloorHealthMultiplier(multiplier);
        var sprite=go.AddComponent<SpriteRenderer>();sprite.sprite=enemySprite;sprite.color=new Color(1,.35f,.4f);sprite.sortingLayerID=Robot.GetComponent<SpriteRenderer>().sortingLayerID;sprite.sortingOrder=69;go.transform.localScale=Vector3.one*.65f;
        return hp;
    }
    private static GameObject Wall(Vector2 position,Vector2 size)
    {var go=new GameObject("Robot QA obstacle");fixtures.Add(go);go.tag="Obstacle";go.transform.position=position;go.AddComponent<BoxCollider2D>().size=size;Physics2D.SyncTransforms();return go;}
    private static void ClearFixtures(){foreach(var go in fixtures)if(go!=null)UnityEngine.Object.DestroyImmediate(go);fixtures.Clear();Physics2D.SyncTransforms();}
    private static IEnumerator Run()
    {
        yield return Wait(()=>TowerRunUI.Instance!=null,"menu");TowerRunUI.Instance.StartRun(RunMode.TwentyFloor);
        yield return Wait(()=>Robot!=null&&Player!=null&&Player.GetComponent<PlayerMovement>().enabled&&Player.GetComponent<PlayerWeaponInputController>().InputEnabled&&UnityEngine.Object.FindAnyObjectByType<FloorManager>()?.IsEncounterActive==true,"run ready");
        Player.GetComponent<PlayerShield>().ResetAfterRespawn(1200);Vector2 original=Player.position;
        enemySprite=UnityEngine.Object.FindAnyObjectByType<EnemyHealth>()?.GetComponentInChildren<SpriteRenderer>()?.sprite;
        Check(SystemInfo.graphicsDeviceType!=UnityEngine.Rendering.GraphicsDeviceType.Null,"validation uses a real graphics device");
        yield return Delay(.4f);Capture("00-search",false);
        yield return Wait(()=>Robot.State==TowerRobotCompanion.Behaviour.Pursuing,"live map pursuit",45);Capture("00-pursuit",false);
        yield return Wait(()=>Robot.Attacks>0&&Has("Robot shot"),"live map autonomous attack",45);
        Check(Powers.RobotDamageDealt>0,"autonomous robot finds and damages a live map enemy");
        Check(Has("Robot hit")&&Has("Robot muzzle"),"basic hit, muzzle and beam appear on damage frame");
        Capture("01-basic-shot");
        if(enemySprite==null)enemySprite=Robot.CurrentTarget?.GetComponentInChildren<SpriteRenderer>()?.sprite;

        Player.GetComponent<Rigidbody2D>().simulated=false;
        Vector2 returnPoint=Robot.transform.position;
        for(int i=0;i<24;i++){float a=i*Mathf.PI/12;Vector2 candidate=(Vector2)Robot.transform.position+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*7;if(!Physics2D.OverlapCircleAll(candidate,.3f).Any(c=>!c.isTrigger&&c.CompareTag("Obstacle"))){returnPoint=candidate;break;}}
        Player.position=returnPoint;yield return Wait(()=>Robot.State==TowerRobotCompanion.Behaviour.Returning,"live map return");Capture("05-return",false);
        yield return Wait(()=>Robot.State!=TowerRobotCompanion.Behaviour.Returning,"live map return completion");Capture("06-return-complete",false);
        Player.position=new Vector2(100,100);
        yield return Wait(()=>Vector2.Distance(Robot.transform.position,Player.position)<1,"teleport recovery");yield return Delay(.1f);
        var start=Robot.transform.position;yield return Delay(1.5f);
        Check(Vector2.Distance(start,Robot.transform.position)>.5f,"no-target search moves around player rather than fixed offset");
        Check(Vector2.Distance(Robot.transform.position,Player.position)<TowerRobotCompanion.ReturnDistance,"search stays within player leash");
        Player.position+=Vector3.right*2;yield return Delay(1.5f);
        Check(Vector2.Distance(Robot.transform.position,Player.position)<4,"search follows a moving player anchor");

        Vector2 anchor=Player.position;Robot.transform.position=anchor+Vector2.left;
        var target=Target(anchor+Vector2.right*4.8f);float hp=target.CurrentHealthExact;double playerDamage=Powers.PlayerDamageDealt;
        yield return Wait(()=>Robot.State==TowerRobotCompanion.Behaviour.Pursuing,"target approach");
        Check(Robot.CurrentTarget==target.gameObject,"robot acquires target inside player-centered search radius");
        Vector2 lastVelocity=Robot.Velocity;float lastGame=Time.time;float maxAcceleration=0,lastAngle=Robot.transform.eulerAngles.z,maxTurnRate=0;float end=Time.time+1;
        while(Time.time<end)
        {
            float dt=Time.time-lastGame;if(dt>.0001f){maxAcceleration=Mathf.Max(maxAcceleration,(Robot.Velocity-lastVelocity).magnitude/dt);lastVelocity=Robot.Velocity;maxTurnRate=Mathf.Max(maxTurnRate,Mathf.Abs(Mathf.DeltaAngle(lastAngle,Robot.transform.eulerAngles.z))/dt);lastAngle=Robot.transform.eulerAngles.z;lastGame=Time.time;}
            yield return null;
        }
        Check(maxAcceleration<12.5f,"movement acceleration and direction changes are bounded");
        Check(maxTurnRate<365f,"heading turns no faster than 360 degrees per game second");
        yield return Wait(()=>target.CurrentHealthExact<hp,"approach then attack");
        Check(Vector2.Distance(Robot.transform.position,target.transform.position)<2.3f,"robot approaches before firing");
        Check(Powers.PlayerDamageDealt==playerDamage,"robot attack retains independent damage ownership");
        UnityEngine.Object.DestroyImmediate(target.gameObject);yield return Delay(.25f);
        Check(Robot.CurrentTarget==null,"destroyed target is released safely");

        Player.position=Robot.transform.position+Vector3.left*7;
        yield return Wait(()=>Robot.State==TowerRobotCompanion.Behaviour.Returning,"return starts beyond leash");int attacks=Robot.Attacks;
        yield return Wait(()=>Vector2.Distance(Robot.transform.position,Player.position)<4.8f,"return crosses outer threshold");
        Check(Robot.State==TowerRobotCompanion.Behaviour.Returning,"return remains latched inside start distance");
        yield return Wait(()=>Robot.State!=TowerRobotCompanion.Behaviour.Returning,"return completes near player");
        Check(Vector2.Distance(Robot.transform.position,Player.position)<1.8f&&Robot.Attacks==attacks,"return completes at inner threshold without shooting");
        yield return Delay(.5f);Check(Robot.State==TowerRobotCompanion.Behaviour.Searching,"robot resumes nearby search after returning");

        Player.position=new Vector2(104,100);yield return Delay(.1f);Robot.transform.position=new Vector2(98.6f,100);
        var wall=Wall(new Vector2(101,100),new Vector2(.5f,4));target=Target(new Vector2(104,101));hp=target.CurrentHealthExact;attacks=Robot.Attacks;
        Check(!TowerPowers.ClearLine(Robot.transform.position,target.transform.position),"fixture wall blocks direct attack sight");
        float until=Time.time+1;bool crossed=false;
        while(Time.time<until){crossed|=wall.GetComponent<Collider2D>().OverlapPoint(Robot.transform.position);yield return null;}
        Check(target.CurrentHealthExact==hp,"robot does not shoot through wall");
        float detour=0;yield return Wait(()=>{crossed|=Physics2D.OverlapCircleAll(Robot.transform.position,.2f).Any(c=>c.gameObject==wall);detour=Mathf.Max(detour,Mathf.Abs(Robot.transform.position.y-100));return target.CurrentHealthExact<hp;},"body-safe route around wall",20);
        Check(!crossed&&detour>2.15f,"robot body navigates around obstacle without crossing its collider");
        ClearFixtures();

        anchor=Player.position;Robot.transform.position=anchor+Vector2.left*4;
        Vector2 trapped=Robot.transform.position;
        Wall(trapped+Vector2.up*.65f,new Vector2(1.5f,.2f));Wall(trapped+Vector2.down*.65f,new Vector2(1.5f,.2f));
        Wall(trapped+Vector2.left*.65f,new Vector2(.2f,1.5f));Wall(trapped+Vector2.right*.65f,new Vector2(.2f,1.5f));
        yield return Wait(()=>Vector2.Distance(Robot.transform.position,Player.position)<1,"unreachable return recovery",15);
        Check(true,"enclosed robot recovers instead of remaining permanently stuck");ClearFixtures();
        var outside=Target((Vector2)Player.position+Vector2.right*8);
        yield return Delay(.4f);Check(Robot.CurrentTarget==null,"target outside player leash is ignored");ClearFixtures();

        Player.GetComponent<PlayerWeaponInputController>().SetInputEnabled(false);var frozen=Robot.transform.position;yield return Delay(.3f);
        Check(Robot.transform.position==frozen&&!Robot.GetComponent<SpriteRenderer>().enabled&&Robot.GetComponentsInChildren<TowerRobotEffect>().Length==0,"transition input lock hides robot and clears lingering effects");
        Player.GetComponent<PlayerWeaponInputController>().SetInputEnabled(true);TowerRunUI.Instance.Run.AdvanceFloor();Player.position=original;
        yield return Wait(()=>Vector2.Distance(Robot.transform.position,Player.position)<1,"floor relocation");Check(Robot.CurrentTarget==null||Vector2.Distance(Robot.CurrentTarget.transform.position,Player.position)<TowerRobotCompanion.ReturnDistance,"floor change relocates robot and discards stale distant targets");

        foreach(var id in new[]{TowerPowerId.Brush,TowerPowerId.Vacuum,TowerPowerId.Spray,TowerPowerId.RobotArc,TowerPowerId.Salvage})Powers.Add(id);
        // Existing sprites and the real map remain in the render; targets are stationary high-HP fixtures.
        Vector2 free=Player.position;var points=new List<Vector2>();
        for(int i=0;i<16;i++){float a=i*Mathf.PI/8;Vector2 p=free+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*1.1f;if(TowerPowers.ClearLine(free,p))points.Add(p);}
        Check(points.Count>=3,"actual map has nearby visible power targets");
        foreach(var p in points.Take(6))Target(p,1000);
        yield return Wait(()=>Has("Robot brush")&&Has("Robot vacuum"),"brush/vacuum feedback");Capture("02-brush-vacuum");
        yield return Wait(()=>Has("Robot arc")&&Has("Robot spray")&&Has("Robot shot"),"arc/spray shot feedback");Capture("03-spray-arc");
        Check(seen.Contains("Robot brush")&&seen.Contains("Robot vacuum")&&seen.Contains("Robot arc")&&seen.Contains("Robot spray"),"all offensive power types produce visible feedback");
        var victim=Robot.CurrentTarget.GetComponent<EnemyHealth>();
        typeof(EnemyHealth).GetField("currentHealth",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).SetValue(victim,.01f);
        yield return Wait(()=>Has("Robot salvage"),"salvage kill feedback",10);Capture("04-salvage");
        Check(Has("Robot salvage"),"robot kill recovery has visible player-directed feedback");
        foreach(var effect in Robot.GetComponentsInChildren<TowerRobotEffect>())Check(effect.Line.sharedMaterial.shader.isSupported&&effect.Line.sortingLayerID==Robot.GetComponent<SpriteRenderer>().sortingLayerID,"effect shader and gameplay sorting layer are valid: "+effect.Kind);

        Time.timeScale=0;frozen=Robot.transform.position;attacks=Robot.Attacks;double real=EditorApplication.timeSinceStartup;
        yield return Wait(()=>EditorApplication.timeSinceStartup-real>.2,"paused observation");
        Check(Robot.transform.position==frozen&&Robot.Attacks==attacks,"pause freezes robot movement and attacks");Time.timeScale=1;
        ClearFixtures();RunRestart.Restart();yield return Wait(()=>TowerRunUI.Instance!=null&&!TowerRunUI.Instance.HasStarted,"restart");
        Check(Robot==null&&UnityEngine.Object.FindObjectsByType<TowerRobotEffect>(FindObjectsInactive.Include).Length==0,"restart removes robot and every attack effect");
        Check(errors==0,"robot validation has no runtime errors");
    }
    private static void Capture(string name,bool requireEffect=true)
    {
        string folder=Environment.GetEnvironmentVariable("TOWER_ROBOT_CAPTURE_DIR");if(string.IsNullOrEmpty(folder))throw new Exception("Capture directory not configured");Directory.CreateDirectory(folder);
        var camera=Camera.main;var oldPosition=camera.transform.position;float oldSize=camera.orthographicSize;
        Vector2 center=Robot.transform.position;if(Robot.CurrentTarget!=null)center=(center+(Vector2)Robot.CurrentTarget.transform.position)*.5f;
        if(Robot.State==TowerRobotCompanion.Behaviour.Returning)center=(center+(Vector2)Player.position)*.5f;
        camera.transform.position=new Vector3(center.x,center.y,-10);camera.orthographicSize=Robot.State==TowerRobotCompanion.Behaviour.Returning?4.8f:3.8f;
        var render=new RenderTexture(1600,900,24);var old=camera.targetTexture;var active=RenderTexture.active;
        var overlays=UnityEngine.Object.FindObjectsByType<Canvas>().Where(c=>c.renderMode==RenderMode.ScreenSpaceOverlay).ToArray();
        var cameras=overlays.Select(c=>c.worldCamera).ToArray();var distances=overlays.Select(c=>c.planeDistance).ToArray();
        camera.targetTexture=render;foreach(var c in overlays){c.renderMode=RenderMode.ScreenSpaceCamera;c.worldCamera=camera;c.planeDistance=1;}
        Canvas.ForceUpdateCanvases();camera.Render();RenderTexture.active=render;
        var pixels=new Texture2D(1600,900,TextureFormat.RGB24,false);pixels.ReadPixels(new Rect(0,0,1600,900),0,0);pixels.Apply();File.WriteAllBytes(Path.Combine(folder,name+".png"),pixels.EncodeToPNG());
        var visible=pixels.GetPixels32();var effects=Robot.GetComponentsInChildren<TowerRobotEffect>();
        foreach(var effect in effects)effect.Line.enabled=false;
        camera.Render();RenderTexture.active=render;pixels.ReadPixels(new Rect(0,0,1600,900),0,0);pixels.Apply();var hidden=pixels.GetPixels32();int changed=0;
        for(int i=0;i<visible.Length;i++)if(Mathf.Abs(visible[i].r-hidden[i].r)+Mathf.Abs(visible[i].g-hidden[i].g)+Mathf.Abs(visible[i].b-hidden[i].b)>20)changed++;
        foreach(var effect in effects)effect.Line.enabled=true;
        for(int i=0;i<overlays.Length;i++){overlays[i].renderMode=RenderMode.ScreenSpaceOverlay;overlays[i].worldCamera=cameras[i];overlays[i].planeDistance=distances[i];}
        if(requireEffect)Check(changed>100,"rendered effect changes visible pixels: "+name+" pixels="+changed);
        camera.targetTexture=old;RenderTexture.active=active;camera.transform.position=oldPosition;camera.orthographicSize=oldSize;UnityEngine.Object.DestroyImmediate(render);UnityEngine.Object.DestroyImmediate(pixels);
        Debug.Log("ROBOT_CAPTURE: "+name);
    }
    private static void Tick()
    {
        if(!SessionState.GetBool(Active,false)||!EditorApplication.isPlaying)return;
        try
        {
            if(Robot!=null)foreach(var effect in Robot.GetComponentsInChildren<TowerRobotEffect>())seen.Add(effect.Kind);
            if(routines==null){routines=new Stack<IEnumerator>();routines.Push(Run());}
            for(int i=0;i<100&&routines.Count>0;i++)
            {var top=routines.Peek();if(!top.MoveNext()){routines.Pop();continue;}if(top.Current is IEnumerator nested){routines.Push(nested);continue;}return;}
            if(routines.Count==0){Debug.Log("TOWER_ROBOT_VALIDATION_PASSED checks="+checks);Finish(0);}
        }
        catch(Exception ex){Debug.LogError("TOWER_ROBOT_VALIDATION_FAILED checks="+checks+" "+ex);Finish(1);}
    }
    private static void Finish(int code){SessionState.SetBool(Active,false);EditorApplication.Exit(code);}
}
