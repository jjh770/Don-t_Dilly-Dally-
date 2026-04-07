using UnityEngine;
using System.Collections;

public class ScreenFadeController : MonoBehaviour
{
    [Header("블랙아웃 설정")]
    [SerializeField] private float minInterval = 120f; 
    [SerializeField] private float maxInterval = 180f; 
    [SerializeField] private float blackoutDuration = 15f;
    [SerializeField] private float fadeSpeed = 2f; 

    private float originalIntensity;
    private bool isBlackout;

    private void Start()
    {
        originalIntensity = RenderSettings.ambientIntensity;
        StartCoroutine(BlackoutCycle());
    }

    private IEnumerator BlackoutCycle()
    {
        while (true)
        {
            float waitTime = Random.Range(minInterval, maxInterval);
            yield return new WaitForSeconds(waitTime);
            yield return StartCoroutine(FadeToBlack());
            yield return new WaitForSeconds(blackoutDuration);
            yield return StartCoroutine(FadeToNormal());
        }
    }

    private IEnumerator FadeToBlack()
    {
        isBlackout = true;
        float currentIntensity = RenderSettings.ambientIntensity;

        while (currentIntensity > 0f)
        {
            currentIntensity -= Time.deltaTime * fadeSpeed;
            currentIntensity = Mathf.Max(0f, currentIntensity);
            RenderSettings.ambientIntensity = currentIntensity;
            yield return null;
        }

        RenderSettings.ambientIntensity = 0f;
    }

    private IEnumerator FadeToNormal()
    {
        float currentIntensity = RenderSettings.ambientIntensity;

        while (currentIntensity < originalIntensity)
        {
            currentIntensity += Time.deltaTime * fadeSpeed;
            currentIntensity = Mathf.Min(originalIntensity, currentIntensity);
            RenderSettings.ambientIntensity = currentIntensity;
            yield return null;
        }

        RenderSettings.ambientIntensity = originalIntensity;
        isBlackout = false;
    }

    private void OnDestroy()
    {
        RenderSettings.ambientIntensity = originalIntensity;
    }
}
