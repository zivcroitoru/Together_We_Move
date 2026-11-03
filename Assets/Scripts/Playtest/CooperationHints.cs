using System.Collections.Generic;
using UnityEngine;

/// <summary>Chooses one real next action from plate/bridge state. No written instructions.</summary>
public sealed class CooperationHints : MonoBehaviour
{
    [SerializeField, Min(1)] float requestCooldownSeconds=2;
    [SerializeField] bool automaticHints=true;
    [SerializeField, Min(5)] float stuckAfterSeconds=25;
    GameManager manager; PlaytestUI ui; PlayerController[] players; PressurePlate[] plates; BridgeController[] bridges;
    float lastActivity; readonly float[] cooldown=new float[3]; Vector3[] lastPositions;
    PressurePlate targetPlate, heldPlate; BridgeController targetBridge; int helperId;
    bool showing; int goalCount;TutorialManager tutorial;
    public bool IsStuck=>Time.unscaledTime-lastActivity>=stuckAfterSeconds;
    public void Initialize(GameManager owner)
    {
        tutorial=GetComponent<TutorialManager>();manager=owner;ui=GetComponent<PlaytestUI>();players=FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
        plates=FindObjectsByType<PressurePlate>(FindObjectsSortMode.None);bridges=FindObjectsByType<BridgeController>(FindObjectsSortMode.None);
        lastPositions=new Vector3[players.Length];for(int i=0;i<players.Length;i++)lastPositions[i]=players[i].transform.position;
        NotifyProgress();
    }
    public void NotifyProgress(){lastActivity=Time.unscaledTime;if(showing)Hide();}
    public void Hide(){showing=false;if(ui)ui.HideHint();}
    void Update()
    {
        if(!manager||manager.LevelCompleted||manager.IsTransitioning||manager.Tracker?.Progress==null||(tutorial&&tutorial.IsActive))return;
        bool moved=false;
        for(int i=0;i<players.Length;i++)if(players[i]&&Vector3.Distance(lastPositions[i],players[i].transform.position)>.12f){moved=true;lastPositions[i]=players[i].transform.position;}
        if(moved)lastActivity=Time.unscaledTime;
        if(showing)
        {
            if((targetPlate&&targetPlate.IsOccupied)||(heldPlate&&!heldPlate.IsOccupied)||manager.Tracker.Progress.GoalCount!=goalCount||
                (targetBridge&&helperId!=0&&manager.Tracker.HasAssisted(targetBridge,helperId)))NotifyProgress();
        }
        else if(automaticHints&&IsStuck){ShowPlan();manager.Log("automatic_hint_shown",0,"system",PlanId());lastActivity=Time.unscaledTime;}
    }
    public void RequestHint(int playerId,string attribution)
    {
        if(!manager||manager.LevelCompleted||manager.IsTransitioning||!CooperationProgress.ValidPlayer(playerId)||Time.unscaledTime<cooldown[playerId])return;
        cooldown[playerId]=Time.unscaledTime+requestCooldownSeconds;
        if(tutorial&&tutorial.IsActive)
        {tutorial.RequestHint(playerId,attribution);return;}
        ShowPlan();manager.Log("hint_requested",playerId,attribution,PlanId());lastActivity=Time.unscaledTime;
    }
    string PlanId()=>targetPlate?"hold_plate:"+CooperationTracker.ObjectId(targetPlate.transform):targetBridge?"cross_bridge:"+CooperationTracker.ObjectId(targetBridge.transform):"join_star";
    bool Allowed(PressurePlate plate,PlayerController player)=>plate.Allowed==PressurePlate.AllowedPlayer.Any||(int)plate.Allowed==player.PlayerId;
    void ShowPlan()
    {
        Hide();targetPlate=null;heldPlate=null;targetBridge=null;helperId=0;goalCount=manager.Tracker.Progress.GoalCount;
        PlayerController lagging=null;foreach(var p in players)if(p&&!manager.Tracker.Progress.HasReachedGoal(p.PlayerId)&&(!lagging||p.transform.position.x<lagging.transform.position.x))lagging=p;
        if(!lagging)return;
        float nearest=float.PositiveInfinity;
        foreach(var bridge in bridges)
        {
            if(!bridge||!bridge.Surface)continue;var surface=bridge.Surface;
            float center=surface.transform.TransformPoint(surface.offset).x;float end=center+surface.size.x*surface.transform.lossyScale.x*.5f;
            if(lagging.transform.position.x>end+.6f)continue;
            float distance=Mathf.Abs(center-lagging.transform.position.x);if(distance<nearest){nearest=distance;targetBridge=bridge;}
        }
        var spots=new List<PlaytestUI.HintSpot>();
        if(targetBridge)
        {
            foreach(var plate in plates)if(plate&&plate.Bridge==targetBridge&&plate.IsOccupied){heldPlate=plate;break;}
            if(heldPlate)
            {
                PlayerController holder=null;
                foreach(var p in players)if(p&&heldPlate.ContainsPlayer(p)){holder=p;break;}
                if(holder)
                {
                    helperId=holder.PlayerId;spots.Add(PlaytestUI.HintSpot.World(heldPlate.transform.position,helperId));
                    foreach(var p in players)if(p&&p!=holder){spots.Add(PlaytestUI.HintSpot.Control(p.PlayerId,"Btn_Right"));}
                }
            }
            else
            {
                PlayerController actor=null;float distance=float.PositiveInfinity;
                foreach(var plate in plates)if(plate&&plate.Bridge==targetBridge)
                    foreach(var p in players)if(p&&Allowed(plate,p))
                    {float d=Vector2.Distance(p.transform.position,plate.transform.position);if(d<distance){distance=d;targetPlate=plate;actor=p;}}
                if(actor&&targetPlate)
                {
                    spots.Add(PlaytestUI.HintSpot.World(targetPlate.transform.position,actor.PlayerId));
                    spots.Add(PlaytestUI.HintSpot.Control(actor.PlayerId,targetPlate.transform.position.x<actor.transform.position.x?"Btn_Left":"Btn_Right"));
                }
            }
        }
        if(spots.Count==0)
        {
            var star=FindFirstObjectByType<SplitStarGoal>();if(star)spots.Add(PlaytestUI.HintSpot.World(star.transform.position,lagging.PlayerId));
            spots.Add(PlaytestUI.HintSpot.Control(lagging.PlayerId,"Btn_Right"));spots.Add(PlaytestUI.HintSpot.Control(lagging.PlayerId,"Btn_Jump"));
        }
        ui.ShowVisualHint(spots);showing=true;
    }
}
