using Fusion;
using System;
using TMPro;
using UnityEditor;
using UnityEngine;

public class PlayerMovement : NetworkBehaviour
{
    [Header("Move")]
    public float moveSpeed = 8f;                 // unidades/seg (¡sin DeltaTime!)
    [SerializeField] private bool stunned = false;
    private Timer stunTimer;
    private float currentStunDuration;

    private float currentSpeedMultiplier = 1f;
    private Timer speedMultTimer;
    private float speedMultDuration;

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
    //Player push
    [SerializeField] private float radius;
    [SerializeField] private float pushForce;
    [SerializeField] private LayerMask playerLayer;
    [SerializeField] private float verticalPushbackMultiplier;
    [SerializeField] private float stunOtherFor;

    [SerializeField] private Tuple<Transform, int> savedCheckPoint;

    [Networked] NetworkButtons PrevButtons { get; set; }
    [Networked] TickTimer DashTimer { get; set; }
    [Networked] TickTimer DashCooldownTimer { get; set; }
    [Networked] TickTimer CoyoteTimer { get; set; }
    [Networked] TickTimer JumpBufferTimer { get; set; }
    [Networked] NetworkBool AirDashUsed { get; set; }
    [Networked] NetworkBool JumpCutDone { get; set; }
    [Networked, OnChangedRender(nameof(SetSpriteDirection))] public int Facing { get; set; }

    public bool IsDashing => DashTimer.IsRunning && !DashTimer.Expired(Runner);
    public bool DashReady => DashCooldownTimer.ExpiredOrNotRunning(Runner) && !AirDashUsed;

    Rigidbody2D rb;

    [Header("Visual display")]
    [SerializeField] private TextMeshPro playerNameDisplay;
    [SerializeField] private Color selfPlayerColor;
    [SerializeField] private SpriteRenderer playerSprite;

    [Header("GetPlayerSprites")]
    [SerializeField] private Sprite player1;
    [SerializeField] private Sprite player2;
    [SerializeField] private Sprite player3;
    [SerializeField] private Sprite player4;

    public override void Spawned()
    {
        rb = GetComponent<Rigidbody2D>();

        if (HasInputAuthority)
        {
            RPC_SetDisplayName(NetworkManager.Instance.LocalNickname);
            RPC_SetPlayerSprites(NetworkManager.Instance.LocalSkin);
            NetworkManager.Instance.SetPlayerTransform(transform);
            NetworkManager.Instance.SetPlayerTransform(rb);
        }

        if (HasStateAuthority) Facing = 1;
    }

