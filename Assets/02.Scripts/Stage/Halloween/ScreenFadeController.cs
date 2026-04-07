using UnityEngine;
using System.Collections;
using Photon.Pun;

public class ScreenFadeController : MonoBehaviour
{
    [Header("블랙아웃 설정")]
    [SerializeField] private float _minInterval = 120f;
    [SerializeField] private float _maxInterval = 180f;
    [SerializeField] private float _blackoutDuration = 15f;
    [SerializeField] private float _fadeSpeed = 1f;

    [Header("스포트라이트 설정")]
    [SerializeField] private GameObject _spotLightPrefab;
    [SerializeField] private string _playerTag = "Player";

    private float _originalIntensity;
    private PlayerSpotLightController _activeSpotLight;

    private void Start()
    {
        _originalIntensity = RenderSettings.ambientIntensity;
        StartCoroutine(BlackoutCycle());
    }

    private IEnumerator BlackoutCycle()
    {
        while (true)
        {
            float waitTime = Random.Range(_minInterval, _maxInterval);
            yield return new WaitForSeconds(waitTime);
            yield return StartCoroutine(FadeToBlack());
            yield return new WaitForSeconds(_blackoutDuration);
            yield return StartCoroutine(FadeToNormal());
        }
    }

    private IEnumerator FadeToBlack()
    {
        SpawnSpotLightForLocalPlayer();
        float currentIntensity = RenderSettings.ambientIntensity;

        while (currentIntensity > 0f)
        {
            currentIntensity -= Time.deltaTime * _fadeSpeed;
            currentIntensity = Mathf.Max(0f, currentIntensity);
            RenderSettings.ambientIntensity = currentIntensity;
            yield return null;
        }

        RenderSettings.ambientIntensity = 0f;
    }

    private IEnumerator FadeToNormal()
    {
        float currentIntensity = RenderSettings.ambientIntensity;

        while (currentIntensity < _originalIntensity)
        {
            currentIntensity += Time.deltaTime * _fadeSpeed;
            currentIntensity = Mathf.Min(_originalIntensity, currentIntensity);
            RenderSettings.ambientIntensity = currentIntensity;
            yield return null;
        }

        RenderSettings.ambientIntensity = _originalIntensity;
        DestroySpotLight();
    }

    private void SpawnSpotLightForLocalPlayer()
    {
        if (_spotLightPrefab == null) return;

        // 로컬 플레이어 찾기
        GameObject[] players = GameObject.FindGameObjectsWithTag(_playerTag);
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

        GameObject spotLightObj = Instantiate(_spotLightPrefab);
        _activeSpotLight = spotLightObj.GetComponent<PlayerSpotLightController>();

        if (_activeSpotLight == null)
        {
            _activeSpotLight = spotLightObj.AddComponent<PlayerSpotLightController>();
        }

        _activeSpotLight.SetTarget(localPlayer.transform);
        _activeSpotLight.SetActive(true);
    }

    private void DestroySpotLight()
    {
        if (_activeSpotLight != null)
        {
            Destroy(_activeSpotLight.gameObject);
            _activeSpotLight = null;
        }
    }

    private void OnDestroy()
    {
        RenderSettings.ambientIntensity = _originalIntensity;
        DestroySpotLight();
    }
}
