using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// shows pulsing reaction prompt during killer shot window
// pulse speeds up and turns red as time runs out
// "TOO EARLY!" and "PERFECT!" popups per player side... need sprites for those? or maybe nice font

public class KillerShotPromptUI : MonoBehaviour
{
    [Header("Main Prompt")]
    [SerializeField] private GameObject promptRoot;     
    [SerializeField] private TextMeshProUGUI promptText;   
    [SerializeField] private Image promptBackground;     

    [Header("Pulse Settings")]
    [SerializeField] private float pulseMinScale   = 0.92f;
    [SerializeField] private float pulseMaxScale   = 1.08f;
    [SerializeField] private float pulseSpeedBase  = 1.5f;       
    [SerializeField] private float pulseSpeedMax   = 5f;     
    [SerializeField] private Color promptColorNormal = new Color(1f, 0.85f, 0f);   
    [SerializeField] private Color promptColorUrgent = new Color(1f, 0.2f, 0.1f);  

    [Header("Feedback Popups")]
    [SerializeField] private TextMeshProUGUI p1FeedbackText;       
    [SerializeField] private TextMeshProUGUI p2FeedbackText;        // right side feedback
    [SerializeField] private float           feedbackDuration = 1.2f;

    [Header("References")]
    [SerializeField] private KillerShotManager killerShotManager;  

    // --- statess n stuff---
    private bool isPulsing = false;
    private Coroutine pulseCoroutine;
    private Coroutine p1FeedbackCoroutine;
    private Coroutine p2FeedbackCoroutine;



    private void Start()
    {
        if (promptRoot != null) promptRoot.SetActive(false);
        if (p1FeedbackText != null) p1FeedbackText.gameObject.SetActive(false);
        if (p2FeedbackText != null) p2FeedbackText.gameObject.SetActive(false);

        if (killerShotManager == null)
        {
            // Debug.LogError("[KillerShotPromptUI] KillerShotManager not assigned in inspector");
            return;
        }

        killerShotManager.OnKillerShotPhaseStarted.AddListener(ShowPrompt);
        killerShotManager.OnKillerShotPhaseEnded.AddListener(HidePrompt);
        killerShotManager.OnEarlyPress.AddListener(ShowEarlyPress);
        killerShotManager.OnPerfectPress.AddListener(ShowPerfect);
    }

    // -------------------------------------------------------
    // show / hide
    // -------------------------------------------------------

    private void ShowPrompt()
    {
        if (promptRoot != null) promptRoot.SetActive(true);

        isPulsing = true;
        if (pulseCoroutine != null) StopCoroutine(pulseCoroutine);
        pulseCoroutine = StartCoroutine(PulseRoutine());
    }

    private void HidePrompt()
    {
        isPulsing = false;

        if (pulseCoroutine != null)
        {
            StopCoroutine(pulseCoroutine);
            pulseCoroutine = null;
        }

        if (promptRoot != null)
        {
            promptRoot.transform.localScale = Vector3.one;
            promptRoot.SetActive(false);
        }
    }

    // -------------------------------------------------------
    // pulse routine — faster n redder as time runs out
    // -------------------------------------------------------

    private IEnumerator PulseRoutine()
    {
        float t = 0f;

        while (isPulsing)
        {
            float timeRatio = (killerShotManager.GetWindowDuration() > 0f)
                ? Mathf.Clamp01(killerShotManager.GetWindowTimeRemaining() / killerShotManager.GetWindowDuration())
                : 1f;

            float speed = Mathf.Lerp(pulseSpeedMax, pulseSpeedBase, timeRatio);

            if (promptBackground != null)
                promptBackground.color = Color.Lerp(promptColorUrgent, promptColorNormal, timeRatio);

            t += Time.deltaTime * speed;
            float scale = Mathf.Lerp(pulseMinScale, pulseMaxScale, (Mathf.Sin(t * Mathf.PI * 2f) + 1f) * 0.5f);

            if (promptRoot != null)
                promptRoot.transform.localScale = Vector3.one * scale;

            yield return null;
        }

        if (promptRoot != null)
            promptRoot.transform.localScale = Vector3.one;
    }

    // -------------------------------------------------------
    // feedback popups
    // -------------------------------------------------------

    private void ShowEarlyPress(int playerID)  => ShowFeedback(playerID, "TOO EARLY!", Color.red);
    private void ShowPerfect(int playerID)     => ShowFeedback(playerID, "PERFECT!", new Color(0.2f, 1f, 0.4f));

    private void ShowFeedback(int playerID, string message, Color color)
    {
        TextMeshProUGUI target = playerID == 1 ? p1FeedbackText : p2FeedbackText;
        if (target == null) return;

        if (playerID == 1 && p1FeedbackCoroutine != null) StopCoroutine(p1FeedbackCoroutine);
        if (playerID == 2 && p2FeedbackCoroutine != null) StopCoroutine(p2FeedbackCoroutine);

        target.text  = message;
        target.color = color;
        target.gameObject.SetActive(true);

        var r = StartCoroutine(FadeOutFeedback(target));
        if (playerID == 1) p1FeedbackCoroutine = r;
        else              
         p2FeedbackCoroutine = r;
    }

    private IEnumerator FadeOutFeedback(TextMeshProUGUI text)
    {
        yield return new WaitForSeconds(feedbackDuration - 0.3f);

        float elapsed = 0f;
        Color start   = text.color;

        while (elapsed < 0.3f)
        {
            elapsed   += Time.deltaTime;
            text.color = new Color(start.r, start.g, start.b, Mathf.Lerp(1f, 0f, elapsed / 0.3f));
            yield return null;
        }

        text.gameObject.SetActive(false);
    }
}