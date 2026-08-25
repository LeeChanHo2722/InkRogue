public sealed class FloorCandidate
{
    public FloorDefinition Floor { get; }

    public FloorDifficulty Difficulty { get; }

    public int EncounterSeed { get; }

    public FloorCandidate(
        FloorDefinition floor,
        FloorDifficulty difficulty,
        int encounterSeed)
    {
        Floor = floor;
        Difficulty = difficulty;
        EncounterSeed = encounterSeed;
    }
}
