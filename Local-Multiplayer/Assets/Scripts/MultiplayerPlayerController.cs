using System;
using UnityEngine;
using UnityEngine.InputSystem;

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
    [SerializeField] private float groundCheckRadius = 0.2f;
    [SerializeField] private LayerMask groundLayer;

    [Header("Character Setup")]
    [SerializeField] private Transform  characterVisualSlot;
    [SerializeField] private GameObject p1CharacterPrefab;
    [SerializeField] private GameObject p2CharacterPrefab;

    [Header("Spawn Points")]
    [SerializeField] private Transform p1SpawnPoint;
    [SerializeField] private Transform p2SpawnPoint;

    [Header("Knockback")]
    [SerializeField] private float knockbackDecay = 10f;

    // --- private ---
    private CharacterController cc;
    private PlayerInput playerInput;
    private Vector2 moveInput;
    private float verticalVelocity;
    private bool isGrounded;
    private bool jumpQueued;
    private Vector3 knockbackVelocity;

    // --- public id ---
    public int PlayerID { get; private set; }

    public event Action OnLightAttackEvent;
    public event Action OnHeavyAttackEvent;
    public event Action OnReactionEvent;
    public event Action OnGetUpEvent;       // north button mash — recover from knockdown

    private bool movementEnabled = false;
    public bool BlockHeld { get; private set; }

    // -------------------------------------------------------

    private void Awake()
    {
        cc          = GetComponent<CharacterController>();
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

        if (movementEnabled)
        {
            HandleJump();
            MoveCharacter();
        }
        else if (knockbackVelocity != Vector3.zero)
        {
            cc.Move((Vector3.up * verticalVelocity + knockbackVelocity) * Time.deltaTime);
        }

        ApplyKnockbackDecay();
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
        cc.enabled = true;
    }

    private void SpawnCharacterVisual()
    {
        GameObject prefab = PlayerID == 1 ? p1CharacterPrefab : p2CharacterPrefab;

        if (prefab == null || characterVisualSlot == null)
            return;

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
            Debug.Log($"[P{PlayerID}] OnHeavyAttack pressed");
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

    // north button — mash while downed to get back up
    public void OnGetUp(InputAction.CallbackContext ctx)
    {
        if (ctx.started)
            OnGetUpEvent?.Invoke();
    }

    // lets knockdownmanager grab the visual slot to rotate/tumble it
    public Transform GetVisualSlot() => characterVisualSlot;

    // voodoo physics layer reads these each frame
    public Vector3 GetMoveDirection() => new Vector3(moveInput.x, 0f, 0f).normalized;
    public bool    IsGrounded         => isGrounded;

    // -------------------------------------------------------
    // movement
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

        Vector3 finalMove = horizontal * moveSpeed + Vector3.up * verticalVelocity + knockbackVelocity;
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
    // knockback
    // -------------------------------------------------------

    public void ApplyKnockback(Vector3 force)
    {
        knockbackVelocity += force;
    }

    private void ApplyKnockbackDecay()
    {
        if (knockbackVelocity == Vector3.zero) return;

        knockbackVelocity = Vector3.MoveTowards(knockbackVelocity, Vector3.zero,
                                                knockbackDecay * Time.deltaTime);
    }

    // -------------------------------------------------------
    // enable / disable
    // -------------------------------------------------------

    public void SetMovementEnabled(bool enabled)
    {
        movementEnabled = enabled;

        if (!enabled)
        {
            moveInput        = Vector2.zero;
            verticalVelocity = 0f;
            jumpQueued       = false;
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