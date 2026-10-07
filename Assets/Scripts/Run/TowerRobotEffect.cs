using UnityEngine;

// World-space, scaled-time combat feedback. Damage is instantaneous, so beams appear on the hit frame.
public sealed class TowerRobotEffect : MonoBehaviour
{
    public string Kind { get; private set; }
    public LineRenderer Line { get; private set; }
    private float age,radius;
    private Vector2 center;
    private Color color;
    private bool ring,inward;
    private const float Lifetime=.32f;
    private static TowerRobotEffect Create(Transform owner,Material material,int layer,string kind,Color color)
    {
        var go=new GameObject(kind);go.transform.SetParent(owner,false);
        var effect=go.AddComponent<TowerRobotEffect>();effect.Kind=kind;effect.color=color;
        var line=effect.Line=go.AddComponent<LineRenderer>();line.useWorldSpace=true;line.sharedMaterial=material;
        line.sortingLayerID=layer;line.sortingOrder=120;line.startColor=line.endColor=color;
        line.widthMultiplier=.09f;line.numCapVertices=4;line.numCornerVertices=3;
        Destroy(go,Lifetime);return effect;
    }
    public static void Beam(Transform owner,Material material,int layer,string kind,Vector2 from,Vector2 to,Color color,bool arc)
    {
        var effect=Create(owner,material,layer,kind,color);var line=effect.Line;
        line.positionCount=arc?7:2;
        Vector2 side=new Vector2(-(to-from).y,(to-from).x).normalized*.12f;
        for(int i=0;i<line.positionCount;i++)line.SetPosition(i,Vector2.Lerp(from,to,(float)i/(line.positionCount-1))+(i>0&&i<line.positionCount-1?side*(i%2==0?1:-1):Vector2.zero));
    }
    public static void Ring(Transform owner,Material material,int layer,string kind,Vector2 center,float radius,Color color,bool inward)
    {
        var effect=Create(owner,material,layer,kind,color);effect.ring=true;effect.inward=inward;effect.center=center;effect.radius=radius;
        effect.Line.loop=true;effect.Line.positionCount=48;effect.DrawRing(0);
    }
    private void DrawRing(float t)
    {
        float size=radius*(inward?Mathf.Lerp(1,.15f,t):Mathf.Lerp(1,1.12f,t));
        for(int i=0;i<48;i++){float angle=i*Mathf.PI/24;Line.SetPosition(i,center+new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*size);}
    }
    private void Update()
    {
        age+=Time.deltaTime;float t=Mathf.Clamp01(age/Lifetime);
        var faded=color;faded.a=1-t;Line.startColor=Line.endColor=faded;Line.widthMultiplier=Mathf.Lerp(.1f,.025f,t);
        if(ring)DrawRing(t);
    }
}
