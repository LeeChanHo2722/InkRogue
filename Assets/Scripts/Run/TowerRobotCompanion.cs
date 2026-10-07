using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public sealed class TowerRobotCompanion : MonoBehaviour
{
    public enum Behaviour { Searching, Pursuing, Attacking, Returning }
    public Behaviour State { get; private set; }
    public Vector2 Velocity { get; private set; }
    public int Attacks { get; private set; }
    public GameObject CurrentTarget { get; private set; }
    public const float SearchRadius = 5.5f, ReturnDistance = 6f, ReturnCompleteDistance = 1.6f;
    private const float BodyRadius = .22f, AttackRange = 2.2f;
    private TowerPowers powers;
    private SpriteRenderer visual;
    private Material effectMaterial;
    private float attackAt,brushAt,vacuumAt,arcAt,salvageAt,scanAt,roamAt,routeAt,blockedFor,roamAngle;
    private int floor;
    private float trailAt, interceptAt, reserveAt;
    private Vector2 lastTrailPosition;
    private bool wasActive;
    private Vector2 roamGoal,roamAnchor,routeGoal;
    private readonly Queue<Vector2> route = new();
    private readonly RaycastHit2D[] obstacleHits = new RaycastHit2D[64];
    private static readonly Color Gold=new(1f,.78f,.16f), Orange=new(1f,.4f,.08f), Blue=new(.2f,.9f,1f);

    public static TowerRobotCompanion Create(TowerPowers powers)
    {
        var go=new GameObject("Robot vacuum companion");
        var robot=go.AddComponent<TowerRobotCompanion>();robot.powers=powers;
        robot.visual=go.AddComponent<SpriteRenderer>();
        var form=FindAnyObjectByType<PlayerFormVisual>();
        var source=form!=null&&form.swimVisual!=null?form.swimVisual.GetComponent<SpriteRenderer>():null;
        if(source!=null){robot.visual.sprite=source.sprite;robot.visual.sortingLayerID=source.sortingLayerID;}
        robot.visual.color=Gold;robot.visual.sortingOrder=70;go.transform.localScale=new Vector3(.45f,.32f,1);
        robot.effectMaterial=new Material(Shader.Find("UI/Default"));
        if(powers.Player!=null)go.transform.position=powers.Player.position;
        return robot;
    }

    private void Update()
    {
        if(powers==null){Destroy(gameObject);return;}
        var player=powers.Player;var ui=TowerRunUI.Instance;
        bool active=player!=null&&ui!=null&&ui.HasStarted&&!ui.HasFinished&&player.GetComponent<PlayerMovement>().enabled
            &&player.GetComponent<PlayerWeaponInputController>().InputEnabled&&player.localScale.x>.1f;
        visual.enabled=active;
        if(!active){if(wasActive)ClearEffects();wasActive=false;CurrentTarget=null;Velocity=Vector2.zero;route.Clear();return;}
        if(Time.timeScale<=0)return;
        Vector2 anchor=player.position;float distance=Vector2.Distance(transform.position,anchor);
        if(!wasActive||floor!=ui.Run.CurrentFloor||distance>14f)
        {Recall(anchor);floor=ui.Run.CurrentFloor;wasActive=true;distance=0;}
        if(distance>ReturnDistance){State=Behaviour.Returning;CurrentTarget=null;}
        if(State==Behaviour.Returning)
        {
            if(distance<=ReturnCompleteDistance){State=Behaviour.Searching;scanAt=0;roamAt=0;route.Clear();}
            else
            {
                Move(anchor,6.5f,anchor);
                // Recovery only for an unreachable return route, not ordinary following.
                if(blockedFor>2.5f)Recall(anchor);
                return;
            }
        }
        if(!ValidTarget(CurrentTarget,anchor))CurrentTarget=null;
        if(Time.time>=scanAt)
        {
            scanAt=Time.time+.2f;
            if(CurrentTarget==null)CurrentTarget=TowerPowers.Targets(anchor,SearchRadius).FirstOrDefault();
        }
        if(CurrentTarget!=null)
        {
            Vector2 goal=CurrentTarget.transform.position;
            bool canAttack=Vector2.Distance(transform.position,goal)<=AttackRange&&TowerPowers.ClearLine(transform.position,goal);
            State=canAttack?Behaviour.Attacking:Behaviour.Pursuing;
            Vector2 standOff=goal+((Vector2)transform.position-goal).normalized*1.7f;
            Move(standOff,3.8f,anchor);
            Face(goal-(Vector2)transform.position);
            if(blockedFor>1.1f){CurrentTarget=null;State=Behaviour.Returning;route.Clear();return;}
            if(canAttack&&Time.time>=attackAt)Attack(player);
        }
        else
        {
            State=Behaviour.Searching;
            if(Time.time>=roamAt||Vector2.Distance(anchor,roamAnchor)>1.2f||Vector2.Distance(transform.position,roamGoal)<.25f)
            {
                roamAnchor=anchor;roamAt=Time.time+2;roamGoal=anchor;
                for(int i=0;i<8;i++)
                {
                    roamAngle+=137.5f;
                    Vector2 candidate=anchor+new Vector2(Mathf.Cos(roamAngle*Mathf.Deg2Rad),Mathf.Sin(roamAngle*Mathf.Deg2Rad))*(1.2f+(i%3)*.6f);
                    if(ClearPath(anchor,candidate)){roamGoal=candidate;break;}
                }
            }
            Move(roamGoal,2.5f,anchor);
            if(blockedFor>1.1f){State=Behaviour.Returning;route.Clear();}
        }
        Powers();
    }

    private bool ValidTarget(GameObject target,Vector2 anchor)
    {
        if(target==null||!target.activeInHierarchy||Vector2.Distance(target.transform.position,anchor)>ReturnDistance)return false;
        var enemy=target.GetComponent<EnemyHealth>();var boss=target.GetComponent<BossHealth>();
        return (enemy!=null&&enemy.CurrentHealthExact>0||boss!=null&&!boss.IsDead&&!boss.IsInvulnerable)
            &&TowerPowers.ClearLine(anchor,target.transform.position);
    }

    private void Recall(Vector2 anchor)
    {
        ClearEffects();transform.position=anchor;Velocity=Vector2.zero;route.Clear();CurrentTarget=null;
        blockedFor=0;scanAt=roamAt=routeAt=0;attackAt=Time.time+.25f;State=Behaviour.Searching;
        Ring("Robot recall",anchor,.45f,Gold,true);
    }

    private void Face(Vector2 direction)
    {
        if(direction.sqrMagnitude>.001f)
            transform.rotation=Quaternion.RotateTowards(transform.rotation,Quaternion.Euler(0,0,Mathf.Atan2(direction.y,direction.x)*Mathf.Rad2Deg-90),360*Time.deltaTime);
    }

    private void Move(Vector2 goal,float speed,Vector2 anchor)
    {
        Vector2 position=transform.position,next=goal;
        bool direct=ClearPath(position,goal);
        if(direct)route.Clear();
        else
        {
            if(Time.time>=routeAt&&(route.Count==0||Vector2.Distance(goal,routeGoal)>.7f))
            {routeAt=Time.time+.5f;routeGoal=goal;FindRoute(position,goal,anchor);}
            while(route.Count>0&&Vector2.Distance(position,route.Peek())<.15f)route.Dequeue();
            next=route.Count>0?route.Peek():position;
        }
        Vector2 delta=next-position;
        Vector2 desired=delta.normalized*Mathf.Min(speed,Mathf.Sqrt(2*12f*delta.magnitude));
        Velocity=Vector2.MoveTowards(Velocity,desired,12f*Time.deltaTime);
        Vector2 moved=Vector2.MoveTowards(position,position+Velocity*Time.deltaTime,delta.magnitude);
        if(ClearPath(position,moved))transform.position=moved;
        else{Velocity=Vector2.zero;route.Clear();}
        bool blocked=Vector2.Distance(position,goal)>.4f&&Vector2.Distance(position,transform.position)<.1f*Time.deltaTime;
        blockedFor=blocked?blockedFor+Time.deltaTime:0;
        if(State==Behaviour.Searching||State==Behaviour.Returning)Face(Velocity);
    }

    private bool ClearPath(Vector2 from,Vector2 to)
    {
        Vector2 delta=to-from;
        var filter=new ContactFilter2D{useTriggers=false};
        int count=Physics2D.CircleCast(from,BodyRadius,delta.normalized,filter,obstacleHits,delta.magnitude);
        if(count==obstacleHits.Length)return false;
        for(int i=0;i<count;i++)if(obstacleHits[i].collider.CompareTag("Obstacle"))return false;
        return true;
    }

    // Bounded local A*, adapted from the existing combat validation's body-safe route search.
    // One companion searches only its leash area; no global navigation service is needed.
    private void FindRoute(Vector2 origin,Vector2 goal,Vector2 anchor)
    {
        route.Clear();const float step=.55f;
        var open=new List<Vector2Int>{Vector2Int.zero};var costs=new Dictionary<Vector2Int,float>{{Vector2Int.zero,0}};
        var parents=new Dictionary<Vector2Int,Vector2Int>();var closed=new HashSet<Vector2Int>();
        float bound=Mathf.Max(8,Vector2.Distance(origin,anchor)+1);
        for(int n=0;n<500&&open.Count>0;n++)
        {
            var cell=open.OrderBy(c=>costs[c]+Vector2.Distance(origin+(Vector2)c*step,goal)).First();open.Remove(cell);closed.Add(cell);
            Vector2 point=origin+(Vector2)cell*step;
            if(Vector2.Distance(point,goal)<step*1.5f&&ClearPath(point,goal))
            {
                var path=new List<Vector2>{goal};
                while(cell!=Vector2Int.zero){path.Add(origin+(Vector2)cell*step);cell=parents[cell];}
                path.Reverse();foreach(var p in path)route.Enqueue(p);return;
            }
            for(int x=-1;x<=1;x++)for(int y=-1;y<=1;y++)
            {
                if(x==0&&y==0)continue;var next=cell+new Vector2Int(x,y);if(closed.Contains(next))continue;
                Vector2 nextPoint=origin+(Vector2)next*step;
                if(Vector2.Distance(nextPoint,anchor)>bound||!ClearPath(point,nextPoint))continue;
                float cost=costs[cell]+Vector2.Distance(point,nextPoint);
                if(costs.TryGetValue(next,out float old)&&old<=cost)continue;
                costs[next]=cost;parents[next]=cell;if(!open.Contains(next))open.Add(next);
            }
        }
    }

    private void Attack(Transform player)
    {
        var target=CurrentTarget;Vector2 hit=target.transform.position;
        float damage=TowerPowers.RobotDamage(powers.Level(TowerPowerId.RobotCore));
        attackAt=Time.time+TowerPowers.RobotInterval(powers.Level(TowerPowerId.RobotCore));Attacks++;
        Beam("Robot shot",transform.position,hit,Gold);Ring("Robot muzzle",transform.position,.2f,Gold);
        Ring("Robot hit",hit,.35f,Gold);
        bool killed=TowerPowers.Damage(target,damage,TowerDamageOrigin.Robot);
        int spray=powers.Level(TowerPowerId.Spray);
        if(spray>0)Area("Robot spray",hit,.8f+spray*.2f,damage*.45f,1+spray,target,Orange);
        int arc=powers.Level(TowerPowerId.RobotArc);
        if(arc>0&&Time.time>=arcAt){arcAt=Time.time+2;Area("Robot arc",hit,3f,damage*.4f,arc,target,Blue);}
        int salvage=powers.Level(TowerPowerId.Salvage);
        if(killed&&salvage>0&&Time.time>=salvageAt)
        {
            salvageAt=Time.time+2;player.GetComponent<PlayerShield>().RecoverShield(.05f+salvage*.1f);
            Beam("Robot salvage",hit,player.position,Color.green);Ring("Robot salvage",player.position,.5f,Color.green);
        }
    }

    private void Powers()
    {
        SupportPowers();
        float damage=TowerPowers.RobotDamage(powers.Level(TowerPowerId.RobotCore));int brush=powers.Level(TowerPowerId.Brush);
        if(brush>0&&Time.time>=brushAt)
        {brushAt=Time.time+1.4f;Area("Robot brush",transform.position,.9f+brush*.3f,damage*(.3f+brush*.2f),12,null,Gold);}
        int vacuum=powers.Level(TowerPowerId.Vacuum);
        if(vacuum>0&&Time.time>=vacuumAt)
        {
            vacuumAt=Time.time+2;float radius=1.6f+vacuum*.4f;
            foreach(var target in TowerPowers.Targets(transform.position,radius).Take(8))
            {
                if(target.GetComponent<BossHealth>()!=null)continue;
                Vector2 from=target.transform.position,moved=Vector2.MoveTowards(from,transform.position,.2f+vacuum*.1f);
                if(ClearPath(from,moved)){target.transform.position=moved;Beam("Robot vacuum",from,transform.position,Blue);}
            }
            Ring("Robot vacuum",transform.position,radius,Blue,true);
        }
    }

    private void SupportPowers()
    {
        int trail=powers.Level(TowerPowerId.CleaningTrail);
        Vector2 position=transform.position;
        if(trail>0&&Time.time>=trailAt&&Vector2.Distance(position,lastTrailPosition)>.2f)
        {trailAt=Time.time+.4f;lastTrailPosition=position;InkMap.Instance?.PaintCircle(position,.3f+.15f*trail,InkTeam.Player);}
        int filter=powers.Level(TowerPowerId.Intercept);
        if(filter>0&&Time.time>=interceptAt)
        {
            var projectiles=new HashSet<GameObject>();
            foreach(var hit in Physics2D.OverlapCircleAll(position,2.2f))
            {
                var boss=hit.GetComponentInParent<BossProjectile>();var enemy=hit.GetComponentInParent<EnemyProjectile>();
                GameObject shot=boss!=null?boss.gameObject:enemy!=null?enemy.gameObject:null;
                if(shot!=null&&shot.activeInHierarchy&&TowerPowers.ClearLine(position,shot.transform.position))projectiles.Add(shot);
            }
            if(projectiles.Count>0)
            {
                interceptAt=Time.time+5;
                foreach(var shot in projectiles.OrderBy(s=>((Vector2)s.transform.position-position).sqrMagnitude).Take(filter))
                {Beam("Robot intercept",position,shot.transform.position,Blue);shot.SetActive(false);Destroy(shot);}
                Ring("Robot filter",position,2.2f,Blue);
            }
        }
        int tank=powers.Level(TowerPowerId.ReserveTank);
        if(tank>0&&Time.time>=reserveAt&&powers.Player!=null&&Vector2.Distance(position,powers.Player.position)<=2.5f)
        {reserveAt=Time.time+6;powers.Player.GetComponent<PlayerInkResource>()?.RecoverInk(1+2*tank);Beam("Robot reserve",position,powers.Player.position,Color.cyan);}
    }

    private void Area(string kind,Vector2 center,float radius,float damage,int cap,GameObject exclude,Color color)
    {
        Ring(kind,center,radius,color);
        foreach(var target in TowerPowers.Targets(center,radius,exclude).Take(cap))
        {
            Vector2 hit=target.transform.position;
            TowerPowers.Damage(target,damage,TowerDamageOrigin.Robot);
            Beam(kind,center,hit,color,kind=="Robot arc");Ring("Robot hit",hit,.25f,color);
        }
    }

    private void Beam(string kind,Vector2 from,Vector2 to,Color color,bool arc=false)
        =>TowerRobotEffect.Beam(transform,effectMaterial,visual.sortingLayerID,kind,from,to,color,arc);
    private void Ring(string kind,Vector2 center,float radius,Color color,bool inward=false)
        =>TowerRobotEffect.Ring(transform,effectMaterial,visual.sortingLayerID,kind,center,radius,color,inward);
    private void ClearEffects(){foreach(var effect in GetComponentsInChildren<TowerRobotEffect>())Destroy(effect.gameObject);}
    private void OnDisable(){Velocity=Vector2.zero;CurrentTarget=null;wasActive=false;ClearEffects();}
    private void OnDestroy(){if(effectMaterial!=null)Destroy(effectMaterial);}
}
