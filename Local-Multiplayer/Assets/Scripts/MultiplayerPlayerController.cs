using UnityEngine;
using UnityEngine.InputSystem;

// PlayerInput component should use the MultiControls asset, action map CALLED PlayerControls
// player 1 gets controller slot 0, player 2 gets controller slot 1 — unity handles this automatically
// PlayerInputManager assigns whichever controller joined first/second

[RequireComponent(typeof(PlayerInput))]
[RequireComponent(typeof(CharacterController))]
public class MultiplayerPlayerController : MonoBehaviour
{
    // --- movement ---
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 6f;
    [SerializeField] private float jumpForce = 5f;
    [SerializeField] private float gravity   = -18f;

    // --- ground check ---
    [Header("Ground Check")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float     groundCheckRadius = 0.2f;
    [SerializeField] private LayerMask groundLayer;

    // --- character visuals ---
    [Header("Character Setup")]
    [SerializeField] private Transform  characterVisualSlot;
    [SerializeField] private GameObject p1CharacterPrefab;
    [SerializeField] private GameObject p2CharacterPrefab;

    // --- spawn points ---
    [Header("Spawn Points")]
    [SerializeField] private Transform p1SpawnPoint;
    [SerializeField] private Transform p2SpawnPoint;

    // --- private ---
    private CharacterController cc;
    private PlayerInput         playerInput;
    private Vector2             moveInput;
    private float               verticalVelocity;
    private bool                isGrounded;
    private bool                jumpQueued;

    // --- public ---
    public int  PlayerID           { get; private set; }
    public bool LightAttackPressed { get; private set; }
    public bool HeavyAttackPressed { get; private set; }
    public bool ReactionPressed    { get; private set; }
    public bool BlockHeld          { get; private set; }

    // -------------------------------------------------------

    private void Awake()
    {
        cc          = GetComponent<CharacterController>();
        playerInput = GetComponent<PlayerInput>();
        PlayerID    = playerInput.playerIndex + 1;
    }

    private void Start()
    {
        MoveToSpawnPoint();
        SpawnCharacterVisual();
    }

    private void Update()
    {
        CheckGround();
        ApplyGravity();
        HandleJump();
        MoveCharacter();

        LightAttackPressed = false;
        HeavyAttackPressed = false;
        ReactionPressed    = false;
    }

    // -------------------------------------------------------
    // spawn
    // -------------------------------------------------------

    private void MoveToSpawnPoint()
    {
        Transform sp = PlayerID == 1 ? p1SpawnPoint : p2SpawnPoint;

        if (sp == null)
        {
            Debug.LogWarning($"[P{PlayerID}] no spawn point assigned");
            return;
        }

        cc.enabled         = false;
        transform.position = sp.position;
        transform.rotation = sp.rotation;
        cc.enabled         = true;

        Debug.Log($"[P{PlayerID}] moved to spawn: {sp.position}");
    }

    private void SpawnCharacterVisual()
    {
        GameObject prefab = PlayerID == 1 ? p1CharacterPrefab : p2CharacterPrefab;

        if (prefab == null || characterVisualSlot == null)
        {
            Debug.LogWarning($"[P{PlayerID}] character prefab or visual slot not assigned");
            return;
        }

        Instantiate(prefab, characterVisualSlot.position, characterVisualSlot.rotation, characterVisualSlot);
        Debug.Log($"[P{PlayerID}] spawned: {prefab.name}");
    }

    // -------------------------------------------------------
    // input callbacks
    // -------------------------------------------------------

    public void OnMove(InputAction.CallbackContext ctx)             { moveInput = ctx.ReadValue<Vector2>(); }
    public void OnLightAttack(InputAction.CallbackContext ctx)      { if (ctx.started) LightAttackPressed = true; }
    public void OnHeavyAttack(InputAction.CallbackContext ctx)      { if (ctx.started) HeavyAttackPressed = true; }
    public void OnReactionTrigger(InputAction.CallbackContext ctx)  { if (ctx.started) ReactionPressed    = true; }

    public void OnJump(InputAction.CallbackContext ctx)
    {
        if (ctx.started && isGrounded) jumpQueued = true;
    }

    public void OnBlock(InputAction.CallbackContext ctx)
    {
        if (ctx.started)  BlockHeld = true;
        if (ctx.canceled) BlockHeld = false;
    }

    // -------------------------------------------------------
    // movement — single cc.Move per frame
    // -------------------------------------------------------

    private void CheckGround()
    {
        Vector3 pos = groundCheck != null
            ? groundCheck.position
            : transform.position + Vector3.down * 0.9f;

        isGrounded = Physics.CheckSphere(pos, groundCheckRadius, groundLayer);
    }

    private void ApplyGravity()
    {
        if (isGrounded && verticalVelocity < 0f)
            verticalVelocity = -2f;
        else
            verticalVelocity += gravity * Time.deltaTime;
    }

    private void HandleJump()
    {
        if (!jumpQueued) return;
        verticalVelocity = jumpForce;
        jumpQueued       = false;
    }

    private void MoveCharacter()
    {
        Vector3 horizontal = new Vector3(moveInput.x, 0f, 0f);
        if (horizontal.magnitude > 1f) horizontal.Normalize();

        Vector3 finalMove = horizontal * moveSpeed + Vector3.up * verticalVelocity;
        cc.Move(finalMove * Time.deltaTime);

        if (horizontal.x != 0f && characterVisualSlot != null)
        {
            characterVisualSlot.localScale = new Vector3(
                Mathf.Sign(horizontal.x) * Mathf.Abs(characterVisualSlot.localScale.x),
                characterVisualSlot.localScale.y,
                characterVisualSlot.localScale.z
            );
        }
    }

    // -------------------------------------------------------
    // gizmos
    // -------------------------------------------------------

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Vector3 pos = groundCheck != null
            ? groundCheck.position
            : transform.position + Vector3.down * 0.9f;
        Gizmos.DrawWireSphere(pos, groundCheckRadius);
    }
}