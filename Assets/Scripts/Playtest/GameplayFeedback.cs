using System.Collections.Generic;
using UnityEngine;
using SharedGameJuice;

/// <summary>Small pooled bursts, shared sound and artwork-only celebrations. Never captures input.</summary>
public sealed class GameplayFeedback : MonoBehaviour
{
    [SerializeField, Min(.15f)] private float minimumInterval = 5f;
    [SerializeField, Range(1,24)] private int cooperationPieces = 7;
    [SerializeField, Range(1,48)] private int finishPieces = 36;
    private readonly HashSet<string> seen = new();
    private PlayerController[] players;
    private PaperConfetti confetti;
    private float nextBurst;
    public int BurstsPlayed { get; private set; }
    public void Initialize(PlayerController[] actors)
    {
        players = actors;
        if (!confetti) confetti = gameObject.AddComponent<PaperConfetti>();
    }
    public void Milestone(string type,int playerId,string target)
    {
        // Normal walking/participation is not a celebration. Confetti is
        // reserved for a genuine cooperative bridge crossing.
        if (type != "assisted_crossing" && type != "goal_reached") return;
        if (!seen.Add(type+":"+playerId+":"+target)) return;
        if (type == "assisted_crossing") Celebrate(playerId);
        else AnimatePlayer(playerId);
    }
    public void Discovery(string target,int playerId)
    {
        if (!seen.Add("discovery:"+target+":"+playerId)) return;
        // Pressure plates and levers get a gentle character response, NOT confetti.
        AnimatePlayer(playerId);
    }
    public void Finish()
    {
        if (!seen.Add("level_finished")) return;
        // Every finish has the same happy treatment, independent of the score.
        foreach (var player in players) if (player) player.GetComponentInChildren<PlayerView>()?.Celebrate();
        nextBurst = Time.unscaledTime + minimumInterval;
        Burst(players != null && players.Length > 0 && players[0] ? players[0].transform.position : Vector3.zero,finishPieces);
    }
    private void AnimatePlayer(int playerId)
    {
        if (players == null) return;
        foreach (var player in players)
            if (player && player.PlayerId == playerId && !player.IsRecovering)
                player.GetComponentInChildren<PlayerView>()?.Celebrate();
    }
    private void Celebrate(int playerId)
    {
        if (players == null) return;
        foreach (var player in players)
            if (player && player.PlayerId == playerId && !player.IsRecovering)
            {
                player.GetComponentInChildren<PlayerView>()?.Celebrate();
                if (Time.unscaledTime >= nextBurst)
                {
                    nextBurst = Time.unscaledTime + minimumInterval;
                    Burst(player.transform.position,cooperationPieces);
                }
            }
    }
    private void Burst(Vector3 position,int count)
    {
        if (!confetti) return;
        bool victory = seen.Contains("level_finished");
        if (!confetti.BurstWorld(position, victory ? Mathf.Max(28,count) : count, victory)) return;
        BurstsPlayed++;
        SharedGameAudio.AudioManager.PlaySound(SharedGameAudio.SoundId.Confetti);
    }
}
