using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using SharedUI;
public static class SharedUIBuilder
{
    const string Root="Assets/SharedUI/";
    static UITheme theme;
    static RectTransform Rect(string name,Transform parent=null)
    {
        var r=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();
        if(parent){r.SetParent(parent,false);r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=r.offsetMax=Vector2.zero;}
        else r.sizeDelta=new Vector2(320,100);
        return r;
    }
    static Image Image(RectTransform r,Color color){var i=r.gameObject.AddComponent<Image>();i.sprite=theme.panel;i.type=UnityEngine.UI.Image.Type.Sliced;i.color=color;i.raycastTarget=false;return i;}
    static TMP_Text Label(Transform parent,string text)
    {
        var r=Rect("Label",parent);var t=r.gameObject.AddComponent<TextMeshProUGUI>();t.font=theme.font;t.text=text;t.color=theme.ink;t.fontSize=theme.bodySize;t.enableAutoSizing=true;t.fontSizeMin=theme.minimumSize;t.fontSizeMax=theme.bodySize;t.alignment=TextAlignmentOptions.Center;t.raycastTarget=false;return t;
    }
    static void Save(RectTransform r,string name){foreach(var icon in r.GetComponentsInChildren<VisualIcon>(true))icon.RefreshArtwork();var style=r.gameObject.AddComponent<UIStyle>();style.theme=theme;style.graphic=r.GetComponent<Graphic>();if(style.graphic){var color=style.graphic.color;style.role=color==theme.secondary?UIStyle.Role.Secondary:color==theme.surface?UIStyle.Role.Surface:color==theme.ink?UIStyle.Role.Ink:color==theme.progress?UIStyle.Role.Progress:color==theme.gold?UIStyle.Role.Gold:UIStyle.Role.Primary;}style.label=r.GetComponentInChildren<TMP_Text>();style.selectable=r.GetComponent<Selectable>();PrefabUtility.SaveAsPrefabAsset(r.gameObject,Root+"Prefabs/Resources/SharedUI/"+name+".prefab");UnityEngine.Object.DestroyImmediate(r.gameObject);}
    static RectTransform Button(string name,Color color,bool label=true)
    {
        var r=Rect(name);var i=Image(r,color);i.raycastTarget=true;var b=r.gameObject.AddComponent<Button>();b.targetGraphic=i;b.colors=theme.buttonColors;b.navigation=new Navigation{mode=Navigation.Mode.None};
        if(label)Label(r,name);return r;
    }
    [MenuItem("Tools/Shared UI/Build library")]
    public static void Build()
    {
        theme=AssetDatabase.LoadAssetAtPath<UITheme>(Root+"Themes/TogetherWeMove.asset");
        if(!theme){theme=ScriptableObject.CreateInstance<UITheme>();AssetDatabase.CreateAsset(theme,Root+"Themes/TogetherWeMove.asset");}
        theme.panel=Resources.Load<Sprite>("KenneyUI/Panel");
        const string fontPath=Root+"Fonts/SharedDefault SDF.asset";
        if(!File.Exists(fontPath))AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(TMP_Settings.defaultFontAsset),fontPath);
        theme.font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(fontPath);
        var colors=ColorBlock.defaultColorBlock;colors.fadeDuration=.1f;theme.buttonColors=colors;EditorUtility.SetDirty(theme);
        var r=Rect("Panel");Image(r,theme.surface);Save(r,"Panel");
        r=Rect("Label");var text=r.gameObject.AddComponent<TextMeshProUGUI>();text.font=theme.font;text.fontSize=28;text.enableAutoSizing=true;text.fontSizeMin=16;text.fontSizeMax=28;text.alignment=TextAlignmentOptions.Center;text.raycastTarget=false;Save(r,"Label");
        Save(Button("PrimaryButton",theme.primary),"PrimaryButton");Save(Button("SecondaryButton",theme.secondary),"SecondaryButton");
        r=Button("DisabledButton",theme.primary);r.GetComponent<Button>().interactable=false;Save(r,"DisabledButton");
        r=Button("IconButton",theme.primary,false);var iconRect=Rect("Icon",r);iconRect.anchorMin=new Vector2(.15f,.12f);iconRect.anchorMax=new Vector2(.85f,.88f);var icon=iconRect.gameObject.AddComponent<VisualIcon>();icon.raycastTarget=false;icon.color=Color.white;Save(r,"IconButton");
        r=Rect("ProgressBar");Image(r,new Color(.83f,.87f,.85f));var fill=Rect("Fill",r);Image(fill,theme.progress);var progress=r.gameObject.AddComponent<UIProgress>();progress.fill=fill;progress.SetValue(.75f);Save(r,"ProgressBar");
        r=Rect("Slider");Image(r,new Color(.83f,.87f,.85f));fill=Rect("Fill",r);Image(fill,theme.progress);var handle=Rect("Handle",r);handle.sizeDelta=new Vector2(32,0);var handleImage=Image(handle,theme.primary);handleImage.raycastTarget=true;var slider=r.gameObject.AddComponent<Slider>();slider.fillRect=fill;slider.handleRect=handle;slider.targetGraphic=handleImage;slider.value=.5f;Save(r,"Slider");
        foreach(string name in new[]{"Popup","Dialog","Notification","TutorialHint","Menu","Navigation"})
        {
            r=Rect(name);r.sizeDelta=new Vector2(700,450);Image(r,theme.surface);r.gameObject.AddComponent<CanvasGroup>();r.gameObject.AddComponent<UIPanel>();var heading=Label(r,name);heading.rectTransform.anchorMin=new Vector2(.05f,.7f);heading.rectTransform.anchorMax=new Vector2(.95f,.95f);
            var button=Button(name=="Dialog"?"Confirm":"Continue",theme.primary);button.SetParent(r,false);button.anchorMin=new Vector2(.3f,.08f);button.anchorMax=new Vector2(.7f,.28f);button.offsetMin=button.offsetMax=Vector2.zero;
            if(name=="Dialog"){var cancel=Button("Cancel",theme.secondary);cancel.SetParent(r,false);cancel.anchorMin=new Vector2(.05f,.08f);cancel.anchorMax=new Vector2(.28f,.28f);cancel.offsetMin=cancel.offsetMax=Vector2.zero;}
            Save(r,name);
        }
        r=Rect("ScoreDisplay");Image(r,theme.surface);var label=Label(r,"75%");progress=r.gameObject.AddComponent<UIProgress>();progress.label=label;progress.SetValue(.75f);Save(r,"ScoreDisplay");
        r=Rect("CompletionIndicator");icon=r.gameObject.AddComponent<VisualIcon>();icon.shape=VisualIcon.Shape.Star;icon.color=theme.gold;icon.raycastTarget=false;Save(r,"CompletionIndicator");
        r=Rect("Icon");icon=r.gameObject.AddComponent<VisualIcon>();icon.raycastTarget=false;Save(r,"Icon");
        r=Rect("HintArrow");icon=r.gameObject.AddComponent<VisualIcon>();icon.shape=VisualIcon.Shape.Arrow;icon.color=new Color(1,.86f,.20f);icon.raycastTarget=false;r.gameObject.AddComponent<UIPulse>();Save(r,"HintArrow");
        r=Rect("HintSpotlight");var shade=r.gameObject.AddComponent<HintSpotlight>();shade.color=new Color(0,0,0,.62f);shade.raycastTarget=false;Save(r,"HintSpotlight");
        r=Rect("ResponsiveCanvas");r.gameObject.AddComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;var scaler=r.gameObject.AddComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=theme.referenceResolution;scaler.matchWidthOrHeight=.5f;r.gameObject.AddComponent<GraphicRaycaster>();Rect("SafeArea",r).gameObject.AddComponent<UISafeArea>();Save(r,"ResponsiveCanvas");
        AssetDatabase.SaveAssets();Validate();
    }
    [MenuItem("Tools/Shared UI/Validate library")]
    public static void Validate()
    {
        var files=AssetDatabase.FindAssets("t:Prefab",new[]{Root+"Prefabs"});
        foreach(var guid in files){var path=AssetDatabase.GUIDToAssetPath(guid);var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(path);foreach(var c in prefab.GetComponentsInChildren<Transform>(true))if(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(c.gameObject)>0)throw new Exception("Missing script "+path);foreach(var b in prefab.GetComponentsInChildren<Button>(true))if(b.onClick.GetPersistentEventCount()!=0)throw new Exception("Game callback in reusable prefab "+path);}
        Directory.CreateDirectory("Logs/SharedUI");File.WriteAllText("Logs/SharedUI/validation.txt","PASS: "+files.Length+" prefabs; no missing scripts or persistent game callbacks.\n");Debug.Log("[SharedUI] Validation passed: "+files.Length);
    }
    [MenuItem("Tools/Shared UI/Export transferable package")]
    public static void Export(){Validate();Directory.CreateDirectory("Exports");AssetDatabase.ExportPackage(Root.TrimEnd('/'),"Exports/TogetherWeMove-SharedUI.unitypackage",ExportPackageOptions.Recurse|ExportPackageOptions.IncludeDependencies);}
}

