using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

// sits on GameManager object
// finds both players via FindObjectsByType — retries every frame until both are found
// subscribes to health events once found — works correctly with dynamic PlayerInputManager spawning

public class KillerShotManager : MonoBehaviour
{
    [Header("Killer Shot Settings")]
    [SerializeField] private float minReactionWindow   = 3f;
    [SerializeField] private float maxReactionWindow   = 6f;
    [SerializeField] private float perfectTimingWindow = 0.4f;
    [SerializeField] private float hapticLowFreq       = 0.3f;
    [SerializeField] private float hapticHighFreq      = 0.8f;

    // --- events ---
    public UnityEvent        OnKillerShotPhaseStarted;
    public UnityEvent        OnKillerShotPhaseEnded;
    public UnityEvent<int>   OnKillerShotWinner;
    public UnityEvent        OnKillerShotExpired;
    public UnityEvent<int>   OnEarlyPress;
    public UnityEvent<int>   OnPerfectPress;

    // --- resolved once both players are found ---
    private PlayerHealth                p1Health;
    private PlayerHealth                p2Health;
    private MultiplayerPlayerController p1Controller;
    private MultiplayerPlayerController p2Controller;
    private CombatSystem                p1Combat;
    private CombatSystem                p2Combat;
    private bool                        playersResolved = false;

    // --- state ---
    private bool      killerShotActive    = false;
    private bool      reactionWindowOpen  = false;
    private float     reactionWindowDuration;
    private float     windowTimer;
    private Coroutine reactionCoroutine;
    private bool      p1PressedEarly      = false;
    private bool      p2PressedEarly      = false;

    private Gamepad p1Gamepad;
    private Gamepad p2Gamepad;

    public float GetWindowTimeRemaining() => windowTimer;
    public float GetWindowDuration()      => reactionWindowDuration;

    // -------------------------------------------------------

    private void Update()
    {
        // keep trying until both players are in the scene with valid PlayerIDs
        if (!playersResolved)
        {
            TryResolvePlayerReferences();
            return;
        }

        if (!killerShotActive) return;

        bool p1Pressed = p1Controller != null && p1Controller.ReactionPressed;
        bool p2Pressed = p2Controller != null && p2Controller.ReactionPressed;

        if (!reactionWindowOpen)
        {
            if (p1Pressed && !p1PressedEarly) { p1PressedEarly = true; OnEarlyPress?.Invoke(1); Debug.Log("[KillerShot] P1 TOO EARLY"); }
            if (p2Pressed && !p2PressedEarly) { p2PressedEarly = true; OnEarlyPress?.Invoke(2); Debug.Log("[KillerShot] P2 TOO EARLY"); }
            return;
        }

        bool p1Valid = p1Pressed && !p1PressedEarly;
        bool p2Valid = p2Pressed && !p2PressedEarly;

        if      (p1Valid && p2Valid) ResolveKillerShot(1);
        else if (p1Valid)          { CheckPerfect(1); ResolveKillerShot(1); }
        else if (p2Valid)          { CheckPerfect(2); ResolveKillerShot(2); }
    }

    // -------------------------------------------------------
    // player resolution — retries until BOTH players have a valid PlayerID
    // -------------------------------------------------------

    private void TryResolvePlayerReferences()
    {
        var controllers = FindObjectsByType<MultiplayerPlayerController>(FindObjectsSortMode.None);

        p1Controller = null;
        p2Controller = null;

        foreach (var c in controllers)
        {
            if (c.PlayerID == 1)      { p1Controller = c; p1Health = c.GetComponent<PlayerHealth>(); p1Combat = c.GetComponent<CombatSystem>(); }
            else if (c.PlayerID == 2) { p2Controller = c; p2Health = c.GetComponent<PlayerHealth>(); p2Combat = c.GetComponent<CombatSystem>(); }
        }

        // only mark resolved once BOTH players are found with valid IDs
        if (p1Controller == null || p2Controller == null) return;

        // subscribe to health events now that we have both
        p1Health.OnKillerShotTriggered.AddListener(ActivateKillerShotPhase);
        p2Health.OnKillerShotTriggered.AddListener(ActivateKillerShotPhase);

        playersResolved = true;
        RefreshGamepads();

        Debug.Log("[KillerShotManager] both players resolved — ready");
    }

    // -------------------------------------------------------
    // phase flow
    // -------------------------------------------------------

    private void ActivateKillerShotPhase()
    {
        if (killerShotActive) return;

        killerShotActive       = true;
        reactionWindowOpen     = false;
        p1PressedEarly         = false;
        p2PressedEarly         = false;
        reactionWindowDuration = Random.Range(minReactionWindow, maxReactionWindow);
        windowTimer            = reactionWindowDuration;

        p1Combat?.SetCombatEnabled(false);
        p2Combat?.SetCombatEnabled(false);

        OnKillerShotPhaseStarted?.Invoke();
        StartHaptics();

        reactionCoroutine = StartCoroutine(ReactionWindowRoutine());
        Debug.Log($"[KillerShot] phase started — window: {reactionWindowDuration:F1}s");
    }

    private IEnumerator ReactionWindowRoutine()
    {
        yield return new WaitForSeconds(0.5f);  // brief delay before window opens

        reactionWindowOpen = true;
        Debug.Log("[KillerShot] window OPEN");

        float elapsed = 0f;
        while (elapsed < reactionWindowDuration && killerShotActive)
        {
            elapsed     += Time.deltaTime;
            windowTimer  = reactionWindowDuration - elapsed;
            yield return null;
        }

        if (killerShotActive)
        {
            killerShotActive   = false;
            reactionWindowOpen = false;
            StopHaptics();
            ReenableCombat();
            OnKillerShotExpired?.Invoke();
            OnKillerShotPhaseEnded?.Invoke();
            Debug.Log("[KillerShot] expired — nobody reacted");
        }
    }

    private void CheckPerfect(int playerID)
    {
        if (windowTimer <= perfectTimingWindow)
        {
            OnPerfectPress?.Invoke(playerID);
            Debug.Log($"[KillerShot] P{playerID} PERFECT — {windowTimer:F2}s left");
        }
    }

    private void ResolveKillerShot(int winnerID)
    {
        if (!killerShotActive) return;

        killerShotActive   = false;
        reactionWindowOpen = false;
        StopHaptics();

        if (reactionCoroutine != null) StopCoroutine(reactionCoroutine);

        if (winnerID == 1) p2Health?.TakeKillerShotDamage();
        else               p1Health?.TakeKillerShotDamage();

        ReenableCombat();
        OnKillerShotWinner?.Invoke(winnerID);
        OnKillerShotPhaseEnded?.Invoke();

        Debug.Log($"[KillerShot] P{winnerID} wins!");
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
    // haptics
    // -------------------------------------------------------

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

    private void OnDestroy() { StopHaptics(); }
}