using UnityEngine;

[CreateAssetMenu(
    fileName = "FloorDefinition",
    menuName = "InkRogue/Floor Definition")]
public class FloorDefinition : ScriptableObject
{
    [Header("Legacy Wave Data")]

    public FloorWaveData floorData =
        new FloorWaveData();

    [Header("Metadata")]

    [SerializeField]
    private string floorId;

    [SerializeField]
    private FloorDifficulty difficulty =
        FloorDifficulty.Easy;

    [SerializeField]
    private EnemyCompositionDefinition enemyComposition;

    [SerializeField]
    private PowerDefinition pairedPower;

    [SerializeField]
    private MapDefinition map;

    public string FloorId => floorId;

    public FloorDifficulty Difficulty => difficulty;

    public EnemyCompositionDefinition EnemyComposition =>
        enemyComposition;

    public PowerDefinition PairedPower => pairedPower;

    public MapDefinition Map => map;
}
