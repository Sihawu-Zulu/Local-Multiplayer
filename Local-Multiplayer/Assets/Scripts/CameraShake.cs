using System.Collections;
using UnityEngine;

public class CameraShake : MonoBehaviour
{
    public static CameraShake Instance { get; private set; }

    [Header("Debug")]
    [SerializeField] private bool logShakeCalls = false;

    private Vector3  originLocalPos;
    private bool originCaptured = false;
    private Coroutine shakeCoroutine;



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
        if (Instance == this) Instance = null;
    }



    public void Shake(float duration, float magnitude)
    {
      
        if (!originCaptured)
        {
            originLocalPos  = transform.localPosition;
            originCaptured  = true;
        }

        if (logShakeCalls)
            Debug.Log($"[CameraShake] Shake dur={duration} mag={magnitude}  obj={gameObject.name}");

        if (shakeCoroutine != null) StopCoroutine(shakeCoroutine);
        shakeCoroutine = StartCoroutine(ShakeRoutine(duration, magnitude));
    }

 
    public void ResetOrigin()
    {
        originLocalPos = transform.localPosition;
        originCaptured = true;
    }

   

    private IEnumerator ShakeRoutine(float duration, float magnitude)
    {
        float elapsed = 0f;
        float seed    = Random.value * 100f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;   

            float t = Mathf.Clamp01(elapsed / duration);
            float strength = Mathf.Lerp(magnitude, 0f, t);   

            float x = (Mathf.PerlinNoise(seed + elapsed * 30f, 0f) - 0.5f) * 2f * strength;
            float y = (Mathf.PerlinNoise(0f, seed + elapsed * 30f) - 0.5f) * 2f * strength;

            transform.localPosition = originLocalPos + new Vector3(x, y, 0f);

            yield return null;
        }

        transform.localPosition = originLocalPos;
        shakeCoroutine = null;
    }
}