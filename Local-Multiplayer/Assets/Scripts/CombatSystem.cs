using System.Collections;
using UnityEngine;

public class CombatSystem : MonoBehaviour
{
    [Header("Damage Values")]
    [SerializeField] private float lightAttackDamage = 10f;
    [SerializeField] private float heavyAttackDamage = 20f;
    [SerializeField] private float blockDamageReduction = 0.5f;

    [Header("Cooldowns")]
    [SerializeField] private float lightAttackCooldown = 0.4f;
    [SerializeField] private float heavyAttackCooldown = 0.8f;

    [Header("Range")]
    [SerializeField] private float attackRange = 4.5f;

    [Header("Knockback")]
    [SerializeField] private float lightKnockbackForce = 4f;
    [SerializeField] private float heavyKnockbackForce = 8f;
    [SerializeField] private float knockbackUpAngle = 0.2f;
    [SerializeField] private float blockedKnockbackMult = 0.4f;

    [Header("Juice Settings")]
    [SerializeField] private float lightHitStopDuration = 0.04f;
    [SerializeField] private float heavyHitStopDuration = 0.09f;
    [SerializeField] private float lightShakeMagnitude = 0.9f;
    [SerializeField] private float heavyShakeMagnitude = 0.18f;
    [SerializeField] private float shakeDuration = 0.15f;

    [Header("Player Separation")]
    [SerializeField] private float minSeparationDistance = 1.2f;  
    [SerializeField] private float separationForce = 6f;     


   
    private PlayerHealth myHealth;
    private PlayerHealth opponentHealth;
    private CombatSystem opponentCombat;
    private MultiplayerPlayerController opponentController;
    private CombatVFX myVFX;
    private CombatVFX opponentVFX;
    private bool opponentLinked = false;

    // --- state ---
    public bool IsBlocking { get; private set; }
    public bool IsAttacking { get; private set; }
    private bool canLight = true;
    private bool canHeavy = true;
    private bool combatEnabled = true;
    private bool heavyEnabled = true;

    private MultiplayerPlayerController controller;

    private MultiplayerPlayerController MultiplayerScript;
    [SerializeField] AnimationManager animationScript;

    

    private void Awake()
    {
        controller = GetComponent<MultiplayerPlayerController>();
        myHealth = GetComponent<PlayerHealth>();
        myVFX = GetComponent<CombatVFX>();

        if (controller == null) Debug.LogError("[CombatSystem] " + gameObject.name + " no controller");
        if (myHealth == null)   Debug.LogError("[CombatSystem] " + gameObject.name + " no health");
    }

    private void Start()
    {
        controller.OnLightAttackEvent += HandleLightAttack;
        controller.OnHeavyAttackEvent += HandleHeavyAttack;
        TryFindOpponent();
        MultiplayerScript = GetComponent<MultiplayerPlayerController>();
        animationScript   = MultiplayerScript.animationScript;
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
        if (!opponentLinked) { TryFindOpponent(); return; }
        if (!combatEnabled) return;

        HandleBlock();
        HandleSeparation();  
    }



