using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

// One atomic book includes both the active checkpoint and history. Uncommitted temp
// files are never candidates for recovery. A terminal receipt prevents an older
// backup from reviving a finished/abandoned run after damage to the main book.
public sealed class RunSaveStore
{
    [Serializable] private sealed class Envelope { public int format; public string payload, sha256; }
    public readonly string DirectoryPath;
    public string MainPath => Path.Combine(DirectoryPath, "runs.json");
    public string Notice { get; private set; }
    public bool Blocked { get; private set; }
    public Action<string> FailureInjection;
    private bool recovered;
    public RunSaveStore(string directory) { DirectoryPath = Path.GetFullPath(directory); }
    public static string DefaultDirectory
    {
        get
        {
            string isolated = Environment.GetEnvironmentVariable("INKROGUE_SAVE_TEST_PATH");
            return string.IsNullOrWhiteSpace(isolated) ? Path.Combine(Application.persistentDataPath, "RunSaves") : Path.GetFullPath(isolated);
        }
    }
    public RunSaveBook Load()
    {
        Notice = null; Blocked = false; recovered = false;
        RunSaveBook book = null;
        if (File.Exists(MainPath))
        {
            try { book = Read(MainPath); }
            catch (NotSupportedException e) { Blocked = true; Notice = e.Message; return null; }
            catch (Exception) { }
        }
        if (book == null && File.Exists(MainPath + ".bak"))
        {
            try { book = Read(MainPath + ".bak"); recovered = true; Notice = "백업에서 복구했습니다. 마지막 저장보다 이전 시점일 수 있습니다."; }
            catch (Exception) { }
        }
        if (book == null)
        {
            if (File.Exists(MainPath) || File.Exists(MainPath + ".bak"))
            { Blocked = true; Notice = "저장을 읽을 수 없습니다. 기존 파일은 보존됩니다. 새 런으로 교체하려면 확인이 필요합니다."; return null; }
            book = RunSaveBook.Empty();
        }
        try
        {
            if (Directory.Exists(DirectoryPath))
                foreach (string file in Directory.GetFiles(DirectoryPath, "ended-*.json"))
                {
                    var receipt = Read(file);
                    // A confirmed replacement may end one run and start another.
                    // Its durable receipt contains that complete active checkpoint.
                    if (receipt.revision > book.revision)
                    { book.active = receipt.active; book.revision = receipt.revision; }
                    foreach (var item in receipt.history)
                    {
                        if (!book.history.Any(h => h.run.runId == item.run.runId)) book.history.Add(item);
                        if (book.active?.runId == item.run.runId) book.active = null;
                    }
                }
        }
        catch (Exception) { Blocked = true; Notice = "종료 기록을 읽을 수 없습니다. 데이터 보존을 위해 새 저장을 중지했습니다."; return null; }
        return book;
    }
    public bool Write(RunSaveBook book, out string error)
    {
        error = null;
        if (Blocked) { error = Notice; return false; }
        try
        {
            Validate(book);
            Directory.CreateDirectory(DirectoryPath);
            // Terminal receipts are durable before replacing the main book.
            foreach (var entry in book.history)
            {
                string receipt = Path.Combine(DirectoryPath, "ended-" + entry.run.runId + ".json");
                if (File.Exists(receipt)) continue;
                var terminal = RunSaveBook.Empty(); terminal.revision = book.revision;
                terminal.active = book.active; terminal.history.Add(entry);
                WriteNew(receipt, Encode(terminal));
                FailureInjection?.Invoke("after-terminal-receipt");
            }
            string temp = MainPath + ".tmp";
            using (var stream = new FileStream(temp, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                var bytes = Encoding.UTF8.GetBytes(Encode(book)); stream.Write(bytes, 0, bytes.Length); stream.Flush(true);
            }
            FailureInjection?.Invoke("after-temp");
            if (File.Exists(MainPath))
            {
                // Do not replace a known good backup with the damaged primary.
                File.Replace(temp, MainPath, recovered ? null : MainPath + ".bak");
            }
            else File.Move(temp, MainPath);
            recovered = false;
            FailureInjection?.Invoke("after-replace");
            return true;
        }
        catch (Exception e) { error = "저장하지 못했습니다: " + e.Message; return false; }
    }
    public bool ArchiveUnreadable(out string error)
    {
        error = null;
        try
        {
            if (Directory.Exists(DirectoryPath))
            {
                string archive = Path.Combine(DirectoryPath, "preserved-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fffffff"));
                Directory.CreateDirectory(archive);
                foreach (string file in Directory.GetFiles(DirectoryPath)) File.Copy(file, Path.Combine(archive, Path.GetFileName(file)), false);
                // Original data is preserved above; only explicit replacement reaches here.
                foreach (string file in Directory.GetFiles(DirectoryPath)) File.Delete(file);
            }
            Blocked = false; recovered = false; Notice = null; return true;
        }
        catch (Exception e) { error = e.Message; return false; }
    }
    private static void WriteNew(string path, string text)
    {
        string temp = path + ".tmp";
        using (var stream = new FileStream(temp, FileMode.Create, FileAccess.Write, FileShare.None))
        { var bytes = Encoding.UTF8.GetBytes(text); stream.Write(bytes, 0, bytes.Length); stream.Flush(true); }
        File.Move(temp, path);
    }
    private static string Hash(string text)
    { using var sha = SHA256.Create(); return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(text))).Replace("-", ""); }
    private static string Encode(RunSaveBook book)
    { string payload = RunSaveJson.ToJson(book); return RunSaveJson.ToJson(new Envelope { format = 1, payload = payload, sha256 = Hash(payload) }, true); }
    private static RunSaveBook Read(string path)
    {
        if (new FileInfo(path).Length > 16 * 1024 * 1024) throw new InvalidDataException("Oversized save");
        var envelope = RunSaveJson.FromJson<Envelope>(File.ReadAllText(path));
        if (envelope != null && envelope.format != 0 && envelope.format != 1) throw new NotSupportedException("지원하지 않는 저장 파일 형식입니다. 파일을 보존했습니다.");
        if (envelope == null || envelope.format != 1 || string.IsNullOrEmpty(envelope.payload) || Hash(envelope.payload) != envelope.sha256)
            throw new InvalidDataException("Invalid save envelope");
        var header = Newtonsoft.Json.Linq.JObject.Parse(envelope.payload);
        int schema = header.Value<int?>("schema") ?? 0;
        if (schema != 0 && schema != 1) throw new NotSupportedException("지원하지 않는 저장 버전입니다. 파일을 보존했습니다.");
        var book = RunSaveJson.FromJson<RunSaveBook>(envelope.payload); Validate(book); return book;
    }
    public static void Validate(RunSaveBook book)
    {
        if (book == null || book.schema == 0) throw new InvalidDataException("Missing save schema");
        if (book.schema != 1) throw new NotSupportedException("지원하지 않는 저장 버전입니다. 파일을 보존했습니다.");
        if (book.history == null || book.revision < 0) throw new InvalidDataException("Missing history");
        if (book.active != null) ValidateRun(book.active, false);
        var ids = new System.Collections.Generic.HashSet<string>();
        foreach (var h in book.history)
        {
            if (h == null || !new[] { "Victory", "Defeat", "Abandoned" }.Contains(h.result) || !DateTime.TryParse(h.endedUtc, out _)) throw new InvalidDataException("Invalid history");
            ValidateRun(h.run, true);
            if (!ids.Add(h.run.runId) || h.run.runId == book.active?.runId) throw new InvalidDataException("Duplicate run");
            if (h.result == "Victory" && (h.run.mode != RunMode.TwentyFloor || h.run.floor != 20)) throw new InvalidDataException("Invalid victory");
        }
    }
    public static void ValidateRun(SavedRun r, bool terminal)
    {
        if (r == null || !Guid.TryParseExact(r.runId, "N", out _) || r.revision < 1 || !DateTime.TryParse(r.savedUtc, out _) || !DateTime.TryParse(r.startedUtc, out _)) throw new InvalidDataException("Invalid run identity");
        if (r.mode != RunMode.Endless && r.mode != RunMode.TwentyFloor || r.floor < 1 || r.floor > 1000000 || r.mode == RunMode.TwentyFloor && r.floor > 20 || !Enum.IsDefined(typeof(RunSaveStage), r.stage)) throw new InvalidDataException("Invalid run stage");
        if (r.lives < (terminal ? 0 : 1) || r.lives > 3 || r.deaths < 0 || r.bosses < 0 || r.bosses > r.floor / (r.mode == RunMode.Endless ? 10 : 5) || r.rewardedFloors < 0 || r.rewardedFloors > r.floor || r.lastRewardedFloor < 0 || r.lastRewardedFloor > r.floor || r.lastBossFloor < 0 || r.lastBossFloor > r.floor || r.lastBossFloor != 0 && !RunManager.IsBossFloorFor(r.mode, r.lastBossFloor)) throw new InvalidDataException("Invalid run counters");
        if (float.IsNaN(r.elapsedSeconds) || float.IsInfinity(r.elapsedSeconds) || r.elapsedSeconds < 0 || r.elapsedSeconds > 1e9f || r.checkpointShield <= 0 || float.IsNaN(r.checkpointShield) || float.IsInfinity(r.checkpointShield) || double.IsNaN(r.playerDamage) || double.IsInfinity(r.playerDamage) || r.playerDamage < 0 || double.IsNaN(r.robotDamage) || double.IsInfinity(r.robotDamage) || r.robotDamage < 0) throw new InvalidDataException("Invalid run stats");
        if (string.IsNullOrWhiteSpace(r.mapScene) || r.inventory == null || r.inventory.Length == 0 || r.inventory.Any(string.IsNullOrWhiteSpace) || r.inventory.Distinct().Count() != r.inventory.Length || r.left?.Length != 3 || r.right?.Length != 3) throw new InvalidDataException("Missing equipment");
        foreach (var hand in new[] { r.left, r.right })
        { var equipped = hand.Where(s => !string.IsNullOrEmpty(s)).ToArray(); if (equipped.Any(s => !r.inventory.Contains(s)) || equipped.Distinct().Count() != equipped.Length) throw new InvalidDataException("Invalid equipment"); }
        if (r.powerLevels?.Length != TowerPowerCatalog.All.Length || r.powerOffers == null || r.powerOffers.Length > 4 || r.powerOffers.Distinct().Count() != r.powerOffers.Length) throw new InvalidDataException("Missing powers");
        foreach (var p in TowerPowerCatalog.All) if (r.powerLevels[(int)p.id] < 0 || r.powerLevels[(int)p.id] > p.maxLevel) throw new InvalidDataException("Invalid power level");
        foreach (int p in r.powerOffers) if (!Enum.IsDefined(typeof(TowerPowerId), p)) throw new InvalidDataException("Invalid power offer");
        if (r.stage == RunSaveStage.PowerChoice && r.powerOffers.Any(id => r.powerLevels[id] >= TowerPowerCatalog.Get((TowerPowerId)id).maxLevel)) throw new InvalidDataException("Exhausted power offer");
        if (r.candidates == null || r.candidates.Length != 0 && r.candidates.Length != 3 || r.recentRoutes == null || r.recentRoutes.Length > 2 || r.stage == RunSaveStage.Travel && r.selected == null) throw new InvalidDataException("Missing routes");
        foreach (var route in r.candidates.Concat(r.recentRoutes).Concat(r.selected == null ? Array.Empty<SavedRoute>() : new[] { r.selected }))
            if (route == null || string.IsNullOrEmpty(route.floorId) || !Enum.IsDefined(typeof(FloorDifficulty), route.difficulty) || !Enum.IsDefined(typeof(FloorEncounterMode), route.encounterMode) || route.encounterSeed < 1) throw new InvalidDataException("Invalid route");
        if (r.candidates.Length == 3 && r.candidates.Select(c => c.encounterMode).Distinct().Count() == 1) throw new InvalidDataException("Three identical objectives");
    }
}
