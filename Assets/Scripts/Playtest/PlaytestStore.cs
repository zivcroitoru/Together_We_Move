using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

[Serializable]
public sealed class PlaytestEvent
{
    public int schemaVersion = 1;
    public string eventId, pairId, sessionId, attemptId, level, timestampUtc, eventType, outcome, attribution, detail;
    public int playerId, attemptNumber, playerRetryCount, playerHintCount, earnedUnits, totalUnits;
    public float cooperationScore,completionSeconds;
    public int participantMask,assistanceStage;
    public bool assistanceUsed,automaticCompletion;
    // Additive v1 fields: older journals and dashboard consumers remain readable.
    public string controlMode;
    public int player1Falls,player2Falls,recoveryCount,cooperationEventCount,hintRequestCount,retryRequestCount,voluntaryReplayCount;
    public string tutorialRunId, tutorialAction;
    public float tutorialElapsedSeconds=-1f, tutorialPhaseSeconds=-1f;
}

public sealed class PlaytestAttempt
{
    public string PairId, Id, Level;
    public int Number;
    public readonly int[] Hints = new int[3];
    public readonly int[] Falls = new int[3];
    public int Recoveries,Cooperations,RetryRequests,VoluntaryReplays;
    public string ControlMode = "buttons";
}

/// <summary>Append-only JSONL journal. Every event is closed to disk immediately;
/// counters and previous scores are rebuilt from it across scene/app restarts.</summary>
public sealed class PlaytestStore
{
    private readonly List<PlaytestEvent> events = new();
    private readonly List<PlaytestEvent> pendingWrites = new();
    private readonly string sessionId = Guid.NewGuid().ToString("N");
    public string DirectoryPath { get; }
    public string JournalPath => Path.Combine(DirectoryPath, "events.jsonl");
    public string PairId { get; private set; }
    public string LastError { get; private set; }
    public IReadOnlyList<PlaytestEvent> Events => events;

    public PlaytestStore(string directory)
    {
        DirectoryPath = directory;
        try
        {
            var seenIds = new HashSet<string>();
            if (File.Exists(JournalPath))
                foreach (string line in File.ReadLines(JournalPath))
                {
                    try
                    {
                        var entry = JsonUtility.FromJson<PlaytestEvent>(line);
                        if (entry == null || entry.schemaVersion != 1 || string.IsNullOrEmpty(entry.eventType)
                            || string.IsNullOrEmpty(entry.pairId)) continue;
                        if (!string.IsNullOrEmpty(entry.eventId) && !seenIds.Add(entry.eventId)) continue;
                        events.Add(entry);
                        PairId = entry.pairId;
                    }
                    catch (ArgumentException)
                    {
                        Debug.LogWarning("[Playtest] Skipping a damaged journal line; other records are retained.");
                    }
                }
        }
        catch (Exception exception) when (IsStorageError(exception)) { StorageError(exception); }
        if (string.IsNullOrEmpty(PairId)) StartNewPair();
    }

    public void StartNewPair()
    {
        PairId = Guid.NewGuid().ToString("N");
        Append(new PlaytestEvent { pairId = PairId, eventType = "pair_started", attribution = "researcher" });
    }

    public PlaytestAttempt BeginAttempt(string level, int totalUnits = 0)
    {
        int number = events.Where(e => e.pairId == PairId && e.level == level && e.eventType == "attempt_started")
            .Select(e => e.attemptNumber).DefaultIfEmpty(0).Max() + 1;
        var attempt = new PlaytestAttempt { PairId = PairId, Id = Guid.NewGuid().ToString("N"), Level = level, Number = number };
        Record(attempt, "attempt_started", 0, 0, 0, totalUnits, "system");
        return attempt;
    }

    public int RetryCount(string level, int playerId) => events.Count(e => e.pairId == PairId && e.level == level
        && e.playerId == playerId && e.eventType == "retry_requested");

