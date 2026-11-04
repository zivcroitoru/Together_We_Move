using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Retains the serialized component and threshold; falls never reload the scene.</summary>
[RequireComponent(typeof(PlayerController))]
public sealed class PlayerDeathController : MonoBehaviour
{
    [SerializeField] private float deathY = -5f;
    [SerializeField, Min(.15f)] private float bounceSeconds = .55f;
    [SerializeField, Min(0f)] private float safeDwellSeconds = .25f;
    [SerializeField, Min(.5f)] private float maximumFallSeconds = 3f;
    private PlayerController player;
    private Rigidbody2D body;
    private Collider2D shape;
    private GameManager manager;
    private Vector2 spawn;
    private Vector2 footprintSize,footprintOffset;
    private readonly List<Vector2> safePositions = new();
    private readonly RaycastHit2D[] hits = new RaycastHit2D[16];
    private readonly Collider2D[] overlaps = new Collider2D[16];
    private float groundedSince = -1f, airborneSince = -1f, armedAt;
    private bool emergencySupport;
    private GameObject rescuePlatform;
    public bool IsRecovering => player && player.IsRecovering;
    public Vector2 LastSafePosition => safePositions.Count > 0 ? safePositions[safePositions.Count - 1] : spawn;

    private void Awake()
    {
        player = GetComponent<PlayerController>(); body = GetComponent<Rigidbody2D>(); shape = GetComponent<Collider2D>();
        spawn = body.position;
        footprintSize = shape.bounds.size; footprintOffset = (Vector2)shape.bounds.center - body.position;
    }
    private void Start() => manager = FindFirstObjectByType<GameManager>();
    private void FixedUpdate()
    {
        if (player.InputLocked || (manager && (manager.LevelCompleted || manager.IsTransitioning))) return;
        if (body.position.y <= deathY || (!player.IsGrounded && airborneSince >= 0f && body.linearVelocity.y < 0f && Time.time - airborneSince >= maximumFallSeconds))
        { TryRecover(); return; }
        if (!player.IsGrounded || Mathf.Abs(body.linearVelocity.y) > .15f)
        {
            groundedSince = -1f;
            if (airborneSince < 0f) airborneSince = Time.time;
            return;
        }
        airborneSince = -1f;
        if (groundedSince < 0f) groundedSince = Time.time;
        if (Time.time - groundedSince < safeDwellSeconds || !IsSafe(body.position)) return;
        if (safePositions.Count == 0 || Vector2.Distance(LastSafePosition, body.position) > .35f)
        {
            safePositions.Add(body.position);
            if (safePositions.Count > 24) safePositions.RemoveAt(0);
        }
    }
    public bool TryRecover()
    {
        if (player.InputLocked || Time.time < armedAt || (manager && (manager.LevelCompleted || manager.IsTransitioning))) return false;
        footprintSize = shape.bounds.size; footprintOffset = (Vector2)shape.bounds.center - body.position;
        player.SetRecovering(true);
        manager?.RecordFall(player.PlayerId);
        manager?.Tracker?.ResetCrossings(player.PlayerId);
        StartCoroutine(Recover());
        return true;
    }
    private ContactFilter2D GroundFilter()
    {
        var filter = new ContactFilter2D(); filter.SetLayerMask(player.GroundLayer); filter.useTriggers = false; return filter;
    }
    private bool IsSafe(Vector2 position)
    {
        if (position.y <= deathY + 1f) return false;
        Vector2 center = position + footprintOffset;
        Vector2 size = footprintSize;
        int count = Physics2D.OverlapBox(center, size * .9f, 0f, GroundFilter(), overlaps);
        for (int i = 0; i < count; i++) if (overlaps[i] != shape) return false;
        // Both feet need currently enabled support. An edge or vanished bridge is not a checkpoint.
        for (int foot = -1; foot <= 1; foot += 2)
        {
            Vector2 origin = center + new Vector2(foot * size.x * .35f, -size.y * .5f + .08f);
            count = Physics2D.Raycast(origin, Vector2.down, GroundFilter(), hits, .3f);
            bool supported = false;
            for (int i = 0; i < count; i++)
                if (hits[i].collider != shape && hits[i].normal.y > .65f &&
                    (!hits[i].rigidbody || hits[i].rigidbody.bodyType == RigidbodyType2D.Static)) supported = true;
            if (!supported) return false;
        }
        return true;
    }
    private Vector2 SelectSafePosition()
    {
        emergencySupport = false;
        for (int i = safePositions.Count - 1; i >= 0; i--)
            if (IsSafe(safePositions[i])) return safePositions[i];
        if (IsSafe(spawn)) return spawn;
        // If all original support has disappeared, a small persistent trampoline breaks fall loops.
        // This system assistance never earns crossing or objective credit.
        emergencySupport = true;
        Vector2 target = new Vector2(spawn.x, Mathf.Max(spawn.y, deathY + 2f));
        if (!rescuePlatform)
        {
            rescuePlatform = new GameObject("Recovery trampoline P" + player.PlayerId);
            var box = rescuePlatform.AddComponent<BoxCollider2D>(); box.size = new Vector2(Mathf.Max(1.5f, footprintSize.x * 2f), .16f);
            int mask = player.GroundLayer.value;
            for (int layer = 0; layer < 32; layer++) if ((mask & (1 << layer)) != 0) { rescuePlatform.layer = layer; break; }
            var visual = rescuePlatform.AddComponent<LineRenderer>(); visual.useWorldSpace = false; visual.positionCount = 3;
            visual.SetPositions(new[]{new Vector3(-.7f,.02f),new Vector3(0,-.04f),new Vector3(.7f,.02f)});
            visual.startWidth = visual.endWidth = .1f; visual.startColor = visual.endColor = new Color(.3f,.9f,.55f);
            var shader = Shader.Find("Sprites/Default"); if (shader) visual.sharedMaterial = new Material(shader);
            visual.sortingOrder = 20;
        }
        // Look upward only, preserving A's fixed horizontal column even in malformed geometry.
        for (int i=0;i<32;i++)
        {
            int count=Physics2D.OverlapBox(target+footprintOffset,footprintSize*.95f,0,GroundFilter(),overlaps);
            bool blocked=false;for(int j=0;j<count;j++)if(overlaps[j]!=shape&&overlaps[j].gameObject!=rescuePlatform)blocked=true;
            if(!blocked)break;target.y+=.5f;
        }
        rescuePlatform.transform.position = target + footprintOffset + Vector2.down * (footprintSize.y * .5f + .08f);
        return target;
    }
    private IEnumerator Recover()
    {
        Vector2 target = SelectSafePosition();
        body.position = target; transform.position = new Vector3(target.x,target.y,transform.position.z);
        Physics2D.SyncTransforms();
        // Animate the artwork at the checked position; B gains no physical jump and A no lateral motion.
        var view = GetComponentInChildren<PlayerView>();
        if (view) view.BounceRecovery(bounceSeconds);
        float end = Time.unscaledTime + Mathf.Max(.15f, bounceSeconds);
        while (Time.unscaledTime < end) yield return null;
        if (!IsSafe(target)) { target = SelectSafePosition(); body.position = target; transform.position = new Vector3(target.x,target.y,transform.position.z); }
        player.SetRecovering(false);
        armedAt = Time.time + .75f; airborneSince = groundedSince = -1f;
        manager?.RecordRecovery(player.PlayerId, emergencySupport);
    }
    private void OnDestroy()
    {
        if (!rescuePlatform) return;
        var line = rescuePlatform.GetComponent<LineRenderer>(); if (line && line.sharedMaterial) Destroy(line.sharedMaterial);
        Destroy(rescuePlatform);
    }
}
