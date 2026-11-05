using UnityEngine;

/// <summary>Visual-only jump juice, automatically installed by PlayerView.</summary>
[DisallowMultipleComponent]
public sealed class PlayerJumpSquash : MonoBehaviour
{
    [SerializeField, Range(.1f, .15f)] private float deformation = .12f;
    [SerializeField, Min(.01f)] private float takeoffSquashSeconds = .045f;
    [SerializeField, Min(.01f)] private float riseStretchSeconds = .09f;
    [SerializeField, Min(.01f)] private float fallRelaxSeconds = .18f;
    [SerializeField, Min(.01f)] private float landingSquashSeconds = .055f;
    [SerializeField, Min(.01f)] private float landingBounceSeconds = .10f;
    [SerializeField, Min(.01f)] private float settleSeconds = .14f;

    private enum Phase { Idle, Takeoff, Stretch, Air, Landing, Bounce, Settle }
    private PlayerController player;
    private Rigidbody2D body;
    private Vector3 originalScale;
    private Phase phase;
    private float elapsed, from, amount;
    private bool initialized, airborne;

    public void Initialize(PlayerController owner, Rigidbody2D physicsBody)
    {
        // Also protect manually attached instances from scaling physics objects.
        if (!owner || !physicsBody || transform == owner.transform ||
            !transform.IsChildOf(owner.transform) || GetComponentInChildren<Collider2D>(true) ||
            GetComponentInChildren<Rigidbody2D>(true)) return;
        if (initialized) ResetPose();
        else originalScale = transform.localScale;
        player = owner;
        body = physicsBody;
        initialized = true;
    }

    public void Takeoff()
    {
        if (!initialized || !isActiveAndEnabled) return;
        airborne = true;
        Begin(Phase.Takeoff); // Replace the current pose transition; never stack routines.
    }

    public void ResetPose()
    {
        if (!initialized) return;
        phase = Phase.Idle;
        airborne = false;
        elapsed = from = amount = 0f;
        transform.localScale = originalScale;
    }

    private void Begin(Phase next)
    {
        phase = next;
        elapsed = 0f;
        from = amount;
    }

    private bool EaseTo(float target, float seconds)
    {
        float t = Mathf.Clamp01(elapsed / Mathf.Max(.01f, seconds));
        amount = Mathf.Lerp(from, target, t * t * (3f - 2f * t));
        return t >= 1f;
    }

    private void LateUpdate()
    {
        if (!initialized) return;
        if (!player || !body || !player.isActiveAndEnabled || player.InputLocked || !body.simulated)
        {
            ResetPose();
            return;
        }
        if (Time.deltaTime <= 0f) return;

        // Positive velocity filters stale ground contacts on the first takeoff frame.
        bool grounded = body.linearVelocity.y <= .15f && player.IsGrounded;
        if (!grounded) airborne = true;
        else if (airborne)
        {
            airborne = false;
            Begin(Phase.Landing);
        }

        // Walking off an edge also gets a gentle return and a landing reaction.
        if (!grounded && (phase == Phase.Idle || phase == Phase.Landing ||
            phase == Phase.Bounce || phase == Phase.Settle)) Begin(Phase.Air);
        elapsed += Time.deltaTime;
        float strength = Mathf.Clamp(deformation, .1f, .15f);
        switch (phase)
        {
            case Phase.Takeoff:
                if (EaseTo(-strength, takeoffSquashSeconds)) Begin(Phase.Stretch);
                break;
            case Phase.Stretch:
                if (EaseTo(strength, riseStretchSeconds)) Begin(Phase.Air);
                break;
            case Phase.Air:
                if (body.linearVelocity.y > 0f) { elapsed = 0f; from = amount; }
                else EaseTo(0f, fallRelaxSeconds);
                break;
            case Phase.Landing:
                if (EaseTo(-strength, landingSquashSeconds)) Begin(Phase.Bounce);
                break;
            case Phase.Bounce:
                if (EaseTo(strength * .35f, landingBounceSeconds)) Begin(Phase.Settle);
                break;
            case Phase.Settle:
                if (EaseTo(0f, settleSeconds)) { ResetPose(); return; }
                break;
        }

        // Multiplication preserves authored size, nonuniform proportions and mirrored scales.
        transform.localScale = new Vector3(originalScale.x * (1f - amount),
            originalScale.y * (1f + amount), originalScale.z);
    }

    private void OnDisable() => ResetPose();
}
