using System.Collections;
using UnityEngine;
using TMPro;

// listens to CountdownManager events and animates the countdown text
// punch scale on each step, colour changes for "FIGHT!"

public class CountdownUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CountdownManager countdownManager;
    [SerializeField] private GameObject       countdownRoot;     // root panel to show/hide
    [SerializeField] private TextMeshProUGUI  countdownText;

    [Header("Colours")]
    [SerializeField] private Color colourReady  = new Color(1f, 1f, 1f, 1f);
    [SerializeField] private Color colourNumber = new Color(1f, 0.85f, 0.1f, 1f);
    [SerializeField] private Color colourFight  = new Color(0.2f, 1f, 0.4f, 1f);

    [Header("Punch Animation")]
    [SerializeField] private float punchScale    = 1.4f;    // scale it punches TO
    [SerializeField] private float punchDuration = 0.12f;   // time to reach peak
    [SerializeField] private float shrinkDuration = 0.18f;  // time to settle back to 1

    [Header("Fight! Scale")]
    [SerializeField] private float fightPunchScale = 1.8f;

    // -------------------------------------------------------

    private void Start()
    {
        if (countdownRoot != null) countdownRoot.SetActive(false);

        if (countdownManager == null)
        {
            Debug.LogWarning("[CountdownUI] CountdownManager not assigned in inspector");
            return;
        }

        countdownManager.OnCountdownStarted.AddListener(ShowPanel);
        countdownManager.OnCountdownStep.AddListener(DisplayStep);
        countdownManager.OnCountdownFinished.AddListener(HidePanel);

        Debug.Log($"[CountdownUI] countdownManager assigned: {countdownManager != null}");
    }

    // -------------------------------------------------------

    private void ShowPanel()
    {
        if (countdownRoot != null) countdownRoot.SetActive(true);
    }

    private void HidePanel()
    {
        if (countdownRoot != null) countdownRoot.SetActive(false);
    }

    private void DisplayStep(string value)
    {
        if (countdownText == null) return;

        StopAllCoroutines();

        countdownText.text = value;

        // colour
        if (value == "Ready?")       countdownText.color = colourReady;
        else if (value == "FIGHT!")  countdownText.color = colourFight;
        else                         countdownText.color = colourNumber;

        float targetScale = value == "FIGHT!" ? fightPunchScale : punchScale;
        StartCoroutine(PunchScale(targetScale));
    }

    // -------------------------------------------------------

    private IEnumerator PunchScale(float target)
    {
        Transform t = countdownText.transform;

        // punch up
        float elapsed = 0f;
        while (elapsed < punchDuration)
        {
            elapsed += Time.deltaTime;
            float s  = Mathf.Lerp(1f, target, elapsed / punchDuration);
            t.localScale = Vector3.one * s;
            yield return null;
        }

        // shrink back
        elapsed = 0f;
        while (elapsed < shrinkDuration)
        {
            elapsed += Time.deltaTime;
            float s  = Mathf.Lerp(target, 1f, elapsed / shrinkDuration);
            t.localScale = Vector3.one * s;
            yield return null;
        }

        t.localScale = Vector3.one;
    }
}
