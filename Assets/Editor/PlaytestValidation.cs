using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem.UI;

/// <summary>Dependency-free regression checks. Run Tools/Playtest/Validate or
/// Unity -batchmode -projectPath ... -executeMethod PlaytestValidation.Run -quit.</summary>
public static class PlaytestValidation
{
    [MenuItem("Tools/Playtest/Validate")]
    public static void Run()
    {
        if (EditorApplication.isPlaying) { Debug.LogWarning("Exit Play Mode before running validation."); return; }
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        var originalSetup = EditorSceneManager.GetSceneManagerSetup();
        try
        {
            CheckProgress();
            CheckPersistence();
            foreach (string path in new[] { "Assets/Scenes/Level_01_Tutorial.unity", "Assets/Scenes/Level_02_ColoredSwitches.unity" })
                CheckScene(path);
            Debug.Log("[PlaytestValidation] PASS: scoring, persistence, CSV, both scenes and input wiring.");
        }
        finally { EditorSceneManager.RestoreSceneManagerSetup(originalSetup); }
    }

    private static void CheckProgress()
    {
        var progress = new CooperationProgress(new[] { "bridge" });
        Require(progress.Score == 0 && progress.TotalUnits == 4, "Initial score");
        Require(!progress.AddAssist("bridge", 1, 1), "Self-help must not earn credit");
        Require(!progress.AddAssist("missing", 1, 2), "Unregistered bridges must not earn credit");
        Require(!progress.AddGoal(0), "Anonymous events must not be assigned to a player");
        Require(progress.AddAssist("bridge", 1, 2) && progress.Score == 25, "Real assist credit");
        Require(!progress.AddAssist("bridge", 1, 2) && progress.Score == 25, "Duplicate assist farming");
        Require(progress.AddGoal(2) && progress.Score == 50 && !progress.IsFull, "Partial finish");
        Require(!progress.AddGoal(2), "Duplicate goal collider");
        progress.AddAssist("bridge", 2, 1);
        progress.AddGoal(1);
        Require(progress.IsFull && progress.Score == 100, "Full cooperation");
        var colored = new CooperationProgress(new[] { "one", "two", "three" });
        Require(colored.TotalUnits == 8, "Three-bridge level denominator");
    }

    private static void CheckPersistence()
    {
        string directory = Path.Combine(Path.GetTempPath(), "TWM-validation-" + Guid.NewGuid().ToString("N"));
        try
        {
            var store = new PlaytestStore(directory);
            var first = store.BeginAttempt("test_level");
            store.Record(first, "hint_requested", 1, 25, 1, 4, "player_key", "hold");
            store.Record(first, "hint_requested", 1, 25, 1, 4, "player_key", "turns");
            store.Record(first, "hint_requested", 2, 25, 1, 4, "player_slot_button", "hold");
            store.Record(first, "attempt_finished", 0, 50, 2, 4, outcome: "partial");
            store.Record(first, "retry_requested", 1, 50, 2, 4, "player_key");
            var reloaded = new PlaytestStore(directory);
            Require(reloaded.PairId == store.PairId, "Pair must persist across sessions");
            Require(reloaded.BeginAttempt("test_level").Number == 2, "Attempt number must persist");
            Require(reloaded.RetryCount("test_level", 1) == 1 && reloaded.RetryCount("test_level", 2) == 0, "Separate retry counts");
            Require(reloaded.PreviousScore("test_level") == 50 && reloaded.BestScore("test_level") == 50, "Score comparison");
            var hint = reloaded.Events.Last(e => e.eventType == "hint_requested" && e.playerId == 1);
            Require(hint.playerHintCount == 2, "Per-player hint counts");
            Require(reloaded.Events.First(e => e.eventType == "hint_requested" && e.playerId == 2).playerHintCount == 1, "Hints isolated by slot");
            string csvPath = reloaded.ExportCsv();
            Require(csvPath != null && File.ReadAllText(csvPath).Contains("timestamp_utc"), "CSV export");
            string csv = PlaytestStore.ToCsv(new[] { new PlaytestEvent { detail = "a,\"b\"\nc", cooperationScore = 12.5f } });
            Require(csv.Contains("\"a,\"\"b\"\"\nc\"") && csv.Contains("\"12.5\""), "CSV quoting and invariant decimal");
            File.AppendAllText(store.JournalPath, "{interrupted");
            var recovered = new PlaytestStore(directory);
            Require(recovered.RetryCount("test_level", 1) == 1, "Damaged tail must not erase previous data");
            recovered.BeginAttempt("test_level");
            Require(new PlaytestStore(directory).Events.Any(e => e.attemptNumber == 3), "Append after interrupted write");
            recovered.StartNewPair();
            Require(recovered.RetryCount("test_level", 1) == 0 && recovered.BeginAttempt("test_level").Number == 1, "New pair partitions counters");
        }
        finally
        {
            // This directory is created above with a unique name, never a user save directory.
            if (Directory.Exists(directory)) Directory.Delete(directory, true);
        }
    }

    private static void CheckScene(string path)
    {
        var scene = EditorSceneManager.OpenScene(path);
        foreach (var root in scene.GetRootGameObjects())
            foreach (var transform in root.GetComponentsInChildren<Transform>(true))
                Require(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(transform.gameObject) == 0,
                    path + ": missing script on " + transform.name);
        // An unused legacy Editor script also declares GameManager; resolve the runtime type explicitly.
        var trackerComponent = UnityEngine.Object.FindFirstObjectByType<CooperationTracker>();
        var manager = trackerComponent ? trackerComponent.GetComponent(typeof(CooperationTracker).Assembly.GetType("GameManager")) : null;
        Require(manager && manager.GetComponent<CooperationTracker>() && manager.GetComponent<CooperationHints>()
            && manager.GetComponent<PlaytestUI>(), path + ": playtest components");
        Require(UnityEngine.Object.FindObjectsByType<PlayerController>(FindObjectsSortMode.None).Select(p => p.PlayerId).OrderBy(p => p)
            .SequenceEqual(new[] { 1, 2 }), path + ": unique two-player identities");
        Require(UnityEngine.Object.FindFirstObjectByType<InputSystemUIInputModule>(), path + ": touch input module");
        Require(UnityEngine.Object.FindFirstObjectByType<SplitStarGoal>(), path + ": split star script");
        var serialized = new SerializedObject(manager);
        Require(serialized.FindProperty("winSequence").objectReferenceValue != null, path + ": celebration reference");
        var tracker = new SerializedObject(manager.GetComponent<CooperationTracker>());
        Require(tracker.FindProperty("bridges").arraySize == (path.Contains("01_") ? 1 : 3), path + ": scored bridges");
        Require(manager.GetComponent<CooperationHints>() != null, path + ": visual hint planner");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException("[PlaytestValidation] " + message);
    }

    [MenuItem("Tools/Playtest/Export CSV")]
    public static void Export()
    {
        string path = PlaytestData.Store.ExportCsv();
        if (path != null) { Debug.Log("[Playtest] CSV: " + path); EditorUtility.RevealInFinder(path); }
    }

    [MenuItem("Tools/Playtest/Start New Anonymous Pair")]
    public static void NewPair()
    {
        if (Application.isPlaying)
        {
            Debug.LogWarning("[Playtest] Exit Play Mode before starting a new pair, so the active attempt stays with its original pair.");
            return;
        }
        PlaytestData.Store.StartNewPair();
        Debug.Log("[Playtest] New anonymous pair started. Previous data is retained.");
    }
}
