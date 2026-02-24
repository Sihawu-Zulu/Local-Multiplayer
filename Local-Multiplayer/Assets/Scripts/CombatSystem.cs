using System.Collections;
using UnityEngine;

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
    [SerializeField] private float attackRange = 4.5f;      // generous but requires players to actually be near each other

    private PlayerHealth myHealth;
    private PlayerHealth opponentHealth;
    private CombatSystem opponentCombat;
    private bool opponentLinked = false;


    public  bool IsBlocking   { get; private set; }
    public  bool IsAttacking  { get; private set; }
    private bool canLight     = true;
    private bool canHeavy     = true;
    private bool combatEnabled = true;

    private MultiplayerPlayerController controller;

    

    private void Awake()
    {
        controller = GetComponent<MultiplayerPlayerController>();
        myHealth   = GetComponent<PlayerHealth>();

        if (controller == null) Debug.LogError($"[CombatSystem] {gameObject.name} — MultiplayerPlayerController missing");
        if (myHealth   == null) Debug.LogError($"[CombatSystem] {gameObject.name} — PlayerHealth missing");
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
        // retry opponent link if not found yet
        if (!opponentLinked)
        {
            TryFindOpponent();
            return;
        }

        if (!combatEnabled) return;

        HandleBlock();
    }

    // -------------------------------------------------------
    // opponent resolution
    // -------------------------------------------------------

    private void TryFindOpponent()
    {
        var allCombat = FindObjectsByType<CombatSystem>(FindObjectsSortMode.None);

        foreach (var other in allCombat)
        {
            if (other == this) continue;
            opponentHealth = other.GetComponent<PlayerHealth>();
            opponentCombat = other;
            opponentLinked = true;
            Debug.Log($"[P{controller.PlayerID} CombatSystem] opponent linked — ready");
            break;
        }
    }

    // -------------------------------------------------------
    // block
    // -------------------------------------------------------

    private void HandleBlock()
    {
        IsBlocking = controller.BlockHeld && !IsAttacking;
        //will swap for animator.SetBool("IsBlocking", IsBlocking) when animations ready 
        //@sihawu
    }

    // -------------------------------------------------------
    // attacks — called directly from C# event, not Update
    // -------------------------------------------------------

    private void HandleLightAttack()
    {
        if (!combatEnabled || !opponentLinked) return;
        if (!canLight || IsBlocking) return;
        StartCoroutine(PerformAttack(lightAttackDamage, lightAttackCooldown, "LIGHT"));
    }

    private void HandleHeavyAttack()
    {
        if (!combatEnabled || !opponentLinked) return;
        if (!canHeavy || IsBlocking) return;
        StartCoroutine(PerformAttack(heavyAttackDamage, heavyAttackCooldown, "HEAVY"));
    }

    private IEnumerator PerformAttack(float damage, float cooldown, string type)
    {
        IsAttacking = true;

        if (type == "LIGHT") canLight = false;
        else                  canHeavy = false;

        // swap for animator.SetTrigger("LightAttack") / ("HeavyAttack") when animations ready
        Debug.Log($"[P{controller.PlayerID}] {type} ATTACK");

        float dist = Vector3.Distance(transform.position, opponentHealth.transform.position);

        if (dist <= attackRange)
        {
            float final = damage;

            if (opponentCombat != null && opponentCombat.IsBlocking)
            {
                final *= (1f - blockDamageReduction);
                Debug.Log($"[P{controller.PlayerID}] {type} BLOCKED — reduced to {final}");
            }

            opponentHealth.TakeDamage(final);
            Debug.Log($"[P{controller.PlayerID}] {type} hit for {final} — opponent HP: {opponentHealth.CurrentHealth}/{opponentHealth.MaxHealth}");
        }
        else
        {
            Debug.Log($"[P{controller.PlayerID}] {type} whiffed — dist {dist:F1} > range {attackRange}");
        }

        // 0.1s active frames — replace with animation event later
        yield return new WaitForSeconds(0.1f);
        IsAttacking = false;

        yield return new WaitForSeconds(cooldown - 0.1f);

        if (type == "LIGHT") canLight = true;
        else                  canHeavy = true;
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
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.purple;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}