    public float? PreviousScore(string level) => events.LastOrDefault(e => e.pairId == PairId && e.level == level
        && e.eventType == "attempt_finished" && (e.outcome == "partial" || e.outcome == "full" || e.outcome == "assisted"))?.cooperationScore;

    public float? BestScore(string level)
    {
        var scores = events.Where(e => e.pairId == PairId && e.level == level && e.eventType == "attempt_finished"
            && (e.outcome == "partial" || e.outcome == "full" || e.outcome == "assisted")).Select(e => e.cooperationScore).ToArray();
        return scores.Length == 0 ? (float?)null : scores.Max();
    }

    public bool Record(PlaytestAttempt attempt, string type, int playerId, float score, int earned, int total,
        string attribution = "system", string detail = "", string outcome = "",
        string tutorialRunId = "", string tutorialAction = "", float tutorialElapsedSeconds = -1f, float tutorialPhaseSeconds = -1f, float completionSeconds=0, int participantMask=0, int assistanceStage=0, bool assistanceUsed=false, bool automaticCompletion=false)
    {
        if (attempt == null) return false;
        if (type == "hint_requested" && CooperationProgress.ValidPlayer(playerId)) attempt.Hints[playerId]++;
        if (type == "player_fell" && CooperationProgress.ValidPlayer(playerId)) attempt.Falls[playerId]++;
        if (type == "player_recovered") attempt.Recoveries++;
        if (type == "cooperation_succeeded") attempt.Cooperations++;
        if (type == "retry_requested") attempt.RetryRequests++;
        if (type == "voluntary_replay") attempt.VoluntaryReplays++;
        if (type == "control_mode_selected") attempt.ControlMode = detail;
        return Append(new PlaytestEvent
        {
            completionSeconds=completionSeconds,participantMask=participantMask,assistanceStage=assistanceStage,assistanceUsed=assistanceUsed,automaticCompletion=automaticCompletion,
            controlMode=attempt.ControlMode,player1Falls=attempt.Falls[1],player2Falls=attempt.Falls[2],recoveryCount=attempt.Recoveries,
            cooperationEventCount=attempt.Cooperations,hintRequestCount=attempt.Hints[1]+attempt.Hints[2],retryRequestCount=attempt.RetryRequests,voluntaryReplayCount=attempt.VoluntaryReplays,
            pairId = attempt.PairId, attemptId = attempt.Id, level = attempt.Level, attemptNumber = attempt.Number,
            eventType = type, playerId = playerId, cooperationScore = score, earnedUnits = earned, totalUnits = total,
            playerRetryCount = RetryCount(attempt.Level, playerId) + (type == "retry_requested" ? 1 : 0),
            playerHintCount = CooperationProgress.ValidPlayer(playerId) ? attempt.Hints[playerId] : 0,
            attribution = attribution, detail = detail, outcome = outcome,
            tutorialRunId=tutorialRunId, tutorialAction=tutorialAction, tutorialElapsedSeconds=tutorialElapsedSeconds, tutorialPhaseSeconds=tutorialPhaseSeconds
        });
    }

