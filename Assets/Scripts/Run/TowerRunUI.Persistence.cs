using System;
using System.Linq;
using UnityEngine;

public sealed partial class TowerRunUI
{
    private GameObject saveErrorPanel;
    private float saveErrorTimeScale;
    private int historyPage;
    private GameObject safeExit;
    private void LateUpdate()
    {
        var persistence = RunPersistence.Instance;
        bool safe = HasStarted && !finished && !paused && persistence != null && !persistence.Pending &&
            (ChoosingPower || FindAnyObjectByType<FloorSelectionUI>()?.IsVisible == true);
        if (safe && safeExit == null)
        {
            safeExit = new GameObject("SafeStageExit", typeof(RectTransform), typeof(Canvas), typeof(UnityEngine.UI.GraphicRaycaster));
            safeExit.transform.SetParent(canvas.transform, false);
            var layer = safeExit.GetComponent<Canvas>(); layer.overrideSorting = true; layer.sortingOrder = 32002;
            Button(safeExit.transform, "저장하고 메뉴로", new Vector2(755, -500), () => persistence.SaveAndLeave(), new Vector2(300, 52));
        }
        if (safeExit != null) safeExit.SetActive(safe);
    }
    private static string ModeName(RunMode mode) => mode == RunMode.Endless ? "무한 청소" : "20층 정화";
    private static string StageName(RunSaveStage stage) => stage switch
    { RunSaveStage.PowerChoice => "파워 선택", RunSaveStage.RouteChoice => "경로 선택", RunSaveStage.Travel => "확정 구역으로 이동", _ => "층 시작" };
    private static string ResultName(string result) => result == "Victory" ? "승리" : result == "Defeat" ? "패배" : "중단";
    private static string DateLabel(string iso) => DateTime.TryParse(iso, out var time) ? time.ToLocalTime().ToString("yyyy-MM-dd HH:mm") : iso;
    private static string Duration(float seconds) => TimeSpan.FromSeconds(seconds).ToString(@"hh\:mm\:ss");

