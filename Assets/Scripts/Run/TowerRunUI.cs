using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;

// Functional run screens; art is loaded separately after art-direction approval.
public sealed partial class TowerRunUI : MonoBehaviour
{
    public static TowerRunUI Instance { get; private set; }
    private MapSceneLoader loader;
    private RunManager run;
    private GameObject panel;
    private TMP_Text status;
    private BossHealthUI bossHud;
    private TMP_FontAsset font;
    private Canvas canvas;
    private bool paused;
    private float pausedTimeScale = 1f, pausedFixedDeltaTime = 0.02f;
    private bool finished;
    public bool HasStarted { get; private set; }
    public bool HasFinished => finished;
    public RunManager Run => run;
    public TMP_FontAsset DisplayFont => font;

    public static void Show(MapSceneLoader loader)
    {
        var ui = loader.gameObject.AddComponent<TowerRunUI>();
        ui.loader = loader;
        ui.run = Object.FindAnyObjectByType<RunManager>();
        ui.gameObject.AddComponent<RunPersistence>();
        ui.Build();
    }

    private void Awake() { Instance = this; }
    private void OnDestroy() { if (Instance == this) Instance = null; if(font!=null && font!=TMP_Settings.defaultFontAsset) Destroy(font); }

    private void Build()
    {
        var go = new GameObject("TowerRunCanvas", typeof(RectTransform), typeof(Canvas),
            typeof(UnityEngine.UI.CanvasScaler), typeof(UnityEngine.UI.GraphicRaycaster));
        go.transform.SetParent(transform, false);
        canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 32000;
        var scaler = go.GetComponent<UnityEngine.UI.CanvasScaler>();
        scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = UnityEngine.UI.CanvasScaler.ScreenMatchMode.Expand;
        if (Object.FindAnyObjectByType<EventSystem>() == null)
            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        font = TMP_FontAsset.CreateFontAsset("Malgun Gothic", "Regular");
        if (font == null) font = TMP_Settings.defaultFontAsset;
        status = Label(go.transform, "", 22, new Vector2(0,480), new Vector2(1450,65));
        bossHud = Object.FindAnyObjectByType<BossHealthUI>(FindObjectsInactive.Include);
        ShowMenu();
    }

    private void ClearPanel()
    {
        if (panel != null) { panel.SetActive(false); Destroy(panel); }
        panel = new GameObject("RunPanel", typeof(RectTransform), typeof(UnityEngine.UI.Image));
        panel.transform.SetParent(canvas.transform, false);
        var rt = panel.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        panel.GetComponent<UnityEngine.UI.Image>().color = new Color(0.025f,0.035f,0.055f,0.97f);
    }

    private void ShowMenu()
    {
        PlayerWeaponInputController.CancelActiveWeaponWheel();
        Time.timeScale = 0f; ClearPanel();
        Label(panel.transform,"TOWER CLEANUP",72,new Vector2(0,355),new Vector2(1400,110));
        Label(panel.transform,"타워 청소 · 구역을 정화하고 로봇과 함께 올라가세요",29,new Vector2(0,255),new Vector2(1500,80));
        AddSaveMenu();
        Button(panel.transform,"20층 모드  ·  타워 정화",new Vector2(-290,-75),()=>RequestNewRun(RunMode.TwentyFloor));
        Button(panel.transform,"무한 모드  ·  최고층 도전",new Vector2(290,-75),()=>RequestNewRun(RunMode.Endless));
        Label(panel.transform,"WASD 이동 · 마우스 조준 / 좌우 버튼 사격 · Shift 잠수/충전\nQ / E 무기 휠 · Esc 일시정지 · 보스 처치 시 목숨 +1 (최대 3)",23,new Vector2(0,-300),new Vector2(1550,100));
        Button(panel.transform,"종료",new Vector2(0,-470),Application.Quit,new Vector2(260,60));
    }

    public void StartRun(RunMode mode)
    {
        if (HasStarted) return;
        PlayerWeaponInputController.CancelActiveWeaponWheel();
        HasStarted = true; finished = false;
        panel.SetActive(false);
        Time.timeScale = 1f;
        Object.FindAnyObjectByType<PlayerLifeManager>()?.ResetForNewRun();
        loader.BeginRun(mode);
        TowerCleaningAudio.Install();
        var powers=gameObject.AddComponent<TowerPowers>();
        TowerRobotCompanion.Create(powers);
        RunPersistence.Instance.BeginSession();
        RunPersistence.Instance.Save(RunSaveStage.FloorStart);
    }

