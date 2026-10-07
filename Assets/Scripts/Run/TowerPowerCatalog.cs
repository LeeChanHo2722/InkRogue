using System;
using System.Linq;

public enum TowerPowerId { Chain, Foam, DiveCharge, Recycle, Guard, Alternating, CloseClean, Sterilize, Vacuum, Brush, Spray, Salvage, RobotArc, Nozzle, RobotCore, DiveWake, Backwash, CleaningTrail, Intercept, ReserveTank }
public enum TowerPowerOwner { Player, Robot }

[Serializable]
public sealed class TowerPowerDefinition
{
    public TowerPowerId id;
    public TowerPowerOwner owner;
    public string title;
    public string description;
    public int maxLevel = 3;
    public TowerPowerDefinition(TowerPowerId id, TowerPowerOwner owner, string title, string description)
    { this.id=id; this.owner=owner; this.title=title; this.description=description; }
}

// Data catalog: effects are implemented once in the owner's runtime, never on weapon assets.
public static class TowerPowerCatalog
{
    public static readonly TowerPowerDefinition[] All = {
        new TowerPowerDefinition(TowerPowerId.Nozzle,TowerPowerOwner.Player,"노즐 보강","이 파워를 선택할 때만 플레이어 기본 화력이 성장합니다. 다른 파워의 총 단계는 기본 화력에 영향 없음. 최대 30단계."){maxLevel=30},
        new TowerPowerDefinition(TowerPowerId.RobotCore,TowerPowerOwner.Robot,"모터 보강","이 파워를 선택할 때만 로봇 기본 공격력·공격 주기가 성장합니다. 다른 파워의 총 단계와 플레이어 화력에는 영향 없음. 최대 30단계."){maxLevel=30},
        new(TowerPowerId.DiveWake,TowerPowerOwner.Player,"부상 세척","0.6초 이상 잠수 후 부상하면 주변을 정화하고 2/3/4 피해. 반경 1.5/1.8/2.1, 재발동 4초. 추가 효과 재발동 없음."),
        new(TowerPowerId.Backwash,TowerPowerOwner.Player,"역류 세척","방어막 파괴 시 반경 2.2의 일반 적을 밀고 3/5/7 피해. 보스는 밀지 않음. 재발동 8초. 추가 효과 재발동 없음."),
        new(TowerPowerId.CleaningTrail,TowerPowerOwner.Robot,"청소 궤적","이동 중 0.4초마다 반경 0.45/0.6/0.75의 아군 잉크를 남겨 충전 경로를 만듭니다."),
        new(TowerPowerId.Intercept,TowerPowerOwner.Robot,"탄막 필터","5초마다 시야 내 반경 2.2의 적 탄환 최대 1/2/3개를 제거. 폭탄·바닥 경고는 제거하지 않음."),
        new(TowerPowerId.ReserveTank,TowerPowerOwner.Robot,"예비 탱크","플레이어가 거리 2.5 이내면 6초마다 잉크 3/5/7 회복. 다른 소유자의 기본 화력 증가 없음."),
        new(TowerPowerId.Chain,TowerPowerOwner.Player,"연쇄 살균","직접 명중 4회마다 주변 적 1/2/3명에게 35% 피해. 재발동 0.5초."),
        new(TowerPowerId.Foam,TowerPowerOwner.Player,"거품 폭발","직접 처치 시 반경 1.5/1.8/2.1에 마지막 피해의 60/80/100% 폭발. 재발동 1초."),
        new(TowerPowerId.DiveCharge,TowerPowerOwner.Player,"압축 세정","잠수 0.6초 후 나오면 4초 이내 첫 명중이 +80/120/160%. 재충전 5초."),
        new(TowerPowerId.Recycle,TowerPowerOwner.Player,"잉크 재활용","직접 처치 시 잉크 5/8/11 회수. 재발동 0.5초."),
        new(TowerPowerId.Guard,TowerPowerOwner.Player,"보호 필터","9/7/5초마다 피격 1회를 막는 필터 생성. 최대 1개."),
        new(TowerPowerId.Alternating,TowerPowerOwner.Player,"교대 압력","서로 다른 손으로 잉크를 소비하면 3초간 양손 피해 +20/35/50%."),
        new(TowerPowerId.CloseClean,TowerPowerOwner.Player,"근접 흡수","거리 1.8 이내 직접 명중 시 방어막 0.15/0.25/0.35 회복. 재발동 0.8초."),
        new(TowerPowerId.Sterilize,TowerPowerOwner.Player,"살균 촉매","아군 잉크 위의 적에게 직접 피해 +20/35/50%. 거품과 잠수 세정에 연계."),
        new(TowerPowerId.Vacuum,TowerPowerOwner.Robot,"흡입 모터","2초마다 반경 2/2.4/2.8의 적을 끌어모음. 보스는 이동하지 않음."),
        new(TowerPowerId.Brush,TowerPowerOwner.Robot,"회전 브러시","1.4초마다 반경 1.2/1.5/1.8에 로봇 공격력의 50/70/90% 피해."),
        new(TowerPowerId.Spray,TowerPowerOwner.Robot,"살균 분사","기본 공격이 목표 주변 1/1.2/1.4 범위의 최대 2/3/4명에게 45% 추가 피해."),
        new(TowerPowerId.Salvage,TowerPowerOwner.Robot,"회수 보조","로봇 직접 처치 시 플레이어 방어막 0.15/0.25/0.35 회복. 재발동 2초. 공격력 전이 없음."),
        new(TowerPowerId.RobotArc,TowerPowerOwner.Robot,"연쇄 방전","2초마다 기본 공격이 다른 적 1/2/3명에게 40% 피해. 추가 연쇄 없음.")
    };
    public static TowerPowerDefinition Get(TowerPowerId id) => All.First(p=>p.id==id);
}
