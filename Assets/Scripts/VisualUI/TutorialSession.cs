using UnityEngine;

/// <summary>Survives level reloads, resets for an app/Play session or a new participant pair.</summary>
public static class TutorialSession
{
    public static bool Started { get; private set; }
    public static bool Completed { get; private set; }
    public static int NextStep { get; private set; }
    public static TutorialProgress Progress { get; private set; } = new TutorialProgress();
    public static void Begin() => Started = true;
    public static void Advance(int nextStep) => NextStep = nextStep;
    public static void Complete() { Started=true; Completed=true; }
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    public static void Reset() { Started=false; Completed=false; NextStep=0; Progress=new TutorialProgress(); }
}
