using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Non-blocking onboarding: observe existing gameplay, never simulate input or pause play.</summary>
[RequireComponent(typeof(PlaytestUI))]
public sealed class TutorialManager : MonoBehaviour
{
    public enum Action { Move, Jump, OccupyPlate, HelpPartnerCross, ReachGoal }
    [Serializable]
    public sealed class Step
    {
        public string id;
        [Tooltip("0 Left, 1 Right, 2 Jump, then objective phases. All children in a phase must finish.")]
        [Min(0)] public int phase;
        public PlayerController player;
        public Action action;
        [Tooltip("Optional exact UI control. Otherwise the player's existing pad is used.")]
        public RectTransform control;
        public PressurePlate plate;
        public BridgeController bridge;
        [Min(.1f)] public float moveDistance=.65f;
        [Range(-1,1)] public int direction=1;
    }
    [SerializeField] bool startAutomatically=true;
    [SerializeField] bool repeatOnEveryVisit;
    [Header("Short iPad tutorial")]
    [Tooltip("Each child's highlight disappears on their first button press. The tutorial ends after both have pressed once.")]
    [SerializeField] bool stopAfterFirstButtonPress = true;
    [SerializeField] Step[] steps;
    [SerializeField,Min(1)] float repeatHandAfterSeconds=5;
    GameManager manager;PlaytestUI ui;
    float[] travels;int[] jumps;PlayerController[] players;
    readonly HashSet<string> objectiveEvidence=new();
    readonly List<PlaytestUI.HintSpot> targets=new();
    int phase;string guideKey;TutorialProgress progress;
    readonly bool[] firstButtonPressed = new bool[3];
    public bool SuppressAutomaticGuidance => stopAfterFirstButtonPress && TutorialSession.Completed;
    public bool IsActive { get; private set; }
    public int CurrentStepIndex => phase;
    public bool ControlsLearned => progress!=null&&progress.ControlsLearned(1)&&progress.ControlsLearned(2);

    public void Initialize(GameManager owner)
    {
        manager=owner;ui=GetComponent<PlaytestUI>();
        if(!startAutomatically||(TutorialSession.Completed&&!repeatOnEveryVisit))return;
        if(steps==null||steps.Length==0){Debug.LogError("[Tutorial] Configure actual gameplay steps.",this);return;}
        foreach(var step in steps)
            if(step==null||!step.player||(step.action==Action.OccupyPlate&&!step.plate)||(step.action==Action.HelpPartnerCross&&(!step.bridge||!step.plate)))
            {Debug.LogError("[Tutorial] Missing action reference; no player identity will be guessed.",this);return;}
        if(!ValidControlPhases())return;
        if(repeatOnEveryVisit)TutorialSession.Reset();
        progress=TutorialSession.Progress;progress.Begin(Time.unscaledTime);TutorialSession.Begin();
        phase=TutorialSession.NextStep;players=FindObjectsByType<PlayerController>(FindObjectsSortMode.None);

        IsActive=true;ui.DemonstrationRepeated+=OnRepeated;
        Metric("tutorial_started",0,"system","");
        if (stopAfterFirstButtonPress)
        {
            InstallFirstPressListeners();
            ShowFirstPressGuide();
        }
        else BeginPhase();
    }

    // Attach to the existing three UI buttons for each player. Pointer-down fires
    // immediately, even if the character cannot yet move due to an obstacle.
    void InstallFirstPressListeners()
    {
        foreach (var canvas in FindObjectsByType<Canvas>(FindObjectsSortMode.None))
        {
            for (int id = 1; id <= 2; id++)
            {
                Transform pad = canvas.transform.Find(id == 1 ? "P1_Controls" : "P2_Controls");
                if (!pad) continue;
                foreach (var name in new[] { "Btn_Left", "Btn_Right", "Btn_Jump" })
                {
                    Transform button = pad.Find(name);
                    if (!button) continue;
                    var listener = button.GetComponent<TutorialFirstPressListener>();
                    if (!listener) listener = button.gameObject.AddComponent<TutorialFirstPressListener>();
                    listener.Configure(this, id, name);
                }
            }
        }
    }

