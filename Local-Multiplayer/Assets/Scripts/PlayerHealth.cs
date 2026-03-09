using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health Settings")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float killerShotThreshold = 30f;  

    [Header("Reaction Settings")]
    [SerializeField] private float reactDuration = 0.5f;      

    [Header("Animation stuff")]
    public bool IsReacting { get; private set; }

    [Header("Debug / Live View")]
    [SerializeField, Range(0f, 100f)] private float currentHealth;


    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;
    public bool IsDefeated { get; private set; }
    public bool KillerShotReady { get; private set; }

  
    public UnityEvent<float, float> OnHealthChanged;      
    public UnityEvent OnKillerShotTriggered;
    public UnityEvent OnPlayerDefeated;

  

    private MultiplayerPlayerController MultiplayerScript;
    private Coroutine reactCoroutine;

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

        
        currentHealth = Mathf.Max(0f, currentHealth - amount);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);

      
        if (reactCoroutine != null) StopCoroutine(reactCoroutine);
        reactCoroutine = StartCoroutine(ReactRoutine());

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
            Debug.Log($"[{gameObject.name}] defeated");

            HitStop.Instance?. Freeze(0.18f);
            CameraShake.Instance?. Shake(0.4f, 0.25f);
        }
    }

    private IEnumerator ReactRoutine()
    {
        IsReacting = true;
        MultiplayerScript.animationScript.PlayTakeDamage();

  
        yield return new WaitForSeconds(reactDuration);

        IsReacting = false;
    }

    public void ResetHealth()
    {
        currentHealth = maxHealth;
        IsDefeated = false;
        KillerShotReady = false;
        IsReacting = false;
        if (reactCoroutine != null) { StopCoroutine(reactCoroutine); reactCoroutine = null; }
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }
}