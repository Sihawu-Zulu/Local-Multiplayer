using System.Collections;
using UnityEngine;

// attach to your main camera
// call CameraShake.Instance.Shake(duration, magnitude) from anywhere
// uses a simple perlin noise offset on localPosition so it stacks cleanly

public class CameraShake : MonoBehaviour
{
    public static CameraShake Instance { get; private set; }

    private Vector3       originalLocalPos;
    private Coroutine     shakeCoroutine;

    // -------------------------------------------------------

    private void Awake()
    {
        Instance         = this;
        originalLocalPos = transform.localPosition;
    }

    // -------------------------------------------------------

    public void Shake(float duration, float magnitude)
    {
        if (shakeCoroutine != null) StopCoroutine(shakeCoroutine);
        shakeCoroutine = StartCoroutine(ShakeRoutine(duration, magnitude));
    }

    private IEnumerator ShakeRoutine(float duration, float magnitude)
    {
        float elapsed = 0f;
        float seed    = Random.value * 100f;   // unique offset per shake so it doesn't repeat

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;   // unscaled so hitstop doesn't kill the shake

            // fade out toward the end
            float strength = Mathf.Lerp(magnitude, 0f, elapsed / duration);

            float x = (Mathf.PerlinNoise(seed + elapsed * 30f, 0f) - 0.5f) * 2f * strength;
            float y = (Mathf.PerlinNoise(0f, seed + elapsed * 30f) - 0.5f) * 2f * strength;

            transform.localPosition = originalLocalPos + new Vector3(x, y, 0f);

            yield return null;
        }

        transform.localPosition = originalLocalPos;
    }
}
