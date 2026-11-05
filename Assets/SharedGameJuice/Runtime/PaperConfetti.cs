using UnityEngine;
using UnityEngine.UI;

namespace SharedGameJuice
{
    /// <summary>One finite paper burst, pooled on a non-interactive overlay. Flutter is purely visual.</summary>
    public sealed class PaperConfetti : MonoBehaviour
    {
        public Sprite paper;
        const int Capacity = 48;
        struct Piece { public RectTransform rect; public Image image; public Vector2 velocity; public float age, spin, phase, life; }
        Piece[] pieces;
        Canvas canvas;
        float nextBurst;
        public int BurstsPlayed { get; private set; }
        static readonly Color[] Palette = { new Color(1,.40f,.48f), new Color(.22f,.72f,1), new Color(1,.77f,.17f), new Color(.32f,.83f,.51f), new Color(.73f,.47f,1) };
        void Prepare()
        {
            if (pieces != null) return;
            if (!paper) paper = Resources.Load<Sprite>("SharedGameJuice/Paper");
            var root = new GameObject("Paper confetti overlay", typeof(Canvas), typeof(CanvasScaler));
            root.transform.SetParent(transform, false);
            canvas = root.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 190;
            var scaler = root.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(2048,1536); scaler.matchWidthOrHeight = .5f;
            pieces = new Piece[Capacity];
            for (int i = 0; i < Capacity; i++)
            {
                var go = new GameObject("Paper", typeof(RectTransform), typeof(Image)); go.transform.SetParent(root.transform, false);
                var image = go.GetComponent<Image>(); image.sprite = paper; image.raycastTarget = false;
                var rect = go.GetComponent<RectTransform>(); rect.sizeDelta = new Vector2(13,21);
                pieces[i] = new Piece { rect = rect, image = image, age = 10, life = 1.4f }; go.SetActive(false);
            }
        }
        public bool BurstWorld(Vector3 position, int count = 36, bool victory = false)
        {
            Prepare();
            if (!victory && Time.unscaledTime < nextBurst) return false;
            nextBurst = Time.unscaledTime + 1.5f; BurstsPlayed++;
            Canvas.ForceUpdateCanvases();
            var camera = Camera.main;
            Vector3 viewport = camera ? camera.WorldToViewportPoint(position + Vector3.up * .5f) : new Vector3(.5f,.5f);
            var area = canvas.GetComponent<RectTransform>().rect;
            var origin = new Vector2((Mathf.Clamp(viewport.x,.12f,.88f)-.5f)*area.width, (Mathf.Clamp(viewport.y,.18f,.8f)-.5f)*area.height);
            int emitted = 0;
            for (int i = 0; i < Capacity && emitted < Mathf.Clamp(count,1,Capacity); i++)
            {
                var p = pieces[i]; if (p.age < p.life) continue;
                p.age = 0; p.life = Random.Range(1.1f,1.55f); p.phase = Random.value * 6.28f; p.spin = Random.Range(-220f,220f);
                p.velocity = new Vector2(Random.Range(-240f,240f), Random.Range(280f,470f));
                p.rect.anchoredPosition = origin; p.rect.localRotation = Quaternion.Euler(0,0,Random.Range(0,360));
                p.rect.localScale = Vector3.one; p.image.color = Palette[i % Palette.Length]; p.rect.gameObject.SetActive(true);
                pieces[i] = p; emitted++;
            }
            return true;
        }
        void Update()
        {
            if (pieces == null) return;
            float dt = Mathf.Min(Time.unscaledDeltaTime,.05f);
            for (int i = 0; i < pieces.Length; i++)
            {
                var p = pieces[i]; if (p.age >= p.life) continue;
                p.age += dt; p.velocity.y -= 540 * dt; p.velocity.x *= 1 - dt * .5f;
                p.rect.anchoredPosition += (p.velocity + Vector2.right * Mathf.Sin(p.age*12+p.phase)*28) * dt;
                p.rect.Rotate(0,0,p.spin*dt); p.rect.localScale = new Vector3(.2f+.8f*Mathf.Abs(Mathf.Cos(p.age*9+p.phase)),1,1);
                var c = p.image.color; c.a = 1 - Mathf.InverseLerp(p.life*.72f,p.life,p.age); p.image.color = c;
                if (p.age >= p.life) p.rect.gameObject.SetActive(false);
                pieces[i] = p;
            }
        }
        void OnDisable()
        {
            if (pieces == null) return;
            for (int i=0;i<pieces.Length;i++) { var p=pieces[i]; p.age=p.life; p.rect.gameObject.SetActive(false); pieces[i]=p; }
        }
    }
}
