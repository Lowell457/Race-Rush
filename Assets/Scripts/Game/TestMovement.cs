using Fusion;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.InputSystem;

public enum Direction { left = -1, right = 1}

public class TestMovement : NetworkBehaviour
{
    [Header("Move")]
    public float moveSpeed = 8f;
    private Direction direction = Direction.right;

    [Header("Jump")]
    public float jumpForce = 16f;
    public float coyoteTime = 0.1f;       // grace after leaving ground
    public float jumpBufferTime = 0.1f;   // grace before landing
    [Range(0f, 1f)] public float jumpCutMultiplier = 0.5f; // variable height

    [Header("Ground Check")]
    public Transform groundCheck;         // empty child at the feet
    public Vector2 groundCheckSize = new Vector2(0.5f, 0.1f);
    public LayerMask groundLayer;

    Rigidbody2D rb;
    float moveInput;
    bool isGrounded;
    float coyoteCounter;
    float jumpBufferCounter;
    bool jumpHeld;

    [Header("Dash")]
    private bool isDashing = false;
    [SerializeField] private float dashDuration;
    private float dashProgress;
    [SerializeField] private float dashSpeed;
    private bool canDash;
    [SerializeField] private float dashCooldown;
    private float dashCooldownTimer;
    [SerializeField] private float minimumDashSpeed;

    public override void Spawned()
    {
        rb = GetComponent<Rigidbody2D>();
        dashProgress = dashDuration;
    }

    // Hooked up from a PlayerInput component (Behavior: Send Messages / Invoke Unity Events)
    public void OnMove(InputAction.CallbackContext ctx)
    {
        if (!HasStateAuthority)
            return;

        moveInput = ctx.ReadValue<float>();

        if (!isDashing)
        {
            if (moveInput < 0)
                direction = Direction.left;
            else if (moveInput > 0)
                direction = Direction.right;
        }
    }

    public void OnJump(InputAction.CallbackContext ctx)
    {
        if (!HasStateAuthority)
            return;

        if (ctx.started) jumpBufferCounter = jumpBufferTime; // press buffered
        jumpHeld = ctx.ReadValueAsButton();
        if (ctx.canceled) jumpHeld = false;
    }

    public void OnDash(InputAction.CallbackContext ctx)
    {
        if (!HasStateAuthority)
            return;

        if (ctx.started && canDash && !isDashing)
        {
            dashProgress = 0f;
            dashCooldownTimer = dashCooldown;
        }
    }

    private void AirStuff()
    {
        if (!HasStateAuthority)
            return;

        // Ground check with an OverlapBox at the feet
        isGrounded = Physics2D.OverlapBox(groundCheck.position, groundCheckSize, 0f, groundLayer);

        if (dashCooldownTimer <= 0 && isGrounded)
        {
            canDash = true;
        }
        else dashCooldownTimer -= Runner.DeltaTime;

        // Coyote time and jump buffer countdowns
        coyoteCounter = isGrounded ? coyoteTime : coyoteCounter - Runner.DeltaTime;
        jumpBufferCounter -= Runner.DeltaTime;

        // Execute a jump if we have buffered input and are within coyote window
        if (jumpBufferCounter > 0f && coyoteCounter > 0f)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            jumpBufferCounter = 0f;
            coyoteCounter = 0f;
            canDash = true;
        }

        // Variable jump height: cut upward velocity when the button is released early
        if (!jumpHeld && rb.linearVelocity.y > 0f)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x,
                                            rb.linearVelocity.y * jumpCutMultiplier);
        }
    }

    public override void FixedUpdateNetwork()
    {
        base.FixedUpdateNetwork();
        CalculateMovement();
        AirStuff();
    }

    private void CalculateMovement()
    {
        if (!HasStateAuthority)
            return;

        rb.linearVelocity = new Vector2(moveInput * moveSpeed * Runner.DeltaTime, rb.linearVelocity.y);

        if (dashProgress < dashDuration)
        {
            isDashing = true;
            canDash = false;
            dashProgress += Runner.DeltaTime;
            float mod = dashDuration / dashProgress;
            Vector2 dash = Vector2.right * dashSpeed * mod;
            dash.x += minimumDashSpeed;
            rb.linearVelocity = dash * (int)direction * Runner.DeltaTime;
        }
        else
            isDashing = false;
    }

    void OnDrawGizmosSelected()
    {
        if (groundCheck == null) return;
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(groundCheck.position, groundCheckSize);
    }
}
