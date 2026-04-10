using DG.Tweening;
using System.Collections.Generic;
using UnityEngine;

public class BowlingStrikeEntrance : PatientEntranceBase
{
    [Header("Slide Settings")]
    [Tooltip("침대 시작 위치 오프셋 (최종 위치 기준). X가 음수면 왼쪽에서 출발.")]
    [SerializeField] private Vector3 _startOffset = new(-10f, 0f, 0f);

    [Tooltip("침대가 시작점에서 최종 위치까지 이동하는 데 걸리는 시간 (초).")]
    [SerializeField] private float _slideDuration = 1.2f;

    [Tooltip("침대 이동 커브. InQuart=점점 빨라짐 (가속 돌진).")]
    [SerializeField] private Ease _slideEase = Ease.InQuart;

    [Header("Pin Settings")]
    [Tooltip("볼링핀 프리팹.")]
    [SerializeField] private GameObject _pinPrefab;

    [Tooltip("핀 위치 배열 (이 오브젝트의 로컬 좌표 기준). 이 오브젝트를 씬에서 옮기면 핀 전체가 같이 이동.")]
    [SerializeField]
    private Vector3[] _pinPositions = new[]
    {
        new Vector3(0f, 0f, 0f),
        new Vector3(0.6f, 0f, -0.4f),
        new Vector3(0.6f, 0f, 0.4f),
        new Vector3(1.2f, 0f, -0.8f),
        new Vector3(1.2f, 0f, 0f),
        new Vector3(1.2f, 0f, 0.8f),
    };

    [Tooltip("핀이 날아가는 시점 (초). 침대가 핀에 부딪히는 타이밍에 맞추세요.")]
    [SerializeField] private float _impactTime = 1.0f;

    [Tooltip("핀이 날아가는 시간 (초). 길수록 천천히 흩어짐.")]
    [SerializeField] private float _scatterDuration = 0.8f;

    [Tooltip("핀이 날아가는 거리 (미터).")]
    [SerializeField] private float _scatterDistance = 3f;

    [Tooltip("핀이 사라지는 시간 (초). 흩어진 후 축소되며 사라짐.")]
    [SerializeField] private float _pinFadeDuration = 0.3f;

    [Header("Door Settings")]
    [Tooltip("Door1 오브젝트 (자식 문짝 2개를 자동으로 찾음).")]
    [SerializeField] private Transform _doorParent;

    [Tooltip("문이 열리는 각도 (도). 연출 시작 시 이미 이 각도로 열려있음.")]
    [SerializeField] private float _doorOpenAngle = 90f;

    [Tooltip("침대 통과 후 문이 닫히기 시작하는 시점 (초).")]
    [SerializeField] private float _doorCloseTime = 1.5f;

    [Tooltip("문이 닫히는 데 걸리는 시간 (초). 자동문처럼 부드럽게.")]
    [SerializeField] private float _doorCloseDuration = 0.6f;

    private Sequence _sequence;
    private readonly List<GameObject> _spawnedPins = new();
    private Quaternion _originalRotationA;
    private Quaternion _originalRotationB;

    public override Sequence Play(
        Transform bedTransform,
        Vector3 finalPosition,
        Quaternion finalRotation)
    {
        ForceComplete(bedTransform, finalPosition, finalRotation);

        if (_pinPrefab == null)
        {
            Debug.LogWarning("[BowlingStrikeEntrance] Pin prefab is not assigned. Skipping.");
            return DOTween.Sequence();
        }

        SpawnPins();

        Vector3 startPosition = finalPosition + _startOffset;
        bedTransform.position = startPosition;
        bedTransform.rotation = finalRotation;

        _sequence = DOTween.Sequence();

        // Door starts already open. Close smoothly after bed passes.
        OpenDoorImmediately();
        _sequence.InsertCallback(_doorCloseTime, CloseDoorSmoothly);

        // Bed charges in immediately (accelerating).
        _sequence.Insert(
            0f,
            bedTransform.DOMove(finalPosition, _slideDuration).SetEase(_slideEase));

        // Impact: scatter pins at user-specified time.
        _sequence.InsertCallback(_impactTime, ScatterPins);

        // Clean up pins after scatter finishes.
        _sequence.InsertCallback(
            _impactTime + _scatterDuration + _pinFadeDuration + 0.1f,
            CleanupPins);

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

        CleanupPins();
        ResetDoor(_doorParent);
    }

    private void OpenDoorImmediately()
    {
        if (_doorParent == null || _doorParent.childCount < 2)
        {
            return;
        }

        Transform panelA = _doorParent.GetChild(0);
        Transform panelB = _doorParent.GetChild(1);

        _originalRotationA = panelA.localRotation;
        _originalRotationB = panelB.localRotation;

        panelA.localRotation = _originalRotationA * Quaternion.Euler(0f, _doorOpenAngle, 0f);
        panelB.localRotation = _originalRotationB * Quaternion.Euler(0f, -_doorOpenAngle, 0f);
    }

    private void CloseDoorSmoothly()
    {
        if (_doorParent == null || _doorParent.childCount < 2)
        {
            return;
        }

        Transform panelA = _doorParent.GetChild(0);
        Transform panelB = _doorParent.GetChild(1);

        panelA.DOLocalRotateQuaternion(_originalRotationA, _doorCloseDuration).SetEase(Ease.InOutQuad);
        panelB.DOLocalRotateQuaternion(_originalRotationB, _doorCloseDuration).SetEase(Ease.InOutQuad);
    }

    private void SpawnPins()
    {
        CleanupPins();

        foreach (Vector3 localOffset in _pinPositions)
        {
            Vector3 worldPosition = transform.TransformPoint(localOffset);
            GameObject pin = Instantiate(_pinPrefab, worldPosition, Quaternion.identity, transform);
            _spawnedPins.Add(pin);
        }
    }

    private void ScatterPins()
    {

        foreach (GameObject pin in _spawnedPins)
        {
            if (pin == null)
            {
                continue;
            }

            Vector3 randomDirection = new(
                Random.Range(-1f, 1f),
                Random.Range(0.5f, 1.5f),
                Random.Range(-1f, 1f));
            randomDirection = randomDirection.normalized * _scatterDistance;

            Vector3 randomSpin = new(
                Random.Range(-720f, 720f),
                Random.Range(-720f, 720f),
                Random.Range(-720f, 720f));

            pin.transform.DOMove(
                pin.transform.position + randomDirection,
                _scatterDuration).SetEase(Ease.OutQuad);

            pin.transform.DORotate(
                randomSpin,
                _scatterDuration,
                RotateMode.FastBeyond360).SetEase(Ease.OutQuad);

            pin.transform.DOScale(Vector3.zero, _pinFadeDuration)
                .SetDelay(_scatterDuration * 0.7f);
        }
        SoundManager.Instance.Play(SFXKey.PatientBowlingStrike, SoundType.Local);
    }

    private void CleanupPins()
    {
        foreach (GameObject pin in _spawnedPins)
        {
            if (pin == null)
            {
                continue;
            }

            pin.transform.DOKill();
            Destroy(pin);
        }

        _spawnedPins.Clear();
    }

    private void OnDestroy()
    {
        CleanupPins();
    }
}
