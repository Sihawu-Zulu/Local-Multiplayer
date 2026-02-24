using System.Collections;
using UnityEngine;

public class CombatSystem : MonoBehaviour
{
    [Header("Damage Values")]
    [SerializeField] private float lightAttackDamage  = 10f;
    [SerializeField] private float heavyAttackDamage    = 20f;
    [SerializeField] private float blockDamageReduction = 0.5f;

    [Header("Cooldowns")]
    [SerializeField] private float lightAttackCooldown = 0.4f;
    [SerializeField] private float heavyAttackCooldown = 0.8f;

    [Header("Range")]
    [SerializeField] private float attackRange = 4.5f;

    [Header("Knockback")]
    [SerializeField] private float lightKnockbackForce  = 4f;
    [SerializeField] private float heavyKnockbackForce = 8f;
    [SerializeField] private float knockbackUpAngle= 0.2f;   
    [SerializeField] private float blockedKnockbackMult = 0.4f; 

    // --- resolved at Start / Update ---
    private PlayerHealth myHealth;
    private PlayerHealth opponentHealth;
    private CombatSystem opponentCombat;
    private MultiplayerPlayerController opponentController;
    private bool opponentLinked = false;

    // --- statez ---
    public  bool IsBlocking   { get; private set; }
    public  bool IsAttacking  { get; private set; }
    private bool canLight = true;
    private bool canHeavy = true;
    private bool combatEnabled = true;

    private MultiplayerPlayerController controller;

    // -------------------------------------------------------

    private void Awake()
    {
        controller = GetComponent<MultiplayerPlayerController>();
        myHealth   = GetComponent<PlayerHealth>();

        if (controller == null) Debug.LogError($"[CombatSystem] {gameObject.name} — MultiplayerPlayerController noy there");
        if (myHealth   == null) Debug.LogError($"[CombatSystem] {gameObject.name} — PlayerHealth noy tjere");
    }

    private void Start()
    {
        controller.OnLightAttackEvent += HandleLightAttack;
        controller.OnHeavyAttackEvent += HandleHeavyAttack;
        TryFindOpponent();
    }

    private void OnDestroy()
    {
        if (controller != null)
        {
            controller.OnLightAttackEvent -= HandleLightAttack;
            controller.OnHeavyAttackEvent -= HandleHeavyAttack;
        }
    }

    private void Update()
    {
        if (!opponentLinked)
        {
            TryFindOpponent();
            return;
        }

        if (!combatEnabled) return;

        HandleBlock();
    }

    // -------------------------------------------------------
    // opponent 
    // -------------------------------------------------------

    private void TryFindOpponent()
    {
        var allCombat = FindObjectsByType<CombatSystem>(FindObjectsSortMode.None);

        foreach (var other in allCombat)
        {
            if (other == this) continue;
            opponentHealth = other.GetComponent<PlayerHealth>();
            opponentCombat  = other;
            opponentController = other.GetComponent<MultiplayerPlayerController>();
            opponentLinked  = true;
            Debug.Log($"[P{controller.PlayerID} CombatSystem] opponent linked... ready");
            break;
        }
    }

    // -------------------------------------------------------
    // block
    // -------------------------------------------------------

    private void HandleBlock()
    {
        IsBlocking = controller.BlockHeld && !IsAttacking;
    }

    // -------------------------------------------------------
    // attacks
    // -------------------------------------------------------

    private void HandleLightAttack()
    {
        if (!combatEnabled || !opponentLinked) return;
        if (!canLight || IsBlocking) return;
        StartCoroutine(PerformAttack(lightAttackDamage, lightKnockbackForce, lightAttackCooldown, "LIGHT"));
    }

    private void HandleHeavyAttack()
    {
        if (!combatEnabled || !opponentLinked) return;
        if (!canHeavy || IsBlocking) return;
        StartCoroutine(PerformAttack(heavyAttackDamage, heavyKnockbackForce, heavyAttackCooldown, "HEAVY"));
    }

    private IEnumerator PerformAttack(float damage, float knockbackForce, float cooldown, string type)
    {
        IsAttacking = true;

        if (type == "LIGHT") canLight = false;
        else                
        canHeavy = false;

        Debug.Log($"[P{controller.PlayerID}] {type} ATTACK");

        float dist = Vector3.Distance(transform.position, opponentHealth.transform.position);

        if (dist <= attackRange)
        {
            bool isBlocked = opponentCombat != null && opponentCombat.IsBlocking;

            float finalDamage    = isBlocked ? damage * (1f - blockDamageReduction) : damage;
            float finalKnockback = isBlocked ? knockbackForce * blockedKnockbackMult : knockbackForce;

            opponentHealth.TakeDamage(finalDamage);

            // knockback direction.... away from attacker, with slight upward angle
            Vector3 diff = opponentHealth.transform.position - transform.position;
            diff.y = 0f;

            // fallback to facing direction if players are exactly overlapping
            Vector3 dir = diff.magnitude > 0.01f
                ? diff.normalized
                : (transform.right * (controller.PlayerID == 1 ? 1f : -1f));

            Vector3 knockbackDir = (dir + Vector3.up * knockbackUpAngle).normalized;
            opponentController?.ApplyKnockback(knockbackDir * finalKnockback);

            if (isBlocked)
                Debug.Log($"[P{controller.PlayerID}] {type} BLOCKED");
            else
                Debug.Log($"[P{controller.PlayerID}] {type} hit");
        }
        else
        {
            Debug.Log($"[P{controller.PlayerID}] {type} ");
        }

        yield return new WaitForSeconds(0.1f);
        IsAttacking = false;

        yield return new WaitForSeconds(cooldown - 0.1f);

        if (type == "LIGHT") canLight = true;
        else                
        canHeavy = true;
    }

    // -------------------------------------------------------

    public void SetCombatEnabled(bool enabled)
    {
        combatEnabled = enabled;

        if (!enabled)
        {
            IsBlocking  = false;
            IsAttacking = false;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.purple;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}