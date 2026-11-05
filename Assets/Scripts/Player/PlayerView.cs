using UnityEngine;
using System.Collections;

public sealed class PlayerView : MonoBehaviour
{
    [SerializeField] private SpriteRenderer spriteRenderer; //[cite: 1]
    [SerializeField] private Animator animator; // Added Animator reference

    // Hash for optimized animator parameter lookups
    private static readonly int IsWalkingHash = Animator.StringToHash("IsWalking");
    private Coroutine reaction;
    private Vector3 restingPosition;
    private bool hasRestingPosition;
    private PlayerJumpSquash jumpSquash;

    public void InitializeJumpAnimation(PlayerController player, Rigidbody2D body)
    {
        // Never deform the physics root or any hierarchy containing a collider.
        if (!transform.IsChildOf(player.transform) || transform == player.transform ||
            GetComponentInChildren<Collider2D>(true) || GetComponentInChildren<Rigidbody2D>(true)) return;
        jumpSquash = GetComponent<PlayerJumpSquash>();
        if (!jumpSquash) jumpSquash = gameObject.AddComponent<PlayerJumpSquash>();
        jumpSquash.Initialize(player, body);
    }

    public void PlayJumpTakeoff() { if (jumpSquash) jumpSquash.Takeoff(); }
    public void ResetJumpAnimation() { if (jumpSquash) jumpSquash.ResetPose(); }

    public void Celebrate() => React(.35f, .12f, false);
    public void BounceRecovery(float seconds) => React(seconds, .5f, true);
    private void React(float seconds, float height, bool recovery)
    {
        // Animate artwork only, keeping the collider and sprite proportions unchanged.
        if (GetComponent<Rigidbody2D>() || !isActiveAndEnabled) return;
        if (!hasRestingPosition) { restingPosition = transform.localPosition; hasRestingPosition = true; }
        if (reaction != null) StopCoroutine(reaction);
        reaction = StartCoroutine(ReactRoutine(seconds, height, recovery));
    }
    private IEnumerator ReactRoutine(float seconds, float height, bool recovery)
    {
        float elapsed = 0f;
        while (elapsed < seconds)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / Mathf.Max(.1f,seconds));
            // A short spring-in from below makes rescue visible, followed by a small settling bounce.
            float offset = recovery && t < .45f
                ? Mathf.Lerp(-height, height, Mathf.Sin(t/.45f*Mathf.PI*.5f))
                : recovery ? Mathf.Abs(Mathf.Cos((t-.45f)/.55f*Mathf.PI*1.5f))*height*(1-t)/.55f
                : Mathf.Abs(Mathf.Sin(t*Mathf.PI*2))*height*(1-t);
            transform.localPosition = restingPosition + Vector3.up * offset;
            yield return null;
        }
        transform.localPosition = restingPosition; reaction = null;
    }
    private void OnDisable() { if (hasRestingPosition) transform.localPosition = restingPosition; reaction = null; }

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
