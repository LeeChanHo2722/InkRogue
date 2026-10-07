using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public partial class FloorSelectionUI : MonoBehaviour
{
    [Serializable] private sealed class FloorSelectionCard
    {
        public UnityEngine.UI.Button button;
        public TMP_Text floorIdText;
        public TMP_Text difficultyText;
    }
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private FloorSelectionCard[] cards = new FloorSelectionCard[3];
    private readonly FloorCandidate[] displayedCandidates = new FloorCandidate[3];
    private readonly TMP_Text[] descriptions = new TMP_Text[3];
    private readonly TMP_Text[] mapLabels = new TMP_Text[3], pressureLabels = new TMP_Text[3];
    private readonly TMP_Text[] rewards = new TMP_Text[3];
    private readonly UnityEngine.UI.Button[] slots = new UnityEngine.UI.Button[6];
    private readonly List<UnityEngine.UI.Button> inventory = new();
    private Action<FloorCandidate> selected;
    private RunManager run;
    private TMP_FontAsset font;
    private TMP_Text heading, subheading, detail, hint, seedLabel;
    private string routeHeading, routeSubheading;
    private UnityEngine.UI.Button confirm, cancel, prepare;
    private GameObject equipment;
    private GameObject routeCanvas;
    private WeaponDefinition pickedWeapon;
    private readonly WeaponDefinition[] draftLeft = new WeaponDefinition[3], draftRight = new WeaponDefinition[3];
    private int highlighted = -1;
    private bool built, confirming;
    private Action retry;
    private static readonly Color Background = new(0.025f,0.035f,0.055f,1);
    private static readonly Color Card = new(0.075f,0.095f,0.13f,1);
    private static readonly Color Accent = new(0.73f,0.95f,0.35f,1);
    private static readonly Color Ink = new(0.91f,0.94f,0.97f,1);
    private static readonly Color Muted = new(0.63f,0.70f,0.77f,1);
    public bool IsVisible => built && panelRoot.activeInHierarchy;
    public bool IsPreparing => IsVisible && equipment.activeSelf;
    public int HighlightedIndex => highlighted;
    public bool IsConfirming => confirming;

    private void Awake() { if(panelRoot!=null) panelRoot.SetActive(false); }
    private void OnDestroy(){if(routeCanvas!=null)Destroy(routeCanvas);}
    private void Update()
    {
        if(!IsVisible || confirming || Keyboard.current==null) return;
        var key = Keyboard.current;
        if(key.escapeKey.wasPressedThisFrame) { Back(); return; }
        if(IsPreparing) return;
        if(key.digit1Key.wasPressedThisFrame) SelectCard(0);
        if(key.digit2Key.wasPressedThisFrame) SelectCard(1);
        if(key.digit3Key.wasPressedThisFrame) SelectCard(2);
        if(key.enterKey.wasPressedThisFrame) Confirm();
    }

    public bool ShowCandidates(IReadOnlyList<FloorCandidate> candidates, Action<FloorCandidate> onSelected)
    {
        if(panelRoot==null || candidates==null || candidates.Count<3 || onSelected==null ||
            cards==null || cards.Length!=3 || cards.Any(c=>c?.button==null || c.floorIdText==null || c.difficultyText==null)) return false;
        Build();
        selected=onSelected; retry=null; highlighted=-1; confirming=false; pickedWeapon=null; appliedPreparation=false;
        SetButton(confirm,"이 구역으로 출발  [Enter]");
        equipment.SetActive(false); panelRoot.SetActive(true);
        for(int i=0;i<3;i++) displayedCandidates[i]=candidates[i];
        bool boss=RunManager.IsBossFloorFor(run.CurrentMode,run.CurrentFloor+1);
        routeHeading=$"{run.CurrentFloor+1:00}층  /  다음 청소 구역";
        routeSubheading=boss ? "보스 층 · 구역 목표를 마치면 보스전으로 이어집니다." : "목표와 위험을 비교하고, 무기를 준비한 뒤 출발하세요.";
        subheading.color=boss ? new Color(1,.70f,.45f) : Muted;
        seedLabel.text=$"RUN {run.RunSeed}   /   {(run.CurrentMode==RunMode.Endless ? "ENDLESS" : "20 FLOORS")}";
        Refresh(); return true;
    }
    public void ShowUnavailable(Action onRetry)
    {
        Build();selected=null;retry=onRetry;highlighted=-1;confirming=false;
        Array.Clear(displayedCandidates,0,3);equipment.SetActive(false);panelRoot.SetActive(true);Refresh();
        heading.text="경로 정보를 불러올 수 없습니다";
        subheading.text="사용 가능한 Floor 데이터가 없습니다. 다시 확인하거나 메뉴로 돌아가세요.";
        detail.text="메뉴로 돌아가면 현재 런은 종료됩니다.";
        SetButton(confirm,"다시 확인  [Enter]");confirm.interactable=true;
        SetButton(cancel,"메뉴로 돌아가기");prepare.interactable=false;
    }
    public void Hide()
    {
        if(panelRoot!=null)panelRoot.SetActive(false);
        selected=null; retry=null; highlighted=-1;
        Array.Clear(displayedCandidates,0,3);
    }
    private void SelectCard(int index)
    {
        if(!IsVisible || confirming || IsPreparing || index<0 || index>=3 || displayedCandidates[index]?.Floor==null) return;
        highlighted=index; Refresh();
    }
    public void Confirm()
    {
        if(IsVisible && !confirming && retry!=null){retry();return;}
        if(!IsVisible || confirming || IsPreparing || highlighted<0 || selected==null) return;
        var candidate=displayedCandidates[highlighted];
        if(candidate?.Floor==null)return;
        confirming=true; confirm.interactable=false; cancel.interactable=false; prepare.interactable=false;
        foreach(var c in cards)c.button.interactable=false;
        detail.text="이동 중…";
        var callback=selected;selected=null;callback(candidate);
    }
    public void Back()
    {
        if(!IsVisible || confirming)return;
        if(retry!=null){RunRestart.Restart();return;}
        if(IsPreparing){equipment.SetActive(false);pickedWeapon=null;}
        else highlighted=-1;
        Refresh();
    }
    public void TogglePreparation()
    {
        if(!IsVisible || confirming || retry!=null)return;
        if(IsPreparing) { ApplyPreparation(); return; }
        for(int i=0;i<3;i++){draftLeft[i]=run.LeftFloorLoadout[i];draftRight[i]=run.RightFloorLoadout[i];}
        editingSlot=0; equipment.SetActive(true);
        pickedWeapon=null; Refresh();
    }
    private void PickWeapon(WeaponDefinition weapon)
    { pickedWeapon=weapon; RefreshEquipment(); }
    public bool AssignSlot(int index)
    {
        if(!IsPreparing || confirming || index<0 || index>=6)return false;
        var draft=index<3 ? draftLeft : draftRight;
        bool ok=pickedWeapon==null || (run.HasRunWeapon(pickedWeapon) && !WeaponLoadoutRules.ContainsInOtherSlot(draft,index%3,pickedWeapon));
        if(ok)draft[index%3]=pickedWeapon;
        RefreshEquipment();
        if(!ok)hint.text="같은 손에는 같은 무기를 두 번 넣을 수 없습니다. 다른 슬롯 또는 무기를 선택하세요.";
        return ok;
    }
    private void Refresh()
    {
        heading.text=IsPreparing ? "무기 편성" : routeHeading;
        subheading.text=IsPreparing ? (highlighted>=0 ? $"선택 구역: {displayedCandidates[highlighted].MapName} · {displayedCandidates[highlighted].ObjectiveName} · {displayedCandidates[highlighted].DifficultyName}" : "구역 미선택 · 편성을 마친 뒤 청소 구역을 선택하세요.") : routeSubheading;
        for(int i=0;i<3;i++)
        {
            cards[i].button.gameObject.SetActive(!IsPreparing);
            var c=displayedCandidates[i]; bool active=c?.Floor!=null;
            cards[i].button.interactable=active&&!confirming&&!IsPreparing;
            cards[i].button.GetComponent<UnityEngine.UI.Image>().color=i==highlighted ? new Color(.15f,.23f,.16f) : Card;
            if(!active){cards[i].floorIdText.text="구역 정보 없음";cards[i].difficultyText.text="";descriptions[i].text="";rewards[i].text="";mapLabels[i].text="";pressureLabels[i].text="";continue;}
            cards[i].floorIdText.text=c.ObjectiveName;
            cards[i].difficultyText.text=$"0{i+1}   /   {c.DifficultyName} {(i==highlighted ? "· 선택됨" : "")}";
            cards[i].difficultyText.color=i==0 ? Accent : i==1 ? new Color(.47f,.78f,1) : new Color(1,.58f,.41f);
            string pressure=FindAnyObjectByType<FloorManager>()?.DescribeRoute(c,run.CurrentFloor+1) ?? "난이도에 따른 적 편성";
            mapLabels[i].text=c.MapName;
            descriptions[i].text=c.ObjectiveDescription;
            pressureLabels[i].text="위험  ·  "+pressure;
            rewards[i].text=run.CurrentMode==RunMode.TwentyFloor && run.CurrentFloor+1>=run.maxFloor
                ? "최종 목표\n<size=30>보스 처치 · 타워 정화 완료</size>"
                : $"클리어 보상\n<size=30>파워 최대 {c.PowerOfferCount}개 중 1개</size>\n<size=19>미완성 파워만 제시 · 모두 완성 시 정비</size>";
        }
        confirm.interactable=!confirming&&(IsPreparing||highlighted>=0);
        cancel.interactable=!confirming;
        prepare.interactable=!confirming;
        prepare.gameObject.SetActive(!IsPreparing);
        SetButton(prepare,"무기 편성 열기");
        SetButton(cancel,IsPreparing ? "취소 · 돌아가기  [Esc]" : "선택 취소  [Esc]");
        SetButton(confirm,IsPreparing ? "편성 적용 · 돌아가기" : "이 구역으로 출발  [Enter]");
        confirm.GetComponent<UnityEngine.UI.Image>().color=confirm.interactable ? Accent : new Color(.22f,.27f,.24f);
        confirm.GetComponentInChildren<TMP_Text>().color=confirm.interactable ? Background : Muted;
        detail.gameObject.SetActive(!IsPreparing);
        detail.text=highlighted<0 ? "아직 선택한 구역이 없습니다.  카드 클릭 또는 1 / 2 / 3" :
            $"{displayedCandidates[highlighted].MapName} · {displayedCandidates[highlighted].ObjectiveName}   /   출발 전까지 변경할 수 있습니다.";
        RefreshEquipment();
    }
    private void Build()
    {
        if(built)return;
        run=FindAnyObjectByType<RunManager>(); font=TowerRunUI.Instance?.DisplayFont ?? TMP_Settings.defaultFontAsset;
        // Existing scene references stay intact; only this screen's runtime layout is adapted.
        var canvasObject=new GameObject("FloorRouteCanvas",typeof(RectTransform),typeof(Canvas),typeof(UnityEngine.UI.CanvasScaler),typeof(UnityEngine.UI.GraphicRaycaster));
        canvasObject.transform.SetParent(transform,false);
        // A root canvas owns its scaler; nesting under the legacy canvas would ignore it.
        canvasObject.transform.SetParent(null,false);routeCanvas=canvasObject;
        var canvas=canvasObject.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=32001;
        var scaler=canvasObject.GetComponent<UnityEngine.UI.CanvasScaler>();scaler.uiScaleMode=UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution=new Vector2(1920,1080);scaler.screenMatchMode=UnityEngine.UI.CanvasScaler.ScreenMatchMode.Expand;
        panelRoot.transform.SetParent(canvas.transform,false);
        var root=(RectTransform)panelRoot.transform;root.anchorMin=Vector2.zero;root.anchorMax=Vector2.one;root.offsetMin=root.offsetMax=Vector2.zero;
        var bg=panelRoot.GetComponent<UnityEngine.UI.Image>() ?? panelRoot.AddComponent<UnityEngine.UI.Image>();bg.color=Background;bg.raycastTarget=true;
        foreach(Transform child in panelRoot.transform)child.gameObject.SetActive(false);
        var content=new GameObject("RouteContent",typeof(RectTransform));content.transform.SetParent(root,false);Place((RectTransform)content.transform,0,0,1920,1080);
        var parent=content.transform;
        Label(parent,"TOWER CLEANUP  /  ROUTE PLANNING",20,-820,466,1600,36,Accent);
        heading=Label(parent,"",46,-820,396,1600,80,Ink);
        subheading=Label(parent,"",24,-820,329,1600,50,Muted);
        seedLabel=Label(parent,"",17,-820,-498,1600,35,Muted);
        for(int i=0;i<3;i++)
        {
            var card=cards[i];card.button.transform.SetParent(parent,false);card.button.gameObject.SetActive(true);
            Place((RectTransform)card.button.transform,-560+i*560,55,530,480);
            foreach(Transform child in card.button.transform)child.gameObject.SetActive(false);
            card.button.onClick.RemoveAllListeners();int index=i;card.button.onClick.AddListener(()=>SelectCard(index));
            var colors=card.button.colors;colors.normalColor=Color.white;colors.highlightedColor=new Color(1.13f,1.13f,1.13f);colors.pressedColor=new Color(.8f,.8f,.8f);colors.disabledColor=new Color(.6f,.6f,.6f);card.button.colors=colors;
            ConfigureText(card.difficultyText,card.button.transform,26,-230,195,460,44,Accent);
            ConfigureText(card.floorIdText,card.button.transform,42,-230,137,460,65,Ink);
            mapLabels[i]=Label(card.button.transform,"",23,-230,82,460,38,Muted);
            descriptions[i]=Label(card.button.transform,"",25,-230,20,460,80,Ink);
            pressureLabels[i]=Label(card.button.transform,"",23,-230,-66,460,84,new Color(.90f,.77f,.62f));
            rewards[i]=Label(card.button.transform,"",22,-230,-171,460,106,Ink);
        }
        detail=Label(parent,"",23,-820,-226,1640,52,Ink);
        hint=Label(parent,"",20,-820,-377,1640,34,Muted);
        cancel=Button(parent,"선택 취소  [Esc]",-660,-435,320,68,Back);
        prepare=Button(parent,"무기 준비",0,-435,350,68,TogglePreparation);
        confirm=Button(parent,"이 구역으로 출발  [Enter]",600,-435,440,68,PrimaryAction);
        confirm.GetComponent<UnityEngine.UI.Image>().color=Accent;confirm.GetComponentInChildren<TMP_Text>().color=Background;
        BuildEquipment(parent); built=true;
    }
    private TMP_Text Label(Transform parent,string value,int size,float x,float y,float width,float height,Color color)
    {
        var go=new GameObject("Label",typeof(RectTransform),typeof(TextMeshProUGUI));var t=go.GetComponent<TMP_Text>();
        ConfigureText(t,parent,size,x,y,width,height,color);t.text=value;return t;
    }
    private void ConfigureText(TMP_Text t,Transform parent,int size,float x,float y,float width,float height,Color color)
    {
        t.transform.SetParent(parent,false);t.gameObject.SetActive(true);t.font=font;t.fontSize=size;t.color=color;
        t.raycastTarget=false;t.alignment=TextAlignmentOptions.MidlineLeft;t.enableAutoSizing=false;t.textWrappingMode=TextWrappingModes.Normal;
        Place(t.rectTransform,x+width/2,y,width,height);
    }
    private UnityEngine.UI.Button Button(Transform parent,string value,float x,float y,float width,float height,Action click)
    {
        var go=new GameObject("Button",typeof(RectTransform),typeof(UnityEngine.UI.Image),typeof(UnityEngine.UI.Button));go.transform.SetParent(parent,false);
        Place((RectTransform)go.transform,x,y,width,height);go.GetComponent<UnityEngine.UI.Image>().color=Card;
        var b=go.GetComponent<UnityEngine.UI.Button>();b.onClick.AddListener(()=>click());
        var t=Label(go.transform,value,25,-width/2+12,0,width-24,height-8,Ink);t.alignment=TextAlignmentOptions.Center;return b;
    }
    private static void SetButton(UnityEngine.UI.Button button,string value){button.GetComponentInChildren<TMP_Text>().text=value;}
    private static void Place(RectTransform r,float x,float y,float width,float height)
    {r.anchorMin=r.anchorMax=r.pivot=new Vector2(.5f,.5f);r.anchoredPosition=new Vector2(x,y);r.sizeDelta=new Vector2(width,height);r.localScale=Vector3.one;r.localRotation=Quaternion.identity;}
}
