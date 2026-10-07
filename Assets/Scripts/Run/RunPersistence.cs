using System;
using System.Collections;
using System.Linq;
using UnityEngine;

public sealed class RunPersistence : MonoBehaviour
{
    public static RunPersistence Instance { get; private set; }
    public RunSaveStore Store { get; private set; }
    public RunSaveBook Book { get; private set; }
    public SavedRun Checkpoint => Book?.active;
    public RunSaveStage? ResumeStage { get; private set; }
    public bool Pending => pending != null;
    private RunSaveBook pending;
    private Action afterWrite;
    private string runId, startedUtc;
    private int deaths;
    private float shieldAtStart;
    private bool activeSession;
    private void Awake()
    {
        Instance = this; Store = new RunSaveStore(RunSaveStore.DefaultDirectory); Book = Store.Load();
    }
    private void OnDestroy() { if (Instance == this) Instance = null; }
    public void BeginSession()
    {
        activeSession = true; runId = Guid.NewGuid().ToString("N"); startedUtc = DateTime.UtcNow.ToString("O"); deaths = 0;
        shieldAtStart = FindAnyObjectByType<PlayerShield>().MaxShield;
        ResumeStage = null;
    }
    public void RestoreSession(SavedRun saved)
    {
        runId = saved.runId; startedUtc = saved.startedUtc; deaths = saved.deaths;
        shieldAtStart = saved.checkpointShield; activeSession = true; ResumeStage = saved.stage;
    }
    public void FinishResume() { ResumeStage = null; }
    public void ApplyResumeHealth()
    {
        if (ResumeStage == RunSaveStage.FloorStart)
            FindAnyObjectByType<PlayerShield>().RestoreCheckpointHealth(shieldAtStart);
        ResumeStage = null;
    }
    public int[] CurrentOffers()
    {
        if (Checkpoint?.stage == RunSaveStage.PowerChoice) return Checkpoint.powerOffers;
        var run = FindAnyObjectByType<RunManager>();
        return TowerPowers.Instance.Offers(run.CurrentPowerOfferCount, unchecked(run.RunSeed * 31 ^ run.CurrentFloor * 1009)).Select(p => (int)p.id).ToArray();
    }
    private SavedRun Capture(RunSaveStage stage)
    {
        var run = FindAnyObjectByType<RunManager>();
        var saved = run.CaptureRun();
        saved.runId = runId; saved.startedUtc = startedUtc; saved.savedUtc = DateTime.UtcNow.ToString("O");
        saved.revision = (Book?.active?.runId == runId ? Book.active.revision : 0) + 1;
        saved.stage = stage; saved.lives = FindAnyObjectByType<PlayerLifeManager>().CurrentLives;
        if (stage == RunSaveStage.FloorStart) shieldAtStart = FindAnyObjectByType<PlayerShield>().CurrentShield;
        saved.deaths = deaths; saved.checkpointShield = shieldAtStart;
        saved.mapScene = FindAnyObjectByType<MapSceneLoader>().CurrentMapName;
        var powers = TowerPowers.Instance;
        saved.powerLevels = new int[TowerPowerCatalog.All.Length];
        foreach (var p in TowerPowerCatalog.All) saved.powerLevels[(int)p.id] = powers.Level(p.id);
        saved.playerDamage = powers.PlayerDamageDealt; saved.robotDamage = powers.RobotDamageDealt;
        saved.powerOffers = stage == RunSaveStage.PowerChoice ? CurrentOffers() : Array.Empty<int>();
        return saved;
    }
    public void Save(RunSaveStage stage, Action success = null)
    {
        if (!activeSession) { success?.Invoke(); return; }
        if (Pending) throw new InvalidOperationException("A save transaction is already pending.");
        var next = Clone(Book ?? RunSaveBook.Empty()); next.active = Capture(stage); next.revision++;
        Submit(next, success);
    }
    public IEnumerator Boundary(RunSaveStage stage)
    { while (Pending) yield return null; Save(stage); while (Pending) yield return null; }
    public void SaveAndLeave()
    {
        if (Pending) return;
        // Combat progress is deliberately discarded. Only elapsed time carries forward.
        if (Checkpoint == null) return;
        var next = Clone(Book); next.revision++; next.active.revision++;
        next.active.elapsedSeconds = FindAnyObjectByType<RunManager>().ElapsedSeconds;
        next.active.savedUtc = DateTime.UtcNow.ToString("O");
        Submit(next, RunRestart.Restart);
    }
    public void RecordDeath(int lives)
    {
        if (!activeSession || Checkpoint == null) return;
        deaths++;
        if (lives == 0) { End("Defeat"); return; }
        var next = Clone(Book); next.revision++; next.active.revision++;
        next.active.lives = lives; next.active.deaths = deaths;
        next.active.elapsedSeconds = FindAnyObjectByType<RunManager>().ElapsedSeconds;
        next.active.savedUtc = DateTime.UtcNow.ToString("O");
        Submit(next, null);
    }
    public void End(string result, Action success = null)
    {
        if (!activeSession) { success?.Invoke(); return; }
        var next = Clone(Book ?? RunSaveBook.Empty());
        if (!next.history.Any(h => h.run.runId == runId))
            next.history.Insert(0, new RunHistoryEntry { result = result, endedUtc = DateTime.UtcNow.ToString("O"), run = Capture(Checkpoint?.stage ?? RunSaveStage.FloorStart) });
        next.active = null; next.revision++;
        activeSession = false;
        Submit(next, success);
    }
    public void ReplaceActive(Action success)
    {
        if (Store.Blocked)
        {
            if (!Store.ArchiveUnreadable(out var error)) { TowerRunUI.Instance.ShowSaveError(error, () => ReplaceActive(success)); return; }
            Book = RunSaveBook.Empty();
        }
        if (Book?.active == null) { success(); return; }
        var next = Clone(Book);
        next.history.Insert(0, new RunHistoryEntry { run = next.active, result = "Abandoned", endedUtc = DateTime.UtcNow.ToString("O") });
        // StartRun synchronously captures the new run into this same transaction.
        // Cancelling the confirmation never reaches here and performs no write.
        next.active = null; next.revision++; Book = next; success();
    }
    private void Submit(RunSaveBook next, Action success)
    { pending = next; afterWrite = success; Retry(); }
    private void Retry()
    {
        if (Store.Write(pending, out string error))
        {
            Book = pending; pending = null; var action = afterWrite; afterWrite = null;
            TowerRunUI.Instance?.HideSaveError(); action?.Invoke();
        }
        else TowerRunUI.Instance.ShowSaveError(error, Retry);
    }
    public static T Clone<T>(T value) => RunSaveJson.FromJson<T>(RunSaveJson.ToJson(value));
}
