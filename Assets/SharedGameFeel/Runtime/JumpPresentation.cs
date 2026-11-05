using UnityEngine;

namespace SharedGameFeel
{
    /// <summary>Explicit presentation inputs only. The host retains all grounding and jump rules.</summary>
    [DisallowMultipleComponent]
    public sealed class JumpPresentation : MonoBehaviour
    {
        [Range(0, .15f)] public float deformation = .10f;
        SpriteRenderer source, proxy;
        bool originalForceOff, initialized, airborne;
        float elapsed, from, amount;
        enum Phase { Idle, Takeoff, Stretch, Air, Landing, Bounce, Settle }
        Phase phase;
        public int Takeoffs { get; private set; }
        public int Landings { get; private set; }
        public float Amount => amount;
        public SpriteRenderer Visual => proxy;

        public void Initialize(SpriteRenderer artwork)
        {
            if (initialized || !artwork) return;
            source = artwork;
            originalForceOff = source.forceRenderingOff;
            var child = new GameObject("Jump rendering proxy");
            child.layer = source.gameObject.layer;
            child.transform.SetParent(source.transform, false);
            proxy = child.AddComponent<SpriteRenderer>();
            initialized = true;
            source.forceRenderingOff = true;
            CopyArtwork();
        }
        public void Takeoff()
        {
            if (!initialized || !isActiveAndEnabled) return;
            airborne = true; Takeoffs++; Begin(Phase.Takeoff);
        }
        // Call every frame with the host controller's existing ground result and velocity.
        public void Observe(bool grounded, float verticalVelocity)
        {
            if (!initialized || !isActiveAndEnabled || Time.deltaTime <= 0) return;
            grounded &= verticalVelocity <= .15f;
            if (!grounded) airborne = true;
            else if (airborne) { airborne = false; Landings++; Begin(Phase.Landing); }
            if (!grounded && (phase == Phase.Idle || phase >= Phase.Landing)) Begin(Phase.Air);
            elapsed += Time.deltaTime;
            float strength = Mathf.Clamp(deformation, 0, .15f);
            switch (phase)
            {
                case Phase.Takeoff: if (Ease(-strength, .045f)) Begin(Phase.Stretch); break;
                case Phase.Stretch: if (Ease(strength, .09f)) Begin(Phase.Air); break;
                case Phase.Air:
                    if (verticalVelocity > 0) { elapsed = 0; from = amount; }
                    else Ease(0, .18f);
                    break;
                case Phase.Landing: if (Ease(-strength, .055f)) Begin(Phase.Bounce); break;
                case Phase.Bounce: if (Ease(strength * .35f, .10f)) Begin(Phase.Settle); break;
                case Phase.Settle: if (Ease(0, .14f)) ResetPose(); break;
            }
        }
        void Begin(Phase next) { phase = next; elapsed = 0; from = amount; }
        bool Ease(float target, float duration)
        {
            float t = Mathf.Clamp01(elapsed / duration);
            amount = Mathf.Lerp(from, target, t*t*(3-2*t));
            return t >= 1;
        }
        void CopyArtwork()
        {
            if (!source || !proxy) return;
            proxy.sprite = source.sprite; proxy.color = source.color;
            proxy.flipX = source.flipX; proxy.flipY = source.flipY;
            proxy.sharedMaterial = source.sharedMaterial;
            proxy.sortingLayerID = source.sortingLayerID; proxy.sortingOrder = source.sortingOrder;
            proxy.maskInteraction = source.maskInteraction;
            proxy.drawMode = source.drawMode; proxy.size = source.size;
            proxy.enabled = source.enabled && isActiveAndEnabled;
            // Local multiplication leaves authored size, sprite proportions and Animator curves intact.
            proxy.transform.localScale = new Vector3(1-amount, 1+amount, 1);
        }
        void LateUpdate() => CopyArtwork();
        public void ResetPose()
        {
            phase = Phase.Idle; amount = from = elapsed = 0; airborne = false;
            if (proxy) proxy.transform.localScale = Vector3.one;
        }
        void OnEnable() { if (source) source.forceRenderingOff = true; }
        void OnDisable()
        {
            ResetPose(); if (source) source.forceRenderingOff = originalForceOff;
            if (proxy) proxy.enabled = false;
        }
        void OnDestroy()
        {
            if (source) source.forceRenderingOff = originalForceOff;
            if (proxy) Destroy(proxy.gameObject);
        }
    }
}
