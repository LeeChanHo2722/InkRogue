using UnityEngine;

[CreateAssetMenu(
    fileName = "FloorDefinition",
    menuName = "InkRogue/Floor Definition")]
public class FloorDefinition : ScriptableObject
{
    public FloorWaveData floorData =
        new FloorWaveData();
}
