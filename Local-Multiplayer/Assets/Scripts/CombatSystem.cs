using System.Collections;
using UnityEngine;

// sits on Player ROOT prefab — same object as MultiplayerPlayerController and PlayerHealth
// finds its opponent automatically via PlayerID at Start — no runtime SetOpponent() needed
// this fixes the build issue where dynamic assignment wouldn't serialize

public class CombatSystem : MonoBehaviour
{
    [Header("Damage Values")]
    [SerializeField] private float lightAttackDamage    = 10f;
    [SerializeField] private float heavyAttackDamage    = 20f;
    [SerializeField] private float blockDamageReduction = 0.5f;

    [Header("Cooldowns")]
    [SerializeField] private float lightAttackCooldown = 0.4f;
    [SerializeField] private float heavyAttackCooldown = 0.8f;

    [Header("Range")]
    [SerializeField] private float attackRange = 2.5f;

    // --- resolved at Start via PlayerID ---
    private PlayerHealth   myHealth;
    private PlayerHealth   opponentHealth;
    private CombatSystem   opponentCombat;

    // --- state ---
    public  bool IsBlocking   { get; private set; }
    public  bool IsAttacking  { get; private set; }
    private bool canLight     = true;
    private bool canHeavy     = true;
    private bool combatEnabled = true;

    private MultiplayerPlayerController controller;

    // -------------------------------------------------------

    private void Awake()
    {
        controller = GetComponent<MultiplayerPlayerController>();
        myHealth   = GetComponent<PlayerHealth>();

        if (controller == null) Debug.LogError($"[CombatSystem] {gameObject.name} — missing MultiplayerPlayerController on same object");
        if (myHealth   == null) Debug.LogError($"[CombatSystem] {gameObject.name} — missing PlayerHealth on same object");
    }

    private void Start()
    {
        // find opponent by searching for all CombatSystems and picking the one with a different PlayerID
        // works reliably as long as both players have spawned before Start runs
        // PlayerInputManager spawns both before Start fires on either, so this is safe
        var allCombat = FindObjectsByType<CombatSystem>(FindObjectsSortMode.None);

        foreach (var other in allCombat)
        {
            if (other == this) continue;

            opponentHealth = other.GetComponent<PlayerHealth>();
            opponentCombat = other;
            break;
        }

        if (opponentHealth == null)
            Debug.LogWarning($"[P{controller.PlayerID} CombatSystem] opponent not found at Start — if only one player has spawned yet this is expected, will resolve when P2 joins");
        else
            Debug.Log($"[P{controller.PlayerID} CombatSystem] opponent linked at Start — combat ready");
    }

    private void Update()
    {
        // lazy re-resolve in case opponent spawned after this player's Start
        if (opponentHealth == null)
            TryFindOpponent();

        if (!combatEnabled) return;

        HandleBlock();
        HandleLightAttack();
        HandleHeavyAttack();
    }

    // -------------------------------------------------------
    // block
    // -------------------------------------------------------

    private void HandleBlock()
    {
        IsBlocking = controller.BlockHeld && !IsAttacking;

        // swap for animator.SetBool("IsBlocking", IsBlocking) when animations ready
        if (IsBlocking)
            Debug.Log($"[P{controller.PlayerID}] BLOCKING");
    }

    // -------------------------------------------------------
    // attacks
    // -------------------------------------------------------

    private void HandleLightAttack()
    {
        if (!controller.LightAttackPressed || !canLight || IsBlocking) return;
        StartCoroutine(PerformAttack(lightAttackDamage, lightAttackCooldown, "LIGHT"));
    }

    private void HandleHeavyAttack()
    {
        if (!controller.HeavyAttackPressed || !canHeavy || IsBlocking) return;
        StartCoroutine(PerformAttack(heavyAttackDamage, heavyAttackCooldown, "HEAVY"));
    }

    private IEnumerator PerformAttack(float damage, float cooldown, string type)
    {
        IsAttacking = true;

        if (type == "LIGHT") canLight = false;
        else                  canHeavy = false;

        // swap for animator.SetTrigger("LightAttack") / ("HeavyAttack") when animations ready
        Debug.Log($"[P{controller.PlayerID}] {type} ATTACK");

        if (opponentHealth != null && IsInRange())
        {
            float final = damage;

            if (opponentCombat != null && opponentCombat.IsBlocking)
            {
                final *= (1f - blockDamageReduction);
                Debug.Log($"[P{controller.PlayerID}] {type} BLOCKED — reduced to {final}");
            }

            opponentHealth.TakeDamage(final);
            Debug.Log($"[P{controller.PlayerID}] {type} hit for {final} — opponent HP: {opponentHealth.CurrentHealth}");
        }
        else if (opponentHealth == null)
        {
            Debug.LogWarning($"[P{controller.PlayerID}] {type} — no opponent found yet");
        }
        else
        {
            float dist = Vector3.Distance(transform.position, opponentHealth.transform.position);
            Debug.Log($"[P{controller.PlayerID}] {type} whiffed — dist {dist:F2} > range {attackRange}");
        }

        // 0.1s active frames — replace with animation event later
        yield return new WaitForSeconds(0.1f);
        IsAttacking = false;

        yield return new WaitForSeconds(cooldown - 0.1f);

        if (type == "LIGHT") canLight = true;
        else                  canHeavy = true;
    }

    // -------------------------------------------------------
    // helpers
    // -------------------------------------------------------

    private bool IsInRange()
    {
        if (opponentHealth == null) return false;
        return Vector3.Distance(transform.position, opponentHealth.transform.position) <= attackRange;
    }

    // called each Update frame until opponent is found — handles late spawn case
    private void TryFindOpponent()
    {
        var allCombat = FindObjectsByType<CombatSystem>(FindObjectsSortMode.None);

        foreach (var other in allCombat)
        {
            if (other == this) continue;
            opponentHealth = other.GetComponent<PlayerHealth>();
            opponentCombat = other;
            Debug.Log($"[P{controller.PlayerID} CombatSystem] opponent found via Update — combat ready");
            break;
        }
    }

    // called by KillerShotManager during reaction window
    public void SetCombatEnabled(bool enabled)
    {
        combatEnabled = enabled;

        if (!enabled)
        {
            IsBlocking  = false;
            IsAttacking = false;
        }

        Debug.Log($"[P{controller.PlayerID}] combat {(enabled ? "ENABLED" : "DISABLED")}");
    }
}