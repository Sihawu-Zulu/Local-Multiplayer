using System.Collections;
using UnityEngine;
using UnityEngine.Events;

// tracks health and fires events
// damage comes from two sources only: combat system hits, and string pulling during knockdown
// killer shot reaction no longer deals direct damage — it just triggers the knockdown phase

public class PlayerHealth : MonoBehaviour
{
    [Header("Health Settings")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float killerShotThreshold = 30f;   // % hp at which killer shot triggers

    [Header("Animation stuff")]
    public bool IsReacting
    { get; private set; }

    [Header("Debug / Live View")]
    [SerializeField, Range(0f, 100f)] private float currentHealth;

    // --- public state ---
    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;
    public bool IsDefeated { get; private set; }
    public bool KillerShotReady { get; private set; }

    // --- events ---
    public UnityEvent<float, float> OnHealthChanged;      // (currentHP, maxHP)
    public UnityEvent OnKillerShotTriggered;
    public UnityEvent OnPlayerDefeated;

    // -------------------------------------------------------



    //Animations----------------------------------------------
    private MultiplayerPlayerController MultiplayerScript;

    private void Start()
    {
        MultiplayerScript = GetComponent<MultiplayerPlayerController>();
    }
    private void Awake()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(float amount)
    {
        if (IsDefeated) return;
        StartCoroutine(HealthUpdateDelay(amount));

    }

    private IEnumerator HealthUpdateDelay(float amount)
    {
        yield return new WaitForSeconds(1f);


        IsReacting = true; // starts reacting

        currentHealth = Mathf.Max(0f, currentHealth - amount);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        MultiplayerScript.animationScript.PlayTakeDamage();

        //float animLength = 0f;
        MultiplayerScript.StartCoroutine(ResetReacting());

        float pct = (currentHealth / maxHealth) * 100f;

        // trigger killer shot phase once hp drops low enough — only fires once per life
        if (!KillerShotReady && pct <= killerShotThreshold)
        {
            KillerShotReady = true;
            OnKillerShotTriggered?.Invoke();

        }

        if (currentHealth <= 0f)
        {
            IsDefeated = true;
            OnPlayerDefeated?.Invoke();
            Debug.Log($"[{gameObject.name}] defeated");
        }


    }

    private IEnumerator ResetReacting()
    {
        yield return new WaitForSeconds(0f);
        IsReacting = false;

    }

    public void ResetHealth()
    {
        currentHealth = maxHealth;
        IsDefeated = false;
        KillerShotReady = false;
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }
}