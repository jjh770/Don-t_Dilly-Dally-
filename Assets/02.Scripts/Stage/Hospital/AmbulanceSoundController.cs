using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using UnityEngine;

public class AmbulanceSoundController : MonoBehaviourPunCallbacks
{
    [Header("재생 설정")]
    [SerializeField] private SFXKey _sfxKey = SFXKey.None;
    [SerializeField] private float _minInterval = 30f;
    [SerializeField] private float _maxInterval = 60f;

    private Coroutine _playCycleCoroutine;

    private void Start()
    {
        TryStartPlayCycle();
    }

    public override void OnMasterClientSwitched(Player newMasterClient)
    {
        TryStartPlayCycle();
    }

    private void TryStartPlayCycle()
    {
        if (!PhotonNetwork.IsMasterClient)
        {
            return;
        }

        if (_playCycleCoroutine != null)
        {
            return;
        }

        _playCycleCoroutine = StartCoroutine(PlayCycle());
    }

    private IEnumerator PlayCycle()
    {
        while (true)
        {
            float waitTime = Random.Range(_minInterval, _maxInterval);
            yield return new WaitForSeconds(waitTime);

            photonView.RPC(nameof(RPC_PlaySound), RpcTarget.All);
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
