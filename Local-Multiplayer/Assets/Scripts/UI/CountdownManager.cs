using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class CountdownManager : MonoBehaviour
{
    [Header("Countdown Text")]
    [SerializeField] private string readyText  = "Ready?";
    [SerializeField] private string fightText  = "FIGHT!";
    [SerializeField] private string[] countdownSteps = { "3", "2", "1" };

    [Header("Timing")]
    [SerializeField] private float readyDuration    = 1.2f;  
    [SerializeField] private float stepDuration     = 0.85f;  
    [SerializeField] private float fightDuration    = 0.9f;   

    // --- events ---
    public UnityEvent OnCountdownStarted;
    public UnityEvent<string>  OnCountdownStep;    
    public UnityEvent  OnFightStarted;     
    public UnityEvent OnCountdownFinished; 


    private bool countdownStarted = false;

    // -------------------------------------------------------


    public void StartCountdown()
    {
 

        if (countdownStarted) return;
        countdownStarted = true;
        StartCoroutine(CountdownRoutine());
    }

    // -------------------------------------------------------

    private IEnumerator CountdownRoutine()
    {
       
        SetPlayersEnabled(false);

        OnCountdownStarted?.Invoke();

       
        OnCountdownStep?.Invoke(readyText);
        yield return new WaitForSeconds(readyDuration);

    
        foreach (string step in countdownSteps)
        {
            OnCountdownStep?.Invoke(step);
            yield return new WaitForSeconds(stepDuration);
        }

    
        SetPlayersEnabled(true);
        OnFightStarted?.Invoke();
        OnCountdownStep?.Invoke(fightText);

        yield return new WaitForSeconds(fightDuration);

        OnCountdownFinished?.Invoke();
    }


    private void SetPlayersEnabled(bool enabled)
    {
        var controllers = FindObjectsByType<MultiplayerPlayerController>(FindObjectsSortMode.None);
        foreach (var c in controllers)
        {
            c.SetMovementEnabled(enabled);
        }

        var combatSystems = FindObjectsByType<CombatSystem>(FindObjectsSortMode.None);
        foreach (var cs in combatSystems)
        {
            cs.SetCombatEnabled(enabled);
        }
    }
}
