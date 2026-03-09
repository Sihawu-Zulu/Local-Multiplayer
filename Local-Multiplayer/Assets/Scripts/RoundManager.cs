using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class RoundManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private KillerShotManager killerShotManager;
    [SerializeField] public KnockdownManager knockdownManager;


    [Header("Round Settings")]
    [SerializeField] private int roundsToWin  = 2;
    [SerializeField] private float roundEndDelay = 2f;
    [SerializeField] private float roundStartDelay = 1.5f;

    // --- states n stuff ---
    public int CurrentRound { get; private set; } = 1;
    public int P1RoundWins  { get; private set; }
    public int P2RoundWins  { get; private set; }
    private bool roundOver = false;
    private bool playersResolved  = false;
    private bool roundStarted  = false;

    private PlayerHealth p1Health;
    private PlayerHealth p2Health;

  
    private MultiplayerPlayerController p1Controller;
    private MultiplayerPlayerController p2Controller;

    // --- events ---
    public UnityEvent<int>  OnRoundStarted;
    public UnityEvent<int>  OnRoundWon;
    public UnityEvent<int>  OnMatchWon;
    public UnityEvent<int, int> OnScoreUpdated;

    // -------------------------------------------------------

    private void Start()
    {
        // round ends only when a player health reaches zero via OnPlayerDefeated
        // killer shot winner no longer ends the round... it triggers knockdown instead
    }

    private void Update()
    {
        if (!playersResolved)
        {
            TryResolvePlayerReferences();
            return;
        }

    
        if (!roundStarted)
        {
            roundStarted = true;
            StartCoroutine(StartRoundWithDelay());
        }
    }


    private void TryResolvePlayerReferences()
    {
        var controllers = FindObjectsByType<MultiplayerPlayerController>(FindObjectsSortMode.None);

        PlayerHealth found1 = null;
        PlayerHealth found2 = null;

    foreach (var c in controllers)
    {
        if (c.PlayerID == 1) { found1 = c.GetComponent<PlayerHealth>(); p1Controller = c; }
        else if
         (c.PlayerID == 2) { found2 = c.GetComponent<PlayerHealth>(); p2Controller = c; }
    }

        if (found1 == null || found2 == null) return;   

        p1Health = found1;
        p2Health = found2;

      
        p1Health.OnPlayerDefeated.AddListener(() => OnPlayerDefeated(1));
        p2Health.OnPlayerDefeated.AddListener(() => OnPlayerDefeated(2));

        playersResolved = true;
       
    }

    // -------------------------------------------------------
    // round flow
   

    private IEnumerator StartRoundWithDelay()
    {
        yield return new WaitForSeconds(roundStartDelay);
        roundOver = false;
        OnRoundStarted?.Invoke(CurrentRound);
      
    }

    private void OnPlayerDefeated(int playerID)
    {
        if (roundOver) return;
        EndRound(winnerID: playerID == 1 ? 2 : 1);
    }

    private void EndRound(int winnerID)
    {
        if (roundOver) 
        return;
        roundOver = true;

        if (winnerID == 1) P1RoundWins++;
        else  
         P2RoundWins++;

        OnRoundWon?.Invoke(winnerID);
        OnScoreUpdated?.Invoke(P1RoundWins, P2RoundWins);

        

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
        knockdownManager?.ResetKnockdown();
        p1Health?.ResetHealth();
        p2Health?.ResetHealth();

    // p1Controller?.MoveToSpawnPoint();
    // p2Controller?.MoveToSpawnPoint();

        StartCoroutine(StartRoundWithDelay());
    }

    private IEnumerator EndMatch(int winnerID)
    {
        yield return new WaitForSeconds(roundEndDelay);
        OnMatchWon?.Invoke(winnerID);
       
       
    }
}