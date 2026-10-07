using Fusion;
using UnityEngine;

public class PlayerMovement : NetworkBehaviour
{
    [Header("Move")]
    public float moveSpeed = 8f;                 // unidades/seg (¡sin DeltaTime!)

    [Header("Jump")]
    public float jumpForce = 16f;
    public float coyoteTime = 0.1f;
    public float jumpBufferTime = 0.1f;
    [Range(0f, 1f)] public float jumpCutMultiplier = 0.5f;

    [Header("Ground Check")]
    public Transform groundCheck;
    public Vector2 groundCheckSize = new Vector2(0.5f, 0.1f);
    public LayerMask groundLayer;

    [Header("Dash")]
    public float dashSpeed = 22f;                // unidades/seg, constante
    public float dashDuration = 0.15f;           // distancia = 22 * 0.15 ≈ 3.3 u
    public float dashCooldown = 0.6f;

    [Networked] NetworkButtons PrevButtons { get; set; }
    [Networked] TickTimer DashTimer { get; set; }
    [Networked] TickTimer DashCooldownTimer { get; set; }
    [Networked] TickTimer CoyoteTimer { get; set; }
    [Networked] TickTimer JumpBufferTimer { get; set; }
    [Networked] NetworkBool AirDashUsed { get; set; }
    [Networked] NetworkBool JumpCutDone { get; set; }
    [Networked] public int Facing { get; set; }

    public bool IsDashing => DashTimer.IsRunning && !DashTimer.Expired(Runner);
    public bool DashReady => DashCooldownTimer.ExpiredOrNotRunning(Runner) && !AirDashUsed;

    Rigidbody2D rb;

    public override void Spawned()
    {
        rb = GetComponent<Rigidbody2D>();
        if (HasStateAuthority) Facing = 1;
    }

    public override void FixedUpdateNetwork()
    {
        if (Runner.IsForward && Runner.Tick % 60 == 0)
            Debug.Log($"[{name}] HasInputAuth={Object.HasInputAuthority} StateAuth={HasStateAuthority} GetInput={GetInput(out PlayerInputData d)} Move={d.Move}");

        if (!GetInput(out PlayerInputData input)) return;

        var pressed = input.Buttons.GetPressed(PrevButtons);
        PrevButtons = input.Buttons;

        bool grounded = Physics2D.OverlapBox(groundCheck.position, groundCheckSize, 0f, groundLayer);
        if (grounded)
        {
            CoyoteTimer = TickTimer.CreateFromSeconds(Runner, coyoteTime);
            AirDashUsed = false;
        }

        if (pressed.IsSet(InputButton.Jump))
            JumpBufferTimer = TickTimer.CreateFromSeconds(Runner, jumpBufferTime);

        // --- Inicio del dash ---
        if (pressed.IsSet(InputButton.Dash) && DashReady && !IsDashing)
        {
            DashTimer = TickTimer.CreateFromSeconds(Runner, dashDuration);
            DashCooldownTimer = TickTimer.CreateFromSeconds(Runner, dashCooldown);
            if (!grounded) AirDashUsed = true;   // un dash aéreo por salto
        }

        // --- Durante el dash: velocidad constante, ignora todo lo demás ---
        if (IsDashing)
        {
            rb.linearVelocity = new Vector2(Facing * dashSpeed, 0f);
            return;
        }

        // --- Movimiento normal ---
        if (input.Move != 0f) Facing = input.Move > 0f ? 1 : -1;
        rb.linearVelocity = new Vector2(input.Move * moveSpeed, rb.linearVelocity.y);

        // --- Salto con coyote + buffer ---
        if (!JumpBufferTimer.ExpiredOrNotRunning(Runner) && !CoyoteTimer.ExpiredOrNotRunning(Runner))
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            JumpBufferTimer = TickTimer.None;
            CoyoteTimer = TickTimer.None;
            JumpCutDone = false;
        }

        // --- Altura variable: se corta UNA vez al soltar ---
        bool jumpHeld = input.Buttons.IsSet(InputButton.Jump);
        if (!jumpHeld && !JumpCutDone && rb.linearVelocity.y > 0f)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * jumpCutMultiplier);
            JumpCutDone = true;
        }
    }

    void OnDrawGizmosSelected()
    {
        if (groundCheck == null) return;
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(groundCheck.position, groundCheckSize);
    }
}