using System;
using UnityEngine;
using UnityEngine.InputSystem;


// player 1 gets controller slot 0, player 2 gets controller slot 1 — unity handles this automatically
// attacks use C# events (not frame flags) so execution order doesn't matter nomore

[RequireComponent(typeof(PlayerInput))]
[RequireComponent(typeof(CharacterController))]
public class MultiplayerPlayerController : MonoBehaviour
{

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 6f;
    [SerializeField] private float jumpForce = 5f;
    [SerializeField] private float gravity   = -18f;


    [Header("Ground Check")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float     groundCheckRadius = 0.2f;
    [SerializeField] private LayerMask groundLayer;


    [Header("Character Setup")]
    [SerializeField] private Transform  characterVisualSlot;
    [SerializeField] private GameObject p1CharacterPrefab;
    [SerializeField] private GameObject p2CharacterPrefab;


    [Header("Spawn Points")]
    [SerializeField] private Transform p1SpawnPoint;
    [SerializeField] private Transform p2SpawnPoint;

    // --- private ---
    private CharacterController cc;
    private PlayerInput playerInput;
    private Vector2 moveInput;
    private float verticalVelocity;
    private bool isGrounded;
    private bool jumpQueued;

    // --- public id ---
    public int PlayerID { get; private set; }

       public event Action OnLightAttackEvent;
    public event Action OnHeavyAttackEvent;
    public event Action OnReactionEvent;


    public bool BlockHeld { get; private set; }

    // -------------------------------------------------------

    private void Awake()
    {
        cc  = GetComponent<CharacterController>();
        playerInput = GetComponent<PlayerInput>();
    }

    private void OnEnable()
    {
        if (playerInput != null)
            PlayerID = playerInput.playerIndex + 1;

        Debug.Log($"[P{PlayerID}] OnEnable... PlayerID confirmed");
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

        cc.enabled = false;
        transform.position = sp.position;
        transform.rotation = sp.rotation;
        cc.enabled  = true;

       
    }

    private void SpawnCharacterVisual()
    {
        GameObject prefab = PlayerID == 1 ? p1CharacterPrefab : p2CharacterPrefab;

        if (prefab == null || characterVisualSlot == null)
        {
            // Debug.LogWarning($"[P{PlayerID}] character prefab or visual slot not assigned");
            return;
        }

        Instantiate(prefab, characterVisualSlot.position, characterVisualSlot.rotation, characterVisualSlot);
        Debug.Log($"[P{PlayerID}] spawned: {prefab.name}");
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
        if (ctx.started && isGrounded) jumpQueued = true;
    }

    public void OnLightAttack(InputAction.CallbackContext ctx)
    {
        if (ctx.started)
        {
            Debug.Log($"[P{PlayerID}] OnLightAttack pressed");
            OnLightAttackEvent?.Invoke();
        }
    }

    public void OnHeavyAttack(InputAction.CallbackContext ctx)
    {
        if (ctx.started)
        {
            Debug.Log($"[P{PlayerID}] OnHeavyAttack presseed");
            OnHeavyAttackEvent?.Invoke();
        }
    }

    public void OnBlock(InputAction.CallbackContext ctx)
    {
        if (ctx.started)  BlockHeld = true;
        if (ctx.canceled) BlockHeld = false;
    }

    public void OnReactionTrigger(InputAction.CallbackContext ctx)
    {
        if (ctx.started)
            OnReactionEvent?.Invoke();
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
    // gizmos 4 visualizingg
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