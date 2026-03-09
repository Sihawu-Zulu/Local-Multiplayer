using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;


public class JoinUIManager : MonoBehaviour
{
    [Header("P1 UI")]
    [SerializeField] private GameObject p1PromptRoot;
    [SerializeField] private TextMeshProUGUI p1PromptText;
    [SerializeField] private TextMeshProUGUI p1JoinedText;
    [Header("P2 UI")]
    [SerializeField] private GameObject p2PromptRoot;
    [SerializeField] private TextMeshProUGUI p2PromptText;
    [SerializeField] private TextMeshProUGUI p2JoinedText;

    [Header("Settings")]
    [SerializeField] private float hideDelay = 1.5f;

    [Header("References")]
    [SerializeField] private CountdownManager countdownManager;  

    private bool p1Joined = false;
    private bool p2Joined = false;

    //======================

    private void Start()
    {
        p1JoinedText.gameObject.SetActive(false);
        p2JoinedText.gameObject.SetActive(false);

        p1PromptText.gameObject.SetActive(true);
        p2PromptText.gameObject.SetActive(true);
    }


    public void OnPlayerJoined(PlayerInput player)
    {
        int id = player.playerIndex + 1;

        if (id == 1 && !p1Joined)
        {
            p1Joined = true;
            ShowJoined(p1PromptText, p1JoinedText);
        }
        else if (id == 2 && !p2Joined)
        {
            p2Joined = true;
            ShowJoined(p2PromptText, p2JoinedText);
        }

        if (p1Joined && p2Joined)
            StartCoroutine(HideAllAfterDelay());
    }

    //=

    private void ShowJoined(TextMeshProUGUI promptText, TextMeshProUGUI joinedText)
    {
        promptText.gameObject.SetActive(false);
        joinedText.gameObject.SetActive(true);
    }

    private IEnumerator HideAllAfterDelay()
    {
        // Debug.Log($"[JoinUIManager] calling StartCountdown, manager null: {countdownManager == null}");

        yield return new WaitForSeconds(hideDelay);

        p1PromptRoot.SetActive(false);
        p2PromptRoot.SetActive(false);


    countdownManager?.StartCountdown();
    }
}