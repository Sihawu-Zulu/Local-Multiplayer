using UnityEngine;
using UnityEngine.InputSystem;


// PlayerInput component should use the MultiControls asset, action map CALLED PlayerControls
// player 1 gets controller slot 0, player 2 gets controller slot 1 — unity handles this automatically.. THANK GOD it works!!
// when you spawn each player, PlayerInputManager assigns whichever controller joined first/second

[RequireComponent(typeof(PlayerInput))]
[RequireComponent(typeof(CharacterController))]
public class MultiplayerPlayerController : MonoBehaviour
{
    // --- movement stuff ---
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 6f;
    [SerializeField] private float jumpForce = 5f;
    [SerializeField] private float gravity = -18f; 

 
    [Header("Ground Check")]
    [SerializeField] private Transform groundCheck;       
    [SerializeField] private float groundCheckRadius = 0.2f;
    [SerializeField] private LayerMask groundLayer;


    private CharacterController cc;
    private PlayerInput playerInput;

    private Vector2 moveInput;
    private Vector3 velocity;      
    private bool isGrounded;
    private bool jumpQueued;       


    public int PlayerID { get; private set; }

    
    public bool LightAttackPressed { get; private set; }
    public bool HeavyAttackPressed { get; private set; }
    public bool ReactionPressed    { get; private set; }

    // -------------------------------------------------------

    private void Awake()
    {
        cc = GetComponent<CharacterController>();
        playerInput = GetComponent<PlayerInput>();

        // grab player index from the PlayerInput component (0 = p1, 1 = p2)
        PlayerID = playerInput.playerIndex + 1;
    }

    private void Update()
    {
        CheckGround();
        HandleGravity();
        HandleMovement();
        HandleJump();

        
        LightAttackPressed = false;
        HeavyAttackPressed = false;
        ReactionPressed    = false;
    }


    public void OnMove(InputAction.CallbackContext ctx)
    {
        moveInput = ctx.ReadValue<Vector2>();
    }

   
    public void OnJump(InputAction.CallbackContext ctx)
    {
        
        if (ctx.started && isGrounded)
            jumpQueued = true;
    }

   
    public void OnLightAttack(InputAction.CallbackContext ctx)
    {
        if (ctx.started)
            LightAttackPressed = true;
    }

    
    public void OnHeavyAttack(InputAction.CallbackContext ctx)
    {
        if (ctx.started)
            HeavyAttackPressed = true;
    }

    
    public void OnReactionTrigger(InputAction.CallbackContext ctx)
    {
      
        if (ctx.started)
            ReactionPressed = true;
    }

    // -------------------------------------------------------
    // movement logic
    // -------------------------------------------------------

    private void CheckGround()
    {
        
        isGrounded = Physics.CheckSphere(
            groundCheck != null ? groundCheck.position : transform.position + Vector3.down * 0.9f,
            groundCheckRadius,
            groundLayer
        );

     
        if (isGrounded && velocity.y < 0f)
            velocity.y = -2f;
    }

    private void HandleGravity()
    {
        velocity.y += gravity * Time.deltaTime;
        cc.Move(velocity * Time.deltaTime);
    }

    private void HandleMovement()
    {
        
        Vector3 move = new Vector3(moveInput.x, 0f, 0f); 

        
        if (move.magnitude > 1f)
            move.Normalize();

        cc.Move(move * moveSpeed * Time.deltaTime);

       
        if (move.x != 0f)
            transform.localScale = new Vector3(
                Mathf.Sign(move.x) * Mathf.Abs(transform.localScale.x),
                transform.localScale.y,
                transform.localScale.z
            );
    }

    private void HandleJump()
    {
        if (!jumpQueued) return;

        velocity.y = jumpForce;
        jumpQueued = false;
    }

    // -------------------------------------------------------
    // debug gizmos — to shows ground check sphere in scene view
    // -------------------------------------------------------

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Vector3 pos = groundCheck != null ? groundCheck.position : transform.position + Vector3.down * 0.9f;
        Gizmos.DrawWireSphere(pos, groundCheckRadius);
    }
}
