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
        ReadInput();
        bool grounded = col.IsTouchingLayers(groundLayer);
        if (view)
        {
            view.SetWalking(grounded && Mathf.Abs(moveInput) > 0.01f);
            if (grounded && Mathf.Abs(moveInput) > 0.01f) view.SetFacingDirection(moveInput > 0 ? 1 : -1);
        }
    }

    private void FixedUpdate() => rb.linearVelocity = new Vector2(col.IsTouchingLayers(groundLayer) ? moveInput * moveSpeed : 0f, rb.linearVelocity.y);

    private void ReadInput()
    {
        moveInput = 0f;
        var kb = Keyboard.current;
        if (kb == null) return;

        if (!isPlayerTwo) {
            if (kb.aKey.isPressed) moveInput -= 1f;
            if (kb.dKey.isPressed) moveInput += 1f;
            if (kb.wKey.wasPressedThisFrame && col.IsTouchingLayers(groundLayer)) rb.linearVelocity = new Vector2(0f, jumpForce);
        } else {
            if (kb.leftArrowKey.isPressed) moveInput -= 1f;
            if (kb.rightArrowKey.isPressed) moveInput += 1f;
            if (kb.upArrowKey.wasPressedThisFrame && col.IsTouchingLayers(groundLayer)) rb.linearVelocity = new Vector2(0f, jumpForce);
        }
    }
}