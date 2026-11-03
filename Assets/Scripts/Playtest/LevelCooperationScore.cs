using System;

/// <summary>Observed participation, unique bridge/helper credits and genuine star arrivals.
/// Assistance scales earned credit; it never invents goals, participation or crossings.</summary>
public static class LevelCooperationScore
{
    public static float Calculate(CooperationProgress progress,float participation,float cooperation,float goals,float independence=1)
    {
        participation=Math.Max(0,participation);cooperation=Math.Max(0,cooperation);goals=Math.Max(0,goals);
        float total=participation+cooperation+goals;
        if(progress==null||total<=0)return 0;
        int players=((progress.ParticipantMask&1)!=0?1:0)+((progress.ParticipantMask&2)!=0?1:0);
        float actions=progress.BridgeCount==0?0:(float)progress.CooperativeActions/(2*progress.BridgeCount);
        return Math.Max(0,Math.Min(100,100*(participation*players/2f+cooperation*actions+goals*progress.GoalCount/2f)/total*Math.Max(0,Math.Min(1,independence))));
    }
}
