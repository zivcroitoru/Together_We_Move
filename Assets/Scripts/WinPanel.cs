using System.Collections;
using UnityEngine;
using TMPro;

[RequireComponent(typeof(CanvasGroup))]
public sealed class WinSequence : MonoBehaviour
{
    [SerializeField] private TMP_Text winText;
    [SerializeField] private float fadeDuration = 0.35f;
    [SerializeField] private float jumpHeight = 35f;
    [SerializeField] private float jumpDuration = 0.22f;
    [SerializeField] private float delayBetweenLetters = 0.08f;

    private CanvasGroup canvasGroup;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        canvasGroup.alpha = 0f;
    }

    public void PlayWin()
    {
        gameObject.SetActive(true);
        StartCoroutine(SequenceRoutine());
    }

    private IEnumerator SequenceRoutine()
    {
        // 1. Fade in the panel
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Clamp01(elapsed / fadeDuration);
            yield return null;
        }
        canvasGroup.alpha = 1f;

        // 2. Animate letters jumping one by one
        if (winText != null)
        {
            winText.ForceMeshUpdate();
            int totalChars = winText.textInfo.characterCount;

            for (int i = 0; i < totalChars; i++)
            {
                if (winText.textInfo.characterInfo[i].isVisible)
                {
                    StartCoroutine(JumpLetter(i));
                    yield return new WaitForSeconds(delayBetweenLetters);
                }
            }
        }
    }

    private IEnumerator JumpLetter(int charIndex)
    {
        float elapsed = 0f;

        while (elapsed < jumpDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / jumpDuration;
            float offsetY = Mathf.Sin(t * Mathf.PI) * jumpHeight;

            winText.ForceMeshUpdate();
            var textInfo = winText.textInfo;

            if (charIndex >= textInfo.characterCount || !textInfo.characterInfo[charIndex].isVisible)
                yield break;

            int matIndex = textInfo.characterInfo[charIndex].materialReferenceIndex;
            int vertIndex = textInfo.characterInfo[charIndex].vertexIndex;
            Vector3[] vertices = textInfo.meshInfo[matIndex].vertices;

            for (int j = 0; j < 4; j++)
            {
                vertices[vertIndex + j].y += offsetY;
            }

            winText.UpdateVertexData(TMP_VertexDataUpdateFlags.Vertices);
            yield return null;
        }

        winText.ForceMeshUpdate();
    }
}