using System.Collections;
using UnityEngine;



public class HitStop : MonoBehaviour
{
    public static HitStop Instance { get; private set; }

    private Coroutine stopCoroutine;

    private void Awake()
    {
        Instance = this;
    }

   
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
