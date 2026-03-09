using System.Collections;
using UnityEngine;

public class HitStop : MonoBehaviour
{
    public static HitStop Instance { get; private set; }

    [Header("Debug")]
    [SerializeField] private bool logFreezeCalls = false;

    private Coroutine stopCoroutine;
    private bool isFrozen = false;


    private void Awake()
    {
        
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void OnDestroy()
    {

        if (isFrozen)
        {
            Time.timeScale = 1f;
            isFrozen = false;
        }

        if (Instance == this) Instance = null;
    }

    public void Freeze(float duration, float freezeScale = 0.05f)
    {
        if (logFreezeCalls)
            Debug.Log($"[HitStop] Freeze dur={duration} scale={freezeScale}");

        if (stopCoroutine != null) StopCoroutine(stopCoroutine);
        stopCoroutine = StartCoroutine(FreezeRoutine(duration, freezeScale));
    }

    private IEnumerator FreezeRoutine(float duration, float freezeScale)
    {
        isFrozen = true;
        Time.timeScale = freezeScale;

        yield return new WaitForSecondsRealtime(duration);

        Time.timeScale = 1f;
        isFrozen= false;
        stopCoroutine  = null;
    }
}