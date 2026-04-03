using DG.Tweening;
using UnityEngine;

public class SlideInEntrance : PatientEntranceBase
{
    [Header("Slide Settings")]
    [Tooltip("침대 시작 위치 오프셋 (최종 위치 기준). X가 음수면 왼쪽에서 출발.")]
    [SerializeField] private Vector3 _startOffset = new(-8f, 0f, 0f);

    [Tooltip("침대가 시작점에서 최종 위치까지 이동하는 데 걸리는 시간 (초).")]
    [SerializeField] private float _slideDuration = 1.2f;

    [Header("Brake Tilt Settings")]
    [Tooltip("급브레이크 기울기가 시작되는 시점 (초). 0이면 즉시, 클수록 늦게 기울어짐.")]
    [SerializeField] private float _brakeStartTime = 0.7f;

    [Tooltip("기울어지는 각도 (도). Z 음수 = 앞으로 꺾임.")]
    [SerializeField] private Vector3 _brakeTiltAngle = new(0f, 0f, -12f);

    [Tooltip("기울어지는 데 걸리는 시간 (초). 짧을수록 급격하게 꺾임.")]
    [SerializeField] private float _tiltDuration = 0.12f;

    [Tooltip("기울어진 후 원래 자세로 돌아오는 시간 (초). 쿵 하고 복귀.")]
    [SerializeField] private float _settleDuration = 0.4f;

    [Tooltip("기울어질 때 Y축 보정 높이. 바퀴가 바닥을 뚫지 않도록 살짝 들어올림.")]
    [SerializeField] private float _pivotLiftHeight = 0.15f;

    [Header("Brake Smoke VFX")]
    [Tooltip("바퀴 연기 파티클 배열. 침대 자식으로 배치하세요. 위치는 직접 조절.")]
    [SerializeField] private ParticleSystem[] _brakeSmokeFx;

    [Tooltip("바퀴 연기가 시작되는 시점 (초). 브레이크 타이밍에 맞추세요.")]
    [SerializeField] private float _brakeSmokeFxStartTime = 0.7f;

    [Tooltip("바퀴 연기가 멈추는 시점 (초). 침대가 완전히 멈춘 뒤로 설정.")]
    [SerializeField] private float _brakeSmokeFxStopTime = 1.5f;

    [Header("Door Settings")]
    [Tooltip("Door1 오브젝트 (자식 문짝 2개를 자동으로 찾음).")]
    [SerializeField] private Transform _doorParent;

    [Tooltip("문이 열리기 시작하는 시점 (초). 침대가 문에 닿는 타이밍에 맞추세요.")]
    [SerializeField] private float _doorOpenTime = 0.15f;

    [Tooltip("문이 열리는 최대 각도 (도).")]
    [SerializeField] private float _doorOpenAngle = 90f;

    [Tooltip("문이 쾅 열리는 데 걸리는 시간 (초). 짧을수록 세게 부딪힌 느낌.")]
    [SerializeField] private float _doorSlamDuration = 0.1f;

    [Tooltip("문이 최대로 열린 채 유지되는 시간 (초). 침대가 통과할 때까지.")]
    [SerializeField] private float _doorStayOpenDuration = 0.5f;

    [Tooltip("문이 털렁털렁 흔들리며 닫히는 전체 시간 (초).")]
    [SerializeField] private float _doorSwingDuration = 2.0f;

    private Sequence _sequence;

    public override Sequence Play(
        Transform bedTransform,
        Vector3 finalPosition,
        Quaternion finalRotation)
    {
        ForceComplete(bedTransform, finalPosition, finalRotation);

        Vector3 startPosition = finalPosition + _startOffset;
        bedTransform.position = startPosition;
        bedTransform.rotation = finalRotation;

        _sequence = DOTween.Sequence();

        // Bed rushes in with sharp deceleration.
        _sequence.Append(
            bedTransform.DOMove(finalPosition, _slideDuration).SetEase(Ease.OutExpo));

        // Brake smoke at user-specified times.
        _sequence.InsertCallback(_brakeSmokeFxStartTime, PlayBrakeSmoke);

        // Brake tilt at user-specified time.
        Quaternion tiltedRotation = finalRotation * Quaternion.Euler(_brakeTiltAngle);

        _sequence.Insert(
            _brakeStartTime,
            bedTransform.DORotateQuaternion(tiltedRotation, _tiltDuration).SetEase(Ease.OutQuad));

        _sequence.Insert(
            _brakeStartTime,
            bedTransform.DOMoveY(finalPosition.y + _pivotLiftHeight, _tiltDuration).SetEase(Ease.OutQuad));

        // Settle back to upright. Stop smoke when settled.
        _sequence.Insert(
            _brakeStartTime + _tiltDuration,
            bedTransform.DORotateQuaternion(finalRotation, _settleDuration).SetEase(Ease.OutBounce));

        _sequence.Insert(
            _brakeStartTime + _tiltDuration,
            bedTransform.DOMoveY(finalPosition.y, _settleDuration * 0.5f).SetEase(Ease.OutBounce));

        _sequence.InsertCallback(_brakeSmokeFxStopTime, StopBrakeSmoke);

        // Door slams open at user-specified time.
        Sequence doorSequence = CreateDoorSequence(
            _doorParent,
            _doorOpenAngle,
            _doorSlamDuration,
            _doorStayOpenDuration,
            _doorSwingDuration);

        _sequence.Insert(_doorOpenTime, doorSequence);

        return _sequence;
    }

    public override void ForceComplete(
        Transform bedTransform,
        Vector3 finalPosition,
        Quaternion finalRotation)
    {
        _sequence?.Kill();
        _sequence = null;

        bedTransform.position = finalPosition;
        bedTransform.rotation = finalRotation;

        ClearBrakeSmoke();
        ResetDoor(_doorParent);
    }

    private void PlayBrakeSmoke()
    {
        if (_brakeSmokeFx == null)
        {
            return;
        }

        foreach (ParticleSystem fx in _brakeSmokeFx)
        {
            if (fx != null)
            {
                fx.Play();
            }
        }
    }

    // Emission stops but existing particles fade out naturally.
    private void StopBrakeSmoke()
    {
        if (_brakeSmokeFx == null)
        {
            return;
        }

        foreach (ParticleSystem fx in _brakeSmokeFx)
        {
            if (fx != null)
            {
                fx.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }
        }
    }

    // Full clear for when animation restarts (ForceComplete).
    private void ClearBrakeSmoke()
    {
        if (_brakeSmokeFx == null)
        {
            return;
        }

        foreach (ParticleSystem fx in _brakeSmokeFx)
        {
            if (fx != null)
            {
                fx.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
        }
    }
}
