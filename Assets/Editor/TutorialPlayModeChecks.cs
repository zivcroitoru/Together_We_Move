using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Opt-in real-physics regression. Uses existing input methods; never part of gameplay.</summary>
[InitializeOnLoad]
public static class TutorialPlayModeChecks
{
    const string Key="TWM.TutorialTest";
    static TutorialManager tutorial;
    static PlayerController blue,red;
    static int stage;
    static float began;
    static string failure;
    static TutorialPlayModeChecks(){EditorApplication.update+=Poll;EditorApplication.playModeStateChanged+=Changed;}
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void IsolateData()
    {
        if(!SessionState.GetBool(Key,false))return;
        // Test attempts must never mix with children's journal.
        typeof(PlaytestData).GetField("store",BindingFlags.Static|BindingFlags.NonPublic).SetValue(null,
            new PlaytestStore(Path.GetFullPath("Logs/Tutorial/PlayModeData/"+DateTime.UtcNow.ToString("yyyyMMddHHmmss"))));
    }
    static void Changed(PlayModeStateChange state)
    {
        if(state!=PlayModeStateChange.EnteredEditMode||!SessionState.GetBool(Key,false))return;
        SessionState.SetBool(Key,false);
        string previous=SessionState.GetString(Key+"Scene","");
        if(!string.IsNullOrEmpty(previous)&&SceneManager.GetActiveScene().path!=previous)EditorSceneManager.OpenScene(previous);
    }
    [MenuItem("Tools/Playtest/Test paired tutorial in Play Mode")]
    public static void Run()
    {
        Directory.CreateDirectory("Logs/Tutorial");
        if(EditorApplication.isPlayingOrWillChangePlaymode||SceneManager.GetActiveScene().isDirty)
        {File.WriteAllText("Logs/Tutorial/play-mode.txt","NOT RUN: exit Play Mode and save scene edits first.");return;}
        SessionState.SetString(Key+"Scene",SceneManager.GetActiveScene().path);
        SessionState.SetBool(Key,true);
        EditorSceneManager.OpenScene("Assets/Scenes/Level_01_Tutorial.unity");
        EditorApplication.EnterPlaymode();
    }
    static void Poll()
    {
        if(EditorApplication.isCompiling||EditorApplication.isUpdating)return;
        if(File.Exists("Logs/tutorial-play.request")) {File.Delete("Logs/tutorial-play.request");Run();return;}
        if(!SessionState.GetBool(Key,false)||!EditorApplication.isPlaying)return;
        try {Tick();}
        catch(Exception error){failure=error.ToString();End();}
    }
    static void Tick()
    {
        if(!tutorial)
        {
            tutorial=UnityEngine.Object.FindFirstObjectByType<TutorialManager>();
            if(!tutorial||!tutorial.IsActive)return;
            blue=UnityEngine.Object.FindObjectsByType<PlayerController>(FindObjectsSortMode.None).Single(p=>p.PlayerId==1);
            red=UnityEngine.Object.FindObjectsByType<PlayerController>(FindObjectsSortMode.None).Single(p=>p.PlayerId==2);
            stage=0;began=Time.unscaledTime;
        }
        float elapsed=Time.unscaledTime-began;
        Require(elapsed<6,"Action timed out at test stage "+stage+" phase "+tutorial.CurrentStepIndex);
        switch(stage)
        {
            case 0:
                Require(tutorial.CurrentStepIndex==0,"Start on left movement");
                if(elapsed<1)return;
                ScreenCapture.CaptureScreenshot(Path.GetFullPath("Logs/Tutorial/left.png"));
                blue.SetTouchMove(-1);Next();break;
            case 1:
                Require(tutorial.CurrentStepIndex==0,"Blue alone must not advance left");
                if(!TutorialSession.Progress.HasLearned(0,1))return;
                blue.SetTouchMove(0);Require(!TutorialSession.Progress.HasLearned(0,2),"Red was not taught automatically");Next();break;
            case 2:
                Require(tutorial.CurrentStepIndex==0&&TutorialSession.Progress.HasLearned(0,1),"Blue keeps left success while waiting");
                if(elapsed<.4f)return;red.SetTouchMove(-1);Next();break;
            case 3:
                if(tutorial.CurrentStepIndex!=1)return;
                red.SetTouchMove(0);ScreenCapture.CaptureScreenshot(Path.GetFullPath("Logs/Tutorial/right.png"));
                blue.SetTouchMove(1);Next();break;
            case 4:
                Require(tutorial.CurrentStepIndex==1,"Blue alone must not advance right");
                if(!TutorialSession.Progress.HasLearned(1,1))return;
                blue.SetTouchMove(0);red.SetTouchMove(1);Next();break;
            case 5:
                if(tutorial.CurrentStepIndex!=2)return;
                red.SetTouchMove(0);ScreenCapture.CaptureScreenshot(Path.GetFullPath("Logs/Tutorial/jump.png"));Next();break;
            case 6:
                Require(tutorial.CurrentStepIndex==2,"No early objective");
                if(elapsed<.5f||!blue.IsGrounded)return;
                blue.Jump();Next();break;
            case 7:
                Require(tutorial.CurrentStepIndex==2,"Blue jump must wait for Red");
                if(!TutorialSession.Progress.HasLearned(2,1)||!red.IsGrounded)return;
                red.Jump();Next();break;
            case 8:
                if(tutorial.CurrentStepIndex!=3)return;
                Require(tutorial.ControlsLearned,"Both children learned all three controls");
                Require(!TutorialSession.Completed,"Objective must still need actual gameplay");
                ScreenCapture.CaptureScreenshot(Path.GetFullPath("Logs/Tutorial/objective.png"));
                Require(!UnityEngine.Object.FindObjectsByType<HintSpotlight>(FindObjectsSortMode.None).Single().raycastTarget,"Overlay blocks input");
                foreach(int p in new[]{1,2})foreach(string action in new[]{"left","right","jump"})
                {
                    Require(PlaytestData.Store.Events.Count(e=>e.playerId==p&&e.tutorialAction==action&&e.eventType=="tutorial_first_success")==1,"First success count");
                    Require(PlaytestData.Store.Events.Count(e=>e.playerId==p&&e.tutorialAction==action&&e.eventType=="tutorial_action_learned")==1,"Learned count");
                }
                File.WriteAllText("Logs/Tutorial/dashboard.txt",TutorialDashboard.Format(PlaytestData.Store));
                Next();break;
            case 9:
                if(elapsed<.5f)return;
                Require(tutorial.IsActive&&!TutorialSession.Completed,"Objective is not completed automatically");End();break;
        }
    }
    static void Next(){stage++;began=Time.unscaledTime;}
    static void Require(bool condition,string message){if(!condition)throw new InvalidOperationException(message);}
    static void End()
    {
        if(blue)blue.SetTouchMove(0);if(red)red.SetTouchMove(0);
        File.WriteAllText("Logs/Tutorial/play-mode.txt",failure??"PASS: real-physics left/right/jump for both children; independent waiting; objective after controls only; no auto objective; exact first/learned journal counts; nonblocking overlay. Touch hit-testing, audio and target-device usability remain manual checks.");
        EditorApplication.ExitPlaymode();
    }
}
