using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

public enum PreschoolControlMode { Buttons, Dragging }

/// <summary>Separate touch inputs for both children; buttons remain the default.</summary>
public sealed class PreschoolTouchControls : MonoBehaviour
{
    [SerializeField] private PreschoolControlMode controlMode = PreschoolControlMode.Buttons;
    [SerializeField, Min(20)] private float characterTouchRadiusPixels = 90;
    [SerializeField, Min(.02f)] private float dragDeadZone = .08f;

    private struct HeldButton
    {
        public PlayerController Player;
        public float Direction;
    }

    private GameManager manager;
    private PlayerController[] players;
    private readonly Dictionary<int, PlayerController> owners = new();
    private readonly Dictionary<int, float> offsets = new();
    private readonly Dictionary<int, HeldButton> heldButtons = new();
    private readonly List<RaycastResult> uiHits = new();
    private readonly List<int> released = new();
    private static PreschoolControlMode? sessionMode;

    public PreschoolControlMode Mode => controlMode;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetMode() => sessionMode = null;

    private void OnEnable() => EnhancedTouchSupport.Enable();
    private void OnDisable() { ReleaseAll(); EnhancedTouchSupport.Disable(); }

    public void Initialize(GameManager owner, PlayerController[] actors)
    {
        manager = owner;
        players = actors;
        if (sessionMode.HasValue) controlMode = sessionMode.Value;
        foreach (var module in FindObjectsByType<InputSystemUIInputModule>(FindObjectsSortMode.None))
            module.pointerBehavior = UIPointerBehavior.AllPointersAsIs;

        ConfigureButtons();
        manager?.Log("control_mode_selected", 0, "researcher", ModeName());
        // UI positions and scaling are authored in the Unity Canvas for iPad 9th gen.
        // No runtime layout component is created here.
    }

    public string ModeName() => controlMode == PreschoolControlMode.Buttons ? "buttons" : "dragging";

    public void SetMode(PreschoolControlMode mode)
    {
        if (mode == controlMode) return;
        ReleaseAll();
        controlMode = mode;
        sessionMode = mode;
        manager?.Log("control_mode_selected", 0, "researcher", ModeName());
        manager?.GetComponent<TutorialManager>()?.RefreshControls();
        manager?.GetComponent<CooperationHints>()?.NotifyProgress();
    }

    private void ConfigureButtons()
    {
        if (players == null) return;
        foreach (var player in players)
        {
            if (!player) continue;
            foreach (var canvas in FindObjectsByType<Canvas>(FindObjectsSortMode.None))
            {
                var pad = canvas.transform.Find(player.PlayerId == 1 ? "P1_Controls" : "P2_Controls");
                if (!pad) continue;

                foreach (var name in new[] { "Btn_Left", "Btn_Right", "Btn_Jump" })
                {
                    var target = pad.Find(name);
                    if (!target) continue;
                    target.gameObject.SetActive(true); // Both players need all three buttons.

                    if (name == "Btn_Jump")
                    {
                        // Replace the old (possibly wrong-player) serialized click callback
                        // at runtime, without modifying the prefab or scene asset.
                        var jumpButton = target.GetComponent<Button>();
                        if (jumpButton != null)
                        {
                            jumpButton.onClick = new Button.ButtonClickedEvent();
                            var actor = player;
                            jumpButton.onClick.AddListener(actor.Jump);
                        }
                        continue;
                    }

                    // The existing hold component provides independent pointer-down/up input.
                    var legacy = target.GetComponent<EventTrigger>();
                    if (legacy) legacy.enabled = false;
                    var hold = target.GetComponent<PreschoolHoldButton>();
                    if (!hold) hold = target.gameObject.AddComponent<PreschoolHoldButton>();
                    hold.Initialize(this, player, name == "Btn_Left" ? -1f : 1f);
                }
            }
        }
    }

    public void HoldButton(PlayerController player, int pointerId, float direction, bool pressed)
    {
        if (!player) return;
        // Every pointer remembers its own player. Opposite players never share movement.
        if (pressed && !player.InputLocked)
        {
            heldButtons[pointerId] = new HeldButton { Player = player, Direction = direction };
        }
        else if (heldButtons.TryGetValue(pointerId, out var existing) && existing.Player == player)
        {
            heldButtons.Remove(pointerId);
        }

        float combined = 0f;
        foreach (var held in heldButtons.Values)
            if (held.Player == player) combined += held.Direction;
        player.SetTouchMove(Mathf.Clamp(combined, -1f, 1f));
    }

