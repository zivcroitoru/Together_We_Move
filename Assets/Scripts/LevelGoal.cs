using UnityEngine;
using System.Collections.Generic;

public sealed class LevelGoal : MonoBehaviour
{
    [SerializeField] private GameManager gameManager;
    private HashSet<PlayerController> playersInGoal = new();

    private void Awake() { if (!gameManager) gameManager = FindFirstObjectByType<GameManager>(); }

    private void OnTriggerEnter2D(Collider2D other)
    {
        var p = other.GetComponentInParent<PlayerController>();
        if (p) playersInGoal.Add(p);
        
        if (playersInGoal.Count >= 2) gameManager?.CompleteLevel();
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        var p = other.GetComponentInParent<PlayerController>();
        if (p) playersInGoal.Remove(p);
    }
}