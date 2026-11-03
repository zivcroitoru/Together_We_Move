using System;

/// <summary>Shared unscaled-time policy for tutorial and ordinary hint demonstrations.</summary>
public sealed class GuidanceTiming
{
    readonly float repeatSeconds;
    float lastActivity, lastDemonstration;
    public GuidanceTiming(float now, float repeatSeconds = 5f)
    {
        this.repeatSeconds = Math.Max(1f, repeatSeconds);
        lastActivity = lastDemonstration = now;
    }
    public void NotifyActivity(float now) => lastActivity = now;
    public void NotifyDemonstration(float now) => lastDemonstration = now;
    public bool ShouldRepeat(float now)
    {
        if (now-lastActivity < repeatSeconds || now-lastDemonstration < repeatSeconds) return false;
        lastDemonstration = now;
        return true;
    }
    public float Emphasis(float now) => Math.Min(2f, Math.Max(0f,(now-lastActivity-repeatSeconds)/repeatSeconds));
}
