using System.Collections;
using UnityEngine;
using UnityEngine.Events;

// sits on GameManager object
// tracks best-of-3 rounds, listens to PlayerHealth + KillerShotManager events
// finds players automatically — no runtime reference passing needed

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
    private bool roundOver  = false;

    private PlayerHealth p1Health;
    private PlayerHealth p2Health;

    // --- events — wire to HealthBarUI in inspector ---
    public UnityEvent<int>      OnRoundStarted;     // int = round number
    public UnityEvent<int>      OnRoundWon;         // int = winner player ID
    public UnityEvent<int>      OnMatchWon;         // int = winner player ID
    public UnityEvent<int, int> OnScoreUpdated;     // (p1wins, p2wins) WITH THIS MAYBE CAMERA SHOWING PLAYER WINNING ??

    // -------------------------------------------------------

    private void Start()
    {
        ResolvePlayerReferences();
        killerShotManager.OnKillerShotWinner.AddListener(OnKillerShotWon);
        StartCoroutine(StartRoundWithDelay());
    }

    private void ResolvePlayerReferences()
    {
        var controllers = FindObjectsByType<MultiplayerPlayerController>(FindObjectsSortMode.None);

        foreach (var c in controllers)
        {
            if (c.PlayerID == 1) p1Health = c.GetComponent<PlayerHealth>();
            else                  p2Health = c.GetComponent<PlayerHealth>();
        }

        if (p1Health != null) p1Health.OnPlayerDefeated.AddListener(() => OnPlayerDefeated(1));
        if (p2Health != null) p2Health.OnPlayerDefeated.AddListener(() => OnPlayerDefeated(2));

        if (p1Health == null || p2Health == null)
            Debug.LogWarning("[RoundManager] couldn't find both players at Start — health events may not fire");
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
        yield return null;      // wait one frame for PlayerHealth.OnPlayerDefeated to fire first
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

        Debug.Log($"[RoundManager] P{winnerID} wins round {CurrentRound} | score {P1RoundWins}-{P2RoundWins}");

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

        // re-resolve in case players moved
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