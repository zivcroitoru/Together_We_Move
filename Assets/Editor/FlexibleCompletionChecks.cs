using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Opt-in controller/ledger integration fixtures for both existing levels.
/// Fixture credits are explicit; this does not substitute for live cooperative crossing tests.</summary>
[InitializeOnLoad]
public static class FlexibleCompletionChecks
{
    const string Key="TWM.FlexibleChecks";
    static readonly string[] Levels={"Assets/Scenes/Level_01_Tutorial.unity","Assets/Scenes/Level_02_ColoredSwitches.unity"};
    static int level,scenario,phase;static float since;static Component manager;static LevelAssistance assistance;
    static string error="";static bool started;static readonly Type ManagerType=typeof(CooperationTracker).Assembly.GetType("GameManager");
    static FlexibleCompletionChecks(){EditorApplication.update+=Poll;EditorApplication.playModeStateChanged+=Changed;}
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)] static void Isolate()
    {
        if(!SessionState.GetBool(Key,false))return;
        typeof(PlaytestData).GetField("store",BindingFlags.Static|BindingFlags.NonPublic).SetValue(null,new PlaytestStore(Path.GetFullPath("Logs/FlexibleCompletion/Data/"+DateTime.UtcNow.ToString("yyyyMMddHHmmss"))));
        Application.logMessageReceived+=Log;
    }
    static void Log(string message,string stack,LogType type){if(type==LogType.Exception||type==LogType.Error)error+=message+"\n";}
    static void Changed(PlayModeStateChange mode)
    {
        if(mode!=PlayModeStateChange.EnteredEditMode||!SessionState.GetBool(Key,false))return;
        SessionState.SetBool(Key,false);var path=SessionState.GetString(Key+"scene","");if(!string.IsNullOrEmpty(path))EditorSceneManager.OpenScene(path);
    }
    [MenuItem("Tools/Playtest/Test flexible completion in both levels")]
    public static void Run()
    {
        Directory.CreateDirectory("Logs/FlexibleCompletion");
        if(EditorApplication.isPlayingOrWillChangePlaymode||SceneManager.GetActiveScene().isDirty)
        {File.WriteAllText("Logs/FlexibleCompletion/result.txt","NOT RUN: save scene and exit Play Mode first.");return;}
        started=false;error="";manager=null;
        SessionState.SetString(Key+"scene",SceneManager.GetActiveScene().path);SessionState.SetBool(Key,true);
        EditorSceneManager.OpenScene(Levels[0]);EditorApplication.EnterPlaymode();
    }
    static void Poll()
    {
        if(EditorApplication.isCompiling||EditorApplication.isUpdating)return;
        if(File.Exists("Logs/flexible-check.request")){File.Delete("Logs/flexible-check.request");Run();return;}
        if(!SessionState.GetBool(Key,false)||!EditorApplication.isPlaying)return;
        try{Tick();}catch(Exception e){End("FAIL: "+e);}
    }
    static object Call(string name,params object[] values)=>ManagerType.GetMethods().Single(m=>m.Name==name&&m.GetParameters().Length==values.Length).Invoke(manager,values);
    static T Property<T>(string name)=>(T)ManagerType.GetProperty(name).GetValue(manager);
    static void Field(object owner,string name,object value)=>owner.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(owner,value);
    static void Require(bool ok,string reason){if(!ok)throw new InvalidOperationException(reason);}
    static void Tick()
    {
        if(!started){started=true;level=scenario=phase=0;since=Time.unscaledTime;}
        if(Time.unscaledTime-since>15)throw new Exception("Timeout level="+level+" scenario="+scenario+" phase="+phase);
        if(phase==0)
        {
            var tracker=UnityEngine.Object.FindFirstObjectByType<CooperationTracker>();if(!tracker||tracker.Progress==null)return;
            manager=tracker.GetComponent(ManagerType);assistance=manager.GetComponent<LevelAssistance>();
            var tutorial=manager.GetComponent<TutorialManager>();if(tutorial)tutorial.enabled=false;
            Require(tracker.Progress.BridgeCount==(level==0?1:3),"Existing bridge denominator");
            Require(assistance.Stage==0,"New attempt must start without assistance");
            foreach(var item in new[]{("highlightAfter",10f),("handAfter",20f),("easeAfter",30f),("finishAfter",45f)})
                Require((float)assistance.GetType().GetField(item.Item1,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(assistance)==item.Item2,"Per-level threshold configuration");
            Field(manager,"partialCompletionGraceSeconds",0f);
            foreach(string name in new[]{"highlightAfter","handAfter","easeAfter","finishAfter"})Field(assistance,name,100f);
            if(scenario<2)
            {
                foreach(var bridge in UnityEngine.Object.FindObjectsByType<BridgeController>(FindObjectsSortMode.None))
                {
                    tracker.Progress.AddAssist(CooperationTracker.ObjectId(bridge.transform),1,2);
                    if(scenario==0)tracker.Progress.AddAssist(CooperationTracker.ObjectId(bridge.transform),2,1);
                }
                Call("RegisterGoal",1);if(scenario==0)Call("RegisterGoal",2);Call("CompleteLevel");
            }
            else
            {
                if(scenario==2)tracker.Progress.Participate(1);
                Field(assistance,"highlightAfter",.1f);Field(assistance,"handAfter",.2f);Field(assistance,"easeAfter",.3f);Field(assistance,"finishAfter",.7f);Field(assistance,"finalVisualSeconds",.15f);
            }
            if(scenario==3)assistance.Progress("fixture_once");
            phase=1;since=Time.unscaledTime;return;
        }
        if(phase==1)
        {
            if(scenario==3)
            {
                assistance.Progress("fixture_once"); // Same milestone every frame must not postpone stages.
                if(Time.unscaledTime-since>.25f)Require(assistance.Stage>=1,"Repeated milestone reset inactivity timer");
            }
            if(scenario>=2&&Time.unscaledTime-since>.35f&&assistance.Stage>=3&&phase==1)
            {
                if(scenario==2)
                {
                    var bridge=UnityEngine.Object.FindObjectsByType<BridgeController>(FindObjectsSortMode.None)[0];bridge.OnButtonEnter(1);bridge.OnButtonExit(1);
                }
                phase=2;
            }
            if(scenario<2)phase=2;else return;
        }
        if(phase==2)
        {
            if(!Property<bool>("LevelCompleted")||!manager.GetComponent<PlaytestUI>().ResultsVisible)return;
            float score=Property<float>("Score");var progress=Property<CooperationTracker>("Tracker").Progress;
            if(scenario==0)Require(score==100&&!assistance.Assisted,"Independent full completion");
            if(scenario==1)Require(score>0&&score<100&&!assistance.Assisted,"Partial completion");
            if(scenario>=2)
            {
                Require(assistance.Automatic&&progress.GoalCount==0,"Fallback must not fabricate star arrivals");
                Require(score<45,"Assisted score comes only from earned actions");
                var rows=PlaytestData.Store.Events.Where(e=>e.level==Levels[level]).ToArray();
                foreach(string eventType in new[]{"automatic_highlight","automatic_demonstration","assistance_eased","final_visual_assistance","automatic_completion"})Require(rows.Any(e=>e.eventType==eventType),"Missing assistance stage "+eventType);
                if(scenario==2)Require(rows.Any(e=>e.eventType=="mechanical_assistance_used"),"Actual release grace not recorded");
            }
            Require((bool)ManagerType.GetProperty("HasNextLevel").GetValue(manager)==(level==0),"Two-level next navigation");
            if(scenario==0)
            {
                Call("RequestRetry",1,"fixture_pointer");Call("RequestRetry",2,"fixture_pointer");scenario=1;phase=3;return;
            }
            if(scenario==1)foreach(int child in new[]{1,2})Require(PlaytestData.Store.RetryCount(Levels[level],child)==1,"Separate persisted replay counts");
            assistance.NewRun();scenario++;
            if(scenario==4){level++;scenario=0;}
            if(level==2)
            {
                File.WriteAllText("Logs/FlexibleCompletion/dashboard.txt",LevelPlaytestDashboard.Format(PlaytestData.Store));
                Require(PlaytestData.Store.ExportCsv()!=null,"CSV export");Require(string.IsNullOrEmpty(error),"Runtime errors: "+error);
                End("PASS: both level configurations; independent full, partial, mechanical assistance, automatic fallback with zero fake goals, zero-action score, both-player replay counts, next-level availability and analytics/CSV. Uses explicit scoring fixtures plus real bridge button methods; live physical crossing/touch checks remain manual.");return;
            }
            phase=0;manager=null;since=Time.unscaledTime;SceneManager.LoadScene(Levels[level]);return;
        }
        if(phase==3&&(!manager||!Property<bool>("LevelCompleted"))){phase=0;since=Time.unscaledTime;}
    }
    static void End(string result)
    {File.WriteAllText("Logs/FlexibleCompletion/result.txt",result);Application.logMessageReceived-=Log;EditorApplication.ExitPlaymode();}
}
