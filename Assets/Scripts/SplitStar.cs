using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public sealed class SplitStarGoal : MonoBehaviour
{
    [Header("Star Halves (SpriteRenderers)")]
    [SerializeField] private SpriteRenderer leftHalf;
    [SerializeField] private SpriteRenderer rightHalf;

    [Header("Base (Dim Outline)")]
    [SerializeField] private SpriteRenderer baseTransparent;

    [Header("Game Flow")]
    [SerializeField] private GameManager gameManager;

    private bool p1Registered;
    private bool p2Registered;
    private bool completed;

    private void Awake()
    {
        if (gameManager == null)
            gameManager = FindFirstObjectByType<GameManager>();

        InitHalf(leftHalf);
        InitHalf(rightHalf);
    }

    private void InitHalf(SpriteRenderer sr)
    {
        if (sr == null) return;
        Color c = sr.color;
        c.a = 0f;
        sr.color = c;
        sr.gameObject.SetActive(false);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (completed) return;

        PlayerController player = other.GetComponentInParent<PlayerController>();
        if (!player) return;

        bool isP2 = IsPlayerTwo(player);

        if (isP2 && !p2Registered)
        {
            p2Registered = true;
            StartCoroutine(JuiceInHalf(rightHalf));
        }
        else if (!isP2 && !p1Registered)
        {
            p1Registered = true;
            StartCoroutine(JuiceInHalf(leftHalf));
        }

        CheckWin();
    }

    private void CheckWin()
    {
        if (p1Registered && p2Registered && !completed)
        {
            completed = true;
            StartCoroutine(WinSequence());
        }
    }

    private IEnumerator JuiceInHalf(SpriteRenderer sr)
    {
        if (sr == null) yield break;

        sr.gameObject.SetActive(true);
        Transform pieceTransform = sr.transform;
        Transform parentTransform = transform;

        Vector3 baseScale = Vector3.one;
        Vector3 parentBaseScale = parentTransform.localScale;

        float elapsed = 0f;
        float dur = 0.2f;

        while (elapsed < dur)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / dur);
            float bounce = Mathf.Sin(progress * Mathf.PI);

            // Punch the side piece scale
            pieceTransform.localScale = baseScale * (1f + bounce * 0.28f);

            // Punch the whole star (including the transparent base)
            parentTransform.localScale = parentBaseScale * (1f + bounce * 0.12f);

            // Fade in alpha
            Color c = sr.color;
            c.a = Mathf.Lerp(0f, 1f, progress);
            sr.color = c;

            yield return null;
        }

        pieceTransform.localScale = baseScale;
        parentTransform.localScale = parentBaseScale;

        Color finalC = sr.color;
        finalC.a = 1f;
        sr.color = finalC;
    }

    private IEnumerator WinSequence()
    {
        Vector3 origScale = transform.localScale;
        float elapsed = 0f;
        float dur = 0.35f;

        while (elapsed < dur)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / dur;
            transform.localScale = origScale * (1f + Mathf.Sin(t * Mathf.PI) * 0.45f);
            yield return null;
        }

        transform.localScale = origScale;
        gameManager?.CompleteLevel();
    }

    private bool IsPlayerTwo(PlayerController player)
    {
        var field = typeof(PlayerController).GetField("isPlayerTwo",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
        return field != null && (bool)field.GetValue(player);
    }
}