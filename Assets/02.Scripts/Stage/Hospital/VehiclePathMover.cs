using System;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;

public class VehiclePathMover : MonoBehaviour
{
    private SplineContainer _spline;
    private float _speed;
    private bool _reverseFacing;
    private bool _reverseDirection;
    private float _driftAngle;
    private float _rollSpeed;
    private float _accumulatedRoll;
    private float _progress;
    private float _splineLength;
    private Action _onComplete;
    private bool _isActive;

    public void Initialize(SplineContainer spline, float speed, bool reverseFacing, bool reverseDirection, float driftAngleDegrees, float rollSpeedDegrees, Action onComplete)
    {
        _spline = spline;
        _speed = Mathf.Max(speed, 0f);
        _reverseFacing = reverseFacing;
        _reverseDirection = reverseDirection;
        _driftAngle = driftAngleDegrees;
        _rollSpeed = rollSpeedDegrees;
        _accumulatedRoll = 0f;
        _onComplete = onComplete;
        _progress = 0f;
        _splineLength = Mathf.Max(spline.CalculateLength(), 0.0001f);
        _isActive = true;

        UpdatePose(CurrentT());
    }

    private void Update()
    {
        if (!_isActive || _spline == null)
        {
            return;
        }

        _progress += _speed / _splineLength * Time.deltaTime;
        _accumulatedRoll += _rollSpeed * Time.deltaTime;

        if (_progress >= 1f)
        {
            _progress = 1f;
            UpdatePose(CurrentT());
            _isActive = false;
            _onComplete?.Invoke();
            return;
        }

        UpdatePose(CurrentT());
    }

    private float CurrentT()
    {
        // 역주행이면 t를 1→0으로 진행.
        return _reverseDirection ? 1f - _progress : _progress;
    }

    private void UpdatePose(float t)
    {
        float3 position = _spline.EvaluatePosition(t);
        float3 tangent = _spline.EvaluateTangent(t);

        transform.position = position;

        Vector3 forward = ((Vector3)tangent).normalized;
        if (forward.sqrMagnitude < 0.0001f)
        {
            return;
        }

        // 역주행이면 실제 이동 방향은 -tangent.
        if (_reverseDirection)
        {
            forward = -forward;
        }

        if (_reverseFacing)
        {
            forward = -forward;
        }

        Quaternion lookRotation = Quaternion.LookRotation(forward, Vector3.up);

        if (_driftAngle != 0f)
        {
            // 차체를 진행 방향 대비 yaw로 틀어 드리프트 느낌 부여.
            lookRotation *= Quaternion.Euler(0f, _driftAngle, 0f);
        }

        if (_accumulatedRoll != 0f)
        {
            // 진행 축 기준 회전(배럴 롤) 누적.
            lookRotation *= Quaternion.Euler(0f, 0f, _accumulatedRoll);
        }

        transform.rotation = lookRotation;
    }
}