    private void TryFindOpponent()
    {
        var allCombat = FindObjectsByType<CombatSystem>(FindObjectsSortMode.None);
        foreach (var other in allCombat)
        {
            if (other == this) continue;
            opponentHealth = other.GetComponent<PlayerHealth>();
            opponentCombat  = other;
            opponentController = other.GetComponent<MultiplayerPlayerController>();
            opponentVFX   = other.GetComponent<CombatVFX>();
            opponentLinked = true;
            // Debug.Log("[P" + controller.PlayerID + " CombatSystem] opponent linked");
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



    private void HandleSeparation()
    {
        if (opponentController == null) return;

        float dist = Vector3.Distance(transform.position, opponentController.transform.position);
        if (dist >= minSeparationDistance || dist < 0.01f) return;

        Vector3 pushDir = (transform.position - opponentController.transform.position).normalized;
        pushDir.y = 0f;

        float strength = (1f - (dist / minSeparationDistance)) * separationForce;
        controller.ApplyKnockback(pushDir * strength * Time.deltaTime);
    }

    // -------------------------------------------------------
    // attacks
    // -------------------------------------------------------

    private void HandleLightAttack()
    {
        if (!combatEnabled || !opponentLinked) return;
        if (!canLight || IsBlocking) return;

        IsAttacking = true;
        StartCoroutine(PerformAttack(lightAttackDamage, lightKnockbackForce,
                                     lightAttackCooldown, lightHitStopDuration,
                                     lightShakeMagnitude, "LIGHT"));
        animationScript.PlayLightAttack();
    }

    private void HandleHeavyAttack()
    {
        if (!combatEnabled || !opponentLinked) return;
        if (!canHeavy || IsBlocking || !heavyEnabled) return;

        IsAttacking = true;
        StartCoroutine(PerformAttack(heavyAttackDamage, heavyKnockbackForce,
                                     heavyAttackCooldown, heavyHitStopDuration,
                                     heavyShakeMagnitude, "HEAVY"));
        animationScript.PlayHeavyAttack();
    }

    private IEnumerator PerformAttack(float damage, float knockbackForce, float cooldown,
                                      float hitStopDur, float shakeMag, string type)
    {
        if (type == "LIGHT") canLight = false;
        else canHeavy = false;

        Debug.Log("[P" + controller.PlayerID + "] " + type + " ATTACK");

        float dist = Vector3.Distance(transform.position, opponentHealth.transform.position);

        if (dist <= attackRange)
        {
            bool isBlocked     = opponentCombat != null && opponentCombat.IsBlocking;
            float finalDamage  = isBlocked ? damage * (1f - blockDamageReduction) : damage;
            float finalKnock   = isBlocked ? knockbackForce * blockedKnockbackMult : knockbackForce;

            opponentHealth.TakeDamage(finalDamage);

            Vector3 diff = opponentHealth.transform.position - transform.position;
            diff.y = 0f;
            Vector3 dir          = diff.magnitude > 0.01f ? diff.normalized : (transform.right * (controller.PlayerID == 1 ? 1f : -1f));
            Vector3 knockbackDir = (dir + Vector3.up * knockbackUpAngle).normalized;
            opponentController?.ApplyKnockback(knockbackDir * finalKnock);

            Vector3 impactPos = opponentHealth.transform.position;

            if (!isBlocked)
            {
                AudioManager.Instance?.Play(
                    type == "LIGHT" ? AudioManager.Instance.lightHit : AudioManager.Instance.heavyHit,
                    1f, 0.08f);

                if (type == "LIGHT") opponentVFX?.PlayLightHit(impactPos);
                else                 opponentVFX?.PlayHeavyHit(impactPos);

                HitStop.Instance?.Freeze(hitStopDur);
                CameraShake.Instance?.Shake(shakeDuration, shakeMag);

                Debug.Log("[P" + controller.PlayerID + "] " + type + " HIT");
            }
            else
            {
                opponentVFX?.PlayBlock(impactPos);
                AudioManager.Instance?.Play(AudioManager.Instance.lightHit, 0.5f, 0.05f);
                Debug.Log("[P" + controller.PlayerID + "] " + type + " BLOCKED");
            }
        }
        else
        {
            Debug.Log("[P" + controller.PlayerID + "] " + type + " miss");
        }

        yield return new WaitForSeconds(0.1f);
        IsAttacking = false;
        animationScript.PlayIdle();

        yield return new WaitForSeconds(cooldown - 0.1f);

        if (type == "LIGHT") canLight = true;
        else canHeavy = true;
    }

    // -------------------------------------------------------
    // enable / disable
    // -------------------------------------------------------

    public void SetCombatEnabled(bool enabled)
    {
        combatEnabled = enabled;
        if (!enabled)
        {
            StopAllCoroutines();
            IsBlocking = false;
            IsAttacking = false;
            canLight = true;
            canHeavy = true;
        }
    }

    public void SetHeavyAttackEnabled(bool enabled)
    {
        heavyEnabled = enabled;
        if (!enabled)
            Debug.Log("[P" + controller.PlayerID + " CombatSystem] heavy attack disabled... arm is gone");
    }

    // -------------------------------------------------------

    // private void OnDrawGizmosSelected()
    // {
    //     Gizmos.color = Color.purple;
    //     Gizmos.DrawWireSphere(transform.position, attackRange);

    //     
    //     Gizmos.color = Color.yellow;
    //     Gizmos.DrawWireSphere(transform.position, minSeparationDistance);
    // }
}