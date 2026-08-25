using System;

[Serializable]
public class EncounterPlan
{
    public int seed;
    public FloorDifficulty difficulty;
    public int totalQuota;
    public EncounterWavePlan[] waves = new EncounterWavePlan[3];
}

[Serializable]
public class EncounterWavePlan
{
    public int waveIndex;
    public int waveQuota;
    public EncounterProfileDefinition profile;
    public int maxAlive;
    public float refillDelay;
}
