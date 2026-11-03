using UnityEngine;

public sealed class LevelGoal : MonoBehaviour
{
    [SerializeField] private GameManager gameManager;

    private void Awake() { if (!gameManager) gameManager = FindFirstObjectByType<GameManager>(); }

    private void OnTriggerEnter2D(Collider2D other)
    {
        var p = other.GetComponentInParent<PlayerController>();
        // Latch each arrival, just like the split star; GameManager applies the thresholds.
        if (p) gameManager?.RegisterGoal(p.PlayerId);
    }
}
