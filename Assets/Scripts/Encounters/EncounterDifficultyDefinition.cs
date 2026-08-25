using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class EncounterProfilePoolEntry
{
    public EncounterProfileDefinition profile;

    [Min(0)]
    public int weight;
}

[CreateAssetMenu(
    fileName = "EncounterDifficulty_",
    menuName = "InkRogue/Encounter Difficulty Definition")]
public class EncounterDifficultyDefinition : ScriptableObject
{
    [SerializeField]
    private FloorDifficulty difficulty;

    [Min(0)]
    [SerializeField]
    private int minTotalQuota;

    [Min(0)]
    [SerializeField]
    private int maxTotalQuota;

    [Min(0)]
    [SerializeField]
    private int baseMaxAlive;

    [Min(0f)]
    [SerializeField]
    private float minRefillDelay;

    [Min(0f)]
    [SerializeField]
    private float maxRefillDelay;

    [SerializeField]
    private List<EncounterProfilePoolEntry> profilePool =
        new List<EncounterProfilePoolEntry>();

    public FloorDifficulty Difficulty => difficulty;

    public int MinTotalQuota => minTotalQuota;

    public int MaxTotalQuota => maxTotalQuota;

    public int BaseMaxAlive => baseMaxAlive;

    public float MinRefillDelay => minRefillDelay;

    public float MaxRefillDelay => maxRefillDelay;

    public IReadOnlyList<EncounterProfilePoolEntry> ProfilePool =>
        profilePool;
}
