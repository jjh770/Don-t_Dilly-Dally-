using UnityEngine;
using System.Collections;
using Photon.Pun;

public class ScreenFadeController : MonoBehaviour
{
    [Header("블랙아웃 설정")]
    [SerializeField] private float minInterval = 120f;
    [SerializeField] private float maxInterval = 180f;
    [SerializeField] private float blackoutDuration = 15f;
    [SerializeField] private float fadeSpeed = 2f;

    [Header("스포트라이트 설정")]
    [SerializeField] private GameObject spotLightPrefab;
    [SerializeField] private string playerTag = "Player";

    private float originalIntensity;
    private PlayerSpotLightController activeSpotLight;

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
        SpawnSpotLightForLocalPlayer();
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
        DestroySpotLight();
    }

    private void SpawnSpotLightForLocalPlayer()
    {
        if (spotLightPrefab == null) return;

        // 로컬 플레이어 찾기
        GameObject[] players = GameObject.FindGameObjectsWithTag(playerTag);
        GameObject localPlayer = null;

        foreach (GameObject player in players)
        {
            PhotonView pv = player.GetComponent<PhotonView>();
            if (pv != null && pv.IsMine)
            {
                localPlayer = player;
                break;
            }
        }

        if (localPlayer == null) return;

        GameObject spotLightObj = Instantiate(spotLightPrefab);
        activeSpotLight = spotLightObj.GetComponent<PlayerSpotLightController>();

        if (activeSpotLight == null)
        {
            activeSpotLight = spotLightObj.AddComponent<PlayerSpotLightController>();
        }

        activeSpotLight.SetTarget(localPlayer.transform);
        activeSpotLight.SetActive(true);
    }

    private void DestroySpotLight()
    {
        if (activeSpotLight != null)
        {
            Destroy(activeSpotLight.gameObject);
            activeSpotLight = null;
        }
    }

    private void OnDestroy()
    {
        RenderSettings.ambientIntensity = originalIntensity;
        DestroySpotLight();
    }
}
