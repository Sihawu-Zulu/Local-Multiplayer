using System.Collections;
using UnityEngine;

// singleton hitstop - briefly freezes timescale on heavy impacts
// camera shake uses unscaledDeltaTime so it still plays during the freeze

public class HitStop : MonoBehaviour
{
    public static HitStop Instance { get; private set; }

    private Coroutine stopCoroutine;

    private void Awake()
    {
        Instance = this;
    }

    // freezeScale: 0 = full stop, 0.05 = near-freeze
    public void Freeze(float duration, float freezeScale = 0.05f)
    {
        if (stopCoroutine != null) StopCoroutine(stopCoroutine);
        stopCoroutine = StartCoroutine(FreezeRoutine(duration, freezeScale));
    }

    private IEnumerator FreezeRoutine(float duration, float freezeScale)
    {
        Time.timeScale = freezeScale;
        yield return new WaitForSecondsRealtime(duration);
        Time.timeScale = 1f;
    }
}
