using System.Collections;
using UnityEngine;

public class RoundAudioController : MonoBehaviour
{
   
    [SerializeField] private RoundManager      roundManager;
    [SerializeField] private KillerShotManager killerShotManager;

    
    [SerializeField] private float bellDelay  = 0.25f;
    [SerializeField] private float chantDelay = 0.55f;

    private bool managersResolved  = false;
    private bool musicKickedOff    = false;

  
    private void Update()
    {
        if (managersResolved) return;
        TryResolveManagers();
    }

    private void TryResolveManagers()
    {
        if (roundManager == null)
            roundManager = FindObjectOfType<RoundManager>();

        if (killerShotManager == null)
            killerShotManager = FindObjectOfType<KillerShotManager>();

        if (roundManager == null || killerShotManager == null) return;

        SubscribeEvents();
        managersResolved = true;

        
        if (!musicKickedOff)
        {
            musicKickedOff = true;
            MusicManager.Instance?.TransitionToPreFight();
        }

        
    }

    private void OnDestroy() => UnsubscribeEvents();

   

    private void SubscribeEvents()
    {
        roundManager.OnRoundStarted.AddListener(OnRoundStarted);
        roundManager.OnRoundWon.AddListener(OnRoundWon);
        roundManager.OnMatchWon.AddListener(OnMatchWon);

        killerShotManager.OnKillerShotPhaseStarted.AddListener(OnKillerShotStarted);
        killerShotManager.OnKillerShotPhaseEnded.AddListener(OnKillerShotEnded);
        killerShotManager.OnKillerShotWinner.AddListener(OnKillerShotWinner);
        killerShotManager.OnEarlyPress.AddListener(OnEarlyPress);
        killerShotManager.OnPerfectPress.AddListener(OnPerfectPress);
    }

    private void UnsubscribeEvents()
    {
        if (roundManager != null)
        {
            roundManager.OnRoundStarted.RemoveListener(OnRoundStarted);
            roundManager.OnRoundWon.RemoveListener(OnRoundWon);
            roundManager.OnMatchWon.RemoveListener(OnMatchWon);
        }

        if (killerShotManager != null)
        {
            killerShotManager.OnKillerShotPhaseStarted.RemoveListener(OnKillerShotStarted);
            killerShotManager.OnKillerShotPhaseEnded.RemoveListener(OnKillerShotEnded);
            killerShotManager.OnKillerShotWinner.RemoveListener(OnKillerShotWinner);
            killerShotManager.OnEarlyPress.RemoveListener(OnEarlyPress);
            killerShotManager.OnPerfectPress.RemoveListener(OnPerfectPress);
        }
    }



    private void OnRoundStarted(int roundNumber)
    {
        StartCoroutine(BellThenFight());
    }

    private IEnumerator BellThenFight()
    {
        yield return new WaitForSeconds(bellDelay);
        AudioManager.Instance?.PlaySFX(AudioManager.Instance.roundStartBell, volume: 1f, pitchVariance: 0.02f);
        MusicManager.Instance?.TransitionToFighting();
    }

    private void OnRoundWon(int winnerID)
    {
        MusicManager.Instance?.TransitionToRoundEnd();
        StartCoroutine(ChantAfterDelay(AudioManager.Instance?.roundWinChant, chantDelay));
    }

    private void OnMatchWon(int winnerID)
    {
        MusicManager.Instance?.TransitionToMatchEnd();
        StartCoroutine(ChantAfterDelay(AudioManager.Instance?.matchWinChant, chantDelay + 0.3f, volume: 1f));
    }

    private IEnumerator ChantAfterDelay(AudioClip clip, float delay, float volume = 0.9f)
    {
        if (clip == null) yield break;
        yield return new WaitForSeconds(delay);
        AudioManager.Instance?.PlaySFX(clip, volume: volume, pitchVariance: 0.03f);
    }



    private void OnKillerShotStarted(int playerID)
    {
        AudioManager.Instance?.PlaySFX(AudioManager.Instance.killerShotTrigger, volume: 0.9f, pitchVariance: 0.04f);
        MusicManager.Instance?.TransitionToKillerShot();
    }

    private void OnKillerShotEnded()
    {
        if (MusicManager.Instance?.CurrentState == MusicManager.MusicState.KillerShot)
            MusicManager.Instance?.TransitionToFighting();
    }

    private void OnKillerShotWinner(int winnerID)
    {
        AudioManager.Instance?.PlaySFX(AudioManager.Instance.killerShotWin, volume: 1f, pitchVariance: 0.02f);
    }

    private void OnEarlyPress(int playerID)
    {
        AudioManager.Instance?.PlaySFX(AudioManager.Instance.killerShotEarly, volume: 0.7f, pitchVariance: 0.05f);
    }

    private void OnPerfectPress(int playerID)
    {
        AudioManager.Instance?.PlaySFX(AudioManager.Instance.killerShotPerfect, volume: 1f);
    }



    public void PlayCountdownTick(bool isFinalTick = false)
    {
        AudioClip clip = isFinalTick
            ? AudioManager.Instance?.countdownFinalTick
            : AudioManager.Instance?.countdownTick;

        AudioManager.Instance?.PlaySFX(clip, volume: 0.85f, pitchVariance: 0.01f);
    }
}