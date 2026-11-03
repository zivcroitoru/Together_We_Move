using System;
using System.IO;
using System.Linq;
class CoreRegression { static int Main() { try { CheckProgress(); CheckPersistence(); CheckTutorialAnalytics(); Console.WriteLine("PASS: scoring, anti-farming, separate player counts, persistence replay, comparisons, CSV escaping, damaged-tail recovery, anonymous pair reset."); return 0; } catch(Exception e) { Console.WriteLine(e); return 1; } }
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

    private static void CheckTutorialAnalytics()
    {
        string directory=Path.Combine(Path.GetTempPath(),"TWM-tutorial-"+Guid.NewGuid().ToString("N"));
        try
        {
            var store=new PlaytestStore(directory);var attempt=store.BeginAttempt("tutorial");
            store.Record(attempt,"tutorial_first_success",1,0,0,4,tutorialRunId:"run",tutorialAction:"left",tutorialElapsedSeconds:1.25f,tutorialPhaseSeconds:1.25f);
            store.Record(attempt,"tutorial_action_learned",1,0,0,4,tutorialRunId:"run",tutorialAction:"left",tutorialElapsedSeconds:2.5f,tutorialPhaseSeconds:2.5f);
            store.Record(attempt,"hint_requested",2,0,0,4,tutorialRunId:"run",tutorialAction:"left");
            store.Record(attempt,"tutorial_demo_repeated",2,0,0,4,tutorialRunId:"run",tutorialAction:"left");
            var loaded=new PlaytestStore(directory);var first=loaded.Events.Single(e=>e.eventType=="tutorial_first_success");
            Require(first.tutorialRunId=="run"&&first.tutorialAction=="left"&&first.tutorialPhaseSeconds==1.25f,"Tutorial fields persist");
            var dashboard=TutorialDashboard.Format(loaded);
            Require(dashboard.Contains("left: first 1.25s / learned 2.50s | hints 0 | repeats 0"),"Blue learning timings");
            Require(dashboard.Contains("left: first waiting / learned waiting | hints 1 | repeats 1"),"Red hint/demo counts independent from Blue");
            string csv=PlaytestStore.ToCsv(loaded.Events);
            Require(csv.Contains("tutorial_run_id,tutorial_action,tutorial_elapsed_seconds,tutorial_phase_seconds")&&csv.Contains("\"run\",\"left\",\"1.25\",\"1.25\""),"Tutorial CSV columns and precision");
        }
        finally {if(Directory.Exists(directory))Directory.Delete(directory,true);}
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException("[PlaytestValidation] " + message);
    }

}
namespace UnityEngine {
 public enum RuntimeInitializeLoadType { SubsystemRegistration }
 public sealed class RuntimeInitializeOnLoadMethodAttribute : System.Attribute { public RuntimeInitializeOnLoadMethodAttribute(RuntimeInitializeLoadType t) {} }
 public static class Application { public static string persistentDataPath => System.IO.Path.GetTempPath(); }
 public static class Debug { public static void LogWarning(object s) { System.Console.WriteLine(s); } public static void LogError(object s) { System.Console.WriteLine(s); } }
 public static class JsonUtility { private static readonly System.Web.Script.Serialization.JavaScriptSerializer json = new System.Web.Script.Serialization.JavaScriptSerializer(); public static string ToJson(object o) => json.Serialize(o); public static T FromJson<T>(string s) { try { return json.Deserialize<T>(s); } catch(System.Exception e) { throw new System.ArgumentException("Invalid JSON",e); } } }
}
public static class PlaytestPlatform { public static void SyncBrowserStorage() {} public static void OfferCsvDownload(string path) {} }
