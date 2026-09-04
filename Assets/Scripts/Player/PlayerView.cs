using UnityEngine;

public sealed class PlayerView : MonoBehaviour
{
    [SerializeField] private SpriteRenderer spriteRenderer; //[cite: 1]
    [SerializeField] private Animator animator; // Added Animator reference

    // Hash for optimized animator parameter lookups
    private static readonly int IsWalkingHash = Animator.StringToHash("IsWalking");

    public void SetFacingDirection(int direction) //[cite: 1]
    {
        if (spriteRenderer == null) //[cite: 1]
            return; //[cite: 1]

        spriteRenderer.flipX = direction < 0; //[cite: 1]
    }

    // Call this from PlayerController
    public void SetWalking(bool isWalking)
    {
        if (animator != null)
            animator.SetBool(IsWalkingHash, isWalking);
    }

    // Ready for configurable skins later.
    public void SetSkin(Sprite sprite) //[cite: 1]
    {
        if (spriteRenderer != null) //[cite: 1]
            spriteRenderer.sprite = sprite; //[cite: 1]
    }
}