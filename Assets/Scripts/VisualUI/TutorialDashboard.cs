using System.Globalization;
using System.Linq;
using System.Text;

/// <summary>Read-only projection of persisted tutorial events; never appears in child UI.</summary>
public static class TutorialDashboard
{
    public static string Format(PlaytestStore store)
    {
        var last=store.Events.LastOrDefault(e=>e.pairId==store.PairId&&!string.IsNullOrEmpty(e.tutorialRunId));
        if(last==null)return "Developer Playtest Dashboard\nNo onboarding data for this pair yet.";
        var events=store.Events.Where(e=>e.pairId==store.PairId&&e.tutorialRunId==last.tutorialRunId).ToArray();
        var text=new StringBuilder("Developer Playtest Dashboard — latest onboarding run\nFirst = first successful action; learned = movement threshold / airborne jump\nTimes below are seconds from phase start (unscaled, including reload wait).\n");
        foreach(int player in new[]{1,2})
        {
            text.Append("\nPlayer ").Append(player).Append(player==1?" (Blue)\n":" (Red)\n");
            foreach(string action in new[]{"left","right","jump"})
            {
                var first=events.FirstOrDefault(e=>e.playerId==player&&e.tutorialAction==action&&e.eventType=="tutorial_first_success");
                var learned=events.FirstOrDefault(e=>e.playerId==player&&e.tutorialAction==action&&e.eventType=="tutorial_action_learned");
                text.Append(action).Append(": first ").Append(Time(first,false)).Append(" / learned ").Append(Time(learned,false));
                text.Append(" | hints ").Append(events.Count(e=>e.playerId==player&&e.tutorialAction==action&&e.eventType=="hint_requested"));
                text.Append(" | repeats ").Append(events.Count(e=>e.playerId==player&&e.tutorialAction==action&&e.eventType=="tutorial_demo_repeated")).Append('\n');
            }
            text.Append("Controls total: ").Append(Time(events.FirstOrDefault(e=>e.playerId==player&&e.eventType=="tutorial_controls_completed"),true));
            text.Append(" | Tutorial total: ").Append(Time(events.FirstOrDefault(e=>e.playerId==player&&e.eventType=="tutorial_player_completed"),true)).Append('\n');
            text.Append("All phases: hints ").Append(events.Count(e=>e.playerId==player&&e.eventType=="hint_requested"));
            text.Append(" | demonstration repeats ").Append(events.Count(e=>e.playerId==player&&e.eventType=="tutorial_demo_repeated")).Append('\n');
        }
        return text.ToString();
    }
    static string Time(PlaytestEvent value,bool total)=>value==null?"waiting":(total?value.tutorialElapsedSeconds:value.tutorialPhaseSeconds).ToString("0.00",CultureInfo.InvariantCulture)+"s";
}
