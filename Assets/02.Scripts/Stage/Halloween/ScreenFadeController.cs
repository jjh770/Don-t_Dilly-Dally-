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

    private Color _originalSkyColor;
    private Color _originalEquatorColor;
    private PlayerSpotLightController _activeSpotLight;
    private PhotonView _photonView;
    private Coroutine _blackoutCoroutine;

    private void Awake()
    {
        _photonView = GetComponent<PhotonView>();
        if (_photonView == null)
        {
            _photonView = gameObject.AddComponent<PhotonView>();
        }
    }

    private void Start()
    {
        _originalSkyColor = RenderSettings.ambientSkyColor;
        _originalEquatorColor = RenderSettings.ambientEquatorColor;

        // 마스터 클라이언트만 블랙아웃 사이클 관리
        if (PhotonNetwork.IsMasterClient)
        {
            StartCoroutine(BlackoutCycle());
        }
    }

    private IEnumerator BlackoutCycle()
    {
        while (true)
        {
            float waitTime = Random.Range(_minInterval, _maxInterval);
            yield return new WaitForSeconds(waitTime);

            // 모든 클라이언트에 블랙아웃 시작 알림
            _photonView.RPC(nameof(RPC_StartBlackout), RpcTarget.All);

            yield return new WaitForSeconds(_blackoutDuration);

            // 모든 클라이언트에 블랙아웃 종료 알림
            _photonView.RPC(nameof(RPC_EndBlackout), RpcTarget.All);
        }
    }

    [PunRPC]
    private void RPC_StartBlackout()
    {
        if (_blackoutCoroutine != null)
        {
            StopCoroutine(_blackoutCoroutine);
        }
        _blackoutCoroutine = StartCoroutine(FadeToBlack());
    }

    [PunRPC]
    private void RPC_EndBlackout()
    {
        if (_blackoutCoroutine != null)
        {
            StopCoroutine(_blackoutCoroutine);
        }
        _blackoutCoroutine = StartCoroutine(FadeToNormal());
    }

    private IEnumerator FadeToBlack()
    {
        SpawnSpotLightForLocalPlayer();
        float t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime * _fadeSpeed;
            t = Mathf.Min(1f, t);
            RenderSettings.ambientSkyColor = Color.Lerp(_originalSkyColor, Color.black, t);
            RenderSettings.ambientEquatorColor = Color.Lerp(_originalEquatorColor, Color.black, t);
            yield return null;
        }

        RenderSettings.ambientSkyColor = Color.black;
        RenderSettings.ambientEquatorColor = Color.black;
    }

    private IEnumerator FadeToNormal()
    {
        float t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime * _fadeSpeed;
            t = Mathf.Min(1f, t);
            RenderSettings.ambientSkyColor = Color.Lerp(Color.black, _originalSkyColor, t);
            RenderSettings.ambientEquatorColor = Color.Lerp(Color.black, _originalEquatorColor, t);
            yield return null;
        }

        RenderSettings.ambientSkyColor = _originalSkyColor;
        RenderSettings.ambientEquatorColor = _originalEquatorColor;
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
        RenderSettings.ambientSkyColor = _originalSkyColor;
        RenderSettings.ambientEquatorColor = _originalEquatorColor;
        DestroySpotLight();
    }
}
