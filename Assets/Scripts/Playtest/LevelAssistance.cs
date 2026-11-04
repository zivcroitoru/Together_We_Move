using System.Collections.Generic;
using UnityEngine;

/// <summary>Per-level encouragement. Only new gameplay milestones reset the inactivity clock.
/// State survives falls so repeated reloads cannot indefinitely postpone the fallback.</summary>
[RequireComponent(typeof(GameManager))]
public sealed class LevelAssistance : MonoBehaviour
{
    [Header("Seconds without new progress")]
    [SerializeField,Min(0)] float highlightAfter=10,handAfter=20,easeAfter=30,finishAfter=45;
    [SerializeField,Min(0)] float finalVisualSeconds=1.5f;
    [SerializeField,Min(0)] float bridgeReleaseGrace=8;
    [Header("Score weights (normalized)")]
    [SerializeField,Min(0)] float participationWeight=20,cooperationWeight=60,goalWeight=20;
    [SerializeField,Range(0,1)] float easedCreditMultiplier=.8f,automaticCreditMultiplier=.45f;
    [SerializeField, Range(.1f,1f)] float recoveryCreditMultiplier=.9f;
    sealed class State
    {
        public float LastProgress,Started,FinalAt=-1;
        public int Stage;
        public int Recoveries;
        public bool MechanicalUsed,Automatic,EaseEnabled;
        public readonly HashSet<string> Seen=new();
    }
    static readonly Dictionary<string,State> sessions=new();
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)] static void ResetSessions()=>sessions.Clear();
    GameManager manager;PlaytestUI ui;CooperationHints hints;TutorialManager tutorial;
    PressurePlate[] plates;PlayerController[] players;Lever[] levers;BridgeController[] bridges;State state;string key;
    public bool Assisted => state!=null&&(state.MechanicalUsed||state.Automatic||state.Recoveries>0);
    public bool Automatic => state!=null&&state.Automatic;
    public float CompletionSeconds => state==null?0:Mathf.Max(0,Time.unscaledTime-state.Started);
    public int Stage => state?.Stage??0;
    public float Score => LevelCooperationScore.Calculate(manager?.Tracker?.Progress,participationWeight,cooperationWeight,goalWeight,
        state==null?1:Mathf.Min(state.Automatic?automaticCreditMultiplier:state.MechanicalUsed?easedCreditMultiplier:1,
            Mathf.Pow(recoveryCreditMultiplier,state.Recoveries)));
    public void RegisterFallRecovery(int playerId)
    {
        if (state == null) return;
        state.Recoveries++;
        manager.Log("mechanical_assistance_used",playerId,"system","fall_recovery");
    }
    public void Initialize(GameManager owner)
    {
        manager=owner;ui=GetComponent<PlaytestUI>();hints=GetComponent<CooperationHints>();tutorial=GetComponent<TutorialManager>();
        key=PlaytestData.Store.PairId+":"+gameObject.scene.path;
        if(!sessions.TryGetValue(key,out state)){state=new State{Started=Time.unscaledTime,LastProgress=Time.unscaledTime};sessions[key]=state;}
        players=FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
        plates=FindObjectsByType<PressurePlate>(FindObjectsSortMode.None);levers=FindObjectsByType<Lever>(FindObjectsSortMode.None);bridges=FindObjectsByType<BridgeController>(FindObjectsSortMode.None);
        foreach(var bridge in bridges){bridge.GraceUsed+=OnGraceUsed;if(state.EaseEnabled)bridge.SetAssistanceGrace(bridgeReleaseGrace);}
        ui.SetHandDemonstrations(state.Stage>=2);
        manager.Log("scoring_config",0,"system",string.Format(System.Globalization.CultureInfo.InvariantCulture,
            "participation={0};cooperation={1};goal={2};eased={3};automatic={4};thresholds={5}/{6}/{7}/{8};releaseGrace={9}",
            participationWeight,cooperationWeight,goalWeight,easedCreditMultiplier,automaticCreditMultiplier,highlightAfter,handAfter,easeAfter,finishAfter,bridgeReleaseGrace));
    }
    public void NewRun(){if(key!=null)sessions.Remove(key);}
    public void Progress(string milestone,int player=0)
    {
        if(state==null||!state.Seen.Add(milestone+":"+player))return;
        state.LastProgress=Time.unscaledTime;state.Stage=0;state.FinalAt=-1;
        ui.SetHandDemonstrations(false);hints.NotifyProgress();
        if (milestone.StartsWith("plate:") || milestone.StartsWith("lever:"))
            GetComponent<GameplayFeedback>()?.Discovery(milestone,player);
        manager.Log("meaningful_progress",player,"gameplay",milestone);
        // Once enabled, the forgiving release window stays for this run; actual use remains logged.
    }
    void OnGraceUsed()
    {
        if(state.MechanicalUsed)return;state.MechanicalUsed=true;
        manager.Log("mechanical_assistance_used",0,"system","bridge_release_grace");
    }
    void Update()
    {
        if(state==null||manager.LevelCompleted||manager.IsTransitioning)return;
        foreach(var plate in plates)foreach(var player in players)
            if(plate.ContainsPlayer(player))Progress("plate:"+CooperationTracker.ObjectId(plate.transform),player.PlayerId);
        foreach(var lever in levers)if(lever.IsFlipped)Progress("lever:"+CooperationTracker.ObjectId(lever.transform));
        float idle=Time.unscaledTime-state.LastProgress;
        if(state.Stage<1&&idle>=highlightAfter){state.Stage=1;Guide(false);manager.Log("automatic_highlight",0,"system");}
        if(state.Stage<2&&idle>=Mathf.Max(highlightAfter,handAfter)){state.Stage=2;Guide(true);manager.Log("automatic_demonstration",0,"system");}
        if(state.Stage<3&&idle>=Mathf.Max(handAfter,easeAfter))
        {state.Stage=3;state.EaseEnabled=true;foreach(var bridge in bridges)bridge.SetAssistanceGrace(bridgeReleaseGrace);manager.Log("assistance_eased",0,"system","bridge_release_grace");}
        if(state.Stage<4&&idle>=Mathf.Max(easeAfter,finishAfter))
        {state.Stage=4;state.FinalAt=Time.unscaledTime;Guide(true);manager.Log("final_visual_assistance",0,"system");}
        if(state.Stage==4&&Time.unscaledTime-state.FinalAt>=finalVisualSeconds)
        {state.Automatic=true;manager.Log("automatic_completion",0,"system","remaining_requirement_waived");manager.CompleteWithAssistance();}
    }
    void Guide(bool hand)
    {
        ui.SetHandDemonstrations(hand);
        if(tutorial&&tutorial.IsActive){if(hand)tutorial.RepeatDemonstration();}
        else hints.ShowAutomatic(hand);
    }
    void OnDestroy(){if(bridges!=null)foreach(var bridge in bridges)if(bridge)bridge.GraceUsed-=OnGraceUsed;}
    void OnValidate()
    {handAfter=Mathf.Max(highlightAfter,handAfter);easeAfter=Mathf.Max(handAfter,easeAfter);finishAfter=Mathf.Max(easeAfter,finishAfter);}
}
