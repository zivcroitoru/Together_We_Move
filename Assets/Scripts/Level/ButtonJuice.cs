using System.Collections;
using UnityEngine;

public sealed class ButtonJuice : MonoBehaviour
{
    [Header("Visual")]
    [SerializeField] private Transform visual;

    [Header("Block Bump")]
    [SerializeField] private float bumpHeight = 0.3f;
    [SerializeField] private float upDuration = 0.07f;
    [SerializeField] private float downDuration = 0.12f;

    [Header("Squash & Stretch")]
    [SerializeField] private float squashX = 1.15f;
    [SerializeField] private float squashY = 0.85f;
    [SerializeField] private float stretchX = 0.95f;
    [SerializeField] private float stretchY = 1.10f;

    [Header("Wobble")]
    [SerializeField] private float wobbleAngle = 4f;

    private Vector3 originalPosition;
    private Vector3 originalScale;
    private Quaternion originalRotation;

    private Coroutine juiceRoutine;

    private void Awake()
    {
        if (visual == null)
            visual = transform;

        originalPosition = visual.localPosition;
        originalScale = visual.localScale;
        originalRotation = visual.localRotation;
    }

    public void Play()
    {
        if (juiceRoutine != null)
            StopCoroutine(juiceRoutine);

        ResetVisual();

        juiceRoutine = StartCoroutine(PlayJuice());
    }

    private IEnumerator PlayJuice()
    {
        // -------------------------
        // 1. BUMP UP
        // -------------------------

        Vector3 topPosition =
            originalPosition + Vector3.up * bumpHeight;

        Vector3 squashScale = new Vector3(
            originalScale.x * squashX,
            originalScale.y * squashY,
            originalScale.z
        );

        yield return Animate(
            originalPosition,
            topPosition,
            originalScale,
            squashScale,
            0f,
            wobbleAngle,
            upDuration
        );

        // -------------------------
        // 2. COME BACK DOWN
        // -------------------------

        Vector3 stretchScale = new Vector3(
            originalScale.x * stretchX,
            originalScale.y * stretchY,
            originalScale.z
        );

        yield return Animate(
            topPosition,
            originalPosition,
            squashScale,
            stretchScale,
            wobbleAngle,
            -wobbleAngle,
            downDuration
        );

        // -------------------------
        // 3. QUICK SETTLE
        // -------------------------

        yield return Animate(
            originalPosition,
            originalPosition,
            stretchScale,
            originalScale,
            -wobbleAngle,
            0f,
            0.07f
        );

        ResetVisual();
        juiceRoutine = null;
    }

    private IEnumerator Animate(
        Vector3 startPosition,
        Vector3 targetPosition,
        Vector3 startScale,
        Vector3 targetScale,
        float startAngle,
        float targetAngle,
        float duration)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            float t = Mathf.Clamp01(elapsed / duration);

            // Cartoony ease-out.
            float eased =
                1f - Mathf.Pow(1f - t, 3f);

            visual.localPosition = Vector3.Lerp(
                startPosition,
                targetPosition,
                eased
            );

            visual.localScale = Vector3.Lerp(
                startScale,
                targetScale,
                eased
            );

            float angle = Mathf.Lerp(
                startAngle,
                targetAngle,
                eased
            );

            visual.localRotation =
                originalRotation *
                Quaternion.Euler(0f, 0f, angle);

            yield return null;
        }
    }

    private void ResetVisual()
    {
        visual.localPosition = originalPosition;
        visual.localScale = originalScale;
        visual.localRotation = originalRotation;
    }
}