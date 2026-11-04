using System;
class TutorialRegression
{
    static void Check(bool condition,string message){if(!condition)throw new Exception(message);}
    static int Main()
    {
        var idle=new GuidanceTiming(10);
        Check(!idle.ShouldRepeat(14.99f),"No premature demonstration");
        Check(idle.ShouldRepeat(15),"Repeat at five unscaled seconds");
        Check(!idle.ShouldRepeat(15.01f),"No per-frame repeat");
        Check(idle.ShouldRepeat(20),"Second repeat after continued inactivity");
        Check(idle.Emphasis(15)==0 && idle.Emphasis(20)==1 && idle.Emphasis(25)==2,"Gradual, bounded escalation");
        Check(idle.Emphasis(100)==2,"No unbounded flashing or growth");
        idle.NotifyActivity(101);Check(!idle.ShouldRepeat(105.9f),"Real activity postpones idle hint");
        Check(idle.Emphasis(101)==0,"Activity resets emphasis");
        idle.NotifyDemonstration(106);Check(!idle.ShouldRepeat(107),"Manual hint does not immediately replay twice");
        Check(idle.ShouldRepeat(111),"Idle repeats resume after manual demonstration");
        var blueClock=new GuidanceTiming(0);var redClock=new GuidanceTiming(0);
        blueClock.NotifyActivity(4);
        Check(!blueClock.ShouldRepeat(5)&&redClock.ShouldRepeat(5),"Activity must only reset that child's clock");
        var progress=new TutorialProgress();progress.Begin(10);progress.BeginPhase(0,10);
        Check(progress.MarkFirstInput(0,1)&&!progress.MarkFirstInput(0,1),"First input logged once per child/phase");
        Check(progress.Learn(0,1)&&!progress.BothLearned(0),"One child must not advance phase");
        Check(progress.HasLearned(0,1)&&!progress.HasLearned(0,2),"Completed child retains independent progress");
        progress.Begin(20);progress.BeginPhase(0,20);
        Check(progress.Elapsed(25)==15&&progress.PhaseElapsed(0,25)==15,"Reload preserves run and phase clocks");
        progress.Learn(0,2);Check(progress.BothLearned(0),"Both successful children unlock next phase");
        Check(!progress.BothLearned(1)&&!progress.ControlsLearned(1),"Left completion does not pre-complete right or jump");
        progress.BeginPhase(1,30);progress.Learn(1,1);progress.Learn(1,2);
        progress.BeginPhase(2,40);progress.Learn(2,1);
        Check(progress.ControlsLearned(1)&&progress.ControlsLearned(2),"A needs only jump; B needs only left/right");
        Check(progress.MarkTotal("controls",1)&&!progress.MarkTotal("controls",1),"Per-child completion logged once");
        progress.Learn(2,2);Check(progress.BothLearned(2)&&progress.ControlsLearned(2),"Both jump gate");
        TutorialSession.Reset();Check(!TutorialSession.Started&&!TutorialSession.Completed&&TutorialSession.NextStep==0,"New play session starts clean");
        TutorialSession.Begin();TutorialSession.Advance(2);Check(TutorialSession.Started&&!TutorialSession.Completed&&TutorialSession.NextStep==2,"Interrupted onboarding retains its step");
        TutorialSession.Complete();Check(TutorialSession.Completed&&TutorialSession.NextStep==2,"Completion persists within session");
        TutorialSession.Reset();Check(!TutorialSession.Completed&&TutorialSession.NextStep==0,"New pair/play session resets completion");
        Console.WriteLine("PASS: independent left/right/jump gates, first-success deduplication, reload retention, per-child completion, independent five-second repeats, escalation, session reset.");return 0;
    }
}
// Isolated test-only substitute for Unity's initialization attribute. Not shipped in Assets.
namespace UnityEngine { public enum RuntimeInitializeLoadType {SubsystemRegistration} public sealed class RuntimeInitializeOnLoadMethodAttribute:Attribute { public RuntimeInitializeOnLoadMethodAttribute(RuntimeInitializeLoadType type) {} } }