    private void Update()
    {
        if (!HasStarted || run == null) return;
        // Keep the run summary clear of the independently authored boss HUD.
        bool bossVisible = bossHud != null && bossHud.IsVisible;
        var anchor = new Vector2(.5f, bossVisible ? 0f : 1f);
        status.rectTransform.anchorMin = status.rectTransform.anchorMax = anchor;
        status.rectTransform.pivot = anchor;
        status.rectTransform.anchoredPosition = new Vector2(0, bossVisible ? 18f : -18f);
        status.text = (run.CurrentMode == RunMode.Endless ? "무한 청소" : "타워 정화")
            + "   |   " + run.CurrentFloor + (run.CurrentMode == RunMode.TwentyFloor ? " / 20층" : "층")
            + "   |   " + Mathf.FloorToInt(run.ElapsedSeconds / 60).ToString("00") + ":"
            + Mathf.FloorToInt(run.ElapsedSeconds % 60).ToString("00")
            + "   |   세정력 +" + Mathf.RoundToInt(((TowerPowers.Instance != null ? TowerPowers.Instance.PlayerDamageMultiplier : 1f)-1f) * 100) + "%"
            + (run.CurrentMode == RunMode.Endless ? "   |   적 체력 ×" + TowerEnemyScaling.Health(run.CurrentFloor).ToString("0.00") : "");
        if (!finished && !ChoosingPower && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (paused) Resume();
            else if (Time.timeScale > 0f)
            {
                var input = Object.FindAnyObjectByType<PlayerWeaponInputController>();
                if (input != null && input.InputEnabled) Pause();
            }
        }
    }

    public bool ChoosingPower { get; private set; }
    public TowerPowerDefinition[] PowerChoices { get; private set; }
    private System.Action powerChosen;

    public void ShowPowerChoices(System.Action next)
    {
        PlayerWeaponInputController.CancelActiveWeaponWheel();
        ChoosingPower=true; powerChosen=next; Time.timeScale=0; ClearPanel();
        var powers=TowerPowers.Instance; PowerChoices=System.Array.ConvertAll(RunPersistence.Instance.CurrentOffers(), id => TowerPowerCatalog.Get((TowerPowerId)id));
        Label(panel.transform,"청소 파워 선택",52,new Vector2(0,360),new Vector2(1500,100));
        Label(panel.transform,"선택한 효과만 강화됩니다  |  기본 화력: 노즐 보강 / 모터 보강",25,new Vector2(0,280),new Vector2(1750,70));
        if(PowerChoices.Length==0) Button(panel.transform,"최대 강화 달성 · 정비 후 계속",Vector2.zero,()=>ChoosePower(0));
        for(int i=0;i<PowerChoices.Length;i++)
        {
            int index=i;var choice=PowerChoices[i];float width = PowerChoices.Length > 3 ? 420 : 545; float x=(i-(PowerChoices.Length-1)*0.5f)*(width+25);
            string owner=choice.owner==TowerPowerOwner.Player ? "플레이어" : "로봇";
            Button(panel.transform,owner+" · "+choice.title+" "+(powers.Level(choice.id)+1),new Vector2(x,90),()=>ChoosePower(index),new Vector2(width,100));
            Label(panel.transform,choice.description,24,new Vector2(x,-65),new Vector2(width-20,200));
        }
        Label(panel.transform,"플레이어: "+powers.Summary(TowerPowerOwner.Player)+"\n로봇: "+powers.Summary(TowerPowerOwner.Robot),22,new Vector2(0,-310),new Vector2(1700,160));
    }
    public void ChoosePower(int index)
    {
        if(!ChoosingPower||index<0||(PowerChoices.Length>0&&index>=PowerChoices.Length))return;
        if(PowerChoices.Length==0)TowerPowers.Instance.Service();
        else if(!TowerPowers.Instance.Add(PowerChoices[index].id))return;
        ChoosingPower=false;panel.SetActive(false);GameAudioManager.Instance?.PlayCardSelect();
        var next=powerChosen;powerChosen=null;
        run.TryPrepareFloorCandidates(Object.FindAnyObjectByType<FloorManager>().FloorDefinitions);
        RunPersistence.Instance.Save(RunSaveStage.RouteChoice, next);
    }