    private bool Append(PlaytestEvent entry)
    {
        entry.sessionId = sessionId;
        entry.eventId = Guid.NewGuid().ToString("N");
        entry.timestampUtc = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture);
        events.Add(entry);
        pendingWrites.Add(entry);
        try
        {
            Directory.CreateDirectory(DirectoryPath);
            // Separate an interrupted final line before appending the next valid event.
            bool needsNewline = false;
            if (File.Exists(JournalPath))
                using (var stream = File.OpenRead(JournalPath))
                    if (stream.Length > 0) { stream.Seek(-1, SeekOrigin.End); needsNewline = stream.ReadByte() != '\n'; }
            var batch = new StringBuilder(needsNewline ? "\n" : "");
            foreach (var pending in pendingWrites) batch.Append(JsonUtility.ToJson(pending)).Append('\n');
            File.AppendAllText(JournalPath, batch.ToString(), new UTF8Encoding(false));
            pendingWrites.Clear();
            PlaytestPlatform.SyncBrowserStorage();
            LastError = null;
            return true;
        }
        catch (Exception exception) when (IsStorageError(exception)) { StorageError(exception); return false; }
    }

    public string ExportCsv()
    {
        try
        {
            Directory.CreateDirectory(DirectoryPath);
            string path = Path.Combine(DirectoryPath, "playtest-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture)
                + "-" + Guid.NewGuid().ToString("N").Substring(0, 6) + ".csv");
            File.WriteAllText(path, ToCsv(events), new UTF8Encoding(true));
            PlaytestPlatform.OfferCsvDownload(path);
            if (pendingWrites.Count == 0) LastError = null;
            return path;
        }
        catch (Exception exception) when (IsStorageError(exception)) { StorageError(exception); return null; }
    }

    public static string ToCsv(IEnumerable<PlaytestEvent> entries)
    {
        var csv = new StringBuilder("schema_version,event_id,pair_id,session_id,attempt_id,level,player_id,attempt_number,event_type,outcome,cooperation_score,earned_units,total_units,player_retry_count,player_hint_count,timestamp_utc,attribution,detail,tutorial_run_id,tutorial_action,tutorial_elapsed_seconds,tutorial_phase_seconds,completion_seconds,participant_mask,assistance_stage,assistance_used,automatic_completion,control_mode,player_1_falls,player_2_falls,recovery_count,cooperation_event_count,hint_request_count,retry_request_count,voluntary_replay_count\r\n");
        foreach (var e in entries)
        {
            object[] columns = { e.schemaVersion, e.eventId, e.pairId, e.sessionId, e.attemptId, e.level, e.playerId,
                e.attemptNumber, e.eventType, e.outcome, e.cooperationScore, e.earnedUnits, e.totalUnits,
                e.playerRetryCount, e.playerHintCount, e.timestampUtc, e.attribution, e.detail, e.tutorialRunId, e.tutorialAction,
                string.IsNullOrEmpty(e.tutorialRunId) || e.tutorialElapsedSeconds<0 ? "" : e.tutorialElapsedSeconds.ToString("0.###",CultureInfo.InvariantCulture),
                string.IsNullOrEmpty(e.tutorialRunId) || e.tutorialPhaseSeconds<0 ? "" : e.tutorialPhaseSeconds.ToString("0.###",CultureInfo.InvariantCulture), e.completionSeconds,e.participantMask,e.assistanceStage,e.assistanceUsed,e.automaticCompletion,
                e.controlMode,e.player1Falls,e.player2Falls,e.recoveryCount,e.cooperationEventCount,e.hintRequestCount,e.retryRequestCount,e.voluntaryReplayCount };
            csv.AppendLine(string.Join(",", columns.Select(value => CsvCell(Convert.ToString(value, CultureInfo.InvariantCulture)))));
        }
        return csv.ToString();
    }

    private static string CsvCell(string value)
    {
        value = value ?? "";
        // Keep user-configured text safe when opened in spreadsheet programs.
        if (value.Length > 0 && "=+-@\t\r".IndexOf(value[0]) >= 0) value = "'" + value;
        return "\"" + value.Replace("\"", "\"\"") + "\"";
    }

    private static bool IsStorageError(Exception e) => e is IOException || e is UnauthorizedAccessException
        || e is System.Security.SecurityException || e is NotSupportedException;
    private void StorageError(Exception exception)
    {
        LastError = "Playtest data could not be saved. Check storage access and export before closing.";
        Debug.LogError("[Playtest] " + LastError + " " + exception.Message);
    }
}

public static class PlaytestData
{
    private static PlaytestStore store;
    public static PlaytestStore Store => store ??= new PlaytestStore(Path.Combine(Application.persistentDataPath, "Playtest"));
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reset() => store = null;
}
