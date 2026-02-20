using UnityEngine;
using UnityEngine.Events;

// tracks health, fires events 


public class PlayerHealth : MonoBehaviour
{
    [Header("Health Settings")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float killerShotThreshold = 30f;   
    [SerializeField] private float killerShotDamage    = 60f; 

   
    [Header("Debug / Live View")]
    [SerializeField, Range(0f, 100f)] private float currentHealth;

    // --- public state ---
    public float CurrentHealth   => currentHealth;
    public float MaxHealth       => maxHealth;
    public bool  IsDefeated      { get; private set; }
    public bool  KillerShotReady { get; private set; }

    // --- events ---
    public UnityEvent<float, float> OnHealthChanged;        // (currentHP, maxHP)
    public UnityEvent OnKillerShotTriggered;  
    public UnityEvent OnPlayerDefeated;       // fires when HP hits 0

    // -------------------------------------------------------

    private void Awake()
    {
        currentHealth = maxHealth;
    }


    public void TakeDamage(float amount)
    {
        if (IsDefeated) return;

        currentHealth = Mathf.Max(0f, currentHealth - amount);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        float pct = (currentHealth / maxHealth) * 100f;

        if (!KillerShotReady && pct <= killerShotThreshold)
        {
            KillerShotReady = true;
            OnKillerShotTriggered?.Invoke();
            
        }

        if (currentHealth <= 0f)
        {
            IsDefeated = true;
            OnPlayerDefeated?.Invoke();
            Debug.Log($"[{gameObject.name}] DEFEATED");
        }
    }

    public void TakeKillerShotDamage()
    {
        TakeDamage(killerShotDamage);
    }

    public void ResetHealth()
    {
        currentHealth   = maxHealth;
        IsDefeated      = false;
        KillerShotReady = false;
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    
    }
}