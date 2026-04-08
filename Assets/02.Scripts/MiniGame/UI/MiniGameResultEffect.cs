using DG.Tweening;
using UnityEngine;

namespace DontDillyDally.MiniGame
{
    // 모든 미니게임에서 재활용 가능한 성공/실패 결과 연출 컴포넌트.
    // MiniGameCanvas 프리팹의 공통 영역에 하나만 배치한다.
    public sealed class MiniGameResultEffect : MonoBehaviour
    {
        [Header("성공/실패 UI 오브젝트")]
        [Tooltip("성공 시 표시할 UI 오브젝트 (CanvasGroup 자동 추가)")]
        [SerializeField] private GameObject _successObject;
        [Tooltip("실패 시 표시할 UI 오브젝트 (CanvasGroup 자동 추가)")]
        [SerializeField] private GameObject _failObject;

        [Header("성공 연출")]
        [Tooltip("페이드 인 시간 (스케일 업과 동시에 진행)")]
        [SerializeField] private float _successFadeInDuration = 0.15f;
        [Tooltip("0 → 오버슈트까지 커지는 시간")]
        [SerializeField] private float _successScaleUpDuration = 0.22f;
        [Tooltip("최대 확대 배율 (1.25면 원래 크기의 125%)")]
        [SerializeField, Range(1f, 2f)] private float _successPeakScaleMultiplier = 1.25f;
        [Tooltip("피크에서 원래 크기로 돌아가는 시간")]
        [SerializeField] private float _successScaleDownDuration = 0.12f;
        [Tooltip("원래 크기에서 머무르는 시간")]
        [SerializeField] private float _successHoldDuration = 0.05f;
        [Tooltip("페이드 아웃 시간")]
        [SerializeField] private float _successFadeOutDuration = 0.15f;

        [Header("실패 연출")]
        [Tooltip("페이드 인 시간")]
        [SerializeField] private float _failFadeInDuration = 0.1f;
        [Tooltip("간판이 기울어질 목표 각도 (Z축, 양수/음수 가능)")]
        [SerializeField, Range(-45f, 45f)] private float _failTiltAngle = -18f;
        [Tooltip("기울어지면서 튕기는 전체 시간")]
        [SerializeField] private float _failTiltDuration = 0.55f;

        [Header("(레거시) 파티클")]
        [SerializeField] private ParticleSystem _successParticle;
        [SerializeField] private ParticleSystem _failParticle;

        // 캐시된 원래 transform 값
        private CanvasGroup _successCanvasGroup;
        private CanvasGroup _failCanvasGroup;
        private Vector3 _successOriginalScale = Vector3.one;
        private Vector3 _failOriginalScale = Vector3.one;
        private Quaternion _failOriginalRotation = Quaternion.identity;
        private bool _originsCaptured;

        // 활성 시퀀스
        private Sequence _successSeq;
        private Sequence _failSeq;

        private void Awake()
        {
            CaptureOriginsIfNeeded();
        }

        // 인스펙터에서 오브젝트가 할당되면 원래 스케일/회전/CanvasGroup을 캐시한다.
        private void CaptureOriginsIfNeeded()
        {
            if (_originsCaptured)
            {
                return;
            }

            if (_successObject != null)
            {
                _successOriginalScale = _successObject.transform.localScale;
                _successCanvasGroup = GetOrAddCanvasGroup(_successObject);
            }

            if (_failObject != null)
            {
                _failOriginalScale = _failObject.transform.localScale;
                _failOriginalRotation = _failObject.transform.localRotation;
                _failCanvasGroup = GetOrAddCanvasGroup(_failObject);
            }

            _originsCaptured = true;
        }

