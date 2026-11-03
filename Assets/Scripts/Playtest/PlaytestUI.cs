using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>Child-facing interface contains only icons, characters and a cooperation bar.</summary>
public sealed class PlaytestUI : MonoBehaviour
{
    [SerializeField] Canvas canvas;
    [SerializeField] Color blue=new Color(.16f,.55f,.86f),red=new Color(.86f,.23f,.28f),ink=new Color(.08f,.19f,.25f);
    [SerializeField] Color progressColor=new Color(.20f,.72f,.44f);
    [SerializeField,Min(.1f)] float scoreAnimationSeconds=1.25f;
    [SerializeField,Range(.25f,.85f)] float hintDimOpacity=.62f;
    GameManager manager;WinSequence celebration;RectTransform hud,panel,card,fill;
    CanvasGroup panelGroup;Sprite rounded;Image[] avatars=new Image[2];RectTransform[] stars=new RectTransform[3];
    RectTransform researcher;UnityEngine.UI.Button newPairButton;TMP_Text researcherStatus,levelSummary,blueSummary,redSummary;string storageError,exportStatus;
    Canvas spotlightCanvas;HintSpotlight shade;readonly List<HintSpot> spots=new();readonly List<RectTransform> arrows=new(),hands=new();
    readonly Dictionary<int,GuidanceTiming> guideClocks=new();readonly Dictionary<int,float> handStarts=new();
    string guidanceContext;float dashboardNextUpdate;PlayerController[] guidePlayers;Vector3[] guidePositions;
    public event System.Action<int> DemonstrationRepeated;
    bool handDemonstrations=true;
    public void SetHandDemonstrations(bool enabled){handDemonstrations=enabled;}
    public bool ResultsVisible{get;private set;}
    public struct HintSpot
    {
        public Vector3 Position;public int Player;public string Button;public Transform WorldTarget;public RectTransform ControlRect;public bool Tap;
        public static HintSpot Follow(Transform target,int player)=>new HintSpot{WorldTarget=target,Player=player};
        public static HintSpot ExactControl(RectTransform target,int player,bool tap=false)=>new HintSpot{ControlRect=target,Player=player,Tap=tap};
        public static HintSpot World(Vector3 position,int player)=>new HintSpot{Position=position,Player=player};
        public static HintSpot Control(int player,string button)=>new HintSpot{Player=player,Button=button,Tap=button=="Btn_Jump"};
    }
    public void Initialize(GameManager owner,WinSequence sequence)
    {
        manager=owner;celebration=sequence;
        if(!canvas)canvas=FindFirstObjectByType<Canvas>();
        if(!canvas){var go=new GameObject("Canvas",typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));canvas=go.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;var sc=go.GetComponent<CanvasScaler>();sc.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;sc.referenceResolution=new Vector2(2048,1536);sc.matchWidthOrHeight=.5f;}
        if(celebration)
        {
            panel=celebration.GetComponent<RectTransform>();var image=celebration.GetComponent<Image>();if(image)rounded=image.sprite;
            foreach(Transform child in panel)child.gameObject.SetActive(false);
            celebration.SetTitle(null);
        }
        if(!panel)panel=Rect("Visual Results",canvas.transform,Vector2.zero,Vector2.one);
        panelGroup=panel.GetComponent<CanvasGroup>();if(!panelGroup)panelGroup=panel.gameObject.AddComponent<CanvasGroup>();
        panelGroup.alpha=0;panelGroup.blocksRaycasts=false;panelGroup.interactable=false;
        rounded=Resources.Load<Sprite>("KenneyUI/Panel");
        hud=Rect("Visual HUD",canvas.transform,Vector2.zero,Vector2.one);
        Button("Blue Hint",hud,new Vector2(.035f,.84f),new Vector2(.145f,.97f),blue,VisualIcon.Shape.Bulb,()=>manager.RequestHint(1));
        Button("Red Hint",hud,new Vector2(.855f,.84f),new Vector2(.965f,.97f),red,VisualIcon.Shape.Bulb,()=>manager.RequestHint(2));
        card=Box("Celebration",panel,new Vector2(.10f,.19f),new Vector2(.90f,.86f),new Color(1,.985f,.93f));
        for(int i=0;i<3;i++)stars[i]=Icon("Celebration star",card,new Vector2(.25f+i*.17f,.76f),new Vector2(.41f+i*.17f,.96f),VisualIcon.Shape.Star,new Color(1,.72f,.12f));
        for(int i=0;i<2;i++)
        {
            float x=i==0?.13f:.69f;var back=Box("Player badge",card,new Vector2(x,.49f),new Vector2(x+.18f,.72f),i==0?blue:red);
            var imageRect=Rect("Character",back,new Vector2(.12f,.08f),new Vector2(.88f,.92f));avatars[i]=imageRect.gameObject.AddComponent<Image>();avatars[i].preserveAspect=true;avatars[i].raycastTarget=false;
            foreach(var p in FindObjectsByType<PlayerController>(FindObjectsSortMode.None))if(p.PlayerId==i+1){var sr=p.GetComponentInChildren<SpriteRenderer>();if(sr){avatars[i].sprite=sr.sprite;avatars[i].color=sr.color;}}
            Icon("Happy",back,new Vector2(.57f,-.06f),new Vector2(1.1f,.47f),VisualIcon.Shape.Smile,new Color(1,.83f,.20f));
        }
        var track=Box("Teamwork bar",card,new Vector2(.12f,.36f),new Vector2(.88f,.47f),new Color(.83f,.87f,.85f));
        fill=Box("Earned teamwork",track,Vector2.zero,new Vector2(0,1),progressColor);
        Button("Blue Replay",card,new Vector2(.10f,.06f),new Vector2(.34f,.29f),blue,VisualIcon.Shape.Replay,()=>manager.RequestRetry(1));
        Button("Next Level",card,new Vector2(.39f,.06f),new Vector2(.61f,.29f),new Color(.22f,.56f,.29f),VisualIcon.Shape.Arrow,manager.Continue);
        Button("Red Replay",card,new Vector2(.66f,.06f),new Vector2(.90f,.29f),red,VisualIcon.Shape.Replay,()=>manager.RequestRetry(2));
        // Researcher-only dashboard. Keep it separate from the children's UI.
        researcher=Box("Researcher tools (F8)",canvas.transform,new Vector2(.035f,.045f),new Vector2(.965f,.955f),new Color(.945f,.957f,.975f));
        AdultLabel(researcher,"PLAYTEST DASHBOARD",new Vector2(.035f,.915f),new Vector2(.66f,.975f),32,ink,TextAlignmentOptions.MidlineLeft);
        AdultButton("Close  X",researcher,new Vector2(.865f,.925f),new Vector2(.97f,.975f),()=>researcher.gameObject.SetActive(false));
        AdultButton("Export CSV",researcher,new Vector2(.035f,.835f),new Vector2(.32f,.9f),()=>{var path=PlaytestData.Store.ExportCsv();exportStatus=path??PlaytestData.Store.LastError;dashboardNextUpdate=0;});
        newPairButton=AdultButton("New anonymous pair",researcher,new Vector2(.34f,.835f),new Vector2(.68f,.9f),manager.StartNewPair);
        AdultLabel(researcher,"F8 to toggle",new Vector2(.72f,.84f),new Vector2(.96f,.89f),21,new Color(.40f,.45f,.52f),TextAlignmentOptions.MidlineRight);

