using Photon.Pun;
using UnityEngine;

[RequireComponent(typeof(HoldableItem))]
public class HoldableItemThrowVfx : MonoBehaviour
{
    [SerializeField] private ThrowTrailVfx _trailPrefab;
    [SerializeField] private GameObject _landingDustPrefab;
    [SerializeField] private float _landingDustDestroyDelay = 3f;

    private HoldableItem _holdableItem;
    private PhotonView _photonView;
    private ThrowTrailVfx _activeTrail;

    private void Awake()
    {
        _holdableItem = GetComponent<HoldableItem>();
        _photonView = GetComponent<PhotonView>();
    }

    private void OnEnable()
    {
        _holdableItem.ThrowStarted += HandleThrowStarted;
        _holdableItem.Landed += HandleLanded;
    }

    private void OnDisable()
    {
        if (_holdableItem != null)
        {
            _holdableItem.ThrowStarted -= HandleThrowStarted;
            _holdableItem.Landed -= HandleLanded;
        }

        StopTrailLocal();
    }

    private void HandleThrowStarted()
    {
        if (CanBroadcast())
        {
            _photonView.RPC(nameof(RpcPlayTrail), RpcTarget.All);
            return;
        }

        PlayTrailLocal();
    }

    private void HandleLanded()
    {
        if (CanBroadcast())
        {
            _photonView.RPC(nameof(RpcStopTrailAndLand), RpcTarget.All);
            return;
        }

        StopTrailLocal();
        SpawnDustLocal();
    }

    private bool CanBroadcast()
    {
        return _photonView != null && PhotonNetwork.IsConnectedAndReady && PhotonNetwork.InRoom;
    }

    [PunRPC]
    private void RpcPlayTrail()
    {
        PlayTrailLocal();
    }

    [PunRPC]
    private void RpcStopTrailAndLand()
    {
        StopTrailLocal();
        SpawnDustLocal();
    }

    private void PlayTrailLocal()
    {
        StopTrailLocal();

        if (_trailPrefab == null)
            return;

        _activeTrail = Instantiate(_trailPrefab, transform);
        _activeTrail.transform.localPosition = Vector3.zero;
        _activeTrail.transform.localRotation = Quaternion.identity;
        _activeTrail.Play();
    }

    private void StopTrailLocal()
    {
        if (_activeTrail == null)
            return;

        _activeTrail.Stop();
        _activeTrail = null;
    }

    private void SpawnDustLocal()
    {
        if (_landingDustPrefab == null)
            return;

        GameObject dust = Instantiate(_landingDustPrefab, transform.position, Quaternion.identity);
        Destroy(dust, _landingDustDestroyDelay);
    }
}
