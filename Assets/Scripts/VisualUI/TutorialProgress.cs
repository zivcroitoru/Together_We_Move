using System;
using System.Collections.Generic;

/// <summary>Independent child progress, retained while waiting for a partner or reloading a level.</summary>
public sealed class TutorialProgress
{
    public readonly string RunId=Guid.NewGuid().ToString("N");
    readonly Dictionary<string,float> phaseStarts=new();
    readonly HashSet<string> learned=new(), firstInputs=new(), totals=new();
    float startedAt;bool started;
    string Key(int phase,int player)=>phase+":"+player;
    public void Begin(float now){if(!started){started=true;startedAt=now;}}
    public float Elapsed(float now)=>Math.Max(0,now-startedAt);
    public void BeginPhase(int phase,float now){if(!phaseStarts.ContainsKey(phase.ToString()))phaseStarts[phase.ToString()]=now;}
    public float PhaseElapsed(int phase,float now)=>phaseStarts.TryGetValue(phase.ToString(),out float time)?Math.Max(0,now-time):0;
    public bool MarkFirstInput(int phase,int player)=>firstInputs.Add(Key(phase,player));
    public bool Learn(int phase,int player)=>learned.Add(Key(phase,player));
    public bool HasLearned(int phase,int player)=>learned.Contains(Key(phase,player));
    public bool BothLearned(int phase)=>HasLearned(phase,1)&&HasLearned(phase,2);
    public bool ControlsLearned(int player)=>player==1?HasLearned(2,player):player==2&&HasLearned(0,player)&&HasLearned(1,player);
    public bool MarkTotal(string metric,int player)=>totals.Add(metric+":"+player);
}
