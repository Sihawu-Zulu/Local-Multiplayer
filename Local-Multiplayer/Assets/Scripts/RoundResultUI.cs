using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class RoundResultUI : MonoBehaviour
{
   
    [Header("Round Banners")]
    [SerializeField] private RectTransform   p1BannerRect;
    [SerializeField] private RectTransform   p2BannerRect;
    [SerializeField] private TextMeshProUGUI p1PlayerLabel;
    [SerializeField] private TextMeshProUGUI p2PlayerLabel;
    [SerializeField] private TextMeshProUGUI p1WinsLabel;
    [SerializeField] private TextMeshProUGUI p2WinsLabel;

    [Header("Slide Settings")]
    [SerializeField] private float offscreenX       = 1200f;
    [SerializeField] private float slideInDuration  = 0.22f;
    [SerializeField] private float holdDuration     = 2.2f;
    [SerializeField] private float slideOutDuration = 0.18f;

 
    [Header("Match Over Panel")]
    [SerializeField] private GameObject matchOverRoot;
    [SerializeField] private RectTransform   matchBannerRect;
    [SerializeField] private TextMeshProUGUI matchWinnerText;
    [SerializeField] private TextMeshProUGUI matchScoreText;
    [SerializeField] private float  matchFadeInDuration = 0.5f;

    [Header("Match Over Buttons")]
    [SerializeField] private Button playAgainButton;
    [SerializeField] private Button mainMenuButton;
    [SerializeField] private Button quitButton;

    [Header("Scene Names")]
    [SerializeField] private string gameSceneName= "Game";
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    [Header("References")]
    [SerializeField] private RoundManager roundManager;

   

    private void Awake()
    {
      
        if (p1BannerRect != null) p1BannerRect.gameObject.SetActive(false);
        if (p2BannerRect != null) p2BannerRect.gameObject.SetActive(false);
        if (matchOverRoot != null) matchOverRoot.SetActive(false);
    }

    private void Start()
    {
        if (roundManager == null)
            roundManager = FindObjectOfType<RoundManager>();

        if (roundManager != null)
        {
            roundManager.OnRoundWon.AddListener(ShowRoundResult);
            roundManager.OnMatchWon.AddListener(ShowMatchOver);
            // Debug.Log("[RoundResultUI] subscribed to RoundManager events");
        }
        else
        {
            // Debug.LogWarning("[RoundResultUI] RoundManager not found!");
        }

        if (playAgainButton != null) playAgainButton.onClick.AddListener(OnPlayAgain);
        if (mainMenuButton  != null) mainMenuButton.onClick.AddListener(OnMainMenu);
        if (quitButton      != null) quitButton.onClick.AddListener(OnQuit);
    }

    private void OnDestroy()
    {
        if (roundManager != null)
        {
            roundManager.OnRoundWon.RemoveListener(ShowRoundResult);
            roundManager.OnMatchWon.RemoveListener(ShowMatchOver);
        }
    }



    public void ShowRoundResult(int winnerID)
    {
        Debug.Log($"[RoundResultUI] ShowRoundResult P{winnerID} — p1null:{p1BannerRect == null} p2null:{p2BannerRect == null}");
        StartCoroutine(RoundBannerRoutine(winnerID));
    }

    private IEnumerator RoundBannerRoutine(int winnerID)
    {
        RectTransform banner = winnerID == 1 ? p1BannerRect : p2BannerRect;
        if (banner == null)
        {
            
            yield break;
        }

        float startX = winnerID == 1 ? -offscreenX : offscreenX;

        // force position before enabling so it doesn't flash at wrong position
        banner.anchoredPosition = new Vector2(startX, banner.anchoredPosition.y);
        banner.gameObject.SetActive(true);

        Debug.Log($"[RoundResultUI] banner P{winnerID} activated at x:{startX}, sliding to 0");

        yield return StartCoroutine(SlideX(banner, startX, 0f, slideInDuration));
        yield return new WaitForSecondsRealtime(holdDuration);   
        yield return StartCoroutine(SlideX(banner, 0f, startX, slideOutDuration));

        banner.gameObject.SetActive(false);
        Debug.Log($"[RoundResultUI] banner P{winnerID} hidden");
    }



    public void ShowMatchOver(int winnerID)
    {
        StartCoroutine(MatchOverRoutine(winnerID));
    }

    private IEnumerator MatchOverRoutine(int winnerID)
    {
        yield return new WaitForSecondsRealtime(0.8f);

        if (matchWinnerText != null)
            // matchWinnerText.text = "PLAYER " + winnerID + " WINS!";

        if (matchScoreText != null && roundManager != null)
            matchScoreText.text = roundManager.P1RoundWins + "  —  " + roundManager.P2RoundWins;

        if (matchOverRoot == null)
        {
            
            yield break;
        }

        SetMatchButtonsInteractable(true);
        matchOverRoot.SetActive(true);

        if (matchBannerRect != null)
        {
            float startX = winnerID == 1 ? -offscreenX : offscreenX;
            matchBannerRect.anchoredPosition = new Vector2(startX, matchBannerRect.anchoredPosition.y);
            yield return StartCoroutine(SlideX(matchBannerRect, startX, 0f, slideInDuration * 1.4f));
        }

        var cg = matchOverRoot.GetComponent<CanvasGroup>();
        if (cg != null)
        {
            cg.alpha = 0f;
            float elapsed = 0f;
            while (elapsed < matchFadeInDuration)
            {
                elapsed  += Time.unscaledDeltaTime;
                cg.alpha  = Mathf.Clamp01(elapsed / matchFadeInDuration);
                yield return null;
            }
            cg.alpha = 1f;
        }
    }

 

    private IEnumerator SlideX(RectTransform rect, float fromX, float toX, float duration)
    {
        float elapsed = 0f;
        float y       = rect.anchoredPosition.y;
        bool  easeOut = toX == 0f;   

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t  = Mathf.Clamp01(elapsed / duration);
            float curved = easeOut ? 1f - Mathf.Pow(1f - t, 4f) : t;
            rect.anchoredPosition = new Vector2(Mathf.Lerp(fromX, toX, curved), y);
            yield return null;
        }

        rect.anchoredPosition = new Vector2(toX, y);
    }


    private void OnPlayAgain()
    {
        SetMatchButtonsInteractable(false);
        Time.timeScale = 1f;
        SceneManager.LoadScene(gameSceneName);
    }

    private void OnMainMenu()
    {
        SetMatchButtonsInteractable(false);
        Time.timeScale = 1f;
        SceneManager.LoadScene(mainMenuSceneName);
    }

    private void OnQuit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void SetMatchButtonsInteractable(bool interactable)
    {
        if (playAgainButton != null) playAgainButton.interactable = interactable;
        if (mainMenuButton  != null) mainMenuButton.interactable  = interactable;
        if (quitButton      != null) quitButton.interactable      = interactable;
    }
}