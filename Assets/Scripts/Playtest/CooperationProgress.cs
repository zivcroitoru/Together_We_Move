using System;
using System.Collections.Generic;

/// <summary>One credit per bridge/helper slot and one per player reaching the star.
/// Repeated actions cannot farm points. No time, hint or retry penalties.</summary>
public sealed class CooperationProgress
{
    private readonly HashSet<string> bridges;
    private readonly HashSet<string> assists = new();
    private readonly HashSet<int> goals = new();

    public CooperationProgress(IEnumerable<string> bridgeIds) => bridges = new HashSet<string>(bridgeIds);
    public int TotalUnits => bridges.Count * 2 + 2;
    public int EarnedUnits => assists.Count + goals.Count;
    public int GoalCount => goals.Count;
    public float Score => 100f * EarnedUnits / TotalUnits;
    public bool IsFull => EarnedUnits == TotalUnits;
    public bool HasReachedGoal(int playerId) => goals.Contains(playerId);
    public bool HasAssisted(string bridgeId, int playerId) => assists.Contains(bridgeId + ":" + playerId);

    public bool AddAssist(string bridgeId, int helperId, int crossingPlayerId)
    {
        if (!bridges.Contains(bridgeId) || !ValidPlayer(helperId) || !ValidPlayer(crossingPlayerId)
            || helperId == crossingPlayerId) return false;
        return assists.Add(bridgeId + ":" + helperId);
    }

    public bool AddGoal(int playerId) => ValidPlayer(playerId) && goals.Add(playerId);
    public static bool ValidPlayer(int playerId) => playerId == 1 || playerId == 2;
}