        var levelCard=DashboardCard("LEVEL RESULTS",researcher,new Vector2(.035f,.45f),new Vector2(.965f,.815f),ink);
        levelSummary=AdultLabel(levelCard,"",new Vector2(.035f,.07f),new Vector2(.965f,.79f),23,ink,TextAlignmentOptions.TopLeft);
        var blueCard=DashboardCard("PLAYER 1  /  BLUE",researcher,new Vector2(.035f,.13f),new Vector2(.49f,.43f),blue);
        blueSummary=AdultLabel(blueCard,"",new Vector2(.045f,.05f),new Vector2(.955f,.78f),22,ink,TextAlignmentOptions.TopLeft);
        var redCard=DashboardCard("PLAYER 2  /  RED",researcher,new Vector2(.51f,.13f),new Vector2(.965f,.43f),red);
        redSummary=AdultLabel(redCard,"",new Vector2(.045f,.05f),new Vector2(.955f,.78f),22,ink,TextAlignmentOptions.TopLeft);
        researcherStatus=AdultLabel(researcher,"",new Vector2(.04f,.025f),new Vector2(.96f,.11f),18,new Color(.35f,.4f,.47f),TextAlignmentOptions.TopLeft);
        researcher.gameObject.SetActive(false);
        var developerCanvas=researcher.gameObject.AddComponent<Canvas>();developerCanvas.overrideSorting=true;developerCanvas.sortingOrder=500;researcher.gameObject.AddComponent<GraphicRaycaster>();
        card.gameObject.SetActive(false);panel.SetAsLastSibling();
        var overlay=new GameObject("Visual Hint Overlay",typeof(Canvas),typeof(CanvasScaler));overlay.transform.SetParent(transform,false);
        spotlightCanvas=overlay.GetComponent<Canvas>();spotlightCanvas.renderMode=RenderMode.ScreenSpaceOverlay;spotlightCanvas.sortingOrder=200;
        var scaler=overlay.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(2048,1536);scaler.matchWidthOrHeight=.5f;
        var dim=Rect("Dim outside targets",overlay.transform,Vector2.zero,Vector2.one);shade=dim.gameObject.AddComponent<HintSpotlight>();shade.color=new Color(0,0,0,hintDimOpacity);shade.raycastTarget=false;
        overlay.SetActive(false);
    }
    void Update()
    {
        if(!manager)return;
        if(newPairButton)newPairButton.interactable=manager.LevelCompleted&&!manager.IsTransitioning;
        if(Keyboard.current!=null&&Keyboard.current.f8Key.wasPressedThisFrame ){researcher.gameObject.SetActive(!researcher.gameObject.activeSelf);dashboardNextUpdate=0;}
        if(researcher.gameObject.activeSelf&&Time.unscaledTime>=dashboardNextUpdate){dashboardNextUpdate=Time.unscaledTime+.25f;RefreshResearcherDashboard();}
        if(spotlightCanvas&&spotlightCanvas.gameObject.activeSelf&&guidePlayers!=null)
        {
            for(int i=0;i<guidePlayers.Length;i++)if(guidePlayers[i]&&IsGuidedPlayer(guidePlayers[i].PlayerId)&&Vector3.Distance(guidePositions[i],guidePlayers[i].transform.position)>.05f)
            {if(guideClocks.TryGetValue(guidePlayers[i].PlayerId,out var clock))clock.NotifyActivity(Time.unscaledTime);guidePositions[i]=guidePlayers[i].transform.position;}
            foreach(var pair in guideClocks)if(handDemonstrations&&pair.Value.ShouldRepeat(Time.unscaledTime)){RepeatHandDemo(pair.Key);DemonstrationRepeated?.Invoke(pair.Key);}
            UpdateSpots();
        }
    }
    // Adapt the existing analytics formatters to compact, readable cards.
    // No new analytics model or hardcoded statistics are introduced.
    void RefreshResearcherDashboard()
    {
        // The footer is the tutorial overview; player details have their own cards.
        string levels = LevelPlaytestDashboard.Format(PlaytestData.Store) ?? "";
        string tutorial = TutorialDashboard.Format(PlaytestData.Store) ?? "";
        if (levelSummary) levelSummary.text = levels.Trim();

        const string blueHeader = "Player 1 (Blue)";
        const string redHeader = "Player 2 (Red)";
        int blueStart = tutorial.IndexOf(blueHeader, System.StringComparison.OrdinalIgnoreCase);
        int redStart = tutorial.IndexOf(redHeader, System.StringComparison.OrdinalIgnoreCase);
        string overview;
        if (blueStart >= 0 && redStart > blueStart)
        {
            if (blueSummary) blueSummary.text = tutorial.Substring(
                blueStart + blueHeader.Length,
                redStart - blueStart - blueHeader.Length).Trim();
            if (redSummary) redSummary.text = tutorial.Substring(redStart + redHeader.Length).Trim();
            overview = tutorial.Substring(0, blueStart).Trim();
        }
        else
        {
            if (blueSummary) blueSummary.text = "No player 1 tutorial data yet";
            if (redSummary) redSummary.text = "No player 2 tutorial data yet";
            overview = tutorial;
        }

        if (researcherStatus)
            researcherStatus.text = overview +
                (string.IsNullOrEmpty(storageError) ? "" : "\nStorage: " + storageError) +
                (string.IsNullOrEmpty(exportStatus) ? "" : "\nCSV: " + exportStatus);
    }

    RectTransform DashboardCard(string heading,Transform parent,Vector2 min,Vector2 max,Color accent)
    {
        var cardRect=Box(heading,parent,min,max,Color.white);
        var stripe=Box("Accent",cardRect,new Vector2(0,0),new Vector2(.008f,1),accent);
        AdultLabel(cardRect,heading,new Vector2(.035f,.80f),new Vector2(.965f,.96f),24,accent,TextAlignmentOptions.MidlineLeft);
        return cardRect;
    }

    TMP_Text AdultLabel(Transform parent,string content,Vector2 min,Vector2 max,float fontSize,Color color,TextAlignmentOptions alignment)
    {
        var rect=Rect("Dashboard text",parent,min,max);
        var label=rect.gameObject.AddComponent<TextMeshProUGUI>();
        label.text=content;label.fontSize=fontSize;label.enableAutoSizing=true;
        label.fontSizeMin=14;label.fontSizeMax=fontSize;label.alignment=alignment;
        label.color=color;label.raycastTarget=false;label.enableWordWrapping=true;
        return label;
    }

    public void ShowVisualHint(List<HintSpot> targets,float repeatAfterSeconds=5f,string contextKey=null)
    {
        // Avoid a lingering dark overlay after the last tutorial objective disappears.
        if(targets==null || targets.Count==0){HideHint();return;}
        bool preserve=contextKey!=null&&contextKey==guidanceContext;
        var previousClocks=preserve?new Dictionary<int,GuidanceTiming>(guideClocks):null;
        var previousStarts=preserve?new Dictionary<int,float>(handStarts):null;
        HideHint();guidanceContext=contextKey;spots.AddRange(targets);spotlightCanvas.gameObject.SetActive(true);
        foreach(var target in targets)if(!string.IsNullOrEmpty(target.Button)||target.ControlRect)
        {
            guideClocks[target.Player]=preserve&&previousClocks.ContainsKey(target.Player)?previousClocks[target.Player]:new GuidanceTiming(Time.unscaledTime,repeatAfterSeconds);
            handStarts[target.Player]=preserve&&previousStarts.ContainsKey(target.Player)?previousStarts[target.Player]:Time.unscaledTime;
        }
        guidePlayers=FindObjectsByType<PlayerController>(FindObjectsSortMode.None);guidePositions=new Vector3[guidePlayers.Length];
        for(int i=0;i<guidePlayers.Length;i++)guidePositions[i]=guidePlayers[i].transform.position;
        foreach(var target in spots)
        {
            var arrow=Icon("Look here",spotlightCanvas.transform,Vector2.zero,Vector2.zero,VisualIcon.Shape.Arrow,new Color(1,.86f,.20f));arrow.GetComponent<VisualIcon>().Rotation=-90;arrows.Add(arrow);
            RectTransform hand=null;
            // Keep the yellow indicator; do not cover the target with a second pointer.
            hands.Add(hand);
        }
        UpdateSpots();
    }
    bool IsGuidedPlayer(int playerId)
    {foreach(var spot in spots)if(spot.Player==playerId&&(!string.IsNullOrEmpty(spot.Button)||spot.ControlRect))return true;return false;}
    public bool RepeatHandDemo(int playerId=0)
    {
        bool replayed=false;foreach(var pair in guideClocks)if(playerId==0||pair.Key==playerId){handStarts[pair.Key]=Time.unscaledTime;pair.Value.NotifyDemonstration(Time.unscaledTime);replayed=true;}return replayed;
    }
    void UpdateSpots()
    {
        shade.Holes.Clear();var root=shade.rectTransform;var camera=Camera.main;
        for(int i=0;i<spots.Count;i++)
        {
            var target=spots[i];Rect rect;
            if(!string.IsNullOrEmpty(target.Button)||target.ControlRect)
            {
                var control=target.ControlRect?target.ControlRect:canvas.transform.Find((target.Player==1?"P1_Controls/":"P2_Controls/")+target.Button) as RectTransform;
                if(!control){arrows[i].gameObject.SetActive(false);if(hands[i])hands[i].gameObject.SetActive(false);continue;}
                var corners=new Vector3[4];control.GetWorldCorners(corners);
                RectTransformUtility.ScreenPointToLocalPointInRectangle(root,RectTransformUtility.WorldToScreenPoint(canvas.renderMode==RenderMode.ScreenSpaceOverlay?null:canvas.worldCamera,corners[0]),null,out var a);
                RectTransformUtility.ScreenPointToLocalPointInRectangle(root,RectTransformUtility.WorldToScreenPoint(canvas.renderMode==RenderMode.ScreenSpaceOverlay?null:canvas.worldCamera,corners[2]),null,out var b);rect=UnityEngine.Rect.MinMaxRect(a.x-16,a.y-16,b.x+16,b.y+16);
            }
            else
            {
                if(!camera)continue;var screen=camera.WorldToScreenPoint(target.WorldTarget?target.WorldTarget.position:target.Position);RectTransformUtility.ScreenPointToLocalPointInRectangle(root,screen,null,out var point);rect=new Rect(point-new Vector2(85,70),new Vector2(170,140));
            }
            shade.Holes.Add(rect);
            float emphasis=guideClocks.TryGetValue(target.Player,out var timing)?timing.Emphasis(Time.unscaledTime):0;
            if(hands[i])
            {
                float elapsed=Time.unscaledTime-(handStarts.TryGetValue(target.Player,out float started)?started:Time.unscaledTime);hands[i].gameObject.SetActive(handDemonstrations&&elapsed<1.8f);
                // The fingertip lands at the actual target center; movement holds longer than a jump tap.
                float phase=Mathf.Clamp01(elapsed/.55f);float press=Mathf.Sin(phase*Mathf.PI*.5f);
                float release=target.Tap?.9f:1.4f;
                float lift=elapsed>release?Mathf.Clamp01((elapsed-release)/.4f)*55:0;
                hands[i].anchorMin=hands[i].anchorMax=Vector2.one*.5f;
                hands[i].sizeDelta=new Vector2(130,150)*(1+.12f*emphasis);
                // Point down from above for bottom-edge controls, keeping the entire hand on screen.
                bool fromAbove=rect.center.y-root.rect.yMin<190*(1+.12f*emphasis);
                var icon=hands[i].GetComponent<VisualIcon>();float rotation=fromAbove?180:0;
                if(icon.Rotation!=rotation){icon.Rotation=rotation;icon.SetVerticesDirty();}
                float sign=fromAbove?-1:1;
                var fingertip=new Vector2(-.06f,.40f)*Mathf.Min(hands[i].sizeDelta.x,hands[i].sizeDelta.y)*sign;
                hands[i].anchoredPosition=rect.center-fingertip-Vector2.up*sign*((1-press)*65+lift);
            }
            arrows[i].anchorMin=arrows[i].anchorMax=Vector2.one*.5f;arrows[i].anchoredPosition=new Vector2(rect.center.x,rect.yMax+55+9*Mathf.Sin(Time.unscaledTime*3));arrows[i].sizeDelta=new Vector2(95,95)*(1+.12f*emphasis);
        }
        float strongest=0;foreach(var clock in guideClocks.Values)strongest=Mathf.Max(strongest,clock.Emphasis(Time.unscaledTime));
        shade.color=new Color(0,0,0,Mathf.Min(.76f,hintDimOpacity+strongest*.04f));
        shade.Refresh();
    }
    public void HideHint()
    {
        if(spotlightCanvas)spotlightCanvas.gameObject.SetActive(false);
        // Disable visuals now, rather than waiting until Unity destroys them at frame end.
        foreach(var arrow in arrows)if(arrow){arrow.gameObject.SetActive(false);Destroy(arrow.gameObject);}
        foreach(var hand in hands)if(hand){hand.gameObject.SetActive(false);Destroy(hand.gameObject);}
        arrows.Clear();hands.Clear();spots.Clear();guideClocks.Clear();handStarts.Clear();guidanceContext=null;
    }
    public void ShowResults(bool full,float score,float? previous,float? best)
    {
        ResultsVisible=true;HideHint();hud.gameObject.SetActive(false);
        foreach(var name in new[]{"P1_Controls","P2_Controls"}){var controls=canvas.transform.Find(name);if(controls)controls.gameObject.SetActive(false);}
        card.gameObject.SetActive(true);panelGroup.blocksRaycasts=true;panelGroup.interactable=true;
        if(celebration)celebration.PlayWin();else panelGroup.alpha=1;
        StartCoroutine(Celebrate(score));
    }
    IEnumerator Celebrate(float score)
    {
        float elapsed=0;
        while(elapsed<scoreAnimationSeconds)
        {
            elapsed+=Time.unscaledDeltaTime;float t=Mathf.Clamp01(elapsed/scoreAnimationSeconds);fill.anchorMax=new Vector2(Mathf.Clamp01(score/100f)*(1-Mathf.Pow(1-t,3)),1);
            for(int i=0;i<stars.Length;i++)stars[i].localScale=Vector3.one*(1+.12f*Mathf.Sin(t*Mathf.PI));yield return null;
        }
        fill.anchorMax=new Vector2(Mathf.Clamp01(score/100f),1);foreach(var star in stars)star.localScale=Vector3.one;
    }
    public void ShowStorageError(string message){storageError=message;Debug.LogError("[Playtest] "+message);}
    RectTransform Box(string name,Transform parent,Vector2 min,Vector2 max,Color color)
    {var rect=Rect(name,parent,min,max);var image=rect.gameObject.AddComponent<Image>();image.sprite=rounded;image.type=rounded?Image.Type.Sliced:Image.Type.Simple;image.color=color;image.raycastTarget=false;return rect;}
    RectTransform Icon(string name,Transform parent,Vector2 min,Vector2 max,VisualIcon.Shape shape,Color color)
    {var rect=Rect(name,parent,min,max);var icon=rect.gameObject.AddComponent<VisualIcon>();icon.shape=shape;icon.color=color;icon.raycastTarget=false;return rect;}
    void Button(string name,Transform parent,Vector2 min,Vector2 max,Color color,VisualIcon.Shape shape,UnityAction action)
    {var rect=Box(name,parent,min,max,color);var image=rect.GetComponent<Image>();image.raycastTarget=true;var button=rect.gameObject.AddComponent<Button>();button.targetGraphic=image;button.navigation=new Navigation{mode=Navigation.Mode.None};button.onClick.AddListener(action);Icon("Icon",rect,new Vector2(.15f,.12f),new Vector2(.85f,.88f),shape,Color.white);}
    UnityEngine.UI.Button AdultButton(string label,Transform parent,Vector2 min,Vector2 max,UnityAction action)
    {var rect=Box(label,parent,min,max,ink);var image=rect.GetComponent<Image>();image.raycastTarget=true;var button=rect.gameObject.AddComponent<Button>();button.targetGraphic=image;button.navigation=new Navigation{mode=Navigation.Mode.None};button.onClick.AddListener(action);var text=AdultText(rect,label,new Vector2(.02f,.02f),new Vector2(.98f,.98f));text.color=Color.white;return button;}
    TMP_Text AdultText(Transform parent,string content,Vector2 min,Vector2 max)
    {var rect=Rect("Adult-only data",parent,min,max);var text=rect.gameObject.AddComponent<TextMeshProUGUI>();text.text=content;text.fontSize=28;text.enableAutoSizing=true;text.fontSizeMin=16;text.fontSizeMax=28;text.alignment=TextAlignmentOptions.Center;text.color=ink;text.raycastTarget=false;return text;}
    static RectTransform Rect(string name,Transform parent,Vector2 min,Vector2 max)
    {var rect=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();rect.SetParent(parent,false);rect.anchorMin=min;rect.anchorMax=max;rect.offsetMin=rect.offsetMax=Vector2.zero;return rect;}
}
