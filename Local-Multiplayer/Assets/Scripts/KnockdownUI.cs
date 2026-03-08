using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;


public class KnockdownUI : MonoBehaviour
{

    [Header("P1 Side - left")]
    [SerializeField] private GameObject p1MashRoot;
    [SerializeField] private Image p1MashIcon;
    [SerializeField] private TextMeshProUGUI p1MashCountText;
    [SerializeField] private GameObject p1HoldRoot;
    [SerializeField] private Image p1HoldIcon;
    [SerializeField] private TextMeshProUGUI p1HoldText;


    [Header("P2 Side - right")]
    [SerializeField] private GameObject p2MashRoot;
    [SerializeField] private Image  p2MashIcon;
    [SerializeField] private TextMeshProUGUI p2MashCountText;
    [SerializeField] private GameObject p2HoldRoot;
    [SerializeField] private Image p2HoldIcon;
    [SerializeField] private TextMeshProUGUI p2HoldText;

    [Header("Labels")]
    [SerializeField] private string holdPromptLabel = "Hold  to pull!";
    [SerializeField] private int  totalMashes = 15;


    [Header("Mash Pulse")]
    [SerializeField] private float pulseMin = 0.85f;
    [SerializeField] private float pulseMax  = 1.2f;
    [SerializeField] private float pulseSpeed  = 4f;
    [SerializeField] private Color pulseColorA = new Color(1f, 0.9f, 0.2f);
    [SerializeField] private Color pulseColorB = new Color(1f, 0.3f, 0.1f);


    [Header("Hold Trigger Pulse")]
    [SerializeField] private float holdPulseMin = 0.9f;
    [SerializeField] private float holdPulseMax   = 1.15f;
    [SerializeField] private float holdPulseSpeedIdle  = 2f;
    [SerializeField] private float holdPulseSpeedActive = 6f;
    [SerializeField] private Color holdColorIdle = new Color(0.9f, 0.85f, 1f);
    [SerializeField] private Color holdColorActive= new Color(1f, 0.3f, 0.1f);


    [Header("Tug Of War Bar")]
    [SerializeField] private GameObject tugBarRoot;
    [SerializeField] private Slider   tugProgressSlider;
    [SerializeField] private Image  tugFillImage;
    [SerializeField] private Color tugColorSafe   = new Color(0.2f, 0.9f, 0.4f);
    [SerializeField] private Color  tugColorDanger = new Color(0.9f, 0.15f, 0.1f);

    // -------------------------------------------------------
    [Header("Script refs")]
    [SerializeField] private KnockdownManager knockdownManager;


    private bool  isMashPulsing = false;
    private bool  isHoldPulsing = false;
    private bool  isHolding  = false;
    private float lastTugProgress = 0f;


    private GameObject activeMashRoot;
    private Image activeMashIcon;
    private TextMeshProUGUI activeMashCount;
    private GameObject activeHoldRoot;
    private Image activeHoldIcon;
    private TextMeshProUGUI activeHoldText;

    private Coroutine mashPulseRoutine;
    private Coroutine holdPulseRoutine;

   

    private void Start()
    {
        HideAll();

        if (knockdownManager == null) return;

        knockdownManager.OnKnockdownStarted.AddListener(OnKnockdownStarted);
        knockdownManager.OnPlayerRecovered.AddListener(_ => HideAll());
        knockdownManager.OnArmDetached.AddListener(_ => HideAll());
        knockdownManager.OnTugProgressChanged.AddListener(OnTugProgressChanged);
        knockdownManager.OnMashProgress.AddListener(UpdateMashCount);
    }


