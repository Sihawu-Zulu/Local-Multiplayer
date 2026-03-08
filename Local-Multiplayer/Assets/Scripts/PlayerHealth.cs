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

    [Header("Reaction Settings")]
    [SerializeField] private float reactDuration = 0.5f;        // how long IsReacting stays true — match your TakeDamage anim length

    [Header("Animation stuff")]
    public bool IsReacting { get; private set; }

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

        // FIX: apply damage immediately — no 1 second delay
        // health bar, vfx, and hitstop all land on the same frame as the hit
        currentHealth = Mathf.Max(0f, currentHealth - amount);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        // play react animation and hold IsReacting for the duration of the clip
        if (reactCoroutine != null) StopCoroutine(reactCoroutine);
        reactCoroutine = StartCoroutine(ReactRoutine());

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

    private IEnumerator ReactRoutine()
    {
        IsReacting = true;
        MultiplayerScript.animationScript.PlayTakeDamage();

        // FIX: hold IsReacting for the actual animation duration
        // previously this was WaitForSeconds(0f) which reset instantly,
        // meaning movement/idle animations immediately clobbered the hit reaction
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