using UnityEngine;

public enum WeaponType
{
    None,
    Shooter,
    SplashBomb,
    Shotgun,
    Cannon,
    Shield,
    Other
}


[CreateAssetMenu(
    fileName = "Weapon_",
    menuName = "Game/Weapon Definition"
)]
public class WeaponDefinition : ScriptableObject
{
    // ==================================================
    // Identity
    // ==================================================

    [Header("Identity")]

    [SerializeField]
    private string weaponId;


    [SerializeField]
    private string displayName;


    [SerializeField]
    private WeaponType weaponType =
        WeaponType.None;


    // ==================================================
    // UI
    // ==================================================

    [Header("UI")]

    [SerializeField]
    private Sprite icon;


    // ==================================================
    // Base Stats
    //
    // 현재 전투 시스템에는 아직 직접 적용하지 않는다.
    // 추후 PlayerShoot / SplashBomb을 이쪽으로 이전할 때 사용.
    // ==================================================

    [Header("Base Stats")]

    [Min(0f)]
    [SerializeField]
    private float baseDamage = 1f;


    [Min(0f)]
    [SerializeField]
    private float baseInkCost = 1f;


    // ==================================================
    // Public
    // ==================================================

    public string WeaponId
    {
        get { return weaponId; }
    }


    public string DisplayName
    {
        get { return displayName; }
    }


    public WeaponType Type
    {
        get { return weaponType; }
    }


    public Sprite Icon
    {
        get { return icon; }
    }


    public float BaseDamage
    {
        get { return baseDamage; }
    }


    public float BaseInkCost
    {
        get { return baseInkCost; }
    }
}