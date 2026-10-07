public sealed class FloorCandidate
{
    public FloorDefinition Floor { get; }

    public FloorDifficulty Difficulty { get; }

    public int EncounterSeed { get; }
    public FloorEncounterMode EncounterMode { get; }
    public int PowerOfferCount => 2 + (int)Difficulty;
    public string ObjectiveName => EncounterMode == FloorEncounterMode.Defense ? "거점 방어"
        : EncounterMode == FloorEncounterMode.Rush ? "시간 버티기" : "전원 소탕";
    public string ObjectiveDescription => EncounterMode == FloorEncounterMode.Defense ? "거점을 지키며 모든 공세를 견디세요."
        : EncounterMode == FloorEncounterMode.Rush ? "제한 시간 동안 살아남으세요." : "모든 웨이브의 적을 처치하세요.";
    public string DifficultyName => Difficulty == FloorDifficulty.Easy ? "안정" : Difficulty == FloorDifficulty.Normal ? "도전" : "위험";
    public string MapName => Floor?.Map == null ? "기본 구역" : Floor.Map.SceneName switch
    {
        "Map_Prototype" => "중앙 작업장", "Map_Combat01" => "분기 통로", "Map_Combat02" => "격리 구역",
        _ => Floor.Map.MapId
    };

    public FloorCandidate(
        FloorDefinition floor,
        FloorDifficulty difficulty,
        int encounterSeed, FloorEncounterMode? encounterMode = null)
    {
        Floor = floor;
        Difficulty = difficulty;
        EncounterSeed = encounterSeed;
        EncounterMode = encounterMode ?? (floor != null ? floor.EncounterMode : FloorEncounterMode.Elimination);
    }
}
