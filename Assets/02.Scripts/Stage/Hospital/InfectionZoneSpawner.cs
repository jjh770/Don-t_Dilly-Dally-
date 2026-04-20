using Photon.Pun;
using System.Collections;
using UnityEngine;

public class InfectionZoneSpawner : MonoBehaviourPun
{
    [Header("스폰 설정")]
    [SerializeField] private Transform[] _spawnPoints;
    [SerializeField] private GameObject _infectionZonePrefab;

    [Header("타이밍")]
    [SerializeField] private float _spawnInterval = 60f;
    [SerializeField] private float _zoneDuration = 15f;

    private GameObject _currentZone;

    private void Start()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            StartCoroutine(SpawnLoop());
        }
    }

    private IEnumerator SpawnLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(_spawnInterval);

            if (_spawnPoints == null || _spawnPoints.Length == 0 || _infectionZonePrefab == null)
            {
                continue;
            }

            if (_currentZone != null)
            {
                continue;
            }

            int randomIndex = Random.Range(0, _spawnPoints.Length);
            photonView.RPC(nameof(RPC_SpawnZone), RpcTarget.All, randomIndex);

            yield return new WaitForSeconds(_zoneDuration);

            photonView.RPC(nameof(RPC_DestroyZone), RpcTarget.All);
        }
    }

    [PunRPC]
    private void RPC_SpawnZone(int spawnIndex)
    {
        if (spawnIndex < 0 || spawnIndex >= _spawnPoints.Length)
        {
            return;
        }

        Transform spawnPoint = _spawnPoints[spawnIndex];
        if (spawnPoint == null)
        {
            return;
        }

        DestroyCurrentZone();
        _currentZone = Instantiate(_infectionZonePrefab, spawnPoint.position, spawnPoint.rotation);
        SoundManager.Instance.Play(SFXKey.Infection, SoundType.Local);
    }

    [PunRPC]
    private void RPC_DestroyZone()
    {
        DestroyCurrentZone();
    }

    private void DestroyCurrentZone()
    {
        if (_currentZone != null)
        {
            Destroy(_currentZone);
            _currentZone = null;
        }
    }

    private void OnDestroy()
    {
        DestroyCurrentZone();
    }
}
