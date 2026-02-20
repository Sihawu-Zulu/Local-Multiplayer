using System.Collections;
using UnityEngine;

// subscribes to controller's C# events directly — no frame flag timing issues
// bypassRangeCheck = true so attacks always land until animations/hitboxes are ready by @sihawu & @jaiden

public class CombatSystem : MonoBehaviour
{
    [Header("Damage Values")]
    [SerializeField] private float lightAttackDamage = 10f;
    [SerializeField] private float heavyAttackDamage  = 20f;
    [SerializeField] private float blockDamageReduction = 0.5f;

    [Header("Cooldowns")]
    [SerializeField] private float lightAttackCooldown = 0.4f;
    [SerializeField] private float heavyAttackCooldown = 0.8f;

    [Header("Range")]
    [SerializeField] private float attackRange      = 2.5f;
    [SerializeField] private bool  bypassRangeCheck = true;   


    private PlayerHealth myHealth;
    private PlayerHealth opponentHealth;
    private CombatSystem opponentCombat;
    private bool opponentLinked = false;

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
        // always unsubscribe to avoid memory leaks lolll
        if (controller != null)
        {
            controller.OnLightAttackEvent -= HandleLightAttack;
            controller.OnHeavyAttackEvent -= HandleHeavyAttack;
        }
    }

    private void Update()
    {
        // retry opponent link if not found yet — this allows for flexible scene setup and ensures we don't miss the link if one player spawns slightly after the other for any reason
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

           
            break;
        }
    }

    // -------------------------------------------------------
    // block — still checked in Update since it's a held state
    // -------------------------------------------------------

    private void HandleBlock()
    {
        IsBlocking = controller.BlockHeld && !IsAttacking;

        // will put for animator.SetBool("IsBlocking", IsBlocking) 
        if (IsBlocking)
            Debug.Log($"[P{controller.PlayerID}] BLOCKING");
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

        // will use for animator.SetTrigger("LightAttack") / ("HeavyAttack") 
        Debug.Log($"[P{controller.PlayerID}] {type} ATTACK");

        bool inRange = bypassRangeCheck || IsInRange();

        if (inRange)
        {
            float final = damage;

            if (opponentCombat != null && opponentCombat.IsBlocking)
            {
                final *= (1f - blockDamageReduction);
              
            }

            opponentHealth.TakeDamage(final);
            // Debug.Log($"[P{controller.PlayerID}] {type} dealt {final} — opponent HP: {opponentHealth.CurrentHealth}/{opponentHealth.MaxHealth}");
        }
        else
        {
            float dist = Vector3.Distance(transform.position, opponentHealth.transform.position);
           
        }

        // 0.1s active frames placeholder — replace with animation event later
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

    // called by KillerShotManager during reaction window
    public void SetCombatEnabled(bool enabled)
    {
        combatEnabled = enabled;

        if (!enabled)
        {
            IsBlocking  = false;
            IsAttacking = false;
        }

        // Debug.Log($"[P{controller.PlayerID}] combat {(enabled ? "ENABLED" : "DISABLED")}");
    }
}