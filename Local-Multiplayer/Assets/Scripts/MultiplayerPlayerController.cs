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

    // --- ground check ---
    [Header("Ground Check")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundCheckRadius = 0.2f;
    [SerializeField] private LayerMask groundLayer;

    // --- character visuals ---
    [Header("Character Setup")]
    [SerializeField] private Transform characterVisualSlot;
    [SerializeField] private GameObject p1CharacterPrefab;
    [SerializeField] private GameObject p2CharacterPrefab;

    // --- spawn points ---
    [Header("Spawn Points")]
    [SerializeField] private Transform p1SpawnPoint;
    [SerializeField] private Transform p2SpawnPoint;


    private CharacterController cc;
    private PlayerInput playerInput;

    private Vector2 moveInput;
    private Vector3 velocity;
    private bool isGrounded;
    private bool jumpQueued;


    public int PlayerID { get; private set; }

    // --- single frame press flags (reset every Update) ---
    public bool LightAttackPressed { get; private set; }
    public bool HeavyAttackPressed { get; private set; }
    public bool ReactionPressed    { get; private set; }

    // --- held flag (stays true while button is held) ---
    // CombatController will read this every frame to know if blocking is active... will add later
    public bool BlockHeld          { get; private set; }

    // -------------------------------------------------------

    private void Awake()
    {
        cc          = GetComponent<CharacterController>();
        playerInput = GetComponent<PlayerInput>();

        // grab player index from the PlayerInput component (0 = p1, 1 = p2)
        PlayerID = playerInput.playerIndex + 1;
    }

    private void Start()
    {
        MoveToSpawnPoint();
        SpawnCharacterVisual();
    }

    private void Update()
    {
        CheckGround();
        HandleGravity();
        HandleMovement();
        HandleJump();

        // reset single-frame flags — BlockHeld is NOT reset here.....it stays until released
        LightAttackPressed = false;
        HeavyAttackPressed = false;
        ReactionPressed    = false;
    }


    private void MoveToSpawnPoint()
    {
        Transform spawnPoint = PlayerID == 1 ? p1SpawnPoint : p2SpawnPoint;

        if (spawnPoint == null)
        {
            Debug.LogWarning($"[Player {PlayerID}] no spawn point assigned — staying at default position");
            return;
        }

     
        cc.enabled = false;
        transform.position = spawnPoint.position;
        transform.rotation = spawnPoint.rotation;
        cc.enabled = true;

        Debug.Log($"[Player {PlayerID}] moved to spawn point: {spawnPoint.position}");
    }

    // -------------------------------------------------------
    // character visual setup
    // -------------------------------------------------------

    private void SpawnCharacterVisual()
    {
        GameObject prefabToSpawn = PlayerID == 1 ? p1CharacterPrefab : p2CharacterPrefab;

        if (prefabToSpawn == null)
        {
            
            return;
        }

        if (characterVisualSlot == null)
        {
        
            return;
        }

        Instantiate(prefabToSpawn, characterVisualSlot.position, characterVisualSlot.rotation, characterVisualSlot);
        Debug.Log($"[Player {PlayerID}] spawned character: {prefabToSpawn.name}");
    }

    // -------------------------------------------------------
    // input callbacks
    // -------------------------------------------------------

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


    public void OnBlock(InputAction.CallbackContext ctx)
    {
        if (ctx.started)   BlockHeld = true;
        if (ctx.canceled)  BlockHeld = false;
    }

    // -------------------------------------------------------
    // movement stuff
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

        // flip the visual slot to face movement direction — don't flip the root (breaks CC learnt that the hard way lol)
        if (move.x != 0f && characterVisualSlot != null)
        {
            characterVisualSlot.localScale = new Vector3(
                Mathf.Sign(move.x) * Mathf.Abs(characterVisualSlot.localScale.x),
                characterVisualSlot.localScale.y,
                characterVisualSlot.localScale.z
            );
        }
    }

    private void HandleJump()
    {
        if (!jumpQueued) return;

        velocity.y = jumpForce;
        jumpQueued = false;
    }

    // -------------------------------------------------------
    // debug gizmos ..... will add more later, just ground check for now.. range n stuff is easy to mess up and not notice without a visual
    // -------------------------------------------------------

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Vector3 pos = groundCheck != null ? groundCheck.position : transform.position + Vector3.down * 0.9f;
        Gizmos.DrawWireSphere(pos, groundCheckRadius);
    }
}