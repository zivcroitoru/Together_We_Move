using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Observes the existing bridge colliders and identified switch/lever helpers.
/// A crossing requires contact with a supported bridge and arrival on the opposite bank.</summary>
public sealed class CooperationTracker : MonoBehaviour
{
    [SerializeField] private BridgeController[] bridges;
    [SerializeField] private PlayerController[] players;
    public CooperationProgress Progress { get; private set; }
    public event Action<string, int, string> Milestone;
    private readonly List<Crossing> crossings = new();
    private bool observing;

    private sealed class Crossing
    {
        public BridgeController Bridge;
        public PlayerController Player;
        public string Id;
        public int Bank;
        public int Helpers;
        public bool Touched;
    }

    public void Initialize()
    {
        if (bridges == null || bridges.Length == 0)
            bridges = FindObjectsByType<BridgeController>(FindObjectsSortMode.None);
        if (players == null || players.Length == 0)
            players = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);

        var ids = new HashSet<string>();
        foreach (var bridge in bridges)
        {
            if (!bridge || !bridge.Surface)
            {
                Debug.LogError("[Playtest] A scored bridge needs its existing BridgeJuice surface collider.", this);
                continue;
            }
            string id = ObjectId(bridge.transform);
            if (!ids.Add(id)) continue;
            foreach (var player in players)
                if (player) crossings.Add(new Crossing { Bridge = bridge, Player = player, Id = id });
        }
        Progress = new CooperationProgress(ids);
        observing = true;
    }

    public void StopObserving() => observing = false;
    public void ResetCrossings(int playerId)
    {
        foreach (var crossing in crossings) if (crossing.Player && crossing.Player.PlayerId == playerId)
        { crossing.Bank = 0; crossing.Helpers = 0; crossing.Touched = false; }
    }

    private void FixedUpdate()
    {
        if (!observing) return;
        foreach(var player in players)
            if(player&&(player.SuccessfulLeftDistance+player.SuccessfulRightDistance>.1f||player.SuccessfulJumpCount>0)&&Progress.Participate(player.PlayerId))
                Milestone?.Invoke("player_participated",player.PlayerId,"controls");
        foreach (var crossing in crossings) Observe(crossing);
    }

    private void Observe(Crossing c)
    {
        if (!c.Player || !c.Bridge || !c.Bridge.CanObserveLocally) return;
        if (c.Player.IsRecovering) { c.Bank = 0; c.Helpers = 0; c.Touched = false; return; }
        var surface = c.Bridge.Surface;
        var body = c.Player.BodyCollider;
        if (!surface || !surface.enabled || !body || !body.enabled)
        {
            c.Touched = false;
            c.Helpers = 0;
            return;
        }

        var bank = surface.bounds;
        var player = body.bounds;
        // The entire body must clear the bridge before a bank is registered.
        int side = player.max.x < bank.min.x ? -1 : player.min.x > bank.max.x ? 1 : 0;
        int helpers = c.Bridge.HelperMask & ~(1 << (c.Player.PlayerId - 1));
        if (c.Touched)
        {
            c.Helpers &= helpers;
            if (c.Helpers == 0) c.Touched = false;
        }

        if (side == 0)
        {
            if (c.Bank != 0 && !c.Touched && helpers != 0 && body.IsTouching(surface))
            {
                c.Touched = true;
                c.Helpers = helpers;
            }
            return;
        }

        if (c.Bank != 0 && side != c.Bank && c.Touched)
        {
            // Falling below the bridge or flying past it is not a safe crossing.
            if (!c.Player.IsGrounded || player.min.y < bank.max.y - 0.25f) return;
            for (int helper = 1; helper <= 2; helper++)
                if ((c.Helpers & (1 << (helper - 1))) != 0 && Progress.AddAssist(c.Id, helper, c.Player.PlayerId))
                    Milestone?.Invoke("assisted_crossing", helper, c.Id);
        }
        c.Bank = side;
        c.Touched = false;
        c.Helpers = 0;
    }

    public bool RegisterGoal(int playerId)
    {
        if (!observing || !Progress.AddGoal(playerId)) return false;
        Milestone?.Invoke("goal_reached", playerId, "finish_star");
        return true;
    }

    public bool HasAssisted(BridgeController bridge, int playerId) =>
        Progress != null && bridge && Progress.HasAssisted(ObjectId(bridge.transform), playerId);

    public static string ObjectId(Transform target)
    {
        string id = target.name + "[" + target.GetSiblingIndex() + "]";
        while (target.parent != null)
        {
            target = target.parent;
            id = target.name + "[" + target.GetSiblingIndex() + "]/" + id;
        }
        return id;
    }
}