    // One spotlight per child until they use any of their own controls.
    // We recommend forward movement at the start, not an unnecessary left detour.
    void ShowFirstPressGuide()
    {
        if (!IsActive || !ui) return;
        targets.Clear();
        var goal = FindFirstObjectByType<SplitStarGoal>();
        foreach (var actor in players)
        {
            if (!actor || !CooperationProgress.ValidPlayer(actor.PlayerId) ||
                firstButtonPressed[actor.PlayerId]) continue;
            bool goalIsLeft = goal && goal.transform.position.x < actor.transform.position.x;
            targets.Add(PlaytestUI.HintSpot.Control(actor.PlayerId,
                goalIsLeft ? "Btn_Left" : "Btn_Right"));
        }
        ui.ShowVisualHint(targets, repeatHandAfterSeconds,
            progress.RunId + ":first_button:" + firstButtonPressed[1] + ":" + firstButtonPressed[2]);
    }

    public void OnFirstButtonPressed(int playerId, string button)
    {
        if (!stopAfterFirstButtonPress || !IsActive ||
            !CooperationProgress.ValidPlayer(playerId) || firstButtonPressed[playerId] ||
            manager == null || manager.IsTransitioning || manager.LevelCompleted) return;

        firstButtonPressed[playerId] = true;
        Metric("tutorial_first_button_pressed", playerId, "touch", button);
        // Record a first use, not full mastery of Left, Right and Jump.
        progress.MarkFirstInput(phase, playerId);

        if (firstButtonPressed[1] && firstButtonPressed[2])
            EndShortTutorial("both_players_pressed");
        else
            ShowFirstPressGuide();
    }

    void EndShortTutorial(string reason)
    {
        if (!IsActive) return;
        IsActive = false;
        ui.DemonstrationRepeated -= OnRepeated;
        ui.HideHint();
        Metric("tutorial_short_completed", 0, "system", reason);
        TutorialSession.Complete();
        GetComponent<CooperationHints>()?.NotifyProgress();
    }

