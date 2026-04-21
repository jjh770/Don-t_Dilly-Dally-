using UnityEngine;

public class SoliderIdleLookAround : MonoBehaviour
{
    [SerializeField] private Animator _animator;
    [SerializeField] private float _minInterval = 5f;
    [SerializeField] private float _maxInterval = 12f;
    [SerializeField] private string _idleStateName = "Skeleton_01_Idle";

    private static readonly int s_lookAroundHash = Animator.StringToHash("LookAround");

    private float _nextTriggerTime;

    private void Reset()
    {
        _animator = GetComponent<Animator>();
    }

    private void OnEnable()
    {
        _nextTriggerTime = Time.time + Random.Range(_minInterval, _maxInterval);
    }

    private void Update()
    {
        if (_animator == null)
        {
            return;
        }

        if (Time.time < _nextTriggerTime)
        {
            return;
        }

        AnimatorStateInfo stateInfo = _animator.GetCurrentAnimatorStateInfo(0);
        bool isIdle = stateInfo.IsName(_idleStateName);

        if (isIdle)
        {
            _animator.SetTrigger(s_lookAroundHash);
        }

        _nextTriggerTime = Time.time + Random.Range(_minInterval, _maxInterval);
    }
}
