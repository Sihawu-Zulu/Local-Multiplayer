using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;


// and a tug-of-war progress bar showing how close the arm is to detaching maybe for jaiden

public class KnockdownUI : MonoBehaviour
{
    [Header("Mash Prompt")]
    [SerializeField] private GameObject  mashRoot; 
    [SerializeField] private Image  mashIcon;          
    [SerializeField] private TextMeshProUGUI mashCountText;      

    [Header("Pulse Settings")]
    [SerializeField] private float pulseMin    = 0.85f;
    [SerializeField] private float pulseMax    = 1.2f;
    [SerializeField] private float pulseSpeed  = 4f;
    [SerializeField] private Color pulseColorA = new Color(1f, 0.9f, 0.2f);
    [SerializeField] private Color pulseColorB = new Color(1f, 0.3f, 0.1f); 

    [Header("Tug Of War Bar")]
    [SerializeField] private GameObject tugBarRoot;
    [SerializeField] private Slider     tugProgressSlider;       
    [SerializeField] private Image      tugFillImage;
    [SerializeField] private Color      tugColorSafe    = new Color(0.2f, 0.9f, 0.4f);
    [SerializeField] private Color      tugColorDanger  = new Color(0.9f, 0.15f, 0.1f);

    [Header("References")]
    [SerializeField] private KnockdownManager knockdownManager;

    // --- state ---
    private bool   isPulsing    = false;
    private int   totalMashes  = 15;    
    private Coroutine pulseRoutine;

    // -------------------------------------------------------

    private void Start()
    {
        HideAll();

        if (knockdownManager == null)
        {
           
            return;
        }

        knockdownManager.OnKnockdownStarted.AddListener(OnKnockdownStarted);
        knockdownManager.OnPlayerRecovered.AddListener(_ => HideAll());
        knockdownManager.OnArmDetached.AddListener(_ => HideAll());
        knockdownManager.OnTugProgressChanged.AddListener(UpdateTugBar);
        knockdownManager.OnMashProgress.AddListener(UpdateMashCount);
    }

    // ===================================

    private void OnKnockdownStarted(int downedPlayerID)
    {
        if (mashRoot    != null) mashRoot.SetActive(true);
        if (tugBarRoot  != null) tugBarRoot.SetActive(true);

        if (mashCountText != null) mashCountText.text = $"0 / {totalMashes}";
        if (tugProgressSlider != null) tugProgressSlider.value = 0f;

        isPulsing = true;
        if (pulseRoutine != null) StopCoroutine(pulseRoutine);
        pulseRoutine = StartCoroutine(PulseRoutine());
    }

    private void HideAll()
    {
        isPulsing = false;

        if (pulseRoutine != null)
        {
            StopCoroutine(pulseRoutine);
            pulseRoutine = null;
        }

        if (mashRoot   != null) { mashRoot.transform.localScale = Vector3.one; mashRoot.SetActive(false); }
        if (tugBarRoot != null) tugBarRoot.SetActive(false);
    }

    // -============================================================

    private void UpdateMashCount(int count)
    {
        if (mashCountText != null)
            mashCountText.text = $"{count} / {totalMashes}";
    }

    private void UpdateTugBar(float progress)
    {
        if (tugProgressSlider != null)
            tugProgressSlider.value = progress;

       
        if (tugFillImage != null)
            tugFillImage.color = Color.Lerp(tugColorSafe, tugColorDanger, progress);
    }

 

    private IEnumerator PulseRoutine()
    {
        float t = 0f;

        while (isPulsing)
        {
            t += Time.deltaTime * pulseSpeed;

            float ping  = (Mathf.Sin(t * Mathf.PI * 2f) + 1f) * 0.5f;
            float scale = Mathf.Lerp(pulseMin, pulseMax, ping);

            if (mashRoot != null)
                mashRoot.transform.localScale = Vector3.one * scale;

            if (mashIcon != null)
                mashIcon.color = Color.Lerp(pulseColorA, pulseColorB, ping);

            yield return null;
        }

        if (mashRoot != null)
            mashRoot.transform.localScale = Vector3.one;
    }
}