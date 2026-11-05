using System;
using UnityEngine;
using UnityEngine.Audio;
namespace SharedGameAudio
{
    /// <summary>Independent 2D audio service. No scene names, gameplay types or per-frame allocations.</summary>
    [DefaultExecutionOrder(-1000)]
    public sealed class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }
        public SoundLibrary library;
        public AudioMixer mixer;
        public AudioMixerGroup musicGroup,sfxGroup;
        [Range(4,24)] public int poolSize=12;
        [Min(.05f)] public float crossfadeSeconds=.8f;
        const string Prefix="SharedGameAudio.v1.";
        public float MusicVolume {get;private set;}
        public float SfxVolume {get;private set;}
        public bool MusicMuted {get;private set;}
        public bool SfxMuted {get;private set;}
        public bool Muted => MusicMuted && SfxMuted;
        public event Action SettingsChanged;
        readonly System.Random random=new System.Random();
        readonly AudioSource[] music=new AudioSource[2];
        AudioSource[] voices;
        SoundId[] voiceIds;
        readonly float[] nextAllowed=new float[Enum.GetValues(typeof(SoundId)).Length*3];
        readonly float[] weights=new float[2],starts=new float[2];
        int target=-1;
        float fadeElapsed;
        bool ready,dirty;
        float saveAt;
        readonly int[] played=new int[Enum.GetValues(typeof(SoundId)).Length];
        public int PlayedCount(SoundId id)=>played[(int)id];
        public AudioClip CurrentMusic => target>=0?music[target].clip:null;
        public int ActiveVoiceCount {get{int count=0;foreach(var voice in voices)if(voice.isPlaying)count++;return count;}}
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics(){Instance=null;}
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Bootstrap()
        {
            if(Instance || FindFirstObjectByType<AudioManager>())return;
            var prefab=Resources.Load<GameObject>("SharedGameAudio/AudioManager");
            if(prefab)Instantiate(prefab).name="Shared Game Audio";
        }
        void Awake()
        {
            if(Instance && Instance!=this){Destroy(gameObject);return;}
            Instance=this;DontDestroyOnLoad(gameObject);
            MusicVolume=Mathf.Clamp01(PlayerPrefs.GetFloat(Prefix+"music",.55f));
            SfxVolume=Mathf.Clamp01(PlayerPrefs.GetFloat(Prefix+"sfx",.75f));
            MusicMuted=PlayerPrefs.GetInt(Prefix+"musicMuted",0)!=0;SfxMuted=PlayerPrefs.GetInt(Prefix+"sfxMuted",0)!=0;
            for(int i=0;i<2;i++){music[i]=Source("Music "+i,musicGroup);music[i].loop=true;music[i].priority=16;music[i].volume=0;}
            voices=new AudioSource[poolSize];voiceIds=new SoundId[poolSize];
            for(int i=0;i<poolSize;i++)voices[i]=Source("SFX "+i,sfxGroup);
        }
        AudioSource Source(string label,AudioMixerGroup group)
        {
            var obj=new GameObject(label);obj.transform.SetParent(transform,false);
            var source=obj.AddComponent<AudioSource>();source.playOnAwake=false;source.spatialBlend=0;source.outputAudioMixerGroup=group;source.dopplerLevel=0;
            return source;
        }
        void Start(){ready=true;ApplyVolumes();}
        public bool Play(SoundId id,int emitter=0)
        {
            if(SfxMuted||SfxVolume<=0||library==null)return false;
            var entry=library.Get(id);int index=(int)id*3+Mathf.Clamp(emitter,0,2);
            if(entry==null||entry.clips==null||entry.clips.Length==0||Time.unscaledTime<nextAllowed[index])return false;
            int active=0,free=-1;
            for(int i=0;i<voices.Length;i++){if(voices[i].isPlaying){if(voiceIds[i]==id)active++;}else if(free<0)free=i;}
            // Optional cues drop at capacity; important cues may replace less important voices.
            if(active>=entry.maxVoices)return false;
            if(free<0)
            {
                int leastImportant=entry.priority;
                for(int i=0;i<voices.Length;i++)if(voices[i].priority>leastImportant){leastImportant=voices[i].priority;free=i;}
                if(free<0)return false;
                voices[free].Stop();
            }
            var clip=entry.clips[random.Next(entry.clips.Length)];if(!clip)return false;
            var voice=voices[free];voiceIds[free]=id;voice.clip=clip;voice.pitch=1+((float)random.NextDouble()*2-1)*entry.pitchVariation;
            voice.priority=entry.priority;voice.volume=entry.volume*(sfxGroup?1:SfxVolume);voice.Play();played[(int)id]++;
            nextAllowed[index]=Time.unscaledTime+entry.cooldown;return true;
        }
        public static void PlaySound(SoundId id,int emitter=0){if(Instance)Instance.Play(id,emitter);}
        public void PlayMusic(AudioClip clip,float fadeSeconds=-1,bool loop=true)
        {
            if(clip && CurrentMusic==clip)return;
            for(int i=0;i<2;i++)starts[i]=weights[i];
            if(!clip)target=-1;
            else
            {
                // Reuse an already playing destination on rapid scene changes.
                int next=music[0].clip==clip&&music[0].isPlaying?0:music[1].clip==clip&&music[1].isPlaying?1:weights[0]<=weights[1]?0:1;
                if(music[next].clip!=clip||!music[next].isPlaying){music[next].Stop();music[next].clip=clip;music[next].loop=loop;music[next].Play();weights[next]=starts[next]=0;}
                target=next;
            }
            fadeElapsed=0;fadeDuration=fadeSeconds<0?crossfadeSeconds:Mathf.Max(.01f,fadeSeconds);
        }
        float fadeDuration=.8f;
        void Update()
        {
            fadeElapsed+=Time.unscaledDeltaTime;float t=Mathf.Clamp01(fadeElapsed/fadeDuration);
            for(int i=0;i<2;i++)
            {
                weights[i]=Mathf.Lerp(starts[i],target==i?1:0,t);
                music[i].volume=weights[i]*(musicGroup?1:MusicMuted?0:MusicVolume);
                if(t>=1&&i!=target&&music[i].isPlaying)music[i].Stop();
            }
            if(dirty&&Time.unscaledTime>=saveAt)SavePreferences();
        }
        public void SetMusicVolume(float value){MusicVolume=Mathf.Clamp01(value);Changed();}
        public void SetSfxVolume(float value){SfxVolume=Mathf.Clamp01(value);Changed();}
        public void SetMusicMuted(bool value){MusicMuted=value;Changed();}
        public void SetSfxMuted(bool value){SfxMuted=value;Changed();}
        public void SetMuted(bool value){MusicMuted=SfxMuted=value;Changed();}
        void Changed(){ApplyVolumes();dirty=true;saveAt=Time.unscaledTime+.4f;SettingsChanged?.Invoke();}
        static float Db(float linear)=>linear<=.0001f?-80:Mathf.Log10(linear)*20;
        void ApplyVolumes()
        {
            if(ready&&mixer){mixer.SetFloat("MusicVolume",Db(MusicMuted?0:MusicVolume));mixer.SetFloat("SFXVolume",Db(SfxMuted?0:SfxVolume));}
            if(voices!=null&&!sfxGroup)for(int i=0;i<voices.Length;i++){var entry=library?library.Get(voiceIds[i]):null;voices[i].volume=SfxMuted?0:(entry?.volume??0)*SfxVolume;}
        }
        public void SavePreferences()
        {
            PlayerPrefs.SetFloat(Prefix+"music",MusicVolume);PlayerPrefs.SetFloat(Prefix+"sfx",SfxVolume);
            PlayerPrefs.SetInt(Prefix+"musicMuted",MusicMuted?1:0);PlayerPrefs.SetInt(Prefix+"sfxMuted",SfxMuted?1:0);PlayerPrefs.Save();dirty=false;
        }
        void OnApplicationPause(bool paused){if(paused&&Instance==this)SavePreferences();}
        void OnApplicationFocus(bool focused){if(!focused&&Instance==this)SavePreferences();}
        void OnDestroy(){if(Instance!=this)return;SavePreferences();Instance=null;}
    }
}
