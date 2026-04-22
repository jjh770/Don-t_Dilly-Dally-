using System;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;

public class VehiclePathMover : MonoBehaviour
{
    private SplineContainer _spline;
    private float _speed;
    private bool _reverseFacing;
    private float _progress;
    private float _splineLength;
    private Action _onComplete;
    private bool _isActive;

    public void Initialize(SplineContainer spline, float speed, bool reverseFacing, Action onComplete)
    {
        _spline = spline;
        _speed = Mathf.Max(speed, 0f);
        _reverseFacing = reverseFacing;
        _onComplete = onComplete;
        _progress = 0f;
        _splineLength = Mathf.Max(spline.CalculateLength(), 0.0001f);
        _isActive = true;

        UpdatePose(0f);
    }

    private void Update()
    {
        if (!_isActive || _spline == null)
        {
            return;
        }

        _progress += _speed / _splineLength * Time.deltaTime;

        if (_progress >= 1f)
        {
            _progress = 1f;
            UpdatePose(_progress);
            _isActive = false;
            _onComplete?.Invoke();
            return;
        }

        UpdatePose(_progress);
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

        if (_reverseFacing)
        {
            forward = -forward;
        }

        transform.rotation = Quaternion.LookRotation(forward, Vector3.up);
    }
}
