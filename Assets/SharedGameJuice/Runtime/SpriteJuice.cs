using UnityEngine;

namespace SharedGameJuice
{
    /// <summary>Animates a rendering proxy only; never scales a collider or rigidbody.</summary>
    public sealed class SpriteJuice : MonoBehaviour
    {
        public SpriteRenderer source;
        public Color accent = new Color(1,.77f,.2f);
        public bool idleGlow;
        public bool useProxy = true;
        SpriteRenderer proxy, halo;
        float pressedAt = -10;
        bool previousForceOff;
        public int Activations { get; private set; }
        public void Initialize(SpriteRenderer renderer, Color color, bool glow = false, bool proxyVisual = true)
        {
            source = renderer; accent = color; idleGlow = glow; useProxy = proxyVisual;
            if (!source || halo) return;
            previousForceOff = source.forceRenderingOff;
            if (useProxy) { proxy = Child("Juice visual",source.transform); source.forceRenderingOff = true; }
            halo = Child("Gentle halo",source.transform);
            halo.sprite = Resources.Load<Sprite>("SharedGameJuice/Glow");
            halo.transform.localScale = Vector3.one * 1.5f;
        }
        static SpriteRenderer Child(string name, Transform parent)
        { var go = new GameObject(name); go.transform.SetParent(parent,false); return go.AddComponent<SpriteRenderer>(); }
        void Start() { if (source && !halo) Initialize(source,accent,idleGlow,useProxy); }
        public void Press()
        {
            if (Time.unscaledTime - pressedAt < .15f) return;
            pressedAt = Time.unscaledTime; Activations++;
            if (source) JuiceBurst.At(source.bounds.center,accent,JuiceBurst.Style.Switch,10);
        }
        void LateUpdate()
        {
            if (!source || !halo) return;
            float age = Time.unscaledTime - pressedAt;
            float punch = age < .3f ? Mathf.Sin(Mathf.Clamp01(age/.3f)*Mathf.PI) : 0;
            if (proxy)
            {
                proxy.sprite = source.sprite; proxy.color = source.color; proxy.flipX = source.flipX; proxy.flipY = source.flipY;
                proxy.sortingLayerID = source.sortingLayerID; proxy.sortingOrder = source.sortingOrder;
                proxy.enabled = source.enabled; proxy.sharedMaterial = source.sharedMaterial;
                proxy.transform.localScale = new Vector3(1 + punch*.12f,1 - punch*.13f,1);
            }
            halo.enabled = source.enabled && (idleGlow || punch > 0);
            halo.sortingLayerID = source.sortingLayerID; halo.sortingOrder = source.sortingOrder - 1;
            float glow = idleGlow ? .10f + .035f*Mathf.Sin(Time.unscaledTime*2.2f) : 0;
            halo.color = new Color(accent.r,accent.g,accent.b,glow + punch*.18f);
            // Size the halo from the artwork, rather than assuming a sprite pixel density.
            float extent = source.sprite ? Mathf.Max(source.sprite.bounds.size.x,source.sprite.bounds.size.y) : 1;
            float haloExtent = halo.sprite ? Mathf.Max(.01f,halo.sprite.bounds.size.x) : 1;
            halo.transform.localPosition = source.sprite ? source.sprite.bounds.center : Vector3.zero;
            halo.transform.localScale = Vector3.one * (extent/haloExtent) * (1.5f + .06f*Mathf.Sin(Time.unscaledTime*2.2f));
        }
        void OnDisable()
        {
            if (source && useProxy) source.forceRenderingOff = previousForceOff;
            if (proxy) { proxy.enabled=false; proxy.transform.localScale=Vector3.one; }
            if (halo) halo.enabled=false;
        }
        void OnEnable(){if(source&&proxy&&useProxy)source.forceRenderingOff=true;}
        void OnDestroy() { if (source && useProxy) source.forceRenderingOff = previousForceOff; if (proxy) Destroy(proxy.gameObject); if (halo) Destroy(halo.gameObject); }
    }
}
