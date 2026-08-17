using UnityEngine;

[CreateAssetMenu(
    fileName = "Map_",
    menuName = "InkRogue/Map Definition")]
public class MapDefinition : ScriptableObject
{
    [SerializeField]
    private string mapId;

    [SerializeField]
    private string sceneName;

    public string MapId => mapId;

    public string SceneName => sceneName;
}