    private void AddSaveMenu()
    {
        var persistence = RunPersistence.Instance;
        var saved = persistence.Checkpoint;
        if (saved != null)
        {
            Button(panel.transform, "이어하기 · " + saved.floor + "층 " + StageName(saved.stage), new Vector2(0, 120), ContinueRun, new Vector2(850, 90));
            Label(panel.transform, $"{ModeName(saved.mode)}  ·  목숨 {saved.lives}/3  ·  {DateLabel(saved.savedUtc)}", 23, new Vector2(0, 45), new Vector2(1450, 50));
        }
        else Label(panel.transform, persistence.Store.Blocked ? "저장 확인 필요 · 기존 파일 보존 중" : "이어갈 청소 기록이 없습니다", 25, new Vector2(0, 105), new Vector2(1400, 80));
        Button(panel.transform, "지난 런 기록", new Vector2(0, -195), () => ShowHistory(0), new Vector2(500, 75));
        if (!string.IsNullOrEmpty(persistence.Store.Notice))
            Label(panel.transform, persistence.Store.Notice, 21, new Vector2(0, -390), new Vector2(1550, 85));
    }
    public void RequestNewRun(RunMode mode)
    {
        var persistence = RunPersistence.Instance;
        if (persistence.Checkpoint == null && !persistence.Store.Blocked) { StartRun(mode); return; }
        ClearPanel();
        Label(panel.transform, "새 청소를 시작할까요?", 48, new Vector2(0, 220), new Vector2(1400, 100));
        Label(panel.transform, persistence.Store.Blocked
            ? "읽을 수 없는 저장은 별도 폴더에 보존한 뒤 새 저장을 만듭니다."
            : "현재 이어하기 런은 중단 기록으로 남습니다.\n새 런이 활성 저장을 대신합니다.", 29, new Vector2(0, 80), new Vector2(1450, 160));
        Button(panel.transform, "취소 · 기존 저장 유지", new Vector2(-300, -150), ShowMenu);
        Button(panel.transform, "확인 · 새 런 시작", new Vector2(300, -150), () => persistence.ReplaceActive(() => StartRun(mode)));
    }
    public void ContinueRun()
    {
        if (HasStarted) return;
        var persistence = RunPersistence.Instance; var saved = persistence.Checkpoint;
        if (saved == null || persistence.Store.Blocked) return;
        try
        {
            bool mapExists = Enumerable.Range(0, UnityEngine.SceneManagement.SceneManager.sceneCountInBuildSettings)
                .Any(i => System.IO.Path.GetFileNameWithoutExtension(UnityEngine.SceneManagement.SceneUtility.GetScenePathByBuildIndex(i)) == saved.mapScene);
            if (!mapExists) throw new InvalidOperationException("저장된 맵을 현재 빌드에서 찾을 수 없습니다.");
            run.RestoreRun(saved, loader.SaveWeapons, FindAnyObjectByType<FloorManager>().FloorDefinitions);
            FindAnyObjectByType<PlayerLifeManager>().RestoreSavedLives(saved.lives);
            var powers = gameObject.AddComponent<TowerPowers>(); powers.RestoreProgress(saved);
            TowerRobotCompanion.Create(powers); TowerCleaningAudio.Install();
            persistence.RestoreSession(saved);
            HasStarted = true; finished = false; panel.SetActive(false); Time.timeScale = 1;
            loader.ResumeRun(saved);
        }
        catch (Exception e)
        {
            ClearPanel();
            Label(panel.transform, "이어하기를 열 수 없습니다\n" + e.Message + "\n저장 파일은 보존됩니다.", 30, Vector2.zero, new Vector2(1450, 250));
            Button(panel.transform, "메뉴로", new Vector2(0, -240), ShowMenu);
        }
    }
    public void ShowSaveError(string message, Action retry)
    {
        if (saveErrorPanel != null) Destroy(saveErrorPanel);
        else saveErrorTimeScale = Time.timeScale;
        Time.timeScale = 0;
        saveErrorPanel = new GameObject("SaveErrorOverlay", typeof(RectTransform), typeof(Canvas), typeof(UnityEngine.UI.GraphicRaycaster), typeof(UnityEngine.UI.Image));
        saveErrorPanel.transform.SetParent(canvas.transform, false);
        var rt = saveErrorPanel.GetComponent<RectTransform>(); rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = rt.offsetMax = Vector2.zero;
        var overlay = saveErrorPanel.GetComponent<Canvas>(); overlay.overrideSorting = true; overlay.sortingOrder = 32760;
        saveErrorPanel.GetComponent<UnityEngine.UI.Image>().color = new Color(.03f, .04f, .06f, .99f);
        Label(saveErrorPanel.transform, "저장에 실패했습니다", 48, new Vector2(0, 180), new Vector2(1450, 90));
        Label(saveErrorPanel.transform, message + "\n이전 유효 저장을 유지합니다. 저장 성공 전에는 진행을 멈춥니다.", 25, Vector2.zero, new Vector2(1500, 230));
        Button(saveErrorPanel.transform, "다시 저장", new Vector2(0, -220), () => retry());
    }
    public void HideSaveError()
    {
        if (saveErrorPanel == null) return;
        Destroy(saveErrorPanel); saveErrorPanel = null; Time.timeScale = saveErrorTimeScale;
    }
    public void ShowHistory(int page)
    {
        ClearPanel();
        var history = RunPersistence.Instance.Book?.history.OrderByDescending(h => h.endedUtc).ToArray() ?? Array.Empty<RunHistoryEntry>();
        historyPage = Mathf.Clamp(page, 0, Mathf.Max(0, (history.Length - 1) / 5));
        Label(panel.transform, "지난 런 기록", 52, new Vector2(0, 400), new Vector2(1500, 90));
        if (history.Length == 0) Label(panel.transform, "완료된 청소 기록이 없습니다.", 30, Vector2.zero, new Vector2(1500, 100));
        for (int i = historyPage * 5; i < Mathf.Min(history.Length, historyPage * 5 + 5); i++)
        {
            var entry = history[i];
            Button(panel.transform, $"{ResultName(entry.result)}  ·  {ModeName(entry.run.mode)}  ·  {entry.run.floor}층  ·  {Duration(entry.run.elapsedSeconds)}  ·  {DateLabel(entry.endedUtc)}",
                new Vector2(0, 265 - (i % 5) * 112), () => ShowHistoryDetail(entry), new Vector2(1550, 92));
        }
        if (historyPage > 0) Button(panel.transform, "이전", new Vector2(-500, -400), () => ShowHistory(historyPage - 1), new Vector2(280, 70));
        if ((historyPage + 1) * 5 < history.Length) Button(panel.transform, "다음", new Vector2(500, -400), () => ShowHistory(historyPage + 1), new Vector2(280, 70));
        Button(panel.transform, "메뉴로", new Vector2(0, -400), ShowMenu, new Vector2(300, 70));
    }
    private void ShowHistoryDetail(RunHistoryEntry entry)
    {
        ClearPanel(); var saved = entry.run;
        Label(panel.transform, $"{ModeName(saved.mode)} · {ResultName(entry.result)}", 49, new Vector2(0, 385), new Vector2(1500, 90));
        Label(panel.transform, $"{DateLabel(entry.endedUtc)}\n도달 {saved.floor}층 · 클리어 {Math.Max(saved.rewardedFloors, entry.result == "Victory" ? 20 : saved.rewardedFloors)}층 · 보스 {saved.bosses}체\n정상 사망 {saved.deaths}회 · 목숨 {saved.lives}/3 · 시간 {Duration(saved.elapsedSeconds)}", 28, new Vector2(0, 230), new Vector2(1500, 190));
        string Powers(TowerPowerOwner owner) => string.Join(" · ", TowerPowerCatalog.All.Where(p => p.owner == owner && saved.powerLevels[(int)p.id] > 0).Select(p => p.title + " " + saved.powerLevels[(int)p.id]));
        Label(panel.transform, "플레이어\n" + Powers(TowerPowerOwner.Player) + "\n\n로봇\n" + Powers(TowerPowerOwner.Robot), 24, new Vector2(0, -35), new Vector2(1650, 300));
        string Hand(string[] ids) => string.Join(" / ", ids.Select(id => string.IsNullOrEmpty(id) ? "빈 슬롯" : loader.SaveWeapons.FirstOrDefault(w => w != null && w.WeaponId == id)?.DisplayName ?? id));
        Label(panel.transform, "왼손: " + Hand(saved.left) + "\n오른손: " + Hand(saved.right), 25, new Vector2(0, -285), new Vector2(1600, 100));
        Button(panel.transform, "기록 목록", new Vector2(0, -435), () => ShowHistory(historyPage), new Vector2(400, 70));
    }
}
