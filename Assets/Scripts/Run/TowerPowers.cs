using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum TowerDamageOrigin { Player, PlayerPower, Robot }

public sealed class TowerPowers : MonoBehaviour
{
    public static TowerPowers Instance { get; private set; }
    public static TowerDamageOrigin Origin { get; private set; }
    private readonly TowerPowerProgression progression = new();
    private PlayerMovement player;
    private PlayerDive dive;
    private PlayerInkResource ink;
    private PlayerShield shield;
    private float diveStarted, chargedUntil, diveReadyAt, guardReadyAt, chainReadyAt, foamReadyAt, recycleReadyAt, closeReadyAt, alternatingUntil;
    private bool wasDiving, hasGuard, hasAttackContext, hasPreviousHand;
    private WeaponSlotSide attackHand, previousHand;
    private int hits;
    private float wakeReadyAt, backwashReadyAt;
    public double PlayerDamageDealt { get; private set; }
    public double RobotDamageDealt { get; private set; }
    public int PlayerLevels => progression.TotalLevels(TowerPowerOwner.Player);
    public int RobotLevels => progression.TotalLevels(TowerPowerOwner.Robot);
    public float PlayerDamageMultiplier => PlayerGrowth(Level(TowerPowerId.Nozzle))*(Time.time<alternatingUntil ? 1f+0.05f+0.15f*Level(TowerPowerId.Alternating) : 1f);
    // Only explicitly selected Nozzle / RobotCore levels increase base damage.
    public static float PlayerGrowth(int points) => Mathf.Pow(1f+Mathf.Max(0,points)*0.27f,2f);
    public static float RobotDamage(int points) => 0.35f*Mathf.Pow(1f+Mathf.Max(0,points)*1.3f,2f);
    public static float RobotInterval(int points) => Mathf.Max(0.7f,1.4f/(1f+Mathf.Max(0,points)*0.12f));
    public int Level(TowerPowerId id) => progression.Level(id);
    public bool HasGuard => hasGuard;
    public Transform Player => player!=null ? player.transform : null;

