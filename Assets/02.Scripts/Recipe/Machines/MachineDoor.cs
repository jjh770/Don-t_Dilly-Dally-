using DG.Tweening;
using Photon.Pun;
using UnityEngine;

namespace DontDillyDally.Data
{
    [RequireComponent(typeof(Collider), typeof(PhotonView))]
    public class MachineDoor : MonoBehaviourPun, IInteractable
    {
        [SerializeField] private Transform _doorVisual;
        [SerializeField] private Vector3 _closedLocalEulerAngles;
        [SerializeField] private Vector3 _openLocalEulerAngles = new(0f, -90f, 0f);
        [SerializeField] private bool _startOpen = true;
        [SerializeField] private float _transitionDuration = 0.3f;
        [SerializeField] private Ease _transitionEase = Ease.OutCubic;

        private Collider _interactionCollider;
        private PhotonView _localPhotonView;
        private Tween _doorTween;
        private bool _isTransitioning;

        public bool IsOpen { get; private set; }
        public bool IsLocked { get; private set; }
        public bool IsInteracting => _isTransitioning;
        public Transform Transform => transform;

        private void Awake()
        {
            if (_doorVisual == null)
            {
                _doorVisual = transform;
            }

            _localPhotonView = GetComponent<PhotonView>();
            _interactionCollider = GetComponent<Collider>();
            IsOpen = _startOpen;
            ApplyImmediateState();
        }

        private void OnDestroy()
        {
            KillDoorTween();
        }

        public void Interact(Transform interactor)
        {
            if (IsLocked || _isTransitioning)
            {
                return;
            }

            if (IsOpen)
            {
                if (TryClose() && PhotonNetwork.InRoom && _localPhotonView != null)
                {
                    _localPhotonView.RPC(nameof(RPC_DoorSetState), RpcTarget.Others, false, false);
                }
                return;
            }

            if (TryOpen() && PhotonNetwork.InRoom && _localPhotonView != null)
            {
                _localPhotonView.RPC(nameof(RPC_DoorSetState), RpcTarget.Others, true, false);
            }
        }

        public void StopInteract()
        {
        }

        public bool TryOpen()
        {
            if (IsLocked || _isTransitioning || IsOpen)
            {
                return false;
            }

            PlayDoorTransition(true);
            return true;
        }

        public bool TryClose()
        {
            if (IsLocked || _isTransitioning || !IsOpen)
            {
                return false;
            }

            PlayDoorTransition(false);
            return true;
        }

        public void LockClosed()
        {
            KillDoorTween();
            _isTransitioning = false;
            IsOpen = false;
            IsLocked = true;
            ApplyImmediateState();
        }

        [PunRPC]
        private void RPC_DoorSetState(bool open, bool locked)
        {
            if (locked)
            {
                LockClosed();
                return;
            }
            if (IsLocked)
            {
                Unlock();
            }
            if (open && !IsOpen)
            {
                TryOpen();
            }
            else if (!open && IsOpen)
            {
                TryClose();
            }
        }

        public void Unlock()
        {
            IsLocked = false;
            ApplyInteractionState();
        }

        private void ApplyImmediateState()
        {
            if (_doorVisual != null)
            {
                Vector3 targetEulerAngles = IsOpen ? _openLocalEulerAngles : _closedLocalEulerAngles;
                _doorVisual.localRotation = Quaternion.Euler(targetEulerAngles);
            }

            ApplyInteractionState();
        }

        private void PlayDoorTransition(bool targetOpenState)
        {
            if (_doorVisual == null)
            {
                IsOpen = targetOpenState;
                ApplyImmediateState();
                return;
            }

            KillDoorTween();
            _isTransitioning = true;

            if (_interactionCollider != null)
            {
                _interactionCollider.enabled = false;
            }

            Vector3 targetEulerAngles = targetOpenState ? _openLocalEulerAngles : _closedLocalEulerAngles;

            _doorTween = _doorVisual
                .DOLocalRotate(targetEulerAngles, _transitionDuration, RotateMode.Fast)
                .SetEase(_transitionEase)
                .OnComplete(() =>
                {
                    _doorTween = null;
                    _isTransitioning = false;
                    IsOpen = targetOpenState;
                    ApplyInteractionState();
                });
        }

        private void ApplyInteractionState()
        {
            if (_interactionCollider == null)
            {
                return;
            }

            _interactionCollider.enabled = IsOpen && !IsLocked;
        }

        private void KillDoorTween()
        {
            if (_doorTween == null)
            {
                return;
            }

            _doorTween.Kill();
            _doorTween = null;
        }
    }
}
