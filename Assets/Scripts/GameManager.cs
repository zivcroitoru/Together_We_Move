using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class GameManager : MonoBehaviour
{
    [SerializeField] private WinSequence winSequence;
    [SerializeField] private float transitionDelay = 2.5f;

    private bool levelCompleted;

    public void CompleteLevel()
    {
        if (levelCompleted) return;
        levelCompleted = true;

        if (winSequence != null)
        {
            winSequence.PlayWin();
        }

        StartCoroutine(LoadNextLevel());
    }

    private IEnumerator LoadNextLevel()
    {
        yield return new WaitForSeconds(transitionDelay);

        int next = SceneManager.GetActiveScene().buildIndex + 1;
        if (next < SceneManager.sceneCountInBuildSettings)
        {
            SceneManager.LoadScene(next);
        }
    }
}