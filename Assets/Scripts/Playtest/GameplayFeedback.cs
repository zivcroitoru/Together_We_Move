using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Small pooled bursts, shared sound and artwork-only celebrations. Never captures input.</summary>
public sealed class GameplayFeedback : MonoBehaviour
{
    [SerializeField] private AudioClip cheerfulSound;
    [SerializeField, Range(0f,.5f)] private float volume = .10f;
    [SerializeField, Min(.15f)] private float minimumInterval = 5f;
    [SerializeField, Range(1,24)] private int cooperationPieces = 7;
    [SerializeField, Range(1,24)] private int finishPieces = 14;
    private readonly HashSet<string> seen = new();
    private readonly List<Piece> pieces = new();
    private PlayerController[] players;
    private AudioSource source;
    private AudioClip generatedSound;
    private Canvas canvas;
    private float nextBurst;
    private struct Piece { public RectTransform Rect; public Image Image; public Vector2 Velocity; public float Age; }
    private static readonly Color[] Colors = { new Color(1,.65f,.15f), new Color(.3f,.8f,1), new Color(1,.4f,.55f), new Color(.4f,.9f,.5f) };
    public int BurstsPlayed { get; private set; }
    public void Initialize(PlayerController[] actors)
    {
        players = actors;
        source = gameObject.AddComponent<AudioSource>(); source.playOnAwake = false; source.spatialBlend = 0;
        if (!cheerfulSound)
        {
            const int rate = 22050; var samples = new float[(int)(rate * .24f)];
            for (int i = 0; i < samples.Length; i++)
            {
                float t = (float)i/rate, note = t < .12f ? 660f : 880f;
                samples[i] = Mathf.Sin(2*Mathf.PI*note*t)*Mathf.Sin(Mathf.PI*t/.24f)*.3f;
            }
            generatedSound = AudioClip.Create("Together cheerful chime",samples.Length,1,rate,false); generatedSound.SetData(samples,0);
        }
        var root = new GameObject("Gameplay confetti",typeof(Canvas),typeof(CanvasScaler));
        root.transform.SetParent(transform,false); canvas = root.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 190;
        var scaler = root.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(1080,1920); scaler.matchWidthOrHeight = 0;
        for (int i=0;i<36;i++)
        {
            var obj = new GameObject("Confetti",typeof(RectTransform),typeof(Image)); obj.transform.SetParent(root.transform,false);
            var rect = obj.GetComponent<RectTransform>(); rect.sizeDelta = new Vector2(10,16);
            var image = obj.GetComponent<Image>(); image.color = Colors[i%Colors.Length]; image.raycastTarget = false; obj.SetActive(false);
            pieces.Add(new Piece{Rect=rect,Image=image,Age=1});
        }
    }
    public void Milestone(string type,int playerId,string target)
    {
        // Normal walking/participation is not a celebration. Confetti is
        // reserved for a genuine cooperative bridge crossing.
        if (type != "assisted_crossing" && type != "goal_reached") return;
        if (!seen.Add(type+":"+playerId+":"+target)) return;
        if (type == "assisted_crossing") Celebrate(playerId);
        else AnimatePlayer(playerId);
    }
    public void Discovery(string target,int playerId)
    {
        if (!seen.Add("discovery:"+target+":"+playerId)) return;
        // Pressure plates and levers get a gentle character response, NOT confetti.
        AnimatePlayer(playerId);
    }
    public void Finish()
    {
        if (!seen.Add("level_finished")) return;
        // Every finish has the same happy treatment, independent of the score.
        foreach (var player in players) if (player) player.GetComponentInChildren<PlayerView>()?.Celebrate();
        nextBurst = Time.unscaledTime + minimumInterval;
        Burst(players != null && players.Length > 0 && players[0] ? players[0].transform.position : Vector3.zero,finishPieces);
    }
    private void AnimatePlayer(int playerId)
    {
        if (players == null) return;
        foreach (var player in players)
            if (player && player.PlayerId == playerId && !player.IsRecovering)
                player.GetComponentInChildren<PlayerView>()?.Celebrate();
    }
    private void Celebrate(int playerId)
    {
        if (players == null) return;
        foreach (var player in players)
            if (player && player.PlayerId == playerId && !player.IsRecovering)
            {
                player.GetComponentInChildren<PlayerView>()?.Celebrate();
                if (Time.unscaledTime >= nextBurst)
                {
                    nextBurst = Time.unscaledTime + minimumInterval;
                    Burst(player.transform.position,cooperationPieces);
                }
            }
    }
    private void Burst(Vector3 position,int count)
    {
        if (!canvas) return;
        BurstsPlayed++;
        if (source) source.PlayOneShot(cheerfulSound ? cheerfulSound : generatedSound,volume);
        var camera = Camera.main; Vector3 viewport = camera ? camera.WorldToViewportPoint(position + Vector3.up*.5f) : new Vector3(.5f,.5f);
        var root = canvas.GetComponent<RectTransform>(); Vector2 origin = new Vector2((Mathf.Clamp(viewport.x,.08f,.92f)-.5f)*root.rect.width,(Mathf.Clamp(viewport.y,.15f,.9f)-.5f)*root.rect.height);
        int emitted = 0;
        for (int i=0;i<pieces.Count && emitted<count;i++)
        {
            var piece = pieces[i]; if (piece.Age < .65f) continue;
            piece.Age = 0; piece.Rect.anchoredPosition = origin;
            piece.Velocity = new Vector2(Random.Range(-160,160),Random.Range(160,300));
            piece.Rect.gameObject.SetActive(true); piece.Image.color = Colors[i%Colors.Length]; pieces[i]=piece; emitted++;
        }
    }
    private void Update()
    {
        for (int i=0;i<pieces.Count;i++)
        {
            var piece = pieces[i]; if (piece.Age >= .65f) continue;
            float dt = Time.unscaledDeltaTime; piece.Age += dt; piece.Velocity.y -= 650*dt;
            piece.Rect.anchoredPosition += piece.Velocity*dt; piece.Rect.Rotate(0,0,180*dt);
            var color = piece.Image.color; color.a = 1-Mathf.Clamp01(piece.Age/.65f); piece.Image.color = color;
            if (piece.Age >= .65f) piece.Rect.gameObject.SetActive(false); pieces[i]=piece;
        }
    }
    private void OnDestroy() { if (generatedSound) Destroy(generatedSound); }
}
