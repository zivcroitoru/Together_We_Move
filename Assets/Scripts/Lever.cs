using UnityEngine;

public sealed class Lever : MonoBehaviour
{
    [SerializeField] private BridgeController bridgeController;

    [Header("Visual Sprites")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Sprite unflippedSprite;
    [SerializeField] private Sprite flippedSprite;

    private bool isFlipped;

    private void Awake()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        if (spriteRenderer != null && unflippedSprite != null)
            spriteRenderer.sprite = unflippedSprite;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isFlipped) return;

        if (other.GetComponentInParent<PlayerController>())
        {
            isFlipped = true;

            if (spriteRenderer != null && flippedSprite != null)
                spriteRenderer.sprite = flippedSprite;

            bridgeController?.PullLever();
        }
    }
}