    private void OnKnockdownStarted(int downedPlayerID)
    {
        int attackerID = downedPlayerID == 1 ? 2 : 1;

        AssignActiveSides(downedPlayerID, attackerID);

  
        if (activeMashRoot  != null) activeMashRoot.SetActive(true);
        if (activeMashCount != null) activeMashCount.text = $"0 / {totalMashes}";

        isMashPulsing = true;
        if (mashPulseRoutine != null) StopCoroutine(mashPulseRoutine);
        mashPulseRoutine = StartCoroutine(MashPulseRoutine());

     
        if (activeHoldRoot != null) activeHoldRoot.SetActive(true);
        if (activeHoldText != null) activeHoldText.text = holdPromptLabel;

        isHoldPulsing = true;
        if (holdPulseRoutine != null) StopCoroutine(holdPulseRoutine);
        holdPulseRoutine = StartCoroutine(HoldPulseRoutine());

     
        if (tugBarRoot        != null) tugBarRoot.SetActive(true);
        if (tugProgressSlider != null) tugProgressSlider.value = 0f;

        lastTugProgress = 0f;
    }

    
    private void AssignActiveSides(int downedPlayerID, int attackerID)
    {
      
        if (downedPlayerID == 1)
        {
            activeMashRoot  = p1MashRoot;
            activeMashIcon  = p1MashIcon;
            activeMashCount = p1MashCountText;
        }
        else
        {
            activeMashRoot  = p2MashRoot;
            activeMashIcon  = p2MashIcon;
            activeMashCount = p2MashCountText;
        }

      
        if (attackerID == 1)
        {
            activeHoldRoot = p1HoldRoot;
            activeHoldIcon = p1HoldIcon;
            activeHoldText = p1HoldText;
        }
        else
        {
            activeHoldRoot = p2HoldRoot;
            activeHoldIcon = p2HoldIcon;
            activeHoldText = p2HoldText;
        }
    }


    private void HideAll()
    {
        isMashPulsing = false;
        isHoldPulsing = false;
        isHolding     = false;

        if (mashPulseRoutine != null) { StopCoroutine(mashPulseRoutine); mashPulseRoutine = null; }
        if (holdPulseRoutine != null) { StopCoroutine(holdPulseRoutine); holdPulseRoutine = null; }


        HideAndReset(p1MashRoot);
        HideAndReset(p2MashRoot);
        HideAndReset(p1HoldRoot);
        HideAndReset(p2HoldRoot);

        if (tugBarRoot != null) tugBarRoot.SetActive(false);

        activeMashRoot  = null;
        activeMashIcon  = null;
        activeMashCount = null;
        activeHoldRoot  = null;
        activeHoldIcon  = null;
        activeHoldText  = null;
    }

    private void HideAndReset(GameObject root)
    {
        if (root == null) return;
        root.transform.localScale = Vector3.one;
        root.SetActive(false);
    }



    private void OnTugProgressChanged(float progress)
    {
        if (tugProgressSlider != null)
            tugProgressSlider.value = progress;

        if (tugFillImage != null)
            tugFillImage.color = Color.Lerp(tugColorSafe, tugColorDanger, progress);

  
        isHolding       = progress > lastTugProgress + 0.001f;
        lastTugProgress = progress;
    }

    private void UpdateMashCount(int count)
    {
        if (activeMashCount != null)
            activeMashCount.text = $"{count} / {totalMashes}";
    }



    private IEnumerator MashPulseRoutine()
    {
        float t = 0f;

        while (isMashPulsing)
        {
            t += Time.deltaTime * pulseSpeed;

            float ping  = (Mathf.Sin(t * Mathf.PI * 2f) + 1f) * 0.5f;
            float scale = Mathf.Lerp(pulseMin, pulseMax, ping);

            if (activeMashRoot != null)
                activeMashRoot.transform.localScale = Vector3.one * scale;

            if (activeMashIcon != null)
                activeMashIcon.color = Color.Lerp(pulseColorA, pulseColorB, ping);

            yield return null;
        }

        if (activeMashRoot != null)
            activeMashRoot.transform.localScale = Vector3.one;
    }


    private IEnumerator HoldPulseRoutine()
    {
        float t = 0f;

        while (isHoldPulsing)
        {
            float speed = isHolding ? holdPulseSpeedActive : holdPulseSpeedIdle;
            t += Time.deltaTime * speed;

            float ping  = (Mathf.Sin(t * Mathf.PI * 2f) + 1f) * 0.5f;
            float scale = Mathf.Lerp(holdPulseMin, holdPulseMax, ping);
            Color col   = Color.Lerp(holdColorIdle, holdColorActive, isHolding ? ping : 0f);

            if (activeHoldRoot != null)
                activeHoldRoot.transform.localScale = Vector3.one * scale;

            if (activeHoldIcon != null)
                activeHoldIcon.color = col;

            if (activeHoldText != null)
                activeHoldText.color = col;

            yield return null;
        }

        if (activeHoldRoot != null)
            activeHoldRoot.transform.localScale = Vector3.one;
    }
}