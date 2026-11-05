using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using SharedGameJuice;
using SharedUI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;

/// <summary>Opt-in integration fixtures; no real child playtest records are written.</summary>
[InitializeOnLoad]
public static class GameJuiceChecks
{
    const string Key="TWM.JuiceChecks";
    static string ProjectPath(string path)=>Path.Combine(Path.GetDirectoryName(Application.dataPath),path);
    static IEnumerator routine;
    static float resumeAt;
    static string errors="";
    static readonly System.Text.StringBuilder report=new();
    static GameJuiceChecks(){EditorApplication.update+=Poll;EditorApplication.playModeStateChanged+=Changed;}
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Isolate()
    {
        if(!SessionState.GetBool(Key,false))return;
        Application.runInBackground=true;
        typeof(PlaytestData).GetField("store",BindingFlags.Static|BindingFlags.NonPublic).SetValue(null,new PlaytestStore(ProjectPath("Logs/Juice/IsolatedPlaytest")));
        Application.logMessageReceived+=Log;
    }
    static void Log(string message,string stack,LogType type){if(type==LogType.Exception||type==LogType.Error)errors+=message+"\n";}
    static void Changed(PlayModeStateChange state)
    {
        if(state!=PlayModeStateChange.EnteredEditMode||!SessionState.GetBool(Key,false))return;
        SessionState.SetBool(Key,false);routine=null;TutorialSession.Reset();
        Application.runInBackground=SessionState.GetBool(Key+"background",false);
        string path=SessionState.GetString(Key+"scene","");if(!string.IsNullOrEmpty(path))EditorSceneManager.OpenScene(path);
    }
    [MenuItem("Tools/Shared Game Juice/Verify both levels")]
    public static void Run()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode||SceneManager.GetActiveScene().isDirty)return;
        Directory.CreateDirectory(ProjectPath("Logs/Juice"));errors="";report.Clear();routine=null;resumeAt=0;
        SessionState.SetString(Key+"scene",SceneManager.GetActiveScene().path);SessionState.SetBool(Key+"background",Application.runInBackground);SessionState.SetBool(Key,true);
        TutorialSession.Reset();EditorSceneManager.OpenScene("Assets/Scenes/Level_01_Tutorial.unity");EditorApplication.EnterPlaymode();
    }
    static void Poll()
    {
        if(EditorApplication.isCompiling||EditorApplication.isUpdating)return;
        // Never interrupt another task's play session or consume a queued request while busy.
        if(File.Exists(ProjectPath("Logs/juice-test.request"))&&!EditorApplication.isPlayingOrWillChangePlaymode&&!SceneManager.GetActiveScene().isDirty)
        {File.Delete(ProjectPath("Logs/juice-test.request"));Run();return;}
        if(!SessionState.GetBool(Key,false)||!EditorApplication.isPlaying||Time.unscaledTime<resumeAt)return;
        try
        {
            File.WriteAllText(ProjectPath("Logs/Juice/state.txt"),"Project="+Application.dataPath+" time="+Time.unscaledTime);
            routine??=Tests();if(!routine.MoveNext()){End("PASS");return;}
            resumeAt=Time.unscaledTime+(routine.Current is float delay?delay:.05f);
        }
        catch(Exception e){End("FAIL: "+e);}
    }
    static void Require(bool value,string reason){if(!value)throw new Exception(reason);}
    static void Trigger(Component receiver,string method,Collider2D other)=>receiver.GetType().GetMethod(method,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(receiver,new object[]{other});
    static IEnumerator Tests()
    {
        for(int level=0;level<2;level++)
        {
            yield return 1f;
            var tracker=Object.FindFirstObjectByType<CooperationTracker>();
            var manager=tracker?tracker.GetComponent(typeof(CooperationTracker).Assembly.GetType("GameManager")):null;Require(manager,"Manager");
            Require(manager.GetComponent<TWMJuiceAdapter>()?.IsBound==true,"Automatic adapter binding");
            var tutorial=manager.GetComponent<TutorialManager>();if(tutorial)tutorial.enabled=false;
            var assistance=manager.GetComponent<LevelAssistance>();if(assistance)assistance.enabled=false;
            foreach(var root in SceneManager.GetActiveScene().GetRootGameObjects())
                foreach(var t in root.GetComponentsInChildren<Transform>(true))Require(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)==0,"Missing scene script "+t.name);
            var players=Object.FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
            foreach(var player in players){player.GetComponent<Rigidbody2D>().simulated=false;player.enabled=false;}
            foreach(var plate in Object.FindObjectsByType<PressurePlate>(FindObjectsSortMode.None))
            {
                var player=players.First(p=>plate.Allowed==PressurePlate.AllowedPlayer.Any||(int)plate.Allowed==p.PlayerId);
                var collider=player.GetComponent<Collider2D>();var fx=plate.GetComponent<SpriteJuice>();Require(fx&&fx.source,"Plate visual reference");
                Vector3 scale=plate.transform.localScale,position=plate.transform.position;
                int before=fx.Activations;
                Trigger(plate,"OnTriggerEnter2D",collider);yield return .08f;
                Trigger(plate,"OnTriggerEnter2D",collider);yield return .4f;
                Require(fx.Activations==before+1,"Held/multi-contact plate must emit once");
                Require(plate.transform.localScale==scale&&plate.transform.position==position,"Plate physics transform changed");
                Trigger(plate,"OnTriggerExit2D",collider);yield return .08f;
                Trigger(plate,"OnTriggerEnter2D",collider);yield return .25f;Require(fx.Activations==before+2,"Plate re-arms after release");
                Trigger(plate,"OnTriggerExit2D",collider);yield return .1f;
            }
            foreach(var lever in Object.FindObjectsByType<Lever>(FindObjectsSortMode.None))
            {
                var fx=lever.GetComponent<SpriteJuice>();int before=fx.Activations;var collider=players[0].GetComponent<Collider2D>();
                Trigger(lever,"OnTriggerEnter2D",collider);yield return .1f;Trigger(lever,"OnTriggerEnter2D",collider);yield return .3f;
                Require(fx.Activations==before+1,"Latched lever repeats");
            }
            var bridges=Object.FindObjectsByType<BridgeJuice>(FindObjectsSortMode.None);Require(bridges.Length==(level==0?1:3),"Both levels bridge count");
            foreach(var bridge in bridges)
            {
                bridge.HideBridge();Require(!bridge.Surface.enabled,"Hide collision must remain immediate");yield return .3f;
                Require(bridge.GetComponentsInChildren<SpriteRenderer>().Length==0,"Hidden bridge leaves a solid-looking deck");
                bridge.ShowBridge();Require(!bridge.Surface.enabled,"Show collision must wait for existing build");yield return 1.3f;
                Require(bridge.Surface.enabled,"Bridge did not become solid");
                var presentation=bridge.GetComponent<BridgePresentation>();Require(presentation&&presentation.Transitions>0,"Bridge VFX observer");
                foreach(var sr in bridge.GetComponentsInChildren<SpriteRenderer>())Require(sr.color.a>.95f,"Solid bridge must be opaque");
                bridge.HideBridge();Require(!bridge.Surface.enabled,"Immediate removal timing");yield return .8f;
            }
            Require(Object.FindObjectsByType<SpriteJuice>(FindObjectsSortMode.None).Any(fx=>fx.idleGlow),"Goal halo");
            var star=Object.FindFirstObjectByType<SplitStarGoal>();Require(star,"Split star");
            foreach(var player in players){Trigger(star,"OnTriggerEnter2D",player.GetComponent<Collider2D>());Trigger(star,"OnTriggerEnter2D",player.GetComponent<Collider2D>());yield return .25f;}
            var feedback=manager.GetComponent<GameplayFeedback>();int bursts=feedback.BurstsPlayed;feedback.Finish();feedback.Finish();
            Require(feedback.BurstsPlayed<=bursts+1,"Victory repeats");
            var paper=manager.GetComponent<PaperConfetti>();Require(paper&&paper.BurstsPlayed==1,"One paper victory burst");
            yield return .1f;ScreenCapture.CaptureScreenshot(ProjectPath("Logs/Juice/level-"+(level+1)+"-victory.png"));
            yield return .65f;
            var progress=Object.FindObjectsByType<UIProgress>(FindObjectsInactive.Include,FindObjectsSortMode.None).First(p=>p.indicators.Length>0);
            Require(progress.GetComponent<ScoreJuice>()&&progress.GetComponent<ScoreJuice>().fill,"Score references");Require(progress.duration==.5f,"Short score easing");
            progress.gameObject.SetActive(true);progress.AnimateTo(.8f);yield return .6f;
            Require(Mathf.Abs(progress.value-.8f)<.001f,"Score animation endpoint");Require(progress.GetComponent<ScoreJuice>().EarnedPops>0,"Earned score sparkles");
            Require(manager.GetComponent<PlaytestUI>().ResultsVisible,"Real goal completion must reveal the score UI");
            ScreenCapture.CaptureScreenshot(ProjectPath("Logs/Juice/level-"+(level+1)+"-score.png"));
            yield return 1f;
            Require(Object.FindObjectsByType<ParticleSystem>(FindObjectsSortMode.None).All(p=>!p.IsAlive()),"Continuous particles left alive");
            Require(Object.FindObjectsByType<JuiceBurst>(FindObjectsSortMode.None).Length<=JuiceBurst.Capacity,"Particle pool bound");
            Require(paper.GetComponentsInChildren<UnityEngine.UI.Image>().Length==0,"Confetti should expire");
            Require(string.IsNullOrEmpty(errors),"Runtime errors: "+errors);
            report.AppendLine("Level "+(level+1)+": PASS automatic integration, scene references, plate rising edges/release, lever latch, physics transforms, bridge collision timing/opacity, goal halo and burst, finite victory, .5s score, bounded/expired particles.");
            if(level==0)SceneManager.LoadScene("Assets/Scenes/Level_02_ColoredSwitches.unity");
        }
    }
    static void End(string result)
    {
        File.WriteAllText(ProjectPath("Logs/Juice/results.txt"),result+"\n"+report+"\n"+errors);
        Application.logMessageReceived-=Log;EditorApplication.ExitPlaymode();
    }
}
