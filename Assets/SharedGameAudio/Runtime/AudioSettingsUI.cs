using UnityEngine;
using UnityEngine.UI;
namespace SharedGameAudio
{
    /// <summary>Self-contained uGUI panel; requires the host game's EventSystem.</summary>
    public sealed class AudioSettingsUI : MonoBehaviour
    {
        public Slider musicSlider,sfxSlider;
        public Toggle musicMute,sfxMute;
        public Button openButton,closeButton,muteButton;
        public GameObject panel;
        public Text muteLabel;
        AudioManager manager;
        RectTransform safe;
        Rect previousSafe;
        void Start()
        {
            manager=AudioManager.Instance;if(!manager)return;
            safe=transform.Find("Safe area") as RectTransform;
            musicSlider.onValueChanged.AddListener(manager.SetMusicVolume);sfxSlider.onValueChanged.AddListener(manager.SetSfxVolume);
            musicMute.onValueChanged.AddListener(manager.SetMusicMuted);sfxMute.onValueChanged.AddListener(manager.SetSfxMuted);
            muteButton.onClick.AddListener(ToggleMute);openButton.onClick.AddListener(Open);closeButton.onClick.AddListener(Close);
            manager.SettingsChanged+=Refresh;Refresh();panel.SetActive(false);
        }
        void Update()
        {
            var area=Screen.safeArea;if(!safe||Screen.width==0||Screen.height==0||area==previousSafe)return;
            previousSafe=area;safe.anchorMin=area.min/new Vector2(Screen.width,Screen.height);safe.anchorMax=area.max/new Vector2(Screen.width,Screen.height);
        }
        public void Open(){panel.SetActive(true);Refresh();}
        public void Close(){panel.SetActive(false);if(manager)manager.SavePreferences();}
        void ToggleMute(){manager.SetMuted(!manager.Muted);}
        void Refresh()
        {
            musicSlider.SetValueWithoutNotify(manager.MusicVolume);sfxSlider.SetValueWithoutNotify(manager.SfxVolume);
            musicMute.SetIsOnWithoutNotify(manager.MusicMuted);sfxMute.SetIsOnWithoutNotify(manager.SfxMuted);
            muteLabel.text=manager.Muted?"Unmute all":"Mute all";
        }
        void OnDestroy(){if(manager)manager.SettingsChanged-=Refresh;}
        static readonly Color Ink=new Color(.08f,.19f,.25f),Blue=new Color(.16f,.55f,.86f),Cream=new Color(1,.985f,.93f);
        static RectTransform Rect(string name,Transform parent,Vector2 min,Vector2 max)
        {
            var rect=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();rect.SetParent(parent,false);
            rect.anchorMin=min;rect.anchorMax=max;rect.offsetMin=rect.offsetMax=Vector2.zero;return rect;
        }
        static Image Image(RectTransform rect,Color color,bool hit=false){var image=rect.gameObject.AddComponent<Image>();image.color=color;image.sprite=Resources.Load<Sprite>("SharedGameAudio/Panel");image.type=UnityEngine.UI.Image.Type.Sliced;image.raycastTarget=hit;return image;}
        static Text Label(string value,Transform parent,Vector2 min,Vector2 max,int size=32)
        {
            var text=Rect(value,parent,min,max).gameObject.AddComponent<Text>();text.text=value;text.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize=size;text.color=Ink;text.alignment=TextAnchor.MiddleCenter;text.raycastTarget=false;return text;
        }
        static Button Button(string label,Transform parent,Vector2 min,Vector2 max)
        {
            var rect=Rect(label,parent,min,max);var image=Image(rect,Blue,true);var button=rect.gameObject.AddComponent<Button>();button.targetGraphic=image;
            button.navigation=new Navigation{mode=Navigation.Mode.None};var text=Label(label,rect,Vector2.zero,Vector2.one);text.color=Color.white;
            rect.gameObject.AddComponent<AudioButton>();return button;
        }
        static Slider Slider(Transform parent,string label,float y)
        {
            Label(label,parent,new Vector2(.05f,y+.12f),new Vector2(.60f,y+.23f));
            var track=Rect(label+" slider",parent,new Vector2(.09f,y),new Vector2(.73f,y+.12f));Image(track,new Color(.77f,.85f,.86f),true);
            var fillArea=Rect("Fill area",track,new Vector2(.04f,.33f),new Vector2(.96f,.67f));var fill=Rect("Fill",fillArea,Vector2.zero,Vector2.one);Image(fill,new Color(.2f,.72f,.44f));
            var handleArea=Rect("Handle area",track,new Vector2(.04f,0),new Vector2(.96f,1));var handle=Rect("Handle",handleArea,Vector2.zero,Vector2.one);handle.sizeDelta=new Vector2(48,0);var knob=Image(handle,Blue,true);
            var slider=track.gameObject.AddComponent<Slider>();slider.fillRect=fill;slider.handleRect=handle;slider.targetGraphic=knob;slider.minValue=0;slider.maxValue=1;
            slider.navigation=new Navigation{mode=Navigation.Mode.None};return slider;
        }
        static Toggle Toggle(Transform parent,float y)
        {
            var rect=Rect("Mute channel",parent,new Vector2(.78f,y),new Vector2(.93f,y+.16f));var image=Image(rect,new Color(.77f,.85f,.86f),true);
            var check=Rect("Muted",rect,new Vector2(.2f,.2f),new Vector2(.8f,.8f));var mark=Image(check,Blue);Label("Mute",parent,new Vector2(.75f,y+.16f),new Vector2(.96f,y+.25f),24);
            var toggle=rect.gameObject.AddComponent<Toggle>();toggle.targetGraphic=image;toggle.graphic=mark;toggle.navigation=new Navigation{mode=Navigation.Mode.None};return toggle;
        }
        public static GameObject Create()
        {
            var root=new GameObject("Audio settings",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster),typeof(AudioSettingsUI));
            var canvas=root.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=250;
            var scaler=root.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(2048,1536);scaler.matchWidthOrHeight=.5f;
            var ui=root.GetComponent<AudioSettingsUI>();var safe=Rect("Safe area",root.transform,Vector2.zero,Vector2.one);
            ui.openButton=Button("Audio",safe,new Vector2(.72f,.90f),new Vector2(.81f,.98f));
            var overlay=Rect("Audio panel",safe,Vector2.zero,Vector2.one);Image(overlay,new Color(.03f,.09f,.12f,.6f),true);ui.panel=overlay.gameObject;
            var card=Rect("Card",overlay,new Vector2(.24f,.27f),new Vector2(.76f,.73f));Image(card,Cream,true);
            Label("Audio",card,new Vector2(.1f,.82f),new Vector2(.9f,.97f),42);
            ui.musicSlider=Slider(card,"Music",.49f);ui.sfxSlider=Slider(card,"Sounds",.22f);
            ui.musicMute=Toggle(card,.49f);ui.sfxMute=Toggle(card,.22f);
            ui.muteButton=Button("Mute all",card,new Vector2(.08f,.03f),new Vector2(.52f,.16f));ui.muteLabel=ui.muteButton.GetComponentInChildren<Text>();
            ui.closeButton=Button("Done",card,new Vector2(.60f,.03f),new Vector2(.92f,.16f));ui.panel.SetActive(false);return root;
        }
    }
}
