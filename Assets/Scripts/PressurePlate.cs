using UnityEngine;

public sealed class PressurePlate : MonoBehaviour
{
    [SerializeField] private BridgeController bridgeController;

    [Header("Visual Sprites")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Sprite unpressedSprite;
    [SerializeField] private Sprite pressedSprite;

    private int playersOnTop;

    private void Awake()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        SetPressedVisual(false);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.GetComponentInParent<PlayerController>())
        {
            playersOnTop++;
            if (playersOnTop == 1)
            {
                SetPressedVisual(true);
                bridgeController?.OnButtonEnter();
            }
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.GetComponentInParent<PlayerController>())
        {
            playersOnTop = Mathf.Max(0, playersOnTop - 1);
            if (playersOnTop == 0)
            {
                SetPressedVisual(false);
                bridgeController?.OnButtonExit();
            }
        }
    }

    private void SetPressedVisual(bool isPressed)
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.sprite = isPressed ? pressedSprite : unpressedSprite;
        }
    }
}