    private bool OverUI(Vector2 screen)
    {
        if (!EventSystem.current) return false;
        uiHits.Clear();
        EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current) { position = screen }, uiHits);
        foreach (var hit in uiHits)
            if (hit.module is GraphicRaycaster) return true;
        return false;
    }

    private Vector2 World(Vector2 screen, PlayerController actor)
    {
        var camera = Camera.main;
        return camera
            ? camera.ScreenToWorldPoint(new Vector3(screen.x, screen.y,
                Mathf.Abs(camera.transform.position.z - actor.transform.position.z)))
            : Vector2.zero;
    }

    // Dragging is retained only as an optional researcher-controlled experiment.
    public void RoutePointer(int id, Vector2 screen, UnityEngine.InputSystem.TouchPhase phase)
    {
        if (phase == UnityEngine.InputSystem.TouchPhase.Ended ||
            phase == UnityEngine.InputSystem.TouchPhase.Canceled)
        {
            if (owners.TryGetValue(id, out var previous) && previous)
                previous.SetDragMove(0f, false);
            owners.Remove(id);
            offsets.Remove(id);
            return;
        }
        if (controlMode != PreschoolControlMode.Dragging || players == null || !manager ||
            manager.LevelCompleted || manager.IsTransitioning) return;

        if (phase == UnityEngine.InputSystem.TouchPhase.Began)
        {
            if (OverUI(screen) || !Camera.main) return;
            PlayerController chosen = null;
            float closest = float.PositiveInfinity;
            foreach (var actor in players)
            {
                if (!actor || actor.InputLocked || owners.ContainsValue(actor)) continue;
                var point = Camera.main.WorldToScreenPoint(actor.BodyCollider.bounds.center);
                if (point.z <= 0f) continue;
                float distance = Vector2.Distance(screen, point);
                if (distance <= characterTouchRadiusPixels && distance < closest)
                {
                    chosen = actor;
                    closest = distance;
                }
            }
            if (!chosen) return;
            owners[id] = chosen;
            offsets[id] = World(screen, chosen).x - chosen.transform.position.x;
        }

        if (!owners.TryGetValue(id, out var player) || !player) return;
        if (player.InputLocked) { player.SetDragMove(0f, false); return; }
        if (OverUI(screen)) { player.SetDragMove(0f, true); return; }

        float delta = World(screen, player).x - offsets[id] - player.transform.position.x;
        player.SetDragMove(Mathf.Abs(delta) <= dragDeadZone
            ? 0f : Mathf.Clamp(delta / Mathf.Max(.1f, dragDeadZone * 3f), -1f, 1f), true);
    }

    private void Update()
    {
        if (players == null) return;
        foreach (var touch in Touch.activeTouches)
            RoutePointer(touch.touchId, touch.screenPosition, touch.phase);

        released.Clear();
        foreach (var pair in owners)
        {
            bool present = false;
            foreach (var touch in Touch.activeTouches)
                if (touch.touchId == pair.Key) { present = true; break; }
            if (!present && pair.Key != -100) released.Add(pair.Key);
        }
        foreach (int id in released)
            RoutePointer(id, Vector2.zero, UnityEngine.InputSystem.TouchPhase.Canceled);

        var mouse = Mouse.current;
        if (mouse != null && Touch.activeTouches.Count == 0)
        {
            if (mouse.leftButton.wasPressedThisFrame)
                RoutePointer(-100, mouse.position.ReadValue(), UnityEngine.InputSystem.TouchPhase.Began);
            else if (mouse.leftButton.wasReleasedThisFrame)
                RoutePointer(-100, mouse.position.ReadValue(), UnityEngine.InputSystem.TouchPhase.Ended);
            else if (mouse.leftButton.isPressed)
                RoutePointer(-100, mouse.position.ReadValue(), UnityEngine.InputSystem.TouchPhase.Moved);
        }
    }

    public void ReleaseAll()
    {
        owners.Clear();
        offsets.Clear();
        heldButtons.Clear();
        if (players != null)
            foreach (var player in players) if (player) player.ClearTouchInput();
    }

    private void OnApplicationFocus(bool focused) { if (!focused) ReleaseAll(); }
    private void OnApplicationPause(bool paused) { if (paused) ReleaseAll(); }
}