        public void PlaySuccess()
        {
            Reset();

            if (_successParticle != null)
            {
                _successParticle.Play();
            }

            // 새 UI 오브젝트 연출
            if (_successObject != null)
            {
                CaptureOriginsIfNeeded();

                Transform t = _successObject.transform;
                _successObject.SetActive(true);

                // 초기 상태: 알파 0, 스케일 0
                if (_successCanvasGroup != null)
                {
                    _successCanvasGroup.alpha = 0f;
                }
                t.localScale = Vector3.zero;

                Vector3 peak = _successOriginalScale * _successPeakScaleMultiplier;

                _successSeq = DOTween.Sequence().SetUpdate(true);

                // 페이드 인 + 스케일 업 (OutBack → 피크에서 살짝 오버슈트)
                if (_successCanvasGroup != null)
                {
                    _successSeq.Join(_successCanvasGroup
                        .DOFade(1f, _successFadeInDuration)
                        .SetEase(Ease.OutQuad));
                }
                _successSeq.Join(t
                    .DOScale(peak, _successScaleUpDuration)
                    .SetEase(Ease.OutBack));

                // 피크 → 원래 크기로 복귀
                _successSeq.Append(t
                    .DOScale(_successOriginalScale, _successScaleDownDuration)
                    .SetEase(Ease.OutQuad));

                // 유지
                if (_successHoldDuration > 0f)
                {
                    _successSeq.AppendInterval(_successHoldDuration);
                }

                // 페이드 아웃 + 즉시 비활성화
                if (_successCanvasGroup != null)
                {
                    _successSeq.Append(_successCanvasGroup
                        .DOFade(0f, _successFadeOutDuration)
                        .SetEase(Ease.InQuad));
                }
                _successSeq.OnComplete(() =>
                {
                    if (_successObject != null)
                    {
                        _successObject.SetActive(false);
                        _successObject.transform.localScale = _successOriginalScale;
                    }
                });
            }
        }

        public void PlayFail()
        {
            Reset();

            if (_failParticle != null)
            {
                _failParticle.Play();
            }

            // 새 UI 오브젝트 연출: 페이드 인 + 간판이 기울면서 튕 튕 튕
            if (_failObject != null)
            {
                CaptureOriginsIfNeeded();

                Transform t = _failObject.transform;
                _failObject.SetActive(true);

                // 초기 상태: 알파 0, 원래 회전/스케일 기준
                if (_failCanvasGroup != null)
                {
                    _failCanvasGroup.alpha = 0f;
                }
                t.localRotation = _failOriginalRotation;
                t.localScale = _failOriginalScale;

                _failSeq = DOTween.Sequence().SetUpdate(true);

                // 페이드 인
                if (_failCanvasGroup != null)
                {
                    _failSeq.Join(_failCanvasGroup
                        .DOFade(1f, _failFadeInDuration)
                        .SetEase(Ease.OutQuad));
                }

                // Z축 기울임 + OutBounce: 한쪽으로 기울면서 튕 튕 튕 바운스 후 기울어진 상태로 정착
                Vector3 targetEuler = _failOriginalRotation.eulerAngles;
                targetEuler.z += _failTiltAngle;

                _failSeq.Join(t
                    .DOLocalRotate(targetEuler, _failTiltDuration, RotateMode.Fast)
                    .SetEase(Ease.OutBounce));
            }
        }

        public void Reset()
        {
            CaptureOriginsIfNeeded();

            // 성공 오브젝트 정리
            if (_successSeq != null && _successSeq.IsActive())
            {
                _successSeq.Kill();
            }
            _successSeq = null;

            if (_successObject != null)
            {
                DOTween.Kill(_successObject.transform);
                _successObject.transform.localScale = _successOriginalScale;
                if (_successCanvasGroup != null)
                {
                    _successCanvasGroup.alpha = 1f;
                }
                _successObject.SetActive(false);
            }

            // 실패 오브젝트 정리
            if (_failSeq != null && _failSeq.IsActive())
            {
                _failSeq.Kill();
            }
            _failSeq = null;

            if (_failObject != null)
            {
                DOTween.Kill(_failObject.transform);
                _failObject.transform.localScale = _failOriginalScale;
                _failObject.transform.localRotation = _failOriginalRotation;
                if (_failCanvasGroup != null)
                {
                    _failCanvasGroup.alpha = 1f;
                }
                _failObject.SetActive(false);
            }

            // 파티클
            if (_successParticle != null)
            {
                _successParticle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }

            if (_failParticle != null)
            {
                _failParticle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
        }

        private void OnDestroy()
        {
            if (_successSeq != null && _successSeq.IsActive()) _successSeq.Kill();
            if (_failSeq != null && _failSeq.IsActive()) _failSeq.Kill();
        }

        private static CanvasGroup GetOrAddCanvasGroup(GameObject obj)
        {
            CanvasGroup cg = obj.GetComponent<CanvasGroup>();
            if (cg == null)
            {
                cg = obj.AddComponent<CanvasGroup>();
            }
            return cg;
        }
    }
}
