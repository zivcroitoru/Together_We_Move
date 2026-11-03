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
    RectTransform researcher;TMP_Text researcherStatus;string storageError;
    Canvas spotlightCanvas;HintSpotlight shade;readonly List<HintSpot> spots=new();readonly List<RectTransform> rings=new(),arrows=new();
    public bool ResultsVisible{get;private set;}
    public struct HintSpot
    {
        public Vector3 Position;public int Player;public string Button;
        public static HintSpot World(Vector3 position,int player)=>new HintSpot{Position=position,Player=player};
        public static HintSpot Control(int player,string button)=>new HintSpot{Player=player,Button=button};
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
        researcher=Box("Researcher tools (F8)",panel,new Vector2(.14f,.01f),new Vector2(.86f,.16f),Color.white);
        AdultButton("Export CSV",researcher,new Vector2(.02f,.5f),new Vector2(.48f,.95f),()=>{var path=PlaytestData.Store.ExportCsv();researcherStatus.text=path??PlaytestData.Store.LastError;});
        AdultButton("New anonymous pair",researcher,new Vector2(.52f,.5f),new Vector2(.98f,.95f),manager.StartNewPair);
        researcherStatus=AdultText(researcher,"",new Vector2(.02f,.02f),new Vector2(.98f,.48f));researcher.gameObject.SetActive(false);
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
        if(Keyboard.current!=null&&Keyboard.current.f8Key.wasPressedThisFrame&&ResultsVisible){researcher.gameObject.SetActive(!researcher.gameObject.activeSelf);researcherStatus.text=storageError??PlaytestData.Store.LastError??"Local anonymous playtest data";}
        if(spotlightCanvas&&spotlightCanvas.gameObject.activeSelf)UpdateSpots();
    }
    public void ShowVisualHint(List<HintSpot> targets)
    {
        HideHint();spots.AddRange(targets);spotlightCanvas.gameObject.SetActive(true);
        foreach(var target in spots)
        {
            var ring=Icon("Pulse highlight",spotlightCanvas.transform,Vector2.zero,Vector2.zero,VisualIcon.Shape.Ring,target.Player==1?blue:red);rings.Add(ring);
            var arrow=Icon("Look here",spotlightCanvas.transform,Vector2.zero,Vector2.zero,VisualIcon.Shape.Arrow,new Color(1,.86f,.20f));arrow.GetComponent<VisualIcon>().Rotation=-90;arrows.Add(arrow);
        }
        UpdateSpots();
    }
    void UpdateSpots()
    {
        shade.Holes.Clear();var root=shade.rectTransform;var camera=Camera.main;
        for(int i=0;i<spots.Count;i++)
        {
            var target=spots[i];Rect rect;
            if(!string.IsNullOrEmpty(target.Button))
            {
                var control=canvas.transform.Find((target.Player==1?"P1_Controls/":"P2_Controls/")+target.Button) as RectTransform;
                if(!control){rings[i].gameObject.SetActive(false);arrows[i].gameObject.SetActive(false);continue;}
                var corners=new Vector3[4];control.GetWorldCorners(corners);
                RectTransformUtility.ScreenPointToLocalPointInRectangle(root,RectTransformUtility.WorldToScreenPoint(canvas.renderMode==RenderMode.ScreenSpaceOverlay?null:canvas.worldCamera,corners[0]),null,out var a);
                RectTransformUtility.ScreenPointToLocalPointInRectangle(root,RectTransformUtility.WorldToScreenPoint(canvas.renderMode==RenderMode.ScreenSpaceOverlay?null:canvas.worldCamera,corners[2]),null,out var b);rect=UnityEngine.Rect.MinMaxRect(a.x-16,a.y-16,b.x+16,b.y+16);
            }
            else
            {
                if(!camera)continue;var screen=camera.WorldToScreenPoint(target.Position);RectTransformUtility.ScreenPointToLocalPointInRectangle(root,screen,null,out var point);rect=new Rect(point-new Vector2(85,70),new Vector2(170,140));
            }
            shade.Holes.Add(rect);
            rings[i].anchorMin=rings[i].anchorMax=Vector2.one*.5f;rings[i].anchoredPosition=rect.center;rings[i].sizeDelta=new Vector2(Mathf.Max(rect.width,rect.height),Mathf.Max(rect.width,rect.height));rings[i].localScale=Vector3.one*(1f+.05f*Mathf.Sin(Time.unscaledTime*3));
            arrows[i].anchorMin=arrows[i].anchorMax=Vector2.one*.5f;arrows[i].anchoredPosition=new Vector2(rect.center.x,rect.yMax+55+9*Mathf.Sin(Time.unscaledTime*3));arrows[i].sizeDelta=new Vector2(95,95);
        }
        shade.Refresh();
    }
    public void HideHint()
    {
        if(spotlightCanvas)spotlightCanvas.gameObject.SetActive(false);
        foreach(var ring in rings)if(ring)Destroy(ring.gameObject);foreach(var arrow in arrows)if(arrow)Destroy(arrow.gameObject);rings.Clear();arrows.Clear();spots.Clear();
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
    void AdultButton(string label,Transform parent,Vector2 min,Vector2 max,UnityAction action)
    {var rect=Box(label,parent,min,max,ink);var image=rect.GetComponent<Image>();image.raycastTarget=true;var button=rect.gameObject.AddComponent<Button>();button.targetGraphic=image;button.navigation=new Navigation{mode=Navigation.Mode.None};button.onClick.AddListener(action);var text=AdultText(rect,label,new Vector2(.02f,.02f),new Vector2(.98f,.98f));text.color=Color.white;}
    TMP_Text AdultText(Transform parent,string content,Vector2 min,Vector2 max)
    {var rect=Rect("Adult-only data",parent,min,max);var text=rect.gameObject.AddComponent<TextMeshProUGUI>();text.text=content;text.fontSize=28;text.enableAutoSizing=true;text.fontSizeMin=16;text.fontSizeMax=28;text.alignment=TextAlignmentOptions.Center;text.color=ink;text.raycastTarget=false;return text;}
    static RectTransform Rect(string name,Transform parent,Vector2 min,Vector2 max)
    {var rect=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();rect.SetParent(parent,false);rect.anchorMin=min;rect.anchorMax=max;rect.offsetMin=rect.offsetMax=Vector2.zero;return rect;}
}
