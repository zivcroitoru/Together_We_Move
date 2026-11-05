using UnityEngine;

namespace SharedGameJuice
{
    /// <summary>Bounded, silent, non-looping Particle System pool. No gameplay dependencies.</summary>
    [RequireComponent(typeof(ParticleSystem))]
    public sealed class JuiceBurst : MonoBehaviour
    {
        public enum Style { Switch, Sparkle, Bridge }
        static JuiceBurst[] pool;
        ParticleSystem particles;
        float availableAt;
        public static int Played { get; private set; }
        public static int Capacity => 8;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetPool() { pool = null; Played = 0; }
        void Awake() { particles = GetComponent<ParticleSystem>(); }
        public void Play(Color color, int count = 9)
        {
            if (!particles) particles = GetComponent<ParticleSystem>();
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = particles.main; main.startColor = color;
            particles.Play(false);
            particles.Emit(Mathf.Clamp(count, 1, 16));
            availableAt = Time.unscaledTime + .8f;
            Played++;
        }
        public static void At(Vector3 position, Color color, Style style = Style.Sparkle, int count = 9)
        {
            if (pool == null) pool = new JuiceBurst[Capacity];
            for (int i = 0; i < pool.Length; i++)
            {
                if (!pool[i])
                {
                    var prefab = Resources.Load<JuiceBurst>("SharedGameJuice/SparkleBurst");
                    if (!prefab) return;
                    pool[i] = Instantiate(prefab);
                }
                var burst = pool[i];
                if (Time.unscaledTime < burst.availableAt) continue;
                burst.transform.position = position;
                burst.transform.localScale = Vector3.one * (style == Style.Bridge ? .8f : 1f);
                burst.Play(color, count);
                return;
            }
        }
    }
}
