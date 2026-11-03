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
    private bool inputLocked;

    // Stable anonymous slots shared by keyboard, touch controls, goals and logging.
    public int PlayerId => isPlayerTwo ? 2 : 1;
    public Collider2D BodyCollider => col;
    public bool IsGrounded => col != null && col.IsTouchingLayers(groundLayer);

    public void SetInputLocked(bool locked)
    {
        inputLocked = locked;
        moveInput = touchMoveInput = 0f;
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.simulated = !locked;
        }
        if (view) view.SetWalking(false);
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        if (!view) view = GetComponentInChildren<PlayerView>();

        // Ignore collisions with other players
        foreach (var otherPlayer in FindObjectsByType<PlayerController>(FindObjectsSortMode.None))
        {
            if (otherPlayer != this)
            {
                Collider2D otherCol = otherPlayer.GetComponent<Collider2D>();
                if (otherCol != null && col != null)
                    Physics2D.IgnoreCollision(col, otherCol, true);
            }
        }
    }

    private void Update()
    {
        if (inputLocked) return;
        ReadInput();
        bool grounded = col.IsTouchingLayers(groundLayer);
        if (view)
        {
            view.SetWalking(grounded && Mathf.Abs(moveInput) > 0.01f);
            if (grounded && Mathf.Abs(moveInput) > 0.01f) view.SetFacingDirection(moveInput > 0 ? 1 : -1);
        }
    }

    private void FixedUpdate()
    {
        if (!inputLocked)
            rb.linearVelocity = new Vector2(col.IsTouchingLayers(groundLayer) ? moveInput * moveSpeed : 0f, rb.linearVelocity.y);
    }

    private void ReadInput()
    {
        float kbInput = 0f;
        var kb = Keyboard.current;

        if (kb != null)
        {
            if (!isPlayerTwo) 
            {
                if (kb.aKey.isPressed) kbInput -= 1f;
                if (kb.dKey.isPressed) kbInput += 1f;
                if (kb.wKey.wasPressedThisFrame) Jump();
            } 
            else 
            {
                if (kb.leftArrowKey.isPressed) kbInput -= 1f;
                if (kb.rightArrowKey.isPressed) kbInput += 1f;
                if (kb.upArrowKey.wasPressedThisFrame) Jump();
            }
        }

        // עדיפות למגע אם לוחצים, אחרת מקלדת
        moveInput = Mathf.Abs(touchMoveInput) > 0.01f ? touchMoveInput : kbInput;
    }

    // פונקציות לכפתורי מסך (UI)
    public void SetTouchMove(float direction)
    {
        if (!inputLocked) touchMoveInput = direction;
    }

    public void Jump()
    {
        if (!inputLocked && col != null && col.IsTouchingLayers(groundLayer))
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
    }
}
