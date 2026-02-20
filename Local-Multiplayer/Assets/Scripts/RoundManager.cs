using System.Collections;
using UnityEngine;
using UnityEngine.Events;

// sits on GameManager object
// tracks best-of-3 rounds — retries finding players every frame until both are found
// only subscribes to health events once both players have valid PlayerIDs

public class RoundManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private KillerShotManager killerShotManager;

    [Header("Round Settings")]
    [SerializeField] private int   roundsToWin     = 2;
    [SerializeField] private float roundEndDelay   = 2f;
    [SerializeField] private float roundStartDelay = 1.5f;

    // --- state ---
    public int CurrentRound { get; private set; } = 1;
    public int P1RoundWins  { get; private set; }
    public int P2RoundWins  { get; private set; }
    private bool roundOver        = false;
    private bool playersResolved  = false;
    private bool roundStarted     = false;

    private PlayerHealth p1Health;
    private PlayerHealth p2Health;

    // --- events ---
    public UnityEvent<int>      OnRoundStarted;
    public UnityEvent<int>      OnRoundWon;
    public UnityEvent<int>      OnMatchWon;
    public UnityEvent<int, int> OnScoreUpdated;

    // -------------------------------------------------------

    private void Start()
    {
        killerShotManager.OnKillerShotWinner.AddListener(OnKillerShotWon);
    }

    private void Update()
    {
        if (!playersResolved)
        {
            TryResolvePlayerReferences();
            return;
        }

        // kick off the first round once — but only after players are resolved
        if (!roundStarted)
        {
            roundStarted = true;
            StartCoroutine(StartRoundWithDelay());
        }
    }

    // -------------------------------------------------------
    // player resolution
    // -------------------------------------------------------

    private void TryResolvePlayerReferences()
    {
        var controllers = FindObjectsByType<MultiplayerPlayerController>(FindObjectsSortMode.None);

        PlayerHealth found1 = null;
        PlayerHealth found2 = null;

        foreach (var c in controllers)
        {
            if (c.PlayerID == 1)      found1 = c.GetComponent<PlayerHealth>();
            else if (c.PlayerID == 2) found2 = c.GetComponent<PlayerHealth>();
        }

        if (found1 == null || found2 == null) return;   // not both ready yet

        p1Health = found1;
        p2Health = found2;

        // subscribe to defeat events now that both players are confirmed
        p1Health.OnPlayerDefeated.AddListener(() => OnPlayerDefeated(1));
        p2Health.OnPlayerDefeated.AddListener(() => OnPlayerDefeated(2));

        playersResolved = true;
        Debug.Log("[RoundManager] both players resolved — health events subscribed");
    }

    // -------------------------------------------------------
    // round flow
    // -------------------------------------------------------

    private IEnumerator StartRoundWithDelay()
    {
        yield return new WaitForSeconds(roundStartDelay);
        roundOver = false;
        OnRoundStarted?.Invoke(CurrentRound);
        Debug.Log($"[RoundManager] round {CurrentRound} started");
    }

    private void OnPlayerDefeated(int playerID)
    {
        if (roundOver) return;
        EndRound(winnerID: playerID == 1 ? 2 : 1);
    }

    private void OnKillerShotWon(int winnerID)
    {
        if (roundOver) return;
        StartCoroutine(CheckRoundEndAfterKillerShot(winnerID));
    }

    private IEnumerator CheckRoundEndAfterKillerShot(int winnerID)
    {
        yield return null;  // wait one frame for OnPlayerDefeated to fire first if HP hit 0
        if (!roundOver) EndRound(winnerID);
    }

    private void EndRound(int winnerID)
    {
        if (roundOver) return;
        roundOver = true;

        if (winnerID == 1) P1RoundWins++;
        else               P2RoundWins++;

        OnRoundWon?.Invoke(winnerID);
        OnScoreUpdated?.Invoke(P1RoundWins, P2RoundWins);

        Debug.Log($"[RoundManager] P{winnerID} wins round {CurrentRound} | {P1RoundWins}-{P2RoundWins}");

        if (P1RoundWins >= roundsToWin || P2RoundWins >= roundsToWin)
            StartCoroutine(EndMatch(winnerID));
        else
        {
            CurrentRound++;
            StartCoroutine(StartNextRound());
        }
    }

    private IEnumerator StartNextRound()
    {
        yield return new WaitForSeconds(roundEndDelay);

        killerShotManager.ResetKillerShot();
        p1Health?.ResetHealth();
        p2Health?.ResetHealth();

        StartCoroutine(StartRoundWithDelay());
    }

    private IEnumerator EndMatch(int winnerID)
    {
        yield return new WaitForSeconds(roundEndDelay);
        OnMatchWon?.Invoke(winnerID);
        Debug.Log($"[RoundManager] P{winnerID} wins the match!");
        // TODO: trigger match end screen / next mini-game
    }
}