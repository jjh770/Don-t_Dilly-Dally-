using Photon.Pun;
using UnityEngine;

[RequireComponent(typeof(PhotonView), typeof(PlayerMovementAbility))]
public class PlayerWalkDustPresenter : MonoBehaviourPun
{
    [Header("발먼지 VFX")]
    [Tooltip("걸음 시작 순간 팍 터지는 버스트 파티클")]
    [SerializeField] private ParticleSystem _burstParticles;

    [Tooltip("걷는 동안 굴뚝처럼 지속되는 루프 파티클")]
    [SerializeField] private ParticleSystem _loopParticles;

    [Tooltip("걸음 시작 시 한 번에 방출할 버스트 파티클 개수")]
    [SerializeField] private int _burstCount = 15;

    [Tooltip("버스트 재방출 쿨다운 (초). 짧게 멈췄다 다시 걸을 땐 동일한 걸음의 연속으로 간주해 중복 버스트를 막습니다.")]
    [SerializeField, Min(0f)] private float _burstCooldown = 0.5f;

    private PlayerMovementAbility _movementAbility;
    private float _lastBurstTime = float.NegativeInfinity;

    private void Awake()
    {
        _movementAbility = GetComponent<PlayerMovementAbility>();
        ResolveParticlesIfMissing();
    }

    private void ResolveParticlesIfMissing()
    {
        if (_burstParticles != null && _loopParticles != null)
        {
            return;
        }

        ParticleSystem[] children = GetComponentsInChildren<ParticleSystem>(true);
        foreach (ParticleSystem ps in children)
        {
            if (_burstParticles == null && ps.gameObject.name == "Player_Walk")
            {
                _burstParticles = ps;
                continue;
            }

            if (_loopParticles == null && ps.gameObject.name == "Smoke")
            {
                _loopParticles = ps;
            }
        }
    }

    private void OnEnable()
    {
        _movementAbility.WalkStateChanged += HandleWalkStateChanged;
    }

    private void OnDisable()
    {
        _movementAbility.WalkStateChanged -= HandleWalkStateChanged;
        StopDustImmediate();
    }

    private void HandleWalkStateChanged(bool isWalking)
    {
        if (!photonView.IsMine)
        {
            return;
        }

        if (isWalking)
        {
            PlayDust();
        }
        else
        {
            StopDust();
        }

        photonView.RPC(nameof(RPC_SetDust), RpcTarget.Others, isWalking);
    }

    [PunRPC]
    private void RPC_SetDust(bool isWalking)
    {
        if (isWalking)
        {
            PlayDust();
            return;
        }

        StopDust();
    }

    private void PlayDust()
    {
        if (_burstParticles != null && Time.time - _lastBurstTime >= _burstCooldown)
        {
            _burstParticles.Emit(_burstCount);
            _lastBurstTime = Time.time;
        }

        if (_loopParticles != null)
        {
            _loopParticles.Play(false);
        }
    }

    private void StopDust()
    {
        if (_loopParticles != null)
        {
            _loopParticles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }
    }

    private void StopDustImmediate()
    {
        if (_loopParticles != null)
        {
            _loopParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }
}
