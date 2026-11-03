using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class PlayerDeathController : MonoBehaviour
{
    [Header("Death")]
    [SerializeField] private float deathY = -5f;

    private bool isDead;

    private void Update()
    {
        if (isDead)
            return;

        if (transform.position.y <= deathY)
            Die();
    }

    private void Die()
    {
        isDead = true;

        var manager = FindFirstObjectByType<GameManager>();
        if (manager != null)
        {
            manager.RestartAfterFall(GetComponent<PlayerController>()?.PlayerId ?? 0);
            return;
        }

        Debug.Log("[PLAYER] Fell out of level - restarting scene.");

        SceneManager.LoadScene(
            SceneManager.GetActiveScene().buildIndex
        );
    }
}
