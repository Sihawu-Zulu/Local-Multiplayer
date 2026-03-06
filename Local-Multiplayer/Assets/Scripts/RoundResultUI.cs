using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class RoundResultUI : MonoBehaviour
{
    [Header("Round Banner")]
    [SerializeField] private RectTransform p1BannerRect;      
    [SerializeField] private RectTransform p2BannerRect;     
    [SerializeField] private TextMeshProUGUI p1RoundText;   
    [SerializeField] private TextMeshProUGUI p2RoundText;  
    [SerializeField] private TextMeshProUGUI p1WinsLabel;  
    [SerializeField] private TextMeshProUGUI p2WinsLabel; 

    [Header("Slide Settings")]
    [SerializeField] private float offscreenX = 1200f;  
    [SerializeField] private float slideInDuration = 0.22f;   
    [SerializeField] private float holdDuration    = 2.2f;
    [SerializeField] private float slideOutDuration = 0.18f;

    [Header("Match Over Panel")]
    [SerializeField] private GameObject matchOverRoot;
    [SerializeField] private RectTransform   matchBannerRect;
    [SerializeField] private TextMeshProUGUI matchWinnerText;
    [SerializeField] private TextMeshProUGUI matchScoreText;
    [SerializeField] private float matchFadeInDuration = 0.4f;

    [Header("Refss")]
    [SerializeField] private RoundManager roundManager;



    private void Awake()
    {
        SetBannerActive(p1BannerRect, false);
        SetBannerActive(p2BannerRect, false);
        if (matchOverRoot != null) matchOverRoot.SetActive(false);
    }

   
    // called by RoundManager.OnRoundWon
    

    public void ShowRoundResult(int winnerID)
    {
        StartCoroutine(RoundBannerRoutine(winnerID));
    }

    private IEnumerator RoundBannerRoutine(int winnerID)
    {
        RectTransform banner = winnerID == 1 ? p1BannerRect : p2BannerRect;
        if (banner == null) yield break;

     
        float startX  = winnerID == 1 ? -offscreenX : offscreenX;
        float endX    = 0f;

        banner.anchoredPosition = new Vector2(startX, banner.anchoredPosition.y);
        SetBannerActive(banner, true);


        yield return StartCoroutine(SlideX(banner, startX, endX, slideInDuration, easeOut: true));

        yield return new WaitForSeconds(holdDuration);

        
        yield return StartCoroutine(SlideX(banner, endX, startX, slideOutDuration, easeOut: false));

        SetBannerActive(banner, false);
    }

    private IEnumerator SlideX(RectTransform rect, float fromX, float toX, float duration, bool easeOut)
    {
        float elapsed = 0f;
        float y       = rect.anchoredPosition.y;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t  = Mathf.Clamp01(elapsed / duration);

            
            float curved = easeOut ? 1f - Mathf.Pow(1f - t, 4f) : t;

            rect.anchoredPosition = new Vector2(Mathf.Lerp(fromX, toX, curved), y);
            yield return null;
        }

        rect.anchoredPosition = new Vector2(toX, y);
    }


    // called by RoundManager.OnMatchWon
    

    public void ShowMatchOver(int winnerID)
    {
        StartCoroutine(MatchOverRoutine(winnerID));
    }

    private IEnumerator MatchOverRoutine(int winnerID)
    {
        yield return new WaitForSeconds(0.6f);

        if (matchWinnerText != null)
            matchWinnerText.text = "PLAYER " + winnerID + " WINS!";

        if (matchScoreText != null && roundManager != null)
            matchScoreText.text  = roundManager.P1RoundWins + "  —  " + roundManager.P2RoundWins;

        if (matchOverRoot == null) yield break;

        matchOverRoot.SetActive(true);

      
        if (matchBannerRect != null)
        {
            float startX = winnerID == 1 ? -offscreenX : offscreenX;
            matchBannerRect.anchoredPosition = new Vector2(startX, matchBannerRect.anchoredPosition.y);
            yield return StartCoroutine(SlideX(matchBannerRect, startX, 0f, slideInDuration * 1.4f, easeOut: true));
        }

       
        var cg = matchOverRoot.GetComponent<CanvasGroup>();
        if (cg != null)
        {
            float elapsed = 0f;
            while (elapsed < matchFadeInDuration)
            {
                elapsed += Time.deltaTime;
                cg.alpha = Mathf.Clamp01(elapsed / matchFadeInDuration);
                yield return null;
            }
        }
    }



    private void SetBannerActive(RectTransform rect, bool active)
    {
        if (rect != null) rect.gameObject.SetActive(active);
    }
}
