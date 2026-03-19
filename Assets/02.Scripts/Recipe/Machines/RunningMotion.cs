using DG.Tweening;
using UnityEngine;

namespace DontDillyDally.Data
{
    public class RunningMotion : MonoBehaviour
    {
        [SerializeField] private Transform _motionTarget;
        [SerializeField] private float _motionCycleDuration = 0.35f;
        [SerializeField] private float _stopBlendDuration = 0.25f;
        [SerializeField] private Vector3 _positionPunch = new(0.03f, 0f, 0f);
        [SerializeField] private Vector3 _rotationPunch = new(0f, 0f, 4f);
        [SerializeField] private int _motionVibrato = 8;
        [SerializeField] [Range(0f, 1f)] private float _motionElasticity = 0.7f;

        private Sequence _runningMotionSequence;
        private Vector3 _initialLocalPosition;
        private Quaternion _initialLocalRotation;

        public bool IsRunning => _runningMotionSequence != null && _runningMotionSequence.IsActive();

        private void Awake()
        {
            if (_motionTarget == null)
            {
                _motionTarget = transform;
            }

            if (_motionTarget == null)
            {
                return;
            }

            _initialLocalPosition = _motionTarget.localPosition;
            _initialLocalRotation = _motionTarget.localRotation;
        }

        private void OnDestroy()
        {
            StopMotionImmediate();
        }

        public bool TryStart()
        {
            if (_motionTarget == null || IsRunning)
            {
                return false;
            }

            StopMotionImmediate();

            _motionTarget.localPosition = _initialLocalPosition;
            _motionTarget.localRotation = _initialLocalRotation;

            _runningMotionSequence = DOTween.Sequence()
                .Append(_motionTarget.DOPunchPosition(
                    _positionPunch,
                    _motionCycleDuration,
                    _motionVibrato,
                    _motionElasticity))
                .Join(_motionTarget.DOPunchRotation(
                    _rotationPunch,
                    _motionCycleDuration,
                    _motionVibrato,
                    _motionElasticity))
                .SetLoops(-1, LoopType.Restart)
                .SetUpdate(UpdateType.Normal);

            return true;
        }

        public void StopMotion()
        {
            if (_runningMotionSequence != null)
            {
                _runningMotionSequence.Kill();
                _runningMotionSequence = null;
            }

            if (_motionTarget == null)
            {
                return;
            }

            _motionTarget.DOKill();
            _motionTarget.DOLocalMove(_initialLocalPosition, _stopBlendDuration).SetEase(Ease.OutCubic);
            _motionTarget.DOLocalRotateQuaternion(_initialLocalRotation, _stopBlendDuration).SetEase(Ease.OutCubic);
        }

        private void StopMotionImmediate()
        {
            if (_runningMotionSequence != null)
            {
                _runningMotionSequence.Kill();
                _runningMotionSequence = null;
            }

            if (_motionTarget == null)
            {
                return;
            }

            _motionTarget.DOKill();
            _motionTarget.localPosition = _initialLocalPosition;
            _motionTarget.localRotation = _initialLocalRotation;
        }
    }
}
