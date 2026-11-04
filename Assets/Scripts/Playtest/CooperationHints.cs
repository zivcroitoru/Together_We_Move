using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Chooses visual guidance from current plate, bridge, and goal state.
/// A player already crossing a bridge is never sent back to an earlier plate.
/// </summary>
public sealed class CooperationHints : MonoBehaviour
{
    [SerializeField, Min(1f)] private float requestCooldownSeconds = 2f;

    private GameManager manager;
    private PlaytestUI ui;
    private TutorialManager tutorial;
    private PlayerController[] players;
    private PressurePlate[] plates;
    private BridgeController[] bridges;
    private readonly float[] cooldown = new float[3];

    private PressurePlate targetPlate;
    private PressurePlate heldPlate;
    private BridgeController targetBridge;
    private PlayerController guidedPlayer;
    private int helperId;
    private int goalCount;
    private bool showing;
    private bool guidingAcrossBridge;

    public void Initialize(GameManager owner)
    {
        manager = owner;
        ui = GetComponent<PlaytestUI>();
        tutorial = GetComponent<TutorialManager>();
        players = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
        plates = FindObjectsByType<PressurePlate>(FindObjectsSortMode.None);
        bridges = FindObjectsByType<BridgeController>(FindObjectsSortMode.None);
        NotifyProgress();
    }

    public void NotifyProgress()
    {
        if (showing) Hide();
    }

    public void Hide()
    {
        showing = false;
        if (ui) ui.HideHint();
    }

    private void Update()
    {
        if (!showing || !manager || manager.LevelCompleted || manager.IsTransitioning ||
            manager.Tracker?.Progress == null || (tutorial && tutorial.IsActive)) return;

        // A hint that used to target a plate is outdated as soon as the player
        // reaches the bridge. Replace it with guidance to keep crossing.
        if (targetBridge && !guidingAcrossBridge &&
            TryGetPlayerOnBridge(targetBridge, out _))
        {
            ShowPlan();
            return;
        }

        // If the player has completely crossed, stop showing a crossing hint.
        // The next hint request can choose their next unfinished objective.
        if (guidingAcrossBridge && guidedPlayer && targetBridge &&
            HasClearedBridge(guidedPlayer, targetBridge))
        {
            Hide();
            return;
        }

        if ((targetPlate && targetPlate.IsOccupied) ||
            (heldPlate && !heldPlate.IsOccupied) ||
            manager.Tracker.Progress.GoalCount != goalCount ||
            (targetBridge && helperId != 0 && manager.Tracker.HasAssisted(targetBridge, helperId)) ||
            (targetBridge && BothPlayersPastBridge(targetBridge)))
        {
            NotifyProgress();
        }
    }

    public void ShowAutomatic(bool hand)
    {
        if (!ui) return;
        ui.SetHandDemonstrations(hand);
        ShowPlan();
    }

    public void RequestHint(int playerId, string attribution)
    {
        if (!manager || manager.LevelCompleted || manager.IsTransitioning ||
            !CooperationProgress.ValidPlayer(playerId) ||
            Time.unscaledTime < cooldown[playerId]) return;

        cooldown[playerId] = Time.unscaledTime + requestCooldownSeconds;
        if (ui) ui.SetHandDemonstrations(true);
        if (tutorial && tutorial.IsActive)
        {
            tutorial.RequestHint(playerId, attribution);
            return;
        }

        ShowPlan();
        manager.Log("hint_requested", playerId, attribution, PlanId());
    }

    private static float BridgeRightEdge(BridgeController bridge)
    {
        var surface = bridge.Surface;
        return surface.transform.TransformPoint(surface.offset).x +
               surface.size.x * Mathf.Abs(surface.transform.lossyScale.x) * 0.5f;
    }

    private static bool HasClearedBridge(PlayerController player, BridgeController bridge)
    {
        return player && player.BodyCollider && bridge && bridge.Surface &&
               player.IsGrounded &&
               player.BodyCollider.bounds.min.x > BridgeRightEdge(bridge) + 0.05f;
    }

    private static bool IsOnBridge(PlayerController player, BridgeController bridge)
    {
        if (!player || !player.BodyCollider || !bridge || !bridge.Surface ||
            player.IsRecovering) return false;

        Bounds surface = bridge.Surface.bounds;
        Bounds body = player.BodyCollider.bounds;

        // The feet are near the bridge's walking surface and the player is
        // already over it. In this state, pointing back to a plate is wrong.
        return body.max.x >= surface.min.x + 0.05f &&
               body.min.x <= surface.max.x + 0.35f &&
               body.min.y >= surface.max.y - 0.9f &&
               body.min.y <= surface.max.y + 1.5f;
    }

    private bool TryGetPlayerOnBridge(BridgeController bridge, out PlayerController crossing)
    {
        crossing = null;
        if (players == null || manager?.Tracker?.Progress == null) return false;

        foreach (var player in players)
        {
            if (!player || manager.Tracker.Progress.HasReachedGoal(player.PlayerId)) continue;
            if (!IsOnBridge(player, bridge)) continue;
            if (!crossing || player.transform.position.x < crossing.transform.position.x)
                crossing = player;
        }
        return crossing;
    }

