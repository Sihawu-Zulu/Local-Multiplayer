using System.Collections.Generic;
using UnityEngine;

// singleton audio manager with a pooled audiosource list
// assign clips in inspector, call AudioManager.Instance.Play() anywhere
// string pull gets its own dedicated looping source

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Pool")]
    [SerializeField] private int poolSize = 12;

    [Header("Hit SFX")]
    public AudioClip lightHit;
    public AudioClip heavyHit;

    [Header("Knockdown SFX")]
    public AudioClip knockdownFall;
    public AudioClip mashGetUp;       // plays each mash press
    public AudioClip standUpSuccess;

    [Header("Arm SFX")]
    public AudioClip armDetach;
    public AudioClip stringPullLoop;  // looping — started/stopped by KnockdownManager

    [Header("Killer Shot SFX")]
    public AudioClip killerShotTrigger;

    [Header("Master Volume")]
    [SerializeField, Range(0f, 1f)] private float masterVolume = 1f;

    private List<AudioSource> pool   = new List<AudioSource>();
    private AudioSource       loopSrc;   // dedicated source for string pull loop

    // -------------------------------------------------------

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        BuildPool();
    }

    private void BuildPool()
    {
        for (int i = 0; i < poolSize; i++)
        {
            var s = gameObject.AddComponent<AudioSource>();
            s.playOnAwake = false;
            pool.Add(s);
        }

        loopSrc             = gameObject.AddComponent<AudioSource>();
        loopSrc.playOnAwake = false;
        loopSrc.loop        = true;
        loopSrc.volume      = masterVolume * 0.55f;
    }

    // -------------------------------------------------------

    // play a one-shot from the pool with optional pitch variance to avoid robotic repetition
    public void Play(AudioClip clip, float volume = 1f, float pitchVariance = 0f)
    {
        if (clip == null) return;

        AudioSource src = GetFree();
        src.clip   = clip;
        src.volume = masterVolume * volume;
        src.pitch  = 1f + Random.Range(-pitchVariance, pitchVariance);
        src.Play();
    }

    public void StartStringPullLoop()
    {
        if (stringPullLoop == null || loopSrc.isPlaying) return;
        loopSrc.clip = stringPullLoop;
        loopSrc.Play();
    }

    public void StopStringPullLoop()
    {
        loopSrc.Stop();
    }

    // -------------------------------------------------------

    private AudioSource GetFree()
    {
        foreach (var s in pool)
            if (!s.isPlaying) return s;

        // pool exhausted — reuse oldest rather than dropping the sound
        return pool[0];
    }
}
