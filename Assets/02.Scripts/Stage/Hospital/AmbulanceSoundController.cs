using Photon.Pun;
using System.Collections;
using UnityEngine;

public class AmbulanceSoundController : MonoBehaviour
{
    [Header("재생 설정")]
    [SerializeField] private SFXKey _sfxKey = SFXKey.None;
    [SerializeField] private float _minInterval = 30f;
    [SerializeField] private float _maxInterval = 60f;

    private PhotonView _photonView;

    private void Start()
    {
        _photonView = GetComponent<PhotonView>();

        if (PhotonNetwork.IsMasterClient)
        {
            StartCoroutine(PlayCycle());
        }
    }

    private IEnumerator PlayCycle()
    {
        while (true)
        {
            float waitTime = Random.Range(_minInterval, _maxInterval);
            yield return new WaitForSeconds(waitTime);

            _photonView.RPC(nameof(RPC_PlaySound), RpcTarget.All);
        }
    }

    [PunRPC]
    private void RPC_PlaySound()
    {
        if (_sfxKey == SFXKey.None)
        {
            return;
        }

        SoundManager.Instance.Play(_sfxKey, SoundType.Local);
    }
}
