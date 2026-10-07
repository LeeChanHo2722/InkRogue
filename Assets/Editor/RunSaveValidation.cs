using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class RunSaveValidation
{
    private static int checks;
    private static void Check(bool ok, string text)
    { if (!ok) throw new Exception(text); checks++; Debug.Log("SAVE_PASS " + text); }
    public static SavedRun Fixture()
    {
        return new SavedRun
        {
            runId = Guid.NewGuid().ToString("N"), startedUtc = DateTime.UtcNow.ToString("O"), savedUtc = DateTime.UtcNow.ToString("O"), revision = 1,
            mode = RunMode.Endless, stage = RunSaveStage.FloorStart, floor = 21, seed = 42, lives = 2,
            checkpointShield = 5, elapsedSeconds = 124, mapScene = "Map_Prototype", inventory = new[] { "shooter", "blade" },
            left = new[] { "blade", "", "shooter" }, right = new[] { "shooter", "blade", "" },
            powerLevels = new int[TowerPowerCatalog.All.Length], powerOffers = Array.Empty<int>(), candidates = Array.Empty<SavedRoute>(), recentRoutes = Array.Empty<SavedRoute>()
        };
    }
    public static void Pure()
    {
        try
        {
            string root = Environment.GetEnvironmentVariable("INKROGUE_SAVE_TEST_PATH");
            if (string.IsNullOrEmpty(root)) throw new Exception("Explicit isolated test path required");
            root = Path.Combine(root, "store-" + Guid.NewGuid().ToString("N"));
            var store = new RunSaveStore(root); var book = store.Load();
            Check(book != null && book.active == null && !store.Blocked, "missing save opens an empty book");
            book.active = Fixture(); book.revision = 1;
            Check(store.Write(book, out _), "first checkpoint writes");
            Check(store.Load().active.runId == book.active.runId, "identity roundtrips");
            book.active.floor = 22; book.active.revision++; book.revision++;
            store.FailureInjection = point => { if (point == "after-temp") throw new IOException("injected before replace"); };
            Check(!store.Write(book, out _), "temp failure is reported");
            Check(store.Load().active.floor == 21, "orphan temp never supersedes committed main");
            File.WriteAllText(store.MainPath + ".tmp", "{unfinished");
            Check(store.Load().active.floor == 21, "partially written temp is ignored");
            store.FailureInjection = point => { if (point == "after-replace") throw new IOException("injected after replace"); };
            Check(!store.Write(book, out _), "post-replace interruption reported");
            Check(store.Load().active.floor == 22, "post-replace recovery contains complete new checkpoint");
            store.FailureInjection = null;
            File.WriteAllText(store.MainPath, "{broken");
            Check(store.Load().active.floor == 21 && store.Notice != null, "damaged main recovers good backup with notice");
            Check(store.Write(book, out _), "recovered book can be saved");
            File.WriteAllText(store.MainPath, "broken again");
            Check(store.Load().active.floor == 21, "recovery write did not put corrupt main into backup");
            Check(store.Write(book, out _), "main restored after second corruption");
            string valid = File.ReadAllText(store.MainPath);
            File.WriteAllText(store.MainPath, valid.Replace("\"format\": 1", "\"format\": 99"));
            Check(store.Load() == null && store.Blocked, "unsupported envelope is preserved even with backup");
            Check(!store.Write(book, out _), "blocked data cannot be silently overwritten");
            File.WriteAllText(store.MainPath, valid); store.Load();
            var raw = Newtonsoft.Json.Linq.JObject.Parse(RunSaveJson.ToJson(book));
            raw["schema"] = 99; WriteRaw(store.MainPath, raw.ToString());
            Check(store.Load() == null && store.Blocked, "future schema on disk blocks fallback and overwrite");
            File.WriteAllText(store.MainPath, valid); store.Load();
            raw = Newtonsoft.Json.Linq.JObject.Parse(RunSaveJson.ToJson(book));
            ((Newtonsoft.Json.Linq.JObject)raw["active"]).Remove("seed");
            string missingDirectory = Path.Combine(root, "missing-field"); Directory.CreateDirectory(missingDirectory);
            var missingStore = new RunSaveStore(missingDirectory); WriteRaw(missingStore.MainPath, raw.ToString());
            Check(missingStore.Load() == null && missingStore.Blocked, "missing required run field cannot silently default");
            var invalid = RunPersistence.Clone(book); invalid.schema = 0; Reject(invalid, "missing schema rejected");
            invalid = RunPersistence.Clone(book); invalid.schema = 8; Reject(invalid, "future schema rejected");
            invalid = RunPersistence.Clone(book); invalid.active.lives = 4; Reject(invalid, "impossible lives rejected");
            invalid = RunPersistence.Clone(book); invalid.active.powerLevels[0] = 999; Reject(invalid, "impossible power level rejected");
            invalid = RunPersistence.Clone(book); invalid.active.left[1] = invalid.active.left[0]; Reject(invalid, "duplicate same-hand weapon rejected");
            invalid = RunPersistence.Clone(book); invalid.active.stage = RunSaveStage.Travel; Reject(invalid, "travel without selected route rejected");
            invalid = RunPersistence.Clone(book); invalid.active.mode = RunMode.TwentyFloor; Reject(invalid, "campaign floor above twenty rejected");
            invalid = RunPersistence.Clone(book); invalid.active.elapsedSeconds = float.NaN; Reject(invalid, "NaN time rejected");
            var terminal = RunPersistence.Clone(book); var ended = terminal.active; ended.lives = 0;
            terminal.history.Add(new RunHistoryEntry { run = ended, result = "Defeat", endedUtc = DateTime.UtcNow.ToString("O") }); terminal.active = null;
            store.FailureInjection = point => { if (point == "after-temp") throw new IOException("end interrupted"); };
            Check(!store.Write(terminal, out _), "terminal main write interruption reported");
            var recovered = store.Load();
            Check(recovered.active == null && recovered.history.Count == 1, "durable terminal receipt prevents resurrection");
            store.FailureInjection = null; Check(store.Write(terminal, out _), "terminal retry succeeds");
            Check(store.Load().history.Count == 1, "terminal retry is idempotent by runId");
            File.WriteAllText(store.MainPath, "broken");
            recovered = store.Load();
            Check(recovered.active == null && recovered.history.Count == 1, "old active backup reconciles terminal receipt");
            File.WriteAllText(store.MainPath + ".bak", "broken");
            Check(store.Load() == null && store.Blocked, "two damaged copies block writes without deleting files");
            Check(File.Exists(store.MainPath) && File.Exists(store.MainPath + ".bak"), "unreadable files remain present");
            Check(store.ArchiveUnreadable(out _) && Directory.GetDirectories(root, "preserved-*").Length == 1, "explicit replacement preserves unreadable originals");
            Check(store.Load().active == null, "explicit replacement starts empty");
            var oldBook = RunSaveBook.Empty(); oldBook.active = Fixture(); oldBook.revision = 10;
            Check(store.Write(oldBook, out _), "replacement fixture writes");
            var replacement = RunSaveBook.Empty(); replacement.revision = 11; replacement.active = Fixture();
            replacement.history.Add(new RunHistoryEntry { run = oldBook.active, result = "Abandoned", endedUtc = DateTime.UtcNow.ToString("O") });
            store.FailureInjection = point => { if (point == "after-terminal-receipt") throw new IOException("replacement interrupted"); };
            Check(!store.Write(replacement, out _), "replacement interruption after receipt reported");
            var restoredReplacement = store.Load();
            Check(restoredReplacement.active.runId == replacement.active.runId && restoredReplacement.history.Count == 1, "replacement recovery includes new active and old history together");
            store.FailureInjection = null; Check(store.Write(replacement, out _), "replacement retry succeeds");
            File.WriteAllText(store.MainPath, "damaged");
            Check(store.Load().active.runId == replacement.active.runId, "replacement receipt cannot revive abandoned backup");
            Debug.Log("RUN_SAVE_STORE_VALIDATION_PASSED checks=" + checks);
            EditorApplication.Exit(0);
        }
        catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
    }
    private static void Reject(RunSaveBook book, string text)
    {
        bool rejected = false;
        try { RunSaveStore.Validate(book); } catch (Exception) { rejected = true; }
        Check(rejected, text);
    }
    private static void WriteRaw(string path, string payload)
    {
        using var sha = System.Security.Cryptography.SHA256.Create();
        string hash = BitConverter.ToString(sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(payload))).Replace("-", "");
        File.WriteAllText(path, RunSaveJson.ToJson(new { format = 1, payload = payload, sha256 = hash }));
    }
}