    private void Pause()
    {
        PlayerWeaponInputController.CancelActiveWeaponWheel();
        pausedTimeScale = Time.timeScale;
        pausedFixedDeltaTime = Time.fixedDeltaTime;
        paused = true; Time.timeScale = 0f; ClearPanel();
        var powers=TowerPowers.Instance;
        if(powers!=null) Label(panel.transform,"플레이어: "+powers.Summary(TowerPowerOwner.Player)+"\n로봇: "+powers.Summary(TowerPowerOwner.Robot),21,new Vector2(0,-395),new Vector2(1700,175));
        Label(panel.transform,"잠시 쉬어가기",55,new Vector2(0,210),new Vector2(1300,100));
        Button(panel.transform,"청소 계속하기",new Vector2(0,30),Resume);
        Button(panel.transform,"저장하고 메뉴로",new Vector2(0,-100),()=>RunPersistence.Instance.SaveAndLeave());
        Label(panel.transform,"이번 층의 전투 진행은 저장되지 않습니다.\n이어하기는 이 층 시작 시점으로 돌아갑니다.",23,new Vector2(0,-250),new Vector2(1300,80));
    }

    private void Resume()
    {
        paused=false; panel.SetActive(false);
        Time.timeScale=pausedTimeScale;
        Time.fixedDeltaTime=pausedFixedDeltaTime;
    }

    public void ShowResult(bool success)
    {
        RunPersistence.Instance?.End(success ? "Victory" : "Defeat");
        PlayerWeaponInputController.CancelActiveWeaponWheel();
        finished = true; paused = false; Time.timeScale = 0f; ClearPanel();
        var powers=TowerPowers.Instance;
        if(powers!=null) Label(panel.transform,"누적 피해  플레이어 "+powers.PlayerDamageDealt.ToString("F0")+"  /  로봇 "+powers.RobotDamageDealt.ToString("F0"),25,new Vector2(0,-400),new Vector2(1500,65));
        Label(panel.transform,success ? "타워 정화 완료!" : "청소 종료",62,new Vector2(0,240),new Vector2(1400,100));
        Label(panel.transform,success ? "20층의 오염원을 모두 정리했습니다." : "다음 도전을 위해 장비를 정비하세요.",28,new Vector2(0,140),new Vector2(1400,80));
        Label(panel.transform,run.CurrentFloor + "층 도달   ·   보스 " + run.BossesDefeated + "체 정리\n소요 시간 " + System.TimeSpan.FromSeconds(run.ElapsedSeconds).ToString(@"hh\:mm\:ss"),31,new Vector2(0,10),new Vector2(1400,120));
        Button(panel.transform,"다시 도전 · 모드 선택",new Vector2(0,-175),RunRestart.Restart);
        Button(panel.transform,"종료",new Vector2(0,-290),Application.Quit,new Vector2(260,65));
    }

    private TMP_Text Label(Transform parent, string text, int size, Vector2 pos, Vector2 dimensions)
    {
        var go = new GameObject("Label",typeof(RectTransform),typeof(TextMeshProUGUI));
        go.transform.SetParent(parent,false);
        var rt=go.GetComponent<RectTransform>(); rt.sizeDelta=dimensions; rt.anchoredPosition=pos;
        var label=go.GetComponent<TextMeshProUGUI>(); label.font=font; label.text=text;
        label.fontSize=size; label.color=Color.white; label.alignment=TextAlignmentOptions.Center;
        label.raycastTarget=false; return label;
    }

    private void Button(Transform parent,string text,Vector2 pos,UnityEngine.Events.UnityAction action,Vector2? size=null)
    {
        var go=new GameObject("Button",typeof(RectTransform),typeof(UnityEngine.UI.Image),typeof(UnityEngine.UI.Button));
        go.transform.SetParent(parent,false);
        var rt=go.GetComponent<RectTransform>(); rt.sizeDelta=size ?? new Vector2(535,95); rt.anchoredPosition=pos;
        go.GetComponent<UnityEngine.UI.Image>().color=new Color(0.16f,0.21f,0.28f);
        go.GetComponent<UnityEngine.UI.Button>().onClick.AddListener(action);
        Label(go.transform,text,27,Vector2.zero,rt.sizeDelta-new Vector2(20,8));
    }
}
