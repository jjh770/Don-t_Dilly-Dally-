using System.Collections;
using UnityEngine;

namespace DontDillyDally.Data
{
    [DisallowMultipleComponent]
    public sealed class TrashBoxShakeEffect : MonoBehaviour
    {
        [Header("대상")]
        [Tooltip("피드백을 적용할 모델 Transform. 비워두면 이 오브젝트의 Transform 사용. 보통 TrashBox_Model을 할당")]
        [SerializeField] private Transform _target;

        [Header("스쿼시")]
        [Tooltip("눌렸다 펴지는 단계의 총 지속 시간 (초)")]
        [SerializeField, Min(0.01f)] private float _squashDuration = 0.12f;

        [Tooltip("Y축 압축 비율 (0.2 = 20% 축소)")]
        [SerializeField, Range(0f, 0.6f)] private float _squashAmount = 0.18f;

        [Tooltip("XZ축 팽창 비율 (부피 보존 효과)")]
        [SerializeField, Range(0f, 0.6f)] private float _stretchAmount = 0.12f;

        [Header("좌우 흔들림")]
        [Tooltip("흔들림 지속 시간 (초)")]
        [SerializeField, Min(0.01f)] private float _shakeDuration = 0.35f;

        [Tooltip("좌우 흔들림 최대 각도 (도)")]
        [SerializeField, Min(0f)] private float _shakeAngle = 8f;

        [Tooltip("흔들림 주파수 (Hz)")]
        [SerializeField, Min(0.1f)] private float _shakeFrequency = 10f;

        [Tooltip("흔들림 축. 기본값은 Z축(좌우 기울기)")]
        [SerializeField] private Vector3 _shakeAxis = Vector3.forward;

        [Header("참조")]
        [Tooltip("구독 대상 쓰레기통. 같은 오브젝트의 컴포넌트가 자동 할당됨")]
        [SerializeField] private TrashBoxInteractable _trashBox;

        private Vector3 _originalScale;
        private Quaternion _originalRotation;
        private Coroutine _feedbackCoroutine;
        private bool _originalsCached;

        private void Reset()
        {
            _trashBox = GetComponent<TrashBoxInteractable>();
        }

        private void Awake()
        {
            if (_trashBox == null)
            {
                _trashBox = GetComponent<TrashBoxInteractable>();
            }

            if (_target == null)
            {
                _target = transform;
            }

            CacheOriginals();
        }

        private void OnEnable()
        {
            if (_trashBox == null)
            {
                return;
            }

            _trashBox.ItemTrashed += HandleItemTrashed;
        }

        private void OnDisable()
        {
            if (_trashBox != null)
            {
                _trashBox.ItemTrashed -= HandleItemTrashed;
            }

            if (_feedbackCoroutine != null)
            {
                StopCoroutine(_feedbackCoroutine);
                _feedbackCoroutine = null;
            }

            RestoreOriginal();
        }

        private void HandleItemTrashed()
        {
            if (_target == null || !_originalsCached)
            {
                return;
            }

            if (_feedbackCoroutine != null)
            {
                StopCoroutine(_feedbackCoroutine);
                RestoreOriginal();
            }

            _feedbackCoroutine = StartCoroutine(PlayFeedbackRoutine());
        }

        private IEnumerator PlayFeedbackRoutine()
        {
            Vector3 squashedScale = new Vector3(
                _originalScale.x * (1f + _stretchAmount),
                _originalScale.y * (1f - _squashAmount),
                _originalScale.z * (1f + _stretchAmount));

            Vector3 shakeAxisNormalized = _shakeAxis.sqrMagnitude > 0f ? _shakeAxis.normalized : Vector3.forward;

            float elapsed = 0f;
            float totalDuration = _squashDuration + _shakeDuration;

            while (elapsed < totalDuration)
            {
                elapsed += Time.deltaTime;

                if (elapsed <= _squashDuration)
                {
                    // 0 → 1 → 0 곡선으로 눌렸다 펴지는 효과
                    float squashT = Mathf.Clamp01(elapsed / _squashDuration);
                    float curve = Mathf.Sin(squashT * Mathf.PI);
                    _target.localScale = Vector3.Lerp(_originalScale, squashedScale, curve);
                    _target.localRotation = _originalRotation;
                }
                else
                {
                    _target.localScale = _originalScale;

                    float shakeElapsed = elapsed - _squashDuration;
                    float shakeT = Mathf.Clamp01(shakeElapsed / _shakeDuration);
                    float damp = 1f - shakeT;
                    float angle = Mathf.Sin(shakeElapsed * _shakeFrequency * Mathf.PI * 2f) * _shakeAngle * damp;
                    _target.localRotation = _originalRotation * Quaternion.AngleAxis(angle, shakeAxisNormalized);
                }

                yield return null;
            }

            RestoreOriginal();
            _feedbackCoroutine = null;
        }

        private void CacheOriginals()
        {
            if (_target == null)
            {
                return;
            }

            _originalScale = _target.localScale;
            _originalRotation = _target.localRotation;
            _originalsCached = true;
        }

        private void RestoreOriginal()
        {
            if (!_originalsCached || _target == null)
            {
                return;
            }

            _target.localScale = _originalScale;
            _target.localRotation = _originalRotation;
        }
    }
}