    private bool BothPlayersPastBridge(BridgeController bridge)
    {
        if (!bridge || !bridge.Surface || players == null) return false;
        int mask = 0;
        foreach (var player in players)
        {
            if (!player || !CooperationProgress.ValidPlayer(player.PlayerId)) continue;
            mask |= 1 << (player.PlayerId - 1);
            if (!HasClearedBridge(player, bridge)) return false;
        }
        return mask == 3;
    }

    private string PlanId()
    {
        if (guidingAcrossBridge && targetBridge)
            return "cross_bridge:" + CooperationTracker.ObjectId(targetBridge.transform);
        if (targetPlate)
            return "hold_plate:" + CooperationTracker.ObjectId(targetPlate.transform);
        return targetBridge
            ? "cross_bridge:" + CooperationTracker.ObjectId(targetBridge.transform)
            : "join_star";
    }

    private static bool Allowed(PressurePlate plate, PlayerController player)
    {
        return plate.Allowed == PressurePlate.AllowedPlayer.Any ||
               (int)plate.Allowed == player.PlayerId;
    }

    private void ShowPlan()
    {
        Hide();
        targetPlate = null;
        heldPlate = null;
        targetBridge = null;
        guidedPlayer = null;
        guidingAcrossBridge = false;
        helperId = 0;
        if (!ui || manager?.Tracker?.Progress == null || players == null) return;

        goalCount = manager.Tracker.Progress.GoalCount;
        PlayerController lagging = null;
        foreach (var player in players)
        {
            if (!player || manager.Tracker.Progress.HasReachedGoal(player.PlayerId)) continue;
            if (!lagging || player.transform.position.x < lagging.transform.position.x)
                lagging = player;
        }
        if (!lagging) return;

        float nearest = float.PositiveInfinity;
        foreach (var bridge in bridges)
        {
            if (!bridge || !bridge.Surface) continue;
            if (HasClearedBridge(lagging, bridge)) continue;
            float distance = Mathf.Abs(bridge.Surface.bounds.center.x - lagging.transform.position.x);
            if (distance >= nearest) continue;
            nearest = distance;
            targetBridge = bridge;
        }

        var spots = new List<PlaytestUI.HintSpot>();
        if (targetBridge)
        {
            // IMPORTANT: On the bridge, the correct next action is to finish
            // crossing, not return to a plate that was previously activated.
            if (TryGetPlayerOnBridge(targetBridge, out var crossing))
            {
                guidingAcrossBridge = true;
                guidedPlayer = crossing;
                spots.Add(PlaytestUI.HintSpot.Control(crossing.PlayerId, "Btn_Right"));
            }
            else
            {
                foreach (var plate in plates)
                    if (plate && plate.Bridge == targetBridge && plate.IsOccupied)
                    {
                        heldPlate = plate;
                        break;
                    }

                if (heldPlate)
                {
                    PlayerController holder = null;
                    foreach (var player in players)
                        if (player && heldPlate.ContainsPlayer(player))
                        {
                            holder = player;
                            break;
                        }

                    if (holder)
                    {
                        helperId = holder.PlayerId;
                        spots.Add(PlaytestUI.HintSpot.World(heldPlate.transform.position, helperId));
                        foreach (var player in players)
                            if (player && player != holder &&
                                !manager.Tracker.Progress.HasReachedGoal(player.PlayerId))
                                spots.Add(PlaytestUI.HintSpot.Control(player.PlayerId, "Btn_Right"));
                    }
                }
                else
                {
                    PlayerController actor = null;
                    float distance = float.PositiveInfinity;
                    foreach (var plate in plates)
                    {
                        if (!plate || plate.Bridge != targetBridge) continue;
                        foreach (var player in players)
                        {
                            if (!player || !Allowed(plate, player)) continue;
                            float d = Vector2.Distance(player.transform.position, plate.transform.position);
                            if (d >= distance) continue;
                            distance = d;
                            targetPlate = plate;
                            actor = player;
                        }
                    }

                    if (actor && targetPlate)
                    {
                        guidedPlayer = actor;
                        spots.Add(PlaytestUI.HintSpot.World(targetPlate.transform.position, actor.PlayerId));
                        spots.Add(PlaytestUI.HintSpot.Control(actor.PlayerId,
                            targetPlate.transform.position.x < actor.transform.position.x ? "Btn_Left" : "Btn_Right"));
                    }
                }
            }
        }

        if (spots.Count == 0)
        {
            var star = FindFirstObjectByType<SplitStarGoal>();
            if (star) spots.Add(PlaytestUI.HintSpot.World(star.transform.position, lagging.PlayerId));
            spots.Add(PlaytestUI.HintSpot.Control(lagging.PlayerId, "Btn_Right"));
            spots.Add(PlaytestUI.HintSpot.Control(lagging.PlayerId, "Btn_Jump"));
        }

        ui.ShowVisualHint(spots);
        showing = true;
    }
}
