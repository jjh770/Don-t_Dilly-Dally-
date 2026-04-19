using Photon.Pun;
using UnityEngine;

namespace DontDillyDally.Data
{
    [RequireComponent(typeof(SyringeFillTarget), typeof(PhotonView))]
    public class SyringeFillVfxPresenter : MonoBehaviourPun
    {
        [Header("주사기 채우기 VFX")]
        [Tooltip("채우는 동안 플레이어 손에 재생할 연기 프리팹 (루프 파티클 권장)")]
        [SerializeField] private GameObject _smokeVfxPrefab;

        private SyringeFillTarget _fillTarget;
        private GameObject _activeSmokeInstance;

        private void Awake()
        {
            _fillTarget = GetComponent<SyringeFillTarget>();
        }

        private void OnEnable()
        {
            _fillTarget.FillStarted += HandleFillStarted;
            _fillTarget.FillEnded += HandleFillEnded;
        }

        private void OnDisable()
        {
            _fillTarget.FillStarted -= HandleFillStarted;
            _fillTarget.FillEnded -= HandleFillEnded;

            DestroyActiveSmokeInstance();
        }

        private void HandleFillStarted(IHeldItemInteractor interactor)
        {
            if (_smokeVfxPrefab == null || interactor == null)
            {
                return;
            }

            PhotonView interactorView = interactor.GetInteractorPhotonView();
            if (interactorView == null)
            {
                return;
            }

            photonView.RPC(nameof(RPC_PlaySmoke), RpcTarget.All, interactorView.ViewID);
        }

        private void HandleFillEnded()
        {
            photonView.RPC(nameof(RPC_StopSmoke), RpcTarget.All);
        }

        [PunRPC]
        private void RPC_PlaySmoke(int interactorViewId)
        {
            if (_smokeVfxPrefab == null)
            {
                return;
            }

            Transform attachPoint = ResolveAttachPoint(interactorViewId);
            if (attachPoint == null)
            {
                return;
            }

            DestroyActiveSmokeInstance();
            _activeSmokeInstance = Instantiate(_smokeVfxPrefab, attachPoint.position, attachPoint.rotation, attachPoint);

            ParticleSystem rootSystem = _activeSmokeInstance.GetComponentInChildren<ParticleSystem>(true);
            if (rootSystem != null)
            {
                rootSystem.Play(true);
            }
        }

        [PunRPC]
        private void RPC_StopSmoke()
        {
            if (_activeSmokeInstance == null)
            {
                return;
            }

            ParticleSystem rootSystem = _activeSmokeInstance.GetComponentInChildren<ParticleSystem>(true);
            if (rootSystem != null)
            {
                rootSystem.Stop(true, ParticleSystemStopBehavior.StopEmitting);
                _activeSmokeInstance.transform.SetParent(null, true);
                _activeSmokeInstance = null;
                return;
            }

            Destroy(_activeSmokeInstance);
            _activeSmokeInstance = null;
        }

        private static Transform ResolveAttachPoint(int interactorViewId)
        {
            PhotonView interactorView = PhotonView.Find(interactorViewId);
            if (interactorView == null)
            {
                return null;
            }

            IHeldItemInteractor interactor = interactorView.GetComponent<IHeldItemInteractor>();
            return interactor?.GetHandAttachPoint();
        }

        private void DestroyActiveSmokeInstance()
        {
            if (_activeSmokeInstance == null)
            {
                return;
            }

            Destroy(_activeSmokeInstance);
            _activeSmokeInstance = null;
        }
    }
}
