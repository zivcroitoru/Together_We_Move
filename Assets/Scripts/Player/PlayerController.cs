using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D)), RequireComponent(typeof(Collider2D))]
public sealed class PlayerController : MonoBehaviour
{
    [SerializeField] private PlayerView view;
    [SerializeField] private float moveSpeed = 4f, jumpForce = 7f;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private bool isPlayerTwo;

    private Rigidbody2D rb;
    private Collider2D col;
    private float moveInput;
    private float touchMoveInput;
    private float dragMoveInput;
    private float lastAppliedMove;
    private float previousPhysicsX;
    private float jumpMoveLockUntil;
    private bool inputLocked;
    private bool recovering;
    private bool dragging;
    private readonly ContactPoint2D[] groundContacts = new ContactPoint2D[16];

    // Player ID determines the control pad and keyboard bindings, NOT abilities.
    public int PlayerId => isPlayerTwo ? 2 : 1;
    public Collider2D BodyCollider => col;
    public bool CanMoveHorizontally => true;
    public bool CanJump => true;
    public bool InputLocked => inputLocked || recovering;
    public bool IsRecovering => recovering;
    public LayerMask GroundLayer => groundLayer;
    public bool IsGrounded
    {
        get
        {
            if (col == null || rb == null || !rb.simulated) return false;
            var filter = new ContactFilter2D();
            filter.SetLayerMask(groundLayer);
            filter.useTriggers = false;
            int count = col.GetContacts(filter, groundContacts);
            for (int i = 0; i < count; i++)
                if (groundContacts[i].normal.y > .65f) return true;
            return false;
        }
    }

    public int SuccessfulJumpCount { get; private set; }
    public float MoveIntent => moveInput;
    public float SuccessfulLeftDistance { get; private set; }
    public float SuccessfulRightDistance { get; private set; }

    public void SetInputLocked(bool locked)
    {
        inputLocked = locked;
        ClearTouchInput();
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.simulated = !locked && !recovering;
        }
        if (view) view.SetWalking(false);
    }

    public void ClearTouchInput()
    {
        moveInput = touchMoveInput = dragMoveInput = 0f;
        dragging = false;
        lastAppliedMove = 0f;
    }

    // Kept for a future optional drag-control experiment.
    public void SetDragMove(float direction, bool active)
    {
        dragging = active && !InputLocked;
        dragMoveInput = dragging ? Mathf.Clamp(direction, -1f, 1f) : 0f;
    }

    public void SetRecovering(bool value)
    {
        recovering = value;
        ClearTouchInput();
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.simulated = !inputLocked && !recovering;
            previousPhysicsX = rb.position.x;
        }
        if (view) view.SetWalking(false);
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        if (!view) view = GetComponentInChildren<PlayerView>();

        // Remove the old Player 1 horizontal lock, including one saved in the Inspector.
        rb.constraints &= ~RigidbodyConstraints2D.FreezePositionX;
        previousPhysicsX = rb.position.x;

        // Maintain the existing non-colliding two-player behavior.
        foreach (var other in FindObjectsByType<PlayerController>(FindObjectsSortMode.None))
        {
            if (other == this) continue;
            var otherCollider = other.GetComponent<Collider2D>();
            if (otherCollider && col) Physics2D.IgnoreCollision(col, otherCollider, true);
        }
    }

    private void Update()
    {
        if (InputLocked) return;
        ReadInput();
        if (!view) return;
        bool walking = IsGrounded && Mathf.Abs(moveInput) > .01f;
        view.SetWalking(walking);
        if (walking) view.SetFacingDirection(moveInput > 0f ? 1 : -1);
    }

    private void FixedUpdate()
    {
        float moved = rb.position.x - previousPhysicsX;
        previousPhysicsX = rb.position.x;
        if (lastAppliedMove < 0f && moved < 0f) SuccessfulLeftDistance -= moved;
        if (lastAppliedMove > 0f && moved > 0f) SuccessfulRightDistance += moved;

        // Both characters can walk, but jumping is straight up (no air steering).
        lastAppliedMove = !InputLocked && IsGrounded && Time.time >= jumpMoveLockUntil
            ? moveInput : 0f;
        if (!InputLocked)
            rb.linearVelocity = new Vector2(lastAppliedMove * moveSpeed, rb.linearVelocity.y);
    }

    private void ReadInput()
    {
        float keyboardMove = 0f;
        var kb = Keyboard.current;
        if (kb != null)
        {
            if (isPlayerTwo)
            {
                if (kb.leftArrowKey.isPressed) keyboardMove -= 1f;
                if (kb.rightArrowKey.isPressed) keyboardMove += 1f;
                if (kb.upArrowKey.wasPressedThisFrame) Jump();
            }
            else
            {
                if (kb.aKey.isPressed) keyboardMove -= 1f;
                if (kb.dKey.isPressed) keyboardMove += 1f;
                if (kb.wKey.wasPressedThisFrame) Jump();
            }
        }

        // Touch buttons take priority over optional drag and keyboard input.
        moveInput = Mathf.Abs(touchMoveInput) > .01f ? touchMoveInput
            : dragging ? dragMoveInput : keyboardMove;
    }

    public void SetTouchMove(float direction)
    {
        if (direction == 0f || !InputLocked)
            touchMoveInput = Mathf.Clamp(direction, -1f, 1f);
    }

    // Called by the correct player's jump button or keyboard binding.
    public void Jump()
    {
        if (InputLocked || !IsGrounded || rb.linearVelocity.y > .15f) return;
        rb.linearVelocity = new Vector2(0f, jumpForce);
        // Prevent ground-contact lag from producing a short diagonal takeoff.
        jumpMoveLockUntil = Time.time + .10f;
        SuccessfulJumpCount++;
    }

    private void OnApplicationFocus(bool focused) { if (!focused) ClearTouchInput(); }
    private void OnApplicationPause(bool paused) { if (paused) ClearTouchInput(); }
}
