using Newtonsoft.Json;
using System;
using System.Collections.Generic;

public enum RunSaveStage { FloorStart = 1, PowerChoice, RouteChoice, Travel }

[Serializable] public sealed class SavedRoute
{
    [JsonProperty(Required = Required.AllowNull)]
    public string floorId;
    [JsonProperty(Required = Required.AllowNull)]
    public int difficulty, encounterMode, encounterSeed;
}

[Serializable] public sealed class SavedRun
{
    [JsonProperty(Required = Required.AllowNull)]
    public string runId, startedUtc, savedUtc, mapScene;
    [JsonProperty(Required = Required.AllowNull)]
    public long revision;
    [JsonProperty(Required = Required.AllowNull)]
    public RunMode mode;
    [JsonProperty(Required = Required.AllowNull)]
    public RunSaveStage stage;
    [JsonProperty(Required = Required.AllowNull)]
    public int floor, seed, lives, deaths, bosses, rewardedFloors, lastRewardedFloor, lastBossFloor;
    [JsonProperty(Required = Required.AllowNull)]
    public int preparedForFloor, firstEncounterSeed;
    [JsonProperty(Required = Required.AllowNull)]
    public bool hasFirstEncounterSeed;
    [JsonProperty(Required = Required.AllowNull)]
    public float elapsedSeconds, checkpointShield;
    [JsonProperty(Required = Required.AllowNull)]
    public double playerDamage, robotDamage;
    [JsonProperty(Required = Required.AllowNull)]
    public string[] inventory, left, right;
    [JsonProperty(Required = Required.AllowNull)]
    public int[] powerLevels, powerOffers;
    [JsonProperty(Required = Required.AllowNull)]
    public SavedRoute selected;
    [JsonProperty(Required = Required.AllowNull)]
    public SavedRoute[] candidates, recentRoutes;
}

[Serializable] public sealed class RunHistoryEntry
{
    [JsonProperty(Required = Required.AllowNull)]
    public string result, endedUtc;
    [JsonProperty(Required = Required.AllowNull)]
    public SavedRun run;
}

[Serializable] public sealed class RunSaveBook
{
    [JsonProperty(Required = Required.AllowNull)]
    public int schema;
    [JsonProperty(Required = Required.AllowNull)]
    public long revision;
    [JsonProperty(Required = Required.AllowNull)]
    public SavedRun active;
    [JsonProperty(Required = Required.AllowNull)]
    public List<RunHistoryEntry> history;
    public static RunSaveBook Empty() => new() { schema = 1, history = new() };
}
