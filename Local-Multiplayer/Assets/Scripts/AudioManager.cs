using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }



    [Header("Master Volumes")]
    [SerializeField, Range(0f, 1f)] private float masterVolume = 1f;
    [SerializeField, Range(0f, 1f)] private float musicVolume  = 0.7f;
    [SerializeField, Range(0f, 1f)] private float sfxVolume    = 1f;

    public float MasterVolume { get => masterVolume; set { masterVolume = value; RefreshMusicVolume(); } }
    public float MusicVolume  { get => musicVolume;  set { musicVolume  = value; RefreshMusicVolume(); } }
    public float SFXVolume    { get => sfxVolume;   set { sfxVolume    = value; } }

   
    [Header("Music")]
    public AudioClip musicTrack;   

   

    [Header("Combat SFX")]
    public AudioClip lightHit;
    public AudioClip heavyHit;
    public AudioClip blockImpact;

    [Header("Knockdown SFX")]
    public AudioClip knockdownFall;
    public AudioClip mashGetUp;
    public AudioClip standUpSuccess;

    [Header("Arm SFX")]
    public AudioClip armDetach;
    public AudioClip stringPullLoop;

    [Header("Killer Shot SFX")]
    public AudioClip killerShotTrigger;
    public AudioClip killerShotWin;
    public AudioClip killerShotEarly;
    public AudioClip killerShotPerfect;

    [Header("Round SFX")]
    public AudioClip roundStartBell;
    public AudioClip roundWinChant;
    public AudioClip matchWinChant;
    public AudioClip countdownTick;
    public AudioClip countdownFinalTick;

    [Header("UI SFX")]
    public AudioClip menuNavigate;
    public AudioClip menuConfirm;
    public AudioClip menuBack;

    [Header("Ambient")]
    public AudioClip ambientLoop;


    [Header("SFX Pool")]
    [SerializeField] private int sfxPoolSize = 16;

    private List<AudioSource> sfxPool = new List<AudioSource>();
    private AudioSource  musicSource;
    private AudioSource ambientSource;
    private AudioSource  stringLoopSource;

    private float musicTargetVolume = 0f;
    private Coroutine musicFadeCoroutine;



    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        BuildSFXPool();
        BuildMusicSource();
        BuildAmbientSource();
        BuildStringLoopSource();
    }

    private void BuildSFXPool()
    {
        for (int i = 0; i < sfxPoolSize; i++)
        {
            var src = gameObject.AddComponent<AudioSource>();
            src.playOnAwake = false;
            sfxPool.Add(src);
        }
    }

    private void BuildMusicSource()
    {
        musicSource  = gameObject.AddComponent<AudioSource>();
        musicSource.clip = musicTrack;
        musicSource.loop = true;
        musicSource.playOnAwake = false;
        musicSource.volume = 0f;

        if (musicTrack != null) musicSource.Play();
    }

    private void BuildAmbientSource()
    {
        ambientSource = gameObject.AddComponent<AudioSource>();
        ambientSource.clip  = ambientLoop;
        ambientSource.loop  = true;
        ambientSource.playOnAwake = false;
        ambientSource.volume = 0f;
    }

    private void BuildStringLoopSource()
    {
        stringLoopSource = gameObject.AddComponent<AudioSource>();
        stringLoopSource.loop  = true;
        stringLoopSource.playOnAwake = false;
        stringLoopSource.volume  = 0f;
    }

 
    public void SetMusicVolume(float targetVolume, float fadeDuration = 1f)
    {
        musicTargetVolume = targetVolume;

        if (musicFadeCoroutine != null) StopCoroutine(musicFadeCoroutine);
        musicFadeCoroutine = StartCoroutine(
            FadeSource(musicSource, masterVolume * musicVolume * targetVolume, fadeDuration));
    }

   
    public void RestartMusic()
    {
        if (musicTrack == null) return;
        musicSource.Stop();
        musicSource.Play();
    }

    private void RefreshMusicVolume()
    {
        musicSource.volume   = masterVolume * musicVolume * musicTargetVolume;
        ambientSource.volume = masterVolume * musicVolume * 0.35f;
    }



    public void StartAmbient(float fadeDuration = 2f)
    {
        if (ambientLoop == null) return;
        if (!ambientSource.isPlaying) ambientSource.Play();
        StartCoroutine(FadeSource(ambientSource, masterVolume * musicVolume * 0.35f, fadeDuration));
    }

    public void StopAmbient(float fadeDuration = 1.5f)
    {
        StartCoroutine(FadeSource(ambientSource, 0f, fadeDuration, stopOnComplete: true));
    }


    public void PlaySFX(AudioClip clip, float volume = 1f, float pitchVariance = 0f)
    {
        if (clip == null) return;
        AudioSource src = GetFreeSFXSource();
        src.clip = clip;
        src.volume  = masterVolume * sfxVolume * volume;
        src.pitch = 1f + Random.Range(-pitchVariance, pitchVariance);
        src.Play();
    }


    public void Play(AudioClip clip, float volume = 1f, float pitchVariance = 0f)
        => PlaySFX(clip, volume, pitchVariance);

    private AudioSource GetFreeSFXSource()
    {
        foreach (var s in sfxPool)
            if (!s.isPlaying) return s;
        return sfxPool[0];    
    }

 
    public void StartStringPullLoop()
    {
        if (stringPullLoop == null || stringLoopSource.isPlaying) return;
        stringLoopSource.clip   = stringPullLoop;
        stringLoopSource.volume = masterVolume * sfxVolume * 0.55f;
        stringLoopSource.Play();
    }

    public void StopStringPullLoop() => stringLoopSource.Stop();



    private IEnumerator FadeSource(AudioSource src, float targetVol, float duration,
                                   bool stopOnComplete = false)
    {
        float startVol = src.volume;
        float elapsed  = 0f;

        while (elapsed < duration)
        {
            elapsed   += Time.unscaledDeltaTime;
            src.volume = Mathf.Lerp(startVol, targetVol, elapsed / duration);
            yield return null;
        }

        src.volume = targetVol;
        if (stopOnComplete) src.Stop();
    }
}