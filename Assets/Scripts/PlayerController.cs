using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;
    public float jumpForce = 10f;

    [Header("Ground Check")]
    public Transform groundCheck;
    public float groundCheckRadius = 0.1f;
    public LayerMask groundLayer;

    private Rigidbody2D rb;
    private bool isGrounded;
    private Vector2 moveInput;
    private bool jumpPressed;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        // Horizontal input
        float horizontal = 0f;
        if (keyboard.leftArrowKey.isPressed || keyboard.aKey.isPressed) horizontal = -1f;
        if (keyboard.rightArrowKey.isPressed || keyboard.dKey.isPressed) horizontal = 1f;
        moveInput = new Vector2(horizontal, 0f);

        // Ground check
        isGrounded = Physics2D.OverlapCircle(
            groundCheck.position,
            groundCheckRadius,
            groundLayer
        );

        // Jump
        if ((keyboard.spaceKey.wasPressedThisFrame ||
             keyboard.upArrowKey.wasPressedThisFrame) && isGrounded)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
        }

        // Flip sprite
        if (moveInput.x > 0) transform.localScale = new Vector3(0.5f, 1f, 1f);
        if (moveInput.x < 0) transform.localScale = new Vector3(-0.5f, 1f, 1f);
    }

    void FixedUpdate()
    {
        rb.linearVelocity = new Vector2(moveInput.x * moveSpeed, rb.linearVelocity.y);
    }
}