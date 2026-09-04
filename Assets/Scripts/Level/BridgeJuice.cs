using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public sealed class BridgeJuice : MonoBehaviour
{
    [Header("Collider")]
    [SerializeField] private BoxCollider2D bridgeCollider;

    [Header("Timing")]
    [SerializeField] private float delayBetweenPieces = 0.05f;
    [SerializeField] private float popDuration = 0.12f;

    [Header("Juice")]
    [SerializeField] private float startYOffset = -0.25f;
    [SerializeField] private float overshootScale = 1.18f;

    private readonly List<Transform> pieces = new();
    private readonly List<Vector3> originalPositions = new();
    private readonly List<Vector3> originalScales = new();

    private Coroutine activeRoutine;
    private bool isShown;

    private void Awake()
    {
        if (bridgeCollider == null)
            bridgeCollider = GetComponent<BoxCollider2D>();

        for (int i = 0; i < transform.childCount; i++)
        {
            Transform piece = transform.GetChild(i);
            pieces.Add(piece);
            originalPositions.Add(piece.localPosition);
            originalScales.Add(piece.localScale);
            piece.gameObject.SetActive(false);
        }

        if (bridgeCollider != null)
            bridgeCollider.enabled = false;
    }

    public void ShowBridge()
    {
        if (isShown) return;
        isShown = true;

        if (activeRoutine != null) StopCoroutine(activeRoutine);
        activeRoutine = StartCoroutine(BuildBridgeRoutine());
    }

    public void HideBridge()
    {
        isShown = false; // Fix: reset flag so the bridge can be triggered again

        if (activeRoutine != null) 
            StopCoroutine(activeRoutine);

        // 1. Wake up players standing on the bridge so they fall immediately
        if (bridgeCollider != null)
        {
            ContactFilter2D filter = new ContactFilter2D();
            filter.NoFilter();
            List<Collider2D> contacts = new List<Collider2D>();

            bridgeCollider.Overlap(filter, contacts);
            foreach (var col in contacts)
            {
                var rb = col.attachedRigidbody;
                if (rb != null)
                {
                    rb.WakeUp(); // Forces Unity physics to recalculate gravity
                }
            }

            // 2. Turn off the collider
            bridgeCollider.enabled = false;
        }

        // 3. Hide visual logs
        for (int i = 0; i < pieces.Count; i++)
        {
            pieces[i].gameObject.SetActive(false);
        }
    }

    private IEnumerator BuildBridgeRoutine()
    {
        for (int i = 0; i < pieces.Count; i++)
        {
            StartCoroutine(PopPieceIn(i));
            yield return new WaitForSeconds(delayBetweenPieces);
        }

        yield return new WaitForSeconds(popDuration);

        if (bridgeCollider != null)
            bridgeCollider.enabled = true;
    }

    private IEnumerator RetractBridgeRoutine()
    {
        if (bridgeCollider != null)
            bridgeCollider.enabled = false;

        // Disappear in reverse order
        for (int i = pieces.Count - 1; i >= 0; i--)
        {
            pieces[i].gameObject.SetActive(false);
            yield return new WaitForSeconds(delayBetweenPieces * 0.5f);
        }
    }

    private IEnumerator PopPieceIn(int index)
    {
        Transform piece = pieces[index];
        Vector3 finalPos = originalPositions[index];
        Vector3 finalScale = originalScales[index];

        piece.localPosition = finalPos + Vector3.up * startYOffset;
        piece.localScale = finalScale * 0.4f;
        piece.gameObject.SetActive(true);

        float elapsed = 0f;
        while (elapsed < popDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / popDuration);
            float eased = 1f - Mathf.Pow(1f - t, 3f);

            piece.localPosition = Vector3.Lerp(finalPos + Vector3.up * startYOffset, finalPos, eased);

            float scaleMult = t < 0.7f 
                ? Mathf.Lerp(0.4f, overshootScale, t / 0.7f) 
                : Mathf.Lerp(overshootScale, 1f, (t - 0.7f) / 0.3f);

            piece.localScale = finalScale * scaleMult;
            yield return null;
        }

        piece.localPosition = finalPos;
        piece.localScale = finalScale;
    }
}