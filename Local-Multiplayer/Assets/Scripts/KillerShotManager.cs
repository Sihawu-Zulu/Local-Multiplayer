using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public class KillerShotManager : MonoBehaviour
{
    [Header("Killer Shot Settings")]
    [SerializeField] private float minReactionWindow   = 3f;
    [SerializeField] private float maxReactionWindow   = 6f;
    [SerializeField] private float perfectTimingWindow = 0.4f;
    [SerializeField] private float hapticLowFreq  = 0.3f;
    [SerializeField] private float hapticHighFreq   = 0.8f;

    [Header("References")]
    [SerializeField] private KnockdownManager knockdownManager;    
    // --- events ---
    public UnityEvent<int> OnKillerShotPhaseStarted;
    public UnityEvent      OnKillerShotPhaseEnded;
    public UnityEvent<int> OnKillerShotWinner;
    public UnityEvent      OnKillerShotExpired;
    public UnityEvent<int> OnEarlyPress;
    public UnityEvent<int> OnPerfectPress;
    public UnityEvent<int> OnTooLate;

    private PlayerHealth p1Health;
    private PlayerHealth p2Health;
    private MultiplayerPlayerController p1Controller;
    private MultiplayerPlayerController p2Controller;
    private CombatSystem p1Combat;
    private CombatSystem p2Combat;
    private bool playersResolved = false;

    // --- state ---
    private bool  killerShotActive   = false;
    private bool  reactionWindowOpen = false;
    private float reactionWindowDuration;
    private float windowTimer;
    private Coroutine reactionCoroutine;
    private bool p1PressedEarly = false;
    private bool p2PressedEarly = false;
    private bool p1ReactionPressed = false;
    private bool p2ReactionPressed = false;
    private int  triggeringPlayerID = 0;
    private bool p1ReactedInWindow  = false;
    private bool p2ReactedInWindow  = false;

    private Gamepad p1Gamepad;
    private Gamepad p2Gamepad;

    public float GetWindowTimeRemaining() => windowTimer;
    public float GetWindowDuration() => reactionWindowDuration;
    public int   GetTriggeringPlayerID() => triggeringPlayerID;



    private void Update()
    {
        if (!playersResolved)
        {
            TryResolvePlayerReferences();
            return;
        }

        if (!killerShotActive) return;

        bool p1Pressed = p1ReactionPressed;
        bool p2Pressed = p2ReactionPressed;
        p1ReactionPressed = false;
        p2ReactionPressed = false;

        if (!reactionWindowOpen)
        {
            if (p1Pressed && !p1PressedEarly) { p1PressedEarly = true; OnEarlyPress?.Invoke(1); Debug.Log("[KillerShot] P1 too early!!!!"); }
            if (p2Pressed && !p2PressedEarly) { p2PressedEarly = true; OnEarlyPress?.Invoke(2); Debug.Log("[KillerShot] P2 too early!"); }
            return;
        }

        bool p1Valid = p1Pressed && !p1PressedEarly;
        bool p2Valid = p2Pressed && !p2PressedEarly;

        if (p1Valid) p1ReactedInWindow = true;
        if (p2Valid) p2ReactedInWindow = true;

        if      (p1Valid && p2Valid) ResolveKillerShot(1);
        else if (p1Valid)          { CheckPerfect(1); ResolveKillerShot(1); }
        else if (p2Valid)          { CheckPerfect(2); ResolveKillerShot(2); }
    }

    private void OnP1Reaction() => p1ReactionPressed = true;
    private void OnP2Reaction() => p2ReactionPressed = true;

   
    private void TryResolvePlayerReferences()
    {
        var controllers = FindObjectsByType<MultiplayerPlayerController>(FindObjectsSortMode.None);

        p1Controller = null;
        p2Controller = null;

        foreach (var c in controllers)
        {
            if      (c.PlayerID == 1) { p1Controller = c; p1Health = c.GetComponent<PlayerHealth>(); p1Combat = c.GetComponent<CombatSystem>(); }
            else if (c.PlayerID == 2) { p2Controller = c; p2Health = c.GetComponent<PlayerHealth>(); p2Combat = c.GetComponent<CombatSystem>(); }
        }

        if (p1Controller == null || p2Controller == null) return;

        p1Health.OnKillerShotTriggered.AddListener(() => ActivateKillerShotPhase(1));
        p2Health.OnKillerShotTriggered.AddListener(() => ActivateKillerShotPhase(2));

        p1Controller.OnReactionEvent += OnP1Reaction;
        p2Controller.OnReactionEvent += OnP2Reaction;

        playersResolved = true;
        RefreshGamepads();
    }

    private void OnDestroy()
    {
        if (p1Controller != null) p1Controller.OnReactionEvent -= OnP1Reaction;
        if (p2Controller != null) p2Controller.OnReactionEvent -= OnP2Reaction;

        StopHaptics();
    }



    private void ActivateKillerShotPhase(int playerID)
    {
        if (killerShotActive) return;

        killerShotActive = true;
        reactionWindowOpen = false;
        triggeringPlayerID = playerID;
        p1PressedEarly = false;
        p2PressedEarly  = false;
        p1ReactionPressed = false;
        p2ReactionPressed = false;
        p1ReactedInWindow = false;
        p2ReactedInWindow  = false;
        reactionWindowDuration = Random.Range(minReactionWindow, maxReactionWindow);
        windowTimer  = reactionWindowDuration;

        p1Combat?.SetCombatEnabled(false);
        p2Combat?.SetCombatEnabled(false);

        OnKillerShotPhaseStarted?.Invoke(playerID);
        StartHaptics();

        reactionCoroutine = StartCoroutine(ReactionWindowRoutine());
    }

    private IEnumerator ReactionWindowRoutine()
    {
        yield return new WaitForSeconds(0.5f);

        reactionWindowOpen = true;
        // Debug.Log("[KillerShot] window OPEN");

        float elapsed = 0f;
        while (elapsed < reactionWindowDuration && killerShotActive)
        {
            elapsed    += Time.deltaTime;
            windowTimer = reactionWindowDuration - elapsed;
            yield return null;
        }

        if (killerShotActive)
        {
            killerShotActive   = false;
            reactionWindowOpen = false;
            StopHaptics();
            ReenableCombat();

            if (!p1ReactedInWindow && !p1PressedEarly) OnTooLate?.Invoke(1);
            if (!p2ReactedInWindow && !p2PressedEarly) OnTooLate?.Invoke(2);
            OnKillerShotExpired?.Invoke();
            OnKillerShotPhaseEnded?.Invoke();
           
        }
    }

    private void CheckPerfect(int playerID)
    {
        if (windowTimer <= perfectTimingWindow)
        {
            OnPerfectPress?.Invoke(playerID);
           
        }
    }

    private void ResolveKillerShot(int winnerID)
    {
        if (!killerShotActive) return;

        killerShotActive   = false;
        reactionWindowOpen = false;
        StopHaptics();

        if (reactionCoroutine != null) StopCoroutine(reactionCoroutine);

       
        int loserID = winnerID == 1 ? 2 : 1;
        knockdownManager?.StartKnockdown(loserID);

        ReenableCombat();
        OnKillerShotWinner?.Invoke(winnerID);
        OnKillerShotPhaseEnded?.Invoke();

        Debug.Log($"[KillerShot] P{winnerID} wins ... P{loserID} knocked down");
    }

    private void ReenableCombat()
    {
        p1Combat?.SetCombatEnabled(true);
        p2Combat?.SetCombatEnabled(true);
    }

    public void ResetKillerShot()
    {
        killerShotActive   = false;
        reactionWindowOpen = false;
        triggeringPlayerID = 0;
        StopHaptics();
        ReenableCombat();

        if (reactionCoroutine != null)
        {
            StopCoroutine(reactionCoroutine);
            reactionCoroutine = null;
        }

        OnKillerShotPhaseEnded?.Invoke();
    }

    // -------------------------------------------------------
    // haptics stuff
    

    private void StartHaptics()
    {
        RefreshGamepads();
        p1Gamepad?.SetMotorSpeeds(hapticLowFreq, hapticHighFreq);
        p2Gamepad?.SetMotorSpeeds(hapticLowFreq, hapticHighFreq);
    }

    private void StopHaptics()
    {
        p1Gamepad?.ResetHaptics();
        p2Gamepad?.ResetHaptics();
    }

    private void RefreshGamepads()
    {
        var pads  = Gamepad.all;
        p1Gamepad = pads.Count > 0 ? pads[0] : null;
        p2Gamepad = pads.Count > 1 ? pads[1] : null;
    }
}