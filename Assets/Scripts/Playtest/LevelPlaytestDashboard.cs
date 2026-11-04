using System.Linq;
using System.Text;
using System.Globalization;

/// <summary>Researcher-only per-level summaries from the journal, including assisted attempts.</summary>
public static class LevelPlaytestDashboard
{
    public static string Format(PlaytestStore store)
    {
        var output=new StringBuilder("LEVEL PLAYTEST RESULTS\n");
        foreach(var level in store.Events.Where(e=>e.pairId==store.PairId&&!string.IsNullOrEmpty(e.level)).GroupBy(e=>e.level))
        {
            var latest=level.LastOrDefault(e=>e.eventType=="attempt_finished"&&(e.outcome=="full"||e.outcome=="partial"||e.outcome=="assisted"))??level.Last();
            var attempt=level.Where(e=>e.attemptId==latest.attemptId).ToArray();
            output.Append(System.IO.Path.GetFileNameWithoutExtension(level.Key)).Append(" | attempt ").Append(latest.attemptNumber)
                .Append(" | score ").Append(latest.cooperationScore.ToString("0.#",CultureInfo.InvariantCulture))
                .Append(" | ").Append(latest.completionSeconds.ToString("0.0",CultureInfo.InvariantCulture)).Append("s | ")
                .Append(latest.assistanceUsed?"assisted":"independent").Append(" / ").Append(latest.outcome??"ongoing").Append('\n');
            output.Append("Both participated: ").Append(latest.participantMask==3?"yes":"no")
                .Append(" | hints P1/P2 ").Append(attempt.Count(e=>e.eventType=="hint_requested"&&e.playerId==1)).Append('/')
                .Append(attempt.Count(e=>e.eventType=="hint_requested"&&e.playerId==2))
                .Append(" | auto visuals ").Append(attempt.Count(e=>e.eventType=="automatic_highlight"||e.eventType=="automatic_demonstration"||e.eventType=="final_visual_assistance"))
                .Append(" | mechanical/finish aid ").Append(latest.assistanceUsed?"yes":"no")
                .Append(" | auto finish ").Append(latest.automaticCompletion?"yes":"no")
                .Append(" | replays P1/P2 ").Append(store.RetryCount(level.Key,1)).Append('/').Append(store.RetryCount(level.Key,2)).Append('\n');
            output.Append("Falls A/B ").Append(attempt.Count(e=>e.eventType=="player_fell"&&e.playerId==1)).Append('/')
                .Append(attempt.Count(e=>e.eventType=="player_fell"&&e.playerId==2))
                .Append(" | recovered ").Append(attempt.Count(e=>e.eventType=="player_recovered"))
                .Append(" | cooperation ").Append(attempt.Count(e=>e.eventType=="cooperation_succeeded"))
                .Append(" | controls ").Append(attempt.LastOrDefault(e=>e.eventType=="control_mode_selected")?.detail??latest.controlMode??"legacy")
                .Append(" | retry requests ").Append(level.Count(e=>e.eventType=="retry_requested"))
                .Append(" | voluntary replays ").Append(level.Count(e=>e.eventType=="voluntary_replay")).Append('\n');
        }
        return output.ToString();
    }
}