    public override void FixedUpdateNetwork()
    {
        /*if (Runner.IsForward && Runner.Tick % 60 == 0)
            Debug.Log($"[{name}] HasInputAuth={Object.HasInputAuthority} StateAuth={HasStateAuthority} GetInput={GetInput(out PlayerInputData d)} Move={d.Move}");
        */
        if (!GetInput(out PlayerInputData input)) return;

        if (stunned && stunTimer.ElapsedInSeconds >= currentStunDuration)
        {
            stunned = false;
            stunTimer.Reset();
            stunTimer.Stop();
            currentStunDuration = 0;
        }

        if (speedMultTimer.ElapsedInSeconds >= speedMultDuration)
        {
            speedMultTimer.Reset();
            speedMultTimer.Stop();
            currentSpeedMultiplier = 1f;
            speedMultDuration = 0f;
        }

        var pressed = input.Buttons.GetPressed(PrevButtons);
        PrevButtons = input.Buttons;

        bool grounded = Physics2D.OverlapBox(groundCheck.position, groundCheckSize, 0f, groundLayer);
        if (grounded)
        {
            CoyoteTimer = TickTimer.CreateFromSeconds(Runner, coyoteTime);
            AirDashUsed = false;
        }

        if (pressed.IsSet(InputButton.Jump) && !stunned)
            JumpBufferTimer = TickTimer.CreateFromSeconds(Runner, jumpBufferTime);

        // --- Inicio del dash ---
        if (pressed.IsSet(InputButton.Dash) && DashReady && !IsDashing && !stunned)
        {
            DashTimer = TickTimer.CreateFromSeconds(Runner, dashDuration);
            DashCooldownTimer = TickTimer.CreateFromSeconds(Runner, dashCooldown);
            if (!grounded) AirDashUsed = true;   // un dash aéreo por salto

            //Push players back
            var players = Physics2D.OverlapCircleAll(transform.position, radius, playerLayer);
            

            foreach (var p in players)
            {
                if (p != null && p != gameObject)
                {
                    Rigidbody2D pRb = p.GetComponent<Rigidbody2D>();
                    PlayerMovement pMov = p.GetComponent<PlayerMovement>();
                    if (pRb != null && pMov != null)
                    {
                        Vector2 pushbackAngle = ((Vector2.left * Facing) + (Vector2.up * verticalPushbackMultiplier)) * pushForce;
                        pRb.AddForce(pushbackAngle, ForceMode2D.Impulse);
                        pMov.StunPlayer(stunOtherFor);
                    }
                }
            }
        }

        // --- Durante el dash: velocidad constante, ignora todo lo demás ---
        if (IsDashing)
        {
            rb.linearVelocity = new Vector2(Facing * dashSpeed, 0f);
            return;
        }

        // --- Movimiento normal ---
        if (input.Move != 0f && !stunned) Facing = input.Move > 0f ? 1 : -1;

        Vector2 movementMade = new Vector2(input.Move * moveSpeed * currentSpeedMultiplier, rb.linearVelocity.y);
        rb.linearVelocity = stunned ? rb.linearVelocity : movementMade;

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

    public void KillPlayer()
    {
        if (!HasStateAuthority)
            return;

        rb.linearVelocity = Vector2.zero;

        transform.position = savedCheckPoint.Item1.position;
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.All)]
    private void RPC_SetDisplayName(string name)
    {
        playerNameDisplay.text = name;

        if (HasInputAuthority) playerNameDisplay.color = selfPlayerColor;
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.All)]
    public void RPC_SetPlayerSprites(int skinId)
    {
        Debug.Log("Skin number: " + skinId);

        switch (skinId)
        {
            case 1:
                playerSprite.sprite = player1;
                break;
            case 2:
                playerSprite.sprite = player2;
                break;
            case 3:
                playerSprite.sprite = player3;
                break;
            case 4:
                playerSprite.sprite = player4;
                break;
        }
    }

    public void SaveCheckPoint(Transform checkpoint, int priority)
    {
        if (!HasStateAuthority)
            return;

        if (savedCheckPoint == null)
            savedCheckPoint = Tuple.Create(checkpoint, priority);
        else if (savedCheckPoint.Item2 <= priority)
            savedCheckPoint = Tuple.Create(checkpoint, priority);
    }


    private void SetSpriteDirection()
    {
        playerSprite.flipX = Facing != 1; 
    }

    void OnDrawGizmosSelected()
    {
        if (groundCheck == null) return;
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(groundCheck.position, groundCheckSize);
        Gizmos.DrawWireSphere(transform.position, radius);
    }

    public void StunPlayer(float duration)
    {
        if (currentStunDuration - stunTimer.ElapsedInSeconds >= duration)
            return;

        if (stunTimer.IsRunning)
            stunTimer.Restart();
        else
            stunTimer.Start();
        currentStunDuration = duration;
        stunned = true;
    }

    public void PushBack(float stunDuration)
    {
        if (!HasStateAuthority)
            return;

        StunPlayer(stunDuration);

        Vector2 pushbackAngle = ((Vector2.left * Facing) + (Vector2.up * verticalPushbackMultiplier)) * pushForce;
        rb.AddForce(pushbackAngle, ForceMode2D.Impulse);
    }

    public void ChangeSpeedMultiplier(float newMult, float duration)
    {
        currentSpeedMultiplier = newMult;
        speedMultDuration = duration;

        if (speedMultTimer.IsRunning)
            speedMultTimer.Restart();
        else
            speedMultTimer.Start();
    }
}