    bool ValidControlPhases()
    {
        var ids=new HashSet<string>();
        var children=new HashSet<string>();
        var phases=new HashSet<int>();
        foreach(var step in steps)
        {
            bool valid=!string.IsNullOrEmpty(step.id)&&ids.Add(step.id)&&CooperationProgress.ValidPlayer(step.player.PlayerId)
                &&children.Add(step.phase+":"+step.player.PlayerId)&&step.phase>=0;
            if(step.phase<3)
                valid&=step.control&&(step.phase==2?step.action==Action.Jump:step.action==Action.Move&&step.direction==(step.phase==0?-1:1));
            else valid&=step.action!=Action.Move&&step.action!=Action.Jump;
            if(!valid){Debug.LogError("[Tutorial] Each phase needs unique child/step IDs and the matching real action/control.",this);return false;}
            phases.Add(step.phase);
        }
        for(int p=0;p<=MaxPhase();p++)
            if(!phases.Contains(p)||(p<3&&(!children.Contains(p+":1")||!children.Contains(p+":2"))))
            {Debug.LogError("[Tutorial] Configure both children for left, right, jump, followed by contiguous objective phases.",this);return false;}
        return true;
    }
    string ActionName => phase==0?"left":phase==1?"right":phase==2?"jump":"objective";
    void Metric(string type,int player,string attribution,string detail)
    {
        manager.Log(type,player,attribution,detail,tutorialRunId:progress.RunId,tutorialAction:ActionName,
            tutorialElapsedSeconds:progress.Elapsed(Time.unscaledTime),tutorialPhaseSeconds:progress.PhaseElapsed(phase,Time.unscaledTime));
    }
    void BeginPhase()
    {
        progress.BeginPhase(phase,Time.unscaledTime);TutorialSession.Advance(phase);
        travels=new float[steps.Length];jumps=new int[steps.Length];
        for(int i=0;i<steps.Length;i++){travels[i]=Travel(steps[i]);jumps[i]=steps[i].player.SuccessfulJumpCount;}
        guideKey=null;RefreshGuide(true);
    }
    void Update()
    {
        if(!IsActive||manager.Tracker?.Progress==null)return;
        if (stopAfterFirstButtonPress)
        {
            if (manager.LevelCompleted) EndShortTutorial("level_completed");
            return; // Button events, rather than movement-distance thresholds, dismiss this tutorial.
        }
        if(manager.LevelCompleted){Finish();return;}
        if(manager.IsTransitioning)return; // A fall reload resumes the same unfinished run.
        // Remember genuine early objective actions without showing objective guidance yet.
        foreach(var step in steps)if(step.phase>=3&&ObjectivePerformed(step))objectiveEvidence.Add(step.id);
        bool all=true;bool any=false;
        for(int i=0;i<steps.Length;i++)
        {
            var step=steps[i];if(step.phase!=phase)continue;any=true;
            if(progress.HasLearned(phase,step.player.PlayerId))continue;
            bool first=FirstSuccess(i);
            if(first&&progress.MarkFirstInput(phase,step.player.PlayerId))Metric("tutorial_first_success",step.player.PlayerId,"gameplay",step.id);
            bool learned=phase>=3?objectiveEvidence.Contains(step.id):Performed(i);
            if(learned)
            {
                if(progress.MarkFirstInput(phase,step.player.PlayerId))Metric("tutorial_first_success",step.player.PlayerId,"gameplay",step.id);
                if(progress.Learn(phase,step.player.PlayerId))
                {
                    Metric("tutorial_action_learned",step.player.PlayerId,"gameplay",step.id);StartCoroutine(Feedback(step.player));
                    if(progress.ControlsLearned(step.player.PlayerId)&&progress.MarkTotal("controls",step.player.PlayerId))Metric("tutorial_controls_completed",step.player.PlayerId,"gameplay","");
                }
            }
            else all=false;
        }
        // The AND gate is per phase: one player's success never advances the other.
        if(any&&all)
        {
            phase++;
            if(phase>MaxPhase()){Finish();return;}
            BeginPhase();
        }
        else RefreshGuide(false);
    }
    int MaxPhase(){int max=0;foreach(var s in steps)max=Mathf.Max(max,s.phase);return max;}
    bool FirstSuccess(int i)
    {
        var step=steps[i];if(phase>=3)return objectiveEvidence.Contains(step.id);
        if(step.action==Action.Jump)return Performed(i);
        return Travel(step)-travels[i]>.015f;
    }
    bool Performed(int i)
    {
        var step=steps[i];
        if(step.action==Action.Jump)return step.player.SuccessfulJumpCount>jumps[i]&&!step.player.IsGrounded;
        if(step.action==Action.Move)return Travel(step)-travels[i]>=step.moveDistance;
        return ObjectivePerformed(step);
    }
    static float Travel(Step step)=>step.direction<0?step.player.SuccessfulLeftDistance:step.player.SuccessfulRightDistance;
    bool ObjectivePerformed(Step step)
    {
        if(step.action==Action.OccupyPlate)return step.plate.ContainsPlayer(step.player);
        if(step.action==Action.HelpPartnerCross)return manager.Tracker.HasAssisted(step.bridge,step.player.PlayerId);
        if(step.action==Action.ReachGoal)return manager.Tracker.Progress.HasReachedGoal(step.player.PlayerId);
        return false;
    }
    // The current objective must follow the player's real position. In particular,
    // a child on an active bridge should continue across it, not return to a plate.
    bool TryCrossingDirection(out PlayerController crossing, out string button)
    {
        crossing = null;
        button = "Btn_Right";
        if (players == null) return false;

        var star = FindFirstObjectByType<SplitStarGoal>();
        float nearestToStart = float.PositiveInfinity;
        foreach (var bridge in FindObjectsByType<BridgeController>(FindObjectsSortMode.None))
        {
            if (!bridge || !bridge.Surface || !bridge.Surface.enabled ||
                !bridge.CanObserveLocally) continue;
            var span = bridge.Surface.bounds;
            bool toRight = !star || star.transform.position.x >= span.center.x;

            foreach (var player in players)
            {
                if (!player || !player.BodyCollider || player.IsRecovering ||
                    manager.Tracker.Progress.HasReachedGoal(player.PlayerId)) continue;
                var body = player.BodyCollider.bounds;
                // The character must actually be standing over this bridge,
                // not merely nearby on a pressure plate or adjacent platform.
                if (body.center.x < span.min.x + .05f ||
                    body.center.x > span.max.x - .05f ||
                    body.min.y < span.max.y - .9f ||
                    body.min.y > span.max.y + 1.2f) continue;

                // Guide the child who still needs to finish crossing.
                float remaining = toRight ? body.center.x - span.min.x
                                          : span.max.x - body.center.x;
                if (remaining >= nearestToStart) continue;
                nearestToStart = remaining;
                crossing = player;
                button = toRight ? "Btn_Right" : "Btn_Left";
            }
        }
        return crossing != null;
    }

