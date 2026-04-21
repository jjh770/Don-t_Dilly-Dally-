using UnityEngine;

public class RhythmOscillator : MonoBehaviour
{
    [Header("Tilt (rocking side to side)")]
    [SerializeField] private bool _enableTilt = true;
    // Forward axis tilts left/right on screen for a Z-looking camera. Swap to right for a side-view camera.
    [SerializeField] private Vector3 _tiltAxis = Vector3.forward;
    [SerializeField] private float _tiltAngle = 10f;
    [SerializeField] private float _tiltPeriod = 1.2f;

    [Header("Squash & Stretch")]
    [SerializeField] private bool _enableSquashStretch = true;
    [SerializeField] private float _stretchAmount = 0.15f;
    [SerializeField] private float _stretchPeriod = 0.6f;
    [SerializeField] private bool _preserveVolume = true;

    [Header("Timing")]
    [SerializeField] private bool _useRandomPhase = true;
    [SerializeField] private float _maxRandomPhase = 2f;

    private Quaternion _startLocalRotation;
    private Vector3 _startLocalScale;
    private float _phaseOffset;

    private void Start()
    {
        _startLocalRotation = transform.localRotation;
        _startLocalScale = transform.localScale;

        if (_useRandomPhase)
        {
            _phaseOffset = Random.Range(0f, _maxRandomPhase);
        }
    }

    private void Update()
    {
        float time = Time.time + _phaseOffset;

        ApplyTilt(time);
        ApplySquashStretch(time);
    }

    private void ApplyTilt(float time)
    {
        if (!_enableTilt || _tiltPeriod <= 0f)
        {
            return;
        }

        float phase = Mathf.Sin(time * Mathf.PI * 2f / _tiltPeriod);
        Quaternion tilt = Quaternion.AngleAxis(_tiltAngle * phase, _tiltAxis.normalized);
        transform.localRotation = _startLocalRotation * tilt;
    }

    private void ApplySquashStretch(float time)
    {
        if (!_enableSquashStretch || _stretchPeriod <= 0f)
        {
            return;
        }

        float phase = Mathf.Sin(time * Mathf.PI * 2f / _stretchPeriod);
        float stretch = 1f + _stretchAmount * phase;

        // Inverse sqrt keeps XZ area so volume stays constant while Y stretches.
        float squash = _preserveVolume ? 1f / Mathf.Sqrt(Mathf.Max(stretch, 0.0001f)) : 1f;

        Vector3 scale = _startLocalScale;
        scale.y *= stretch;
        scale.x *= squash;
        scale.z *= squash;
        transform.localScale = scale;
    }
}
