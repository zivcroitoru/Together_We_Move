using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

/// <summary>Opt-in real scene/physics/multitouch checks. The children's journal is never used.</summary>
[InitializeOnLoad]
public static class PreschoolPlaytestChecks
{
    const string Key="TWM.PreschoolChecks";
    static readonly string[] Levels={"Assets/Scenes/Level_01_Tutorial.unity","Assets/Scenes/Level_02_ColoredSwitches.unity"};
    static IEnumerator routine;
    static float resumeAt;
    static string errors="";
    static Touchscreen touchscreen;
    static InputSettings.EditorInputBehaviorInPlayMode previousInputBehavior;
    static readonly System.Text.StringBuilder report=new();
    static PreschoolPlaytestChecks(){EditorApplication.update+=Poll;EditorApplication.playModeStateChanged+=Changed;}
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Isolate()
    {
        if(!SessionState.GetBool(Key,false))return;
        errors="";
        typeof(PlaytestData).GetField("store",BindingFlags.Static|BindingFlags.NonPublic).SetValue(null,
            new PlaytestStore(Path.GetFullPath("Logs/Preschool/Data/"+DateTime.UtcNow.ToString("yyyyMMddHHmmss"))));
        Application.logMessageReceived+=OnLog;
    }
    static void OnLog(string message,string stack,LogType type){if(type==LogType.Error||type==LogType.Exception)errors+=message+"\n";}
    static void Changed(PlayModeStateChange state)
    {
        if(state!=PlayModeStateChange.EnteredEditMode||!SessionState.GetBool(Key,false))return;
        SessionState.SetBool(Key,false);routine=null;
        string scene=SessionState.GetString(Key+"scene","");if(!string.IsNullOrEmpty(scene))EditorSceneManager.OpenScene(scene);
    }
    [MenuItem("Tools/Playtest/Test preschool recovery and touch in both levels")]
    public static void Run()
    {
        Directory.CreateDirectory("Logs/Preschool");
        if(EditorApplication.isPlayingOrWillChangePlaymode||SceneManager.GetActiveScene().isDirty)
        {File.WriteAllText("Logs/Preschool/result.txt","NOT RUN: save scene edits and exit Play Mode first.");return;}
        SessionState.SetString(Key+"scene",SceneManager.GetActiveScene().path);SessionState.SetBool(Key,true);
        report.Clear();routine=null;resumeAt=0;
        EditorSceneManager.OpenScene(Levels[0]);EditorApplication.EnterPlaymode();
    }
    static void Poll()
    {
        if(EditorApplication.isCompiling||EditorApplication.isUpdating)return;
        if(File.Exists("Logs/preschool-check.request")){File.Delete("Logs/preschool-check.request");Run();return;}
        if(!SessionState.GetBool(Key,false)||!EditorApplication.isPlaying||Time.unscaledTime<resumeAt)return;
        try
        {
            routine??=Tests();
            if(!routine.MoveNext()){End("PASS\n"+report);return;}
            resumeAt=Time.unscaledTime+(routine.Current is float wait?wait:0);
        }
        catch(Exception error){End("FAIL: "+error+"\n"+report);}
    }
    static void Require(bool condition,string message){if(!condition)throw new InvalidOperationException(message);}
    static void Field(object owner,string name,object value)=>owner.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(owner,value);
    static GameObject Button(int player,string name)=>Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None).First(c=>c.transform.Find("P1_Controls")).transform.Find((player==1?"P1_Controls/":"P2_Controls/")+name).gameObject;
    static void Hold(int player,string name,int id,bool down)
    {
        var data=new PointerEventData(EventSystem.current){pointerId=id};
        if(down)ExecuteEvents.Execute(Button(player,name),data,ExecuteEvents.pointerDownHandler);
        else ExecuteEvents.Execute(Button(player,name),data,ExecuteEvents.pointerUpHandler);
    }
    static readonly Type ManagerType=typeof(CooperationTracker).Assembly.GetType("GameManager");
    static T Property<T>(Component manager,string name)=>(T)ManagerType.GetProperty(name).GetValue(manager);
    static void Call(Component manager,string name,params object[] args)=>ManagerType.GetMethods().Single(m=>m.Name==name&&m.GetParameters().Length==args.Length).Invoke(manager,args);
    static void Touch(int slot,int id,Vector2 position,UnityEngine.InputSystem.TouchPhase phase)=>InputSystem.QueueStateEvent(touchscreen,new TouchState{touchId=id,position=position,phase=phase,pressure=phase==UnityEngine.InputSystem.TouchPhase.Ended?0:1});
    static Vector2 ScreenPoint(PlayerController player)=>Camera.main.WorldToScreenPoint(player.BodyCollider.bounds.center);
    static IEnumerator Tests()
    {
        previousInputBehavior=InputSystem.settings.editorInputBehaviorInPlayMode;
        InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        touchscreen=InputSystem.AddDevice<Touchscreen>();
        for(int level=0;level<2;level++)
        {
            if(level>0)SceneManager.LoadScene(Levels[level]);
            yield return 1.2f;
            var manager=Object.FindFirstObjectByType<CooperationTracker>().GetComponent(ManagerType);
            var assistance=manager.GetComponent<LevelAssistance>();var tutorial=manager.GetComponent<TutorialManager>();
            foreach(string field in new[]{"highlightAfter","handAfter","easeAfter","finishAfter"})Field(assistance,field,1000f);
            var players=Object.FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
            var a=players.Single(p=>p.PlayerId==1);var b=players.Single(p=>p.PlayerId==2);
            var controls=manager.GetComponent<PreschoolTouchControls>();
            Canvas.ForceUpdateCanvases();
            foreach(var button in new[]{Button(1,"Btn_Jump"),Button(2,"Btn_Left"),Button(2,"Btn_Right")})
            {
                var corners=new Vector3[4];button.GetComponent<RectTransform>().GetWorldCorners(corners);
                Require(corners.All(p=>p.x>=0&&p.x<=Screen.width&&p.y>=0&&p.y<=Screen.height),"Controls stay inside "+Screen.width+"x"+Screen.height);
                Require(button.GetComponent<Image>().preserveAspect,"Preserve button artwork proportions");
            }
            Require(a.IsGrounded&&b.IsGrounded,"Initial grounded contacts in "+Levels[level]);
            Require(Button(1,"Btn_Jump").activeInHierarchy&&!Button(1,"Btn_Left").activeSelf&&!Button(2,"Btn_Jump").activeSelf,"Only allowed role buttons visible");
            float ax=a.transform.position.x,bx=b.transform.position.x;
            a.SetTouchMove(1);b.Jump();
            Hold(2,"Btn_Left",101,true);yield return .35f;Hold(2,"Btn_Left",101,false);
            Require(Mathf.Abs(a.transform.position.x-ax)<.01f&&b.SuccessfulJumpCount==0,"Role restrictions for button/Jump API");
            Require(b.transform.position.x<bx-.3f,"Existing left button moves B through physics");
            yield return .1f;
            if(tutorial)Require(tutorial.CurrentStepIndex==1&&!TutorialSession.Progress.HasLearned(0,1),"Only B learns horizontal tutorial; no fake A credit");
            Hold(2,"Btn_Right",102,true);yield return .4f;Hold(2,"Btn_Right",102,false);yield return .1f;
            a.Jump();yield return .1f;
            Require(a.SuccessfulJumpCount==1&&Mathf.Abs(a.transform.position.x-ax)<.01f,"A jumps vertically");
            if(tutorial){Require(tutorial.ControlsLearned&&!TutorialSession.Progress.HasLearned(2,2),"Role tutorial completes allowed controls only");tutorial.enabled=false;}
            yield return 1.8f;
            controls.SetMode(PreschoolControlMode.Dragging);
            Vector2 touchA=ScreenPoint(a),touchB=ScreenPoint(b);bx=b.transform.position.x;
            Touch(0,21,touchA,UnityEngine.InputSystem.TouchPhase.Began);Touch(1,22,touchB,UnityEngine.InputSystem.TouchPhase.Began);
            yield return .08f;
            Touch(1,22,touchB+Vector2.right*100,UnityEngine.InputSystem.TouchPhase.Moved);
            yield return .25f;
            Require(a.SuccessfulJumpCount==2&&b.transform.position.x>bx+.15f,"Simultaneous touchscreen input: A jumps="+a.SuccessfulJumpCount+" B moved="+(b.transform.position.x-bx)+" touches="+UnityEngine.InputSystem.EnhancedTouch.Touch.activeTouches.Count);
            Require(Mathf.Abs(a.transform.position.x-ax)<.01f&&b.SuccessfulJumpCount==0,"Touch preserves roles");
            Touch(0,21,touchA,UnityEngine.InputSystem.TouchPhase.Ended);Touch(1,22,touchB,UnityEngine.InputSystem.TouchPhase.Ended);yield return .15f;
            bx=b.transform.position.x;yield return .2f;Require(Mathf.Abs(b.transform.position.x-bx)<.06f,"Drag release stops B");
            var hint=Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None).SelectMany(c=>c.GetComponentsInChildren<Button>()).First(x=>x.name=="Blue Hint");
            Vector2 ui=RectTransformUtility.WorldToScreenPoint(null,hint.transform.position);
            int jumps=a.SuccessfulJumpCount;
            Touch(0,23,ui,UnityEngine.InputSystem.TouchPhase.Began);yield return .08f;
            Touch(0,23,ScreenPoint(b)+Vector2.right*100,UnityEngine.InputSystem.TouchPhase.Moved);yield return .2f;
            Require(a.SuccessfulJumpCount==jumps&&Mathf.Abs(b.transform.position.x-bx)<.06f,"Touch originating on UI cannot capture either character");
            Touch(0,23,ui,UnityEngine.InputSystem.TouchPhase.Ended);yield return .1f;
            controls.SetMode(PreschoolControlMode.Buttons);
            Hold(2,"Btn_Right",31,true);Hold(2,"Btn_Right",32,true);Hold(2,"Btn_Right",31,false);bx=b.transform.position.x;
            yield return .15f;Require(b.transform.position.x>bx+.1f,"Releasing one held finger preserves the second");Hold(2,"Btn_Right",32,false);
            yield return .6f;
            ScreenCapture.CaptureScreenshot(Path.GetFullPath("Logs/Preschool/level"+(level+1)+"-gameplay.png"));
            var recoveryA=a.GetComponent<PlayerDeathController>();var recoveryB=b.GetComponent<PlayerDeathController>();
            Vector2 safeA=recoveryA.LastSafePosition,safeB=recoveryB.LastSafePosition;
            int attempt=Property<int>(manager,"AttemptNumber"),earned=Property<CooperationTracker>(manager,"Tracker").Progress.EarnedUnits;
            float scoreBefore=Property<float>(manager,"Score");
            var rbA=a.GetComponent<Rigidbody2D>();var rbB=b.GetComponent<Rigidbody2D>();
            rbA.position=new Vector2(ax,-25);rbB.position=new Vector2(b.transform.position.x,-25);Physics2D.SyncTransforms();
            yield return .12f;
            Require(a.IsRecovering&&b.IsRecovering&&!Property<bool>(manager,"IsTransitioning"),"Both falls recover without scene restart");
            Require(!recoveryA.TryRecover()&&!recoveryB.TryRecover(),"Repeated fall requests cannot start duplicate recoveries");
            yield return 1.5f;
            Require(!a.IsRecovering&&!b.IsRecovering&&a.IsGrounded&&b.IsGrounded,"Recoveries settle without a loop");
            Require(Vector2.Distance(rbA.position,safeA)<.3f&&Vector2.Distance(rbB.position,safeB)<.3f,"Return to last stable safe positions");
            Require(Property<int>(manager,"AttemptNumber")==attempt&&Property<CooperationTracker>(manager,"Tracker").Progress.EarnedUnits==earned,"Attempt and credits preserved; recovery creates no crossing credit");
            Require(assistance.Assisted,"Recovery reduces internal score through existing assistance scoring");
            Require(Property<float>(manager,"Score")<scoreBefore,"Earned internal score is discounted by recovery");
            var rows=PlaytestData.Store.Events.Where(e=>e.level==Levels[level]).ToArray();
            Require(rows.Count(e=>e.eventType=="player_fell")==2&&rows.Count(e=>e.eventType=="player_recovered")==2,"Exact per-player fall/recovery counts");
            var feedback=manager.GetComponent<GameplayFeedback>();int bursts=feedback.BurstsPlayed;
            feedback.Milestone("assisted_crossing",1,"fixture");for(int i=0;i<20;i++)feedback.Milestone("assisted_crossing",1,"fixture");
            yield return .8f;Require(feedback.BurstsPlayed==bursts+1,"Cooperation feedback never repeats per frame");
            Require(feedback.GetComponentsInChildren<Image>(true).All(i=>!i.raycastTarget),"Confetti cannot block touch");
            Require(manager.GetComponent<PreschoolTouchControls>().Mode==PreschoolControlMode.Buttons,"Buttons retained as fallback");
            // Actual mechanism collider remains responsive following recovery.
            var plate=Object.FindObjectsByType<PressurePlate>(FindObjectsSortMode.None).First(p=>p.Allowed!=PressurePlate.AllowedPlayer.Player1);
            Vector2 platePosition=plate.GetComponent<Collider2D>().bounds.center;
            rbB.position=platePosition;Physics2D.SyncTransforms();yield return .15f;
            Require(plate.ContainsPlayer(b),"Pressure plate still accepts recovered B collision");
            // Remove support in a temporary test fixture: a vanished checkpoint must not form a rescue loop.
            a.SetInputLocked(true);
            var ground=Object.FindObjectsByType<Collider2D>(FindObjectsSortMode.None).Where(c=>c.enabled&&!c.GetComponentInParent<PlayerController>()&&(a.GroundLayer.value&(1<<c.gameObject.layer))!=0).ToArray();
            foreach(var surface in ground)surface.enabled=false;
            rbB.position=new Vector2(rbB.position.x,-25);Physics2D.SyncTransforms();yield return 1.1f;
            Require(!b.IsRecovering&&b.IsGrounded,"Emergency trampoline breaks a missing-ground loop");
            Require(PlaytestData.Store.Events.Any(e=>e.level==Levels[level]&&e.eventType=="player_recovered"&&e.detail=="emergency_trampoline"),"Emergency aid logged");
            int fallen=PlaytestData.Store.Events.Count(e=>e.level==Levels[level]&&e.eventType=="player_fell");
            yield return 1f;
            Require(PlaytestData.Store.Events.Count(e=>e.level==Levels[level]&&e.eventType=="player_fell")==fallen,"No repeated falls on emergency support");
            foreach(var surface in ground)if(surface)surface.enabled=true;
            a.SetInputLocked(false);
            Call(manager,"RequestHint",1);Call(manager,"RequestHint",2);
            yield return .1f;
            Require(PlaytestData.Store.Events.Any(e=>e.level==Levels[level]&&e.eventType=="hint_requested"),"Hint analytics retained");
            Call(manager,"CompleteWithAssistance");yield return .9f;
            Require(Property<bool>(manager,"LevelCompleted")&&manager.GetComponent<PlaytestUI>().ResultsVisible,"Assisted finish keeps existing celebratory result/progress UI");
            Require(feedback.BurstsPlayed>=bursts+2,"Every finish celebrates regardless of score");
            ScreenCapture.CaptureScreenshot(Path.GetFullPath("Logs/Preschool/level"+(level+1)+"-results.png"));yield return .15f;
            report.AppendLine(Levels[level]+": PASS roles, tutorial, buttons, simultaneous Input System touches, UI exclusion, release, recovery, exact analytics, collider and assisted finish.");
            assistance.NewRun();
        }
        Require(PlaytestData.Store.ExportCsv()!=null,"Compatible CSV export");
        File.WriteAllText("Logs/Preschool/dashboard.txt",LevelPlaytestDashboard.Format(PlaytestData.Store));
        Require(string.IsNullOrEmpty(errors),"Runtime errors: "+errors);
    }
    static void End(string result)
    {
        File.WriteAllText("Logs/Preschool/result.txt",result);Application.logMessageReceived-=OnLog;
        if(touchscreen!=null){InputSystem.RemoveDevice(touchscreen);touchscreen=null;}
        InputSystem.settings.editorInputBehaviorInPlayMode=previousInputBehavior;
        EditorApplication.ExitPlaymode();
    }
}
