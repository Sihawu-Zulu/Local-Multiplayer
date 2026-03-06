using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(AudioManager))]
public class MusicManager : MonoBehaviour
{
    public static MusicManager Instance { get; private set; }

    [Header("Scenes")]
    [SerializeField] private string menuSceneName = "MainMenu";
    [SerializeField] private string gameSceneName = "Game";

   
    [SerializeField, Range(0f, 1f)] private float menuVolume       = 0.45f;
    [SerializeField, Range(0f, 1f)] private float preFightVolume   = 0.7f;
    [SerializeField, Range(0f, 1f)] private float fightingVolume   = 1f;
    [SerializeField, Range(0f, 1f)] private float killerShotVolume = 0.75f;
    [SerializeField, Range(0f, 1f)] private float roundEndVolume   = 0.25f;
    [SerializeField, Range(0f, 1f)] private float matchEndVolume   = 0f;

    [Header("Fade durationns (seconds)")]
    [SerializeField] private float menuFadeIn  = 2f;   
    [SerializeField] private float preFightFade = 2.5f;  
    [SerializeField] private float fightSnapFade = 0.5f;  
    [SerializeField] private float killerShotFade= 0.8f;
    [SerializeField] private float roundEndFade  = 0.4f; 
    [SerializeField] private float matchEndFade = 3f;

  

    public enum MusicState { None, Menu, PreFight, Fighting, KillerShot, RoundEnd, MatchEnd }
    public MusicState CurrentState { get; private set; } = MusicState.None;

    private AudioManager audio;

    

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        audio = GetComponent<AudioManager>();
    }

    private void Start()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        ApplyCurrentScene(SceneManager.GetActiveScene().name);
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }



    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ApplyCurrentScene(scene.name);
    }

    private void ApplyCurrentScene(string sceneName)
    {
        if (sceneName == menuSceneName)
        {
            audio.StartAmbient(fadeDuration: 3f);
            TransitionTo(MusicState.Menu);
        }
        else if (sceneName == gameSceneName)
        {
            
        }
    }

    

    public void TransitionToPreFight() => TransitionTo(MusicState.PreFight);
    public void TransitionToFighting()  => TransitionTo(MusicState.Fighting);
    public void TransitionToKillerShot() => TransitionTo(MusicState.KillerShot);
    public void TransitionToRoundEnd() => TransitionTo(MusicState.RoundEnd);
    public void TransitionToMatchEnd() => TransitionTo(MusicState.MatchEnd);

  

    private void TransitionTo(MusicState newState)
    {
        if (newState == CurrentState) return;
        CurrentState = newState;

        switch (newState)
        {
            case MusicState.Menu:
                audio.SetMusicVolume(menuVolume, CurrentState == MusicState.None ? 0.05f : menuFadeIn);
                break;

            case MusicState.PreFight:
            
                audio.SetMusicVolume(preFightVolume, preFightFade);
                break;

            case MusicState.Fighting:
              
                audio.SetMusicVolume(fightingVolume, fightSnapFade);
                break;

            case MusicState.KillerShot:
             
                audio.SetMusicVolume(killerShotVolume, killerShotFade);
                break;

            case MusicState.RoundEnd:
                
                audio.SetMusicVolume(roundEndVolume, roundEndFade);
                break;

            case MusicState.MatchEnd:
                
                audio.SetMusicVolume(matchEndVolume, matchEndFade);
                audio.StartAmbient(fadeDuration: matchEndFade);
                break;
        }
    }
}