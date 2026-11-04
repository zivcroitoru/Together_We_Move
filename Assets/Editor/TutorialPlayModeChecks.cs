using System.IO;
using UnityEditor;

/// <summary>Preserves the old test entry point; the role-aware suite supersedes six-control onboarding.</summary>
[InitializeOnLoad]
public static class TutorialPlayModeChecks
{
    static TutorialPlayModeChecks() { EditorApplication.update += Poll; }
    static void Poll()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating || !File.Exists("Logs/tutorial-play.request")) return;
        File.Delete("Logs/tutorial-play.request"); Run();
    }
    [MenuItem("Tools/Playtest/Test paired tutorial in Play Mode")]
    public static void Run() => PreschoolPlaytestChecks.Run();
}
