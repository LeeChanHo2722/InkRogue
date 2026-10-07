using UnityEngine;

public enum TowerBossPattern { Sludge, Duct, Compactor, Core }

[CreateAssetMenu(menuName="InkRogue/Tower Boss")]
public sealed class TowerBossDefinition : ScriptableObject
{
    public string displayName;
    public TowerBossPattern pattern;
    public float healthMultiplier = 1f;
    public float attackGap = 1.2f;
    public float projectileSpeed = 8f;
    public float shotsPerSecond = 4f;
    public float bombRadius = 1.25f;
    public float bombWarning = 0.9f;
    public int extraBombs;
    public string approvedSpriteResource;
    public string Counterplay => pattern switch
    {
        TowerBossPattern.Sludge => "발밑 경고를 유도하고 정화한 길로 이동",
        TowerBossPattern.Duct => "측면 이동 후 조준이 고정되면 옆으로 회피",
        TowerBossPattern.Compactor => "긴 경고선 밖으로 회피 · 돌진 후 반격",
        _ => "초록 방향의 탄막 틈으로 이동 · 십자 경고 회피"
    };

    public static int IndexFor(RunMode mode, int floor)
    {
        if (mode == RunMode.TwentyFloor && floor >= 20) return 3;
        int interval=mode == RunMode.Endless ? 10 : 5;
        return Mathf.Max(0, floor/interval-1)%4;
    }

    public static TowerBossDefinition For(RunManager run)
    {
        int index=IndexFor(run != null ? run.CurrentMode : RunMode.TwentyFloor,run != null ? run.CurrentFloor : 20);
        var all=Resources.LoadAll<TowerBossDefinition>("TowerCleaning/Bosses");
        foreach(var boss in all) if ((int)boss.pattern==index) return boss;
        return CreateDefault(index);
    }

    public static TowerBossDefinition CreateDefault(int index)
    {
        var d=CreateInstance<TowerBossDefinition>(); d.pattern=(TowerBossPattern)index;
        d.displayName=new[]{"오수 저장체","환풍구 군주","폐기물 압축기","타워 오염핵"}[index];
        d.healthMultiplier=new[]{1f,1.2f,1.5f,2f}[index];
        d.attackGap=new[]{1.3f,1.1f,1.6f,0.95f}[index];
        d.projectileSpeed=new[]{6f,9f,5.5f,8.5f}[index];
        d.shotsPerSecond=new[]{3f,6f,2.5f,5f}[index];
        d.bombRadius=new[]{1.25f,0.9f,1.9f,1.4f}[index];
        d.bombWarning=new[]{0.95f,0.9f,1.3f,0.85f}[index];
        d.extraBombs=new[]{2,0,1,3}[index];
        return d;
    }

    // Preserve the first boss; later bosses must survive owner-specific power growth.
    // Cap this campaign curve at floor 30; Endless retains its separate linear depth curve.
    public static float ProgressionHealth(int floor) =>
        Mathf.Pow(1f + 0.14f * Mathf.Clamp(floor - 5, 0, 25), 2f);

    public void Configure(GameObject boss,RunManager run)
    {
        var health=boss.GetComponent<BossHealth>();
        float depth=run!=null && run.CurrentMode==RunMode.Endless ? TowerEnemyScaling.Health(run.CurrentFloor) : 1f;
        health.ConfigureMaxHealth(Mathf.RoundToInt(health.maxHealth*healthMultiplier*depth*ProgressionHealth(run!=null ? run.CurrentFloor : 5)));
        var attack=boss.GetComponent<BossAttackController>();
        attack.towerPattern=pattern; attack.extraBombs=extraBombs;
        attack.phase1AttackGap=attackGap; attack.phase2AttackGap=attackGap*0.85f; attack.phase3AttackGap=attackGap*0.7f;
        attack.projectileSpeed=projectileSpeed; attack.shootFireRate=shotsPerSecond;
        attack.bombDamageRadius=bombRadius; attack.bombWarningDuration=bombWarning;
        if(run!=null && run.CurrentMode==RunMode.Endless)
        { attack.bombDamage*=TowerEnemyScaling.Damage(run.CurrentFloor); attack.projectileDamage*=TowerEnemyScaling.Damage(run.CurrentFloor); }
        boss.name=displayName;
    }
}