    void RefreshGuide(bool force)
    {
        targets.Clear();
        string key = phase.ToString();

        // The first three phases explicitly teach Left, Right and Jump.
        // For later objective phases, the current bridge traversal takes priority
        // over an old plate instruction or a serialized default direction.
        if (phase >= 3 && TryCrossingDirection(out var crossing, out var crossingButton))
        {
            targets.Add(PlaytestUI.HintSpot.Control(crossing.PlayerId, crossingButton));
            key += ":active_bridge:" + crossing.PlayerId + ":" + crossingButton;
        }
        else
        {
            foreach (var step in steps)
            {
                if (step.phase != phase || progress.HasLearned(phase, step.player.PlayerId))
                    continue;

                var actor = step.player;
                string control = step.action == Action.Jump ? "Btn_Jump"
                               : step.direction < 0 ? "Btn_Left" : "Btn_Right";

                if (step.action == Action.OccupyPlate ||
                    step.action == Action.HelpPartnerCross)
                {
                    targets.Add(PlaytestUI.HintSpot.Follow(step.plate.transform, actor.PlayerId));
                    if (step.action == Action.HelpPartnerCross && step.plate.ContainsPlayer(actor))
                    {
                        foreach (var other in players)
                            if (other && other.PlayerId != actor.PlayerId)
                            { actor = other; break; }
                        // The other player must cross to the goal, not go back.
                        control = "Btn_Right";
                    }
                    else
                    {
                        control = step.plate.transform.position.x < actor.transform.position.x
                                  ? "Btn_Left" : "Btn_Right";
                    }
                }
                else if (step.action == Action.ReachGoal)
                {
                    var star = FindFirstObjectByType<SplitStarGoal>();
                    if (star)
                    {
                        targets.Add(PlaytestUI.HintSpot.Follow(star.transform, actor.PlayerId));
                        control = star.transform.position.x < actor.transform.position.x
                                  ? "Btn_Left" : "Btn_Right";
                    }
                }

                // An Inspector-assigned control belongs only to the initial
                // Left/Right/Jump lessons. Never force a stale button for a
                // moving plate/bridge/goal objective.
                bool useExactControl = phase < 3 && actor == step.player &&
                                       step.control && step.control.name == control;
                targets.Add(useExactControl
                    ? PlaytestUI.HintSpot.ExactControl(step.control, actor.PlayerId,
                                                        step.action == Action.Jump)
                    : PlaytestUI.HintSpot.Control(actor.PlayerId, control));
                key += ":" + actor.PlayerId + ":" + control;
            }
        }

        if (!force && guideKey == key) return;
        guideKey = key;
        ui.ShowVisualHint(targets, repeatHandAfterSeconds, progress.RunId + ":" + key);
    }

    // Called when the researcher switches input modes.
    public void RefreshControls()
    {
        if (IsActive) RefreshGuide(true);
    }
    public void RequestHint(int playerId,string attribution)
    {
        if(!IsActive)return;
        Metric("hint_requested",playerId,attribution,"onboarding");
        if(ui.RepeatHandDemo(playerId))Metric("tutorial_demo_repeated",playerId,"hint","manual");
    }
    public void RepeatDemonstration(){if(IsActive)ui.RepeatHandDemo();}
    void OnRepeated(int playerId){if(IsActive)Metric("tutorial_demo_repeated",playerId,"system","inactivity");}
    IEnumerator Feedback(PlayerController player)
    {
        // Tutorial sound follows its real progress event through TWMGameAudio.
        var view=player.GetComponentInChildren<PlayerView>();var visual=view?view.transform:null;var scale=visual?visual.localScale:Vector3.one;
        float elapsed=0;while(elapsed<.25f){elapsed+=Time.unscaledDeltaTime;if(visual)visual.localScale=scale*(1+.1f*Mathf.Sin(Mathf.Clamp01(elapsed/.25f)*Mathf.PI));yield return null;}if(visual)visual.localScale=scale;
    }
    public void EndForAssistedCompletion()
    {
        if(!IsActive)return;
        Metric("tutorial_ended_with_assistance",0,"system","");
        IsActive=false;ui.DemonstrationRepeated-=OnRepeated;ui.HideHint();
        // End this session without claiming the child learned unperformed controls.
        TutorialSession.Complete();
    }
    void Finish()
    {
        if(!IsActive)return;IsActive=false;ui.DemonstrationRepeated-=OnRepeated;ui.HideHint();
        if(ControlsLearned)
        {
            foreach(int player in new[]{1,2})if(progress.MarkTotal("tutorial",player))Metric("tutorial_player_completed",player,"gameplay","");
            TutorialSession.Complete();Metric("tutorial_completed",0,"system","");
        }
        GetComponent<CooperationHints>()?.NotifyProgress();
    }
    void OnDisable(){if(ui)ui.DemonstrationRepeated-=OnRepeated;if(IsActive){IsActive=false;if(ui)ui.HideHint();}}
}
