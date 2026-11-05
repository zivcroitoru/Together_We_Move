using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using SharedGameAudio;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;
using AudioManager=SharedGameAudio.AudioManager;
/// <summary>Opt-in checks use a separate playtest journal and restore audio preferences.</summary>
[InitializeOnLoad] public static class AudioSystemChecks
{
    const string Key="TWM.AudioChecks";
    static IEnumerator routine;static float resumeAt;static string errors="";
    static readonly System.Text.StringBuilder report=new();
    [Serializable] class Preferences {public float music,sfx;public int musicMute,sfxMute;public bool hasMusic,hasSfx,hasMusicMute,hasSfxMute;}
    static AudioSystemChecks(){EditorApplication.update+=Poll;EditorApplication.playModeStateChanged+=Changed;}
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]static void Isolate()
    {
        if(!SessionState.GetBool(Key,false))return;Application.runInBackground=true;
        typeof(PlaytestData).GetField("store",BindingFlags.Static|BindingFlags.NonPublic).SetValue(null,new PlaytestStore(Path.GetFullPath("Logs/Audio/IsolatedPlaytest")));
        Application.logMessageReceived+=Log;
    }
    static void Log(string message,string stack,LogType type){if(type==LogType.Error||type==LogType.Exception)errors+=message+"\n";}
    static void Changed(PlayModeStateChange state)
    {
        if(state!=PlayModeStateChange.EnteredEditMode||!SessionState.GetBool(Key,false))return;
        SessionState.SetBool(Key,false);RestorePrefs();routine=null;TutorialSession.Reset();
        var path=SessionState.GetString(Key+"scene","");if(!string.IsNullOrEmpty(path))EditorSceneManager.OpenScene(path);if(Application.isBatchMode)EditorApplication.delayCall+=()=>EditorApplication.Exit(SessionState.GetBool(Key+"passed",false)?0:1);
    }
    static void RestorePrefs()
    {
        var p=JsonUtility.FromJson<Preferences>(SessionState.GetString(Key+"prefs","{}"));
        Restore("music",p.hasMusic,p.music);Restore("sfx",p.hasSfx,p.sfx);Restore("musicMuted",p.hasMusicMute,p.musicMute,true);Restore("sfxMuted",p.hasSfxMute,p.sfxMute,true);PlayerPrefs.Save();
    }
    static void Restore(string name,bool exists,float value,bool integer=false){string key="SharedGameAudio.v1."+name;if(!exists)PlayerPrefs.DeleteKey(key);else if(integer)PlayerPrefs.SetInt(key,(int)value);else PlayerPrefs.SetFloat(key,value);}
    [MenuItem("Tools/Shared Game Audio/Test both TWM levels")]
    public static void Run()
    {
        Directory.CreateDirectory("Logs/Audio");if(EditorApplication.isPlayingOrWillChangePlaymode||SceneManager.GetActiveScene().isDirty){File.WriteAllText("Logs/Audio/tests.txt","NOT RUN: save scene edits and exit Play Mode first.");return;}
        const string prefix="SharedGameAudio.v1.";
        var prefs=new Preferences{music=PlayerPrefs.GetFloat(prefix+"music"),sfx=PlayerPrefs.GetFloat(prefix+"sfx"),musicMute=PlayerPrefs.GetInt(prefix+"musicMuted"),sfxMute=PlayerPrefs.GetInt(prefix+"sfxMuted"),hasMusic=PlayerPrefs.HasKey(prefix+"music"),hasSfx=PlayerPrefs.HasKey(prefix+"sfx"),hasMusicMute=PlayerPrefs.HasKey(prefix+"musicMuted"),hasSfxMute=PlayerPrefs.HasKey(prefix+"sfxMuted")};
        SessionState.SetString(Key+"prefs",JsonUtility.ToJson(prefs));SessionState.SetString(Key+"scene",SceneManager.GetActiveScene().path);SessionState.SetBool(Key,true);
        errors="";report.Clear();routine=null;resumeAt=0;TutorialSession.Reset();EditorSceneManager.OpenScene("Assets/Scenes/Level_01_Tutorial.unity");EditorApplication.EnterPlaymode();
    }
    public static void RunBatch(){PlayerSettings.productName="TWM Audio Validation";Run();}
    static void Poll()
    {
        if(EditorApplication.isCompiling||EditorApplication.isUpdating)return;
        if(File.Exists("Logs/audio-test.request")){File.Delete("Logs/audio-test.request");Run();return;}
        if(!SessionState.GetBool(Key,false)||!EditorApplication.isPlaying||Time.unscaledTime<resumeAt)return;
        try{routine??=Tests();if(!routine.MoveNext()){End("PASS\n"+report);return;}resumeAt=Time.unscaledTime+(routine.Current is float seconds?seconds:0);}
        catch(Exception e){End("FAIL: "+e+"\n"+report+errors);}
    }
    static void Require(bool condition,string message){if(!condition)throw new Exception(message);}
    static void End(string result){SessionState.SetBool(Key+"passed",result.StartsWith("PASS"));File.WriteAllText("Logs/Audio/tests.txt",result);EditorApplication.ExitPlaymode();}
    static IEnumerator Tests()
    {
        for(int level=1;level<=2;level++)
        {
            if(level==2){SceneManager.LoadScene("Level_02_ColoredSwitches");yield return 1f;}
            else yield return 1f;
            var audio=AudioManager.Instance;Require(audio,"Bootstrap missing");Require(Object.FindObjectsByType<AudioManager>(FindObjectsSortMode.None).Length==1,"Duplicate manager");
            Require(Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None).Count(x=>x.isActiveAndEnabled)==1,"AudioListener count");
            Require(audio.CurrentMusic==(level==1?audio.library.levelOneMusic:audio.library.levelTwoMusic),"Wrong scene music");
            foreach(SoundId id in Enum.GetValues(typeof(SoundId)))Require(audio.library.Get(id)?.clips.All(c=>c)!=false&&audio.library.Get(id)!=null,"Missing cue "+id);
            foreach(var source in audio.GetComponentsInChildren<AudioSource>())Require(source.outputAudioMixerGroup,"Unrouted source");
            var current=audio.CurrentMusic;var musicSources=audio.GetComponentsInChildren<AudioSource>().Where(x=>x.outputAudioMixerGroup==audio.musicGroup).ToArray();
            audio.PlayMusic(audio.library.menuMusic,.3f);yield return .12f;Require(musicSources.Count(x=>x.isPlaying)==2,"Crossfade did not overlap two sources");yield return .4f;
            audio.PlayMusic(current,.3f);yield return .4f;Require(musicSources.Count(x=>x.isPlaying)==1,"Crossfade left an old track playing");
            var loop=musicSources.First(x=>x.isPlaying);loop.time=Mathf.Max(0,current.length-.10f);yield return .4f;Require(loop.loop&&loop.isPlaying&&loop.time<1,"Music did not wrap its loop");
            audio.SetMuted(false);audio.SetMusicVolume(.37f);audio.SetSfxVolume(.63f);audio.SavePreferences();
            Require(Mathf.Abs(PlayerPrefs.GetFloat("SharedGameAudio.v1.music")-.37f)<.001f,"Music not saved");
            audio.SetMusicMuted(true);yield return .05f;Require(audio.mixer.GetFloat("MusicVolume",out var db)&&db<=-79,"Music mute not applied");Require(audio.mixer.GetFloat("SFXVolume",out db)&&db>-79,"SFX incorrectly muted");audio.SetMuted(false);
            var settings=Object.FindFirstObjectByType<AudioSettingsUI>();Require(settings,"Settings missing");Require(settings.openButton.GetComponent<UnityEngine.UI.Image>().sprite,"Settings sprite missing");settings.Open();yield return .15f;if(!Application.isBatchMode)ScreenCapture.CaptureScreenshot("Logs/Audio/Level"+level+"-settings.png");yield return .2f;settings.musicSlider.value=.42f;Require(Mathf.Abs(audio.MusicVolume-.42f)<.001f,"Slider disconnected");settings.Close();
            audio.SetSfxMuted(true);Require(!audio.Play(SoundId.Button),"Muted SFX accepted");audio.SetSfxMuted(false);
            yield return .5f;int before=audio.PlayedCount(SoundId.Jump);Require(audio.Play(SoundId.Jump,1),"First player cue rejected");Require(audio.Play(SoundId.Jump,2),"Second player cue rejected");Require(!audio.Play(SoundId.Jump,1),"Cooldown failed");Require(audio.PlayedCount(SoundId.Jump)==before+2,"Two-player cue mismatch");
            for(int i=0;i<100;i++)audio.Play((SoundId)(i%19));Require(audio.ActiveVoiceCount<=audio.poolSize,"Unbounded voices");yield return 3f;
            var manager=Object.FindFirstObjectByType<CooperationTracker>().GetComponent(typeof(CooperationTracker).Assembly.GetType("GameManager"));var tutorial=manager.GetComponent<TutorialManager>();
            if(tutorial&&tutorial.IsActive){tutorial.OnFirstButtonPressed(1,"Btn_Left");yield return .35f;tutorial.OnFirstButtonPressed(2,"Btn_Left");}
            var players=Object.FindObjectsByType<PlayerController>(FindObjectsSortMode.None);yield return .6f;
            before=audio.PlayedCount(SoundId.Jump);foreach(var player in players)player.Jump();yield return .12f;Require(audio.PlayedCount(SoundId.Jump)>before,"Real jump did not reach audio observer");yield return 1f;
            before=audio.PlayedCount(SoundId.PlateOn);var plate=Object.FindFirstObjectByType<PressurePlate>();var actor=players.First(p=>plate.Allowed==PressurePlate.AllowedPlayer.Any||(int)plate.Allowed==p.PlayerId);
            plate.SendMessage("OnTriggerEnter2D",actor.BodyCollider);yield return 1f;Require(audio.PlayedCount(SoundId.PlateOn)>before,"Plate event disconnected");Require(audio.PlayedCount(SoundId.BridgeOn)>0,"Bridge event disconnected");plate.SendMessage("OnTriggerExit2D",actor.BodyCollider);yield return .5f;
            foreach(var lever in Object.FindObjectsByType<Lever>(FindObjectsSortMode.None))lever.SendMessage("OnTriggerEnter2D",players[0].BodyCollider);yield return .2f;
            before=audio.PlayedCount(SoundId.Hint);manager.GetType().GetMethod("RequestHint",new[]{typeof(int)}).Invoke(manager,new object[]{1});yield return .1f;Require(audio.PlayedCount(SoundId.Hint)>before,"Hint disconnected");
            manager.GetType().GetMethod("RegisterGoal").Invoke(manager,new object[]{1});manager.GetType().GetMethod("RegisterGoal").Invoke(manager,new object[]{2});yield return 1.5f;Require((bool)manager.GetType().GetProperty("LevelCompleted").GetValue(manager),"Goal completion unavailable");Require(audio.CurrentMusic==audio.library.Get(SoundId.Victory).clips[0],"Victory music disconnected");
            Require(audio.PlayedCount(SoundId.Goal)>0&&audio.PlayedCount(SoundId.Confetti)>0&&audio.PlayedCount(SoundId.Star)>0,"Result feedback disconnected");
            // Inspect the full runtime hierarchy for missing scripts, including generated UI.
            foreach(var root in SceneManager.GetActiveScene().GetRootGameObjects())foreach(var transform in root.GetComponentsInChildren<Transform>(true))Require(transform.GetComponents<Component>().All(c=>c),"Missing component at "+transform.name);
            Require(string.IsNullOrEmpty(errors),"Runtime errors: "+errors);report.AppendLine("Level "+level+": routing, preferences, UI, pool/cooldown, both-player cues, actual jump, plate/bridge, hint, goals and victory/results PASS.");
            // Recreate service from disk preferences, then allow next scene to create a fresh bound panel.
            if(level==1){audio.SetMusicVolume(.42f);audio.SavePreferences();Object.Destroy(settings.gameObject);Object.Destroy(audio.gameObject);yield return .1f;audio=Object.Instantiate(Resources.Load<GameObject>("SharedGameAudio/AudioManager")).GetComponent<AudioManager>();Require(Mathf.Abs(audio.MusicVolume-.42f)<.001f,"Preference reload failed");}
        }
        yield return .1f;
    }
}