    private void Awake()
    {
        Instance=this; Origin=TowerDamageOrigin.Player;
        player=FindAnyObjectByType<PlayerMovement>();
        if(player!=null){dive=player.GetComponent<PlayerDive>();ink=player.GetComponent<PlayerInkResource>();shield=player.GetComponent<PlayerShield>();if(shield!=null)shield.ShieldBroken+=Backwash;}
    }
    private void OnDestroy(){if(shield!=null)shield.ShieldBroken-=Backwash;if(Instance==this){Instance=null;Origin=TowerDamageOrigin.Player;}}
    public void RestoreProgress(SavedRun saved)
    {
        foreach (var p in TowerPowerCatalog.All)
            for (int i = Level(p.id); i < saved.powerLevels[(int)p.id]; i++) Add(p.id);
        PlayerDamageDealt = saved.playerDamage; RobotDamageDealt = saved.robotDamage;
    }
    public bool Add(TowerPowerId id)
    {
        if(!progression.TryAdd(id))return false;
        if(id==TowerPowerId.Guard){hasGuard=true;guardReadyAt=Time.time+11-2*Level(id);}
        return true;
    }
    public TowerPowerDefinition[] Offers(int count = 3, int? seed = null)
    {
        return progression.Offers(count, seed ?? UnityEngine.Random.Range(1, int.MaxValue));
    }
    public string Summary(TowerPowerOwner owner) => progression.Summary(owner);
    public void Service(){ink?.FillInk();if(shield!=null)shield.RecoverShield(shield.MaxShield);}
    private void Update()
    {
        if(Time.timeScale<=0||dive==null)return;
        if(!player.enabled || (TowerRunUI.Instance!=null && TowerRunUI.Instance.HasFinished)){wasDiving=false;return;}
        bool diving=dive.IsDiving;
        if(diving&&!wasDiving)diveStarted=Time.time;
        if(!diving&&wasDiving&&Time.time-diveStarted>=0.6f&&Time.time>=diveReadyAt&&Level(TowerPowerId.DiveCharge)>0)
        {chargedUntil=Time.time+4;diveReadyAt=Time.time+5;}
        if(!diving&&wasDiving)DiveWake(Time.time-diveStarted);
        wasDiving=diving;
        if(Level(TowerPowerId.Guard)>0&&!hasGuard&&Time.time>=guardReadyAt)hasGuard=true;
    }
    private void DiveWake(float duration)
    {
        int level=Level(TowerPowerId.DiveWake);
        if(level==0||duration<.6f||Time.time<wakeReadyAt||player==null)return;
        wakeReadyAt=Time.time+4;
        float radius=1.2f+.3f*level;
        InkMap.Instance?.PaintCircle(player.transform.position,radius,InkTeam.Player);
        Area(player.transform.position,radius,1+level,12,null,TowerDamageOrigin.PlayerPower);
    }
    private void Backwash()
    {
        int level=Level(TowerPowerId.Backwash);
        if(level==0||Time.time<backwashReadyAt||player==null)return;
        backwashReadyAt=Time.time+8;
        Vector2 center=player.transform.position;
        foreach(var target in Targets(center,2.2f))
        {
            if(target.GetComponent<BossHealth>()==null)
            {
                Vector2 from=target.transform.position;
                Vector2 to=from+(from-center).normalized*(.4f+.15f*level);
                if(ClearLine(from,to)&&Physics2D.OverlapCircleAll(to,.3f).All(c=>c.isTrigger||!c.CompareTag("Obstacle")))target.transform.position=to;
            }
            Damage(target,1+2*level,TowerDamageOrigin.PlayerPower);
        }
        TowerPowerPulse.Show(center,2.2f,Color.cyan);
    }
    public bool AbsorbHit()
    {
        if(!hasGuard)return false;
        hasGuard=false;guardReadyAt=Time.time+11-2*Level(TowerPowerId.Guard);
        if(Player!=null)TowerPowerPulse.Show(Player.position,0.7f,Color.cyan);return true;
    }
    public void BeginAttack(WeaponSlotSide hand){hasAttackContext=true;attackHand=hand;}
    public void EndAttack(){hasAttackContext=false;}
    public void InkSpent(float amount)
    {
        if(!hasAttackContext||amount<=0)return;
        if(hasPreviousHand&&previousHand!=attackHand&&Level(TowerPowerId.Alternating)>0)alternatingUntil=Time.time+3;
        previousHand=attackHand;hasPreviousHand=true;
    }
    public float ModifyHit(Vector2 position,float damage)
    {
        if(Origin!=TowerDamageOrigin.Player)return damage;
        if(chargedUntil>Time.time){chargedUntil=0;damage*=1.4f+0.4f*Level(TowerPowerId.DiveCharge);TowerPowerPulse.Show(position,1f,Color.cyan);}
        int catalyst=Level(TowerPowerId.Sterilize);
        if(catalyst>0&&InkMap.Instance!=null&&InkMap.Instance.GetDominantInkTeam(position,0.15f)==InkTeam.Player)damage*=1.05f+0.15f*catalyst;
        return damage;
    }
    public void Hit(GameObject target,float applied,bool killed)
    {
        if(Origin==TowerDamageOrigin.Robot){RobotDamageDealt+=applied;return;}
        PlayerDamageDealt+=applied;
        if(Origin!=TowerDamageOrigin.Player||player==null)return;
        Vector2 position=target.transform.position;hits++;
        int chain=Level(TowerPowerId.Chain);
        if(chain>0&&hits%4==0&&Time.time>=chainReadyAt)
        {chainReadyAt=Time.time+0.5f;Area(position,3f,applied*0.35f,chain,target,TowerDamageOrigin.PlayerPower);}
        int close=Level(TowerPowerId.CloseClean);
        if(close>0&&Vector2.Distance(position,player.transform.position)<=1.8f&&Time.time>=closeReadyAt)
        {closeReadyAt=Time.time+0.8f;shield?.RecoverShield(0.05f+close*0.1f);}
        if(!killed)return;
        int recycle=Level(TowerPowerId.Recycle);
        if(recycle>0&&Time.time>=recycleReadyAt){recycleReadyAt=Time.time+0.5f;ink?.RecoverInk(2+3*recycle);}
        int foam=Level(TowerPowerId.Foam);
        if(foam>0&&Time.time>=foamReadyAt)
        {foamReadyAt=Time.time+1;float radius=1.2f+0.3f*foam;InkMap.Instance?.PaintCircle(position,radius,InkTeam.Player);Area(position,radius,applied*(0.4f+0.2f*foam),12,target,TowerDamageOrigin.PlayerPower);}
    }
    public static bool ClearLine(Vector2 from,Vector2 to)
    {
        foreach(var hit in Physics2D.LinecastAll(from,to))if(hit.collider!=null&&!hit.collider.isTrigger&&hit.collider.CompareTag("Obstacle"))return false;
        return true;
    }
    public static List<GameObject> Targets(Vector2 position,float radius,GameObject exclude=null)
    {
        var targets=new HashSet<GameObject>();
        foreach(var c in Physics2D.OverlapCircleAll(position,radius))
        {
            var enemy=c.GetComponentInParent<EnemyHealth>();var boss=c.GetComponentInParent<BossHealth>();
            GameObject obj=enemy!=null&&enemy.CurrentHealthExact>0?enemy.gameObject:boss!=null&&!boss.IsDead&&!boss.IsInvulnerable?boss.gameObject:null;
            if(obj!=null&&obj!=exclude&&ClearLine(position,obj.transform.position))targets.Add(obj);
        }
        return targets.OrderBy(t=>((Vector2)t.transform.position-position).sqrMagnitude).ToList();
    }
    public static bool Damage(GameObject target,float damage,TowerDamageOrigin origin)
    {
        if(target==null||damage<=0)return false;
        var old=Origin;Origin=origin;
        try
        {
            var enemy=target.GetComponent<EnemyHealth>();if(enemy!=null){bool alive=enemy.CurrentHealthExact>0;enemy.TakeDamage(damage);return alive&&enemy.CurrentHealthExact<=0;}
            var boss=target.GetComponent<BossHealth>();if(boss!=null){bool alive=!boss.IsDead;boss.TakeDamage(damage);return alive&&boss.IsDead;}
            return false;
        }
        finally{Origin=old;}
    }
    public static void Area(Vector2 position,float radius,float damage,int cap,GameObject exclude,TowerDamageOrigin origin)
    {
        foreach(var target in Targets(position,radius,exclude).Take(cap))Damage(target,damage,origin);
        TowerPowerPulse.Show(position,radius,origin==TowerDamageOrigin.Robot?new Color(1f,0.75f,0.2f):Color.cyan);
    }
}

public sealed class TowerPowerPulse : MonoBehaviour
{
    private LineRenderer line;private float age;
    public static void Show(Vector2 position,float radius,Color color)
    {
        var go=new GameObject("Power pulse");go.transform.position=position;var pulse=go.AddComponent<TowerPowerPulse>();
        pulse.line=go.AddComponent<LineRenderer>();pulse.line.useWorldSpace=false;pulse.line.loop=true;pulse.line.positionCount=24;
        pulse.line.widthMultiplier=0.045f;pulse.line.sortingOrder=110;pulse.line.startColor=pulse.line.endColor=color;
        pulse.line.material=new Material(Shader.Find("Sprites/Default"));
        for(int i=0;i<24;i++){float angle=i*Mathf.PI/12;pulse.line.SetPosition(i,new Vector3(Mathf.Cos(angle)*radius,Mathf.Sin(angle)*radius,0));}
        Destroy(go,0.25f);
    }
    private void Update(){age+=Time.deltaTime;if(line!=null){var c=line.startColor;c.a=Mathf.Clamp01(1-age/0.25f);line.startColor=line.endColor=c;}}
    private void OnDestroy(){if(line!=null)Destroy(line.material);}
}
