using DG.Tweening;
using UnityEngine;

public class PoofSkeletonDeath : PatientDeathBase
{
    [Header("Phase 0: Trembling")]
    [SerializeField] private float _trembleDuration = 0.3f;
    [SerializeField] private float _trembleStrength = 0.05f;

    [Header("Phase 1: Smoke Burst")]
    [Tooltip("환자+침대를 가리는 큰 연기 파티클. Play On Awake 끄기.")]
    [SerializeField] private ParticleSystem _smokeFx;

    [Tooltip("연기 재생 시작 시점 (초).")]
    [SerializeField] private float _smokeStartTime = 0.3f;

    [Header("Phase 2: Model Swap")]
    [Tooltip("해골 모델. PatientObject 자식으로 미리 배치, 비활성 상태.")]
    [SerializeField] private GameObject _skeletonModel;

    [Tooltip("모델 교체 시점 (초). 연기가 가장 짙을 때.")]
    [SerializeField] private float _swapTime = 0.8f;

    [Header("Phase 3: Skeleton Reaction")]
    [Tooltip("해골 흔들림 시작 시점 (초).")]
    [SerializeField] private float _skeletonShakeTime = 2.0f;

    [SerializeField] private float _skeletonShakeDuration = 0.5f;
    [SerializeField] private float _skeletonShakeStrength = 15f;

    private Sequence _sequence;

    public override Sequence Play(
        Transform patientRoot,
        Transform bedTransform,
        Transform patientTransform)
    {
        ForceComplete(patientRoot, bedTransform, patientTransform);

        _sequence = DOTween.Sequence();

        // Phase 0: 전체(침대+환자) 떨림.
        _sequence.Append(
            patientRoot.DOShakePosition(_trembleDuration, _trembleStrength, vibrato: 20));

        // Phase 1: 연기 폭발.
        _sequence.InsertCallback(_smokeStartTime, () => PlayFx(_smokeFx));

        // Phase 2: 연기 속에서 환자→해골 교체.
        _sequence.InsertCallback(_swapTime, () =>
        {
            patientTransform.gameObject.SetActive(false);

            if (_skeletonModel != null)
            {
                _skeletonModel.SetActive(true);
            }
        });

        // Phase 3: 해골 덜그럭 흔들림.
        _sequence.InsertCallback(_skeletonShakeTime, () =>
        {
            if (_skeletonModel != null)
            {
                _skeletonModel.transform
                    .DOShakeRotation(_skeletonShakeDuration, _skeletonShakeStrength, vibrato: 10)
                    .SetTarget(_skeletonModel.transform);
            }
        });

        return _sequence;
    }

    public override void ForceComplete(
        Transform patientRoot,
        Transform bedTransform,
        Transform patientTransform)
    {
        _sequence?.Kill();
        _sequence = null;

        if (_skeletonModel != null)
        {
            _skeletonModel.transform.DOKill();
        }

        ClearFx(_smokeFx);
    }
}
