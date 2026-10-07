using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

// Characterization of the pre-refactor public behavior, not a second implementation.
public static class RefactorBehaviorValidation
{
    [Serializable] private sealed class Fingerprints
    {
        public string sourceCommit, routes, powers, serializedSchema;
        public int routeSteps, powerSteps;
    }
    private const string BaselinePath = "Docs/refactor-behavior-baseline.json";
    public static void Routes()
    {
        Verify();
        FloorRouteValidation.Pure();
    }
    public static void Powers()
    {
        Verify();
        TowerPowerValidation.Start();
    }
    public static void VerifySerializedBaselineAssembly()
    {
        // Use the preserved pre-refactor player assembly, never the new source,
        // to correct the fingerprint's accidental inclusion of compiler closures.
        byte[] bytes = File.ReadAllBytes(Environment.GetEnvironmentVariable("REFACTOR_BASELINE_ASSEMBLY"));
        using var sha = SHA256.Create();
        string artifact = BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-", "");
        if (artifact != "B5A8ECBEC96906FF9A6F595F332DDBBFDD7B64C9766CF5149AA1BBB741DE68C4") throw new Exception("Wrong pre-refactor assembly");
        var original = Assembly.Load(bytes);
        string expectedSchema = SerializedSchema(original);
        string actualSchema = SerializedSchema(typeof(RunManager).Assembly);
        File.WriteAllText(Path.Combine(Path.GetTempPath(), "inkrogue-original-schema.txt"), expectedSchema);
        File.WriteAllText(Path.Combine(Path.GetTempPath(), "inkrogue-current-schema.txt"), actualSchema);
        if (expectedSchema != actualSchema) throw new Exception("Pre-refactor player and current serialized schemas differ");
        var expected = JsonUtility.FromJson<Fingerprints>(File.ReadAllText(BaselinePath));
        expected.serializedSchema = Hash(expectedSchema);
        File.WriteAllText(BaselinePath, JsonUtility.ToJson(expected, true));
        Debug.Log("REFACTOR_SERIALIZED_BASELINE_VERIFIED originalAssembly=" + artifact + " schema=" + expected.serializedSchema);
    }
    public static void Capture()
    {
        var result = Measure(); result.sourceCommit = "2207b3998ba56bdedf4af6b35d8472fd4a6c7083";
        File.WriteAllText(BaselinePath, JsonUtility.ToJson(result, true));
        Debug.Log("REFACTOR_BASELINE_CAPTURED " + JsonUtility.ToJson(result));
    }
    public static void Verify()
    {
        var expected = JsonUtility.FromJson<Fingerprints>(File.ReadAllText(BaselinePath));
        var actual = Measure();
        if (actual.routes != expected.routes) throw new Exception("Route sequence differs from pre-refactor baseline");
        if (actual.powers != expected.powers) throw new Exception("Power progression/offers differ from pre-refactor baseline");
        if (actual.serializedSchema != expected.serializedSchema) throw new Exception("Unity serialized field/enum schema changed");
        Debug.Log("REFACTOR_BEHAVIOR_PASSED routeSteps=" + actual.routeSteps + " powerSteps=" + actual.powerSteps + " serializedSchema=" + actual.serializedSchema);
    }
    private static Fingerprints Measure()
    {
        var result = new Fingerprints(); var output = new StringBuilder();
        var floors = AssetDatabase.FindAssets("t:FloorDefinition")
            .Select(g => AssetDatabase.LoadAssetAtPath<FloorDefinition>(AssetDatabase.GUIDToAssetPath(g)))
            .OrderBy(f => f.name, StringComparer.Ordinal).ToArray();
        var holder = new GameObject("Refactor characterization");
        var run = holder.AddComponent<RunManager>();
        try
        {
            foreach (var mode in new[] { RunMode.TwentyFloor, RunMode.Endless })
            for (int seed = 1; seed <= 64; seed++)
            {
                run.InitializeRun(mode, Array.Empty<WeaponDefinition>(), seed);
                for (int floor = 1; floor <= 39; floor++)
                {
                    output.Append(run.TryPrepareFloorCandidates(floors)).Append(':');
                    foreach (var candidate in run.FloorCandidates)
                        output.Append(candidate.Floor.name).Append('/').Append((int)candidate.Difficulty).Append('/').Append((int)candidate.EncounterMode).Append('/').Append(candidate.EncounterSeed).Append(';');
                    output.Append(run.IsBossFloor).Append('/').Append(run.CurrentPowerOfferCount).AppendLine();
                    run.TrySelectFloorCandidate(run.FloorCandidates[(seed + floor) % 3]);
                    run.AdvanceFloor(); result.routeSteps++;
                }
            }
            result.routes = Hash(output.ToString()); output.Clear();
            for (int seed = 1; seed <= 24; seed++)
            {
                var powers = holder.AddComponent<TowerPowers>();
                for (int step = 0; step < 120; step++)
                {
                    var offers = powers.Offers(step % 5 + 1, seed * 173 + step);
                    output.Append(string.Join(",", offers.Select(p => (int)p.id))).Append(':');
                    if (offers.Length > 0) output.Append(powers.Add(offers[(step + seed) % offers.Length].id));
                    output.Append('/').Append(powers.PlayerLevels).Append('/').Append(powers.RobotLevels).Append(':');
                    foreach (var power in TowerPowerCatalog.All) output.Append(powers.Level(power.id)).Append(',');
                    output.Append(powers.Summary(TowerPowerOwner.Player)).Append('|').Append(powers.Summary(TowerPowerOwner.Robot)).AppendLine();
                    result.powerSteps++;
                }
                foreach (var power in TowerPowerCatalog.All) output.Append(powers.Add(power.id));
                Object.DestroyImmediate(powers);
            }
            for (int level = -1; level <= 31; level++)
                output.Append(TowerPowers.PlayerGrowth(level).ToString("R", CultureInfo.InvariantCulture)).Append('/')
                    .Append(TowerPowers.RobotDamage(level).ToString("R", CultureInfo.InvariantCulture)).Append('/')
                    .Append(TowerPowers.RobotInterval(level).ToString("R", CultureInfo.InvariantCulture)).AppendLine();
            result.powers = Hash(output.ToString());
            result.serializedSchema = Hash(SerializedSchema(typeof(RunManager).Assembly));
            return result;
        }
        finally { Object.DestroyImmediate(holder); }
    }
    private static string SerializedSchema(Assembly assembly)
    {
        var output = new StringBuilder();
        foreach (var type in assembly.GetTypes().OrderBy(t => t.FullName, StringComparer.Ordinal))
        {
            // Bundled TMP sample debug tools declare editor-only public fields.
            // They are not game schema and differ between Editor and player builds.
            if (type.Namespace == "TMPro.Examples") continue;
            // Lambda/cache classes can be marked Serializable by the compiler;
            // they are implementation details, not Unity's authored field schema.
            if (type.IsDefined(typeof(CompilerGeneratedAttribute), false)) continue;
            if (type.IsEnum)
            {
                output.Append(type.FullName).Append(':');
                foreach (var value in Enum.GetValues(type)) output.Append(value).Append('=').Append(Convert.ToInt64(value)).Append(';');
                output.AppendLine(); continue;
            }
            if (!typeof(MonoBehaviour).IsAssignableFrom(type) && !typeof(ScriptableObject).IsAssignableFrom(type) && !type.IsDefined(typeof(SerializableAttribute), false)) continue;
            output.AppendLine(type.FullName);
            foreach (var field in type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly).OrderBy(f => f.Name, StringComparer.Ordinal))
                if (!field.IsInitOnly && !field.IsNotSerialized && (field.IsPublic || field.IsDefined(typeof(SerializeField), false)))
                    output.Append(field.Name).Append(':').AppendLine(field.FieldType.FullName);
        }
        return output.ToString();
    }
    private static string Hash(string value)
    {
        using var hash = SHA256.Create();
        return BitConverter.ToString(hash.ComputeHash(Encoding.UTF8.GetBytes(value))).Replace("-", "");
    }
}
