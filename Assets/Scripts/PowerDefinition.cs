using UnityEngine;

[CreateAssetMenu(
    fileName = "Power_",
    menuName = "Game/Power Definition"
)]
public class PowerDefinition : ScriptableObject
{
    [Header("Identity")]

    [SerializeField]
    private string powerId;

    [SerializeField]
    private string displayName;

    [SerializeField]
    private Sprite icon;

    [Header("Classification")]

    [SerializeField]
    private PowerCategory category;

    [Tooltip("Used only when Category is Weapon.")]
    [SerializeField]
    private WeaponFamily targetFamily =
        WeaponFamily.Unassigned;

    public string PowerId => powerId;

    public string DisplayName => displayName;

    public Sprite Icon => icon;

    public PowerCategory Category => category;

    public WeaponFamily TargetFamily => targetFamily;
}
