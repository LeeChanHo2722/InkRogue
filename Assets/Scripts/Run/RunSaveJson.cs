using Newtonsoft.Json;

// JsonUtility turns null inline classes into empty objects; persistence requires
// real nulls for "no active run" and "no selected route". This package is already
// part of the project. No runtime type metadata is accepted from save files.
public static class RunSaveJson
{
    private static readonly JsonSerializerSettings Settings = new()
    {
        TypeNameHandling = TypeNameHandling.None,
        MissingMemberHandling = MissingMemberHandling.Error,
        MaxDepth = 64,
        Culture = System.Globalization.CultureInfo.InvariantCulture
    };
    public static string ToJson(object value, bool pretty = false) => JsonConvert.SerializeObject(value, pretty ? Formatting.Indented : Formatting.None, Settings);
    public static T FromJson<T>(string text) => JsonConvert.DeserializeObject<T>(text, Settings);
}
