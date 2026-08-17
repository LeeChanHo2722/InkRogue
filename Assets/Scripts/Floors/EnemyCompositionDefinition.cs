using UnityEngine;

[CreateAssetMenu(
    fileName = "EnemyComposition_",
    menuName = "InkRogue/Enemy Composition Definition")]
public class EnemyCompositionDefinition : ScriptableObject
{
    [SerializeField]
    private string compositionId;

    [Tooltip("Temporary bridge to the current Wave system.")]
    [SerializeField]
    private FloorWaveData floorData =
        new FloorWaveData();

    public string CompositionId => compositionId;

    public FloorWaveData FloorData => floorData;
}
