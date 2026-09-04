using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class GameManager : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject winScreen;

    [Header("Level")]
    [SerializeField] private float nextLevelDelay = 1.5f;

    private bool levelCompleted;

    public void CompleteLevel(PlayerController player)
    {
        if (levelCompleted)
            return;

        levelCompleted = true;

        // Stop player movement.
        player.enabled = false;

        Rigidbody2D rb = player.GetComponent<Rigidbody2D>();

        if (rb != null)
            rb.linearVelocity = Vector2.zero;

        // Show win screen.
        if (winScreen != null)
            winScreen.SetActive(true);

        StartCoroutine(LoadNextLevel());
    }

    private IEnumerator LoadNextLevel()
    {
        yield return new WaitForSeconds(nextLevelDelay);

        int nextSceneIndex =
            SceneManager.GetActiveScene().buildIndex + 1;

        if (nextSceneIndex < SceneManager.sceneCountInBuildSettings)
            SceneManager.LoadScene(nextSceneIndex);
    }
}