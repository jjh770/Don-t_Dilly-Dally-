using System.Collections.Generic;
using UnityEngine;

public class PlayerMovementAbility : PlayerAbility
{
    [SerializeField] private float _moveSpeed = 3f;
    [SerializeField] private float _rotationSpeed = 10f;
    [SerializeField] private float _acceleration = 5f;

    [Header("얼음 미끄러짐")]
    [SerializeField] private float _iceDeceleration = 1.5f;
    [SerializeField] private float _iceRotationMultiplier = 0.6f;

    private Vector3 _moveDirection;
    private PlayerGroundDetector _groundDetector;
    private bool IsOnIce => _groundDetector != null && _groundDetector.IsOnIce;
    private float _currentSpeed;
    private float _moveSpeedMultiplier = 1f;
    private float _rotationSpeedMultiplier = 1f;
    private readonly HashSet<object> _movementLockSources = new();
    private Rigidbody _rigidbody;
    private PlayerAnimator _playerAnimator;

    public Vector3 MoveDirection => _moveDirection;
    public float CurrentSpeed => _currentSpeed * _moveSpeedMultiplier;

    private const string HorizontalAxis = "Horizontal";
    private const string VerticalAxis = "Vertical";
    private const float MinMoveSqrMagnitude = 0.01f;
    private bool IsMovementLocked => _movementLockSources.Count > 0;

    protected override void Awake()
    {
        base.Awake();
        _rigidbody = GetComponent<Rigidbody>();
        _playerAnimator = GetComponent<PlayerAnimator>();
        _groundDetector = GetComponent<PlayerGroundDetector>();
    }

    private void Start()
    {
        if (_owner.PhotonView != null && !_owner.PhotonView.IsMine)
        {
            _rigidbody.isKinematic = true;
        }
    }

    private void Update()
    {
        if (_owner.PhotonView != null && !_owner.PhotonView.IsMine)
        {
            return;
        }

        if (IsMovementLocked)
        {
            _moveDirection = Vector3.zero;
            UpdateAnimation();
            return;
        }

        HandleInput();
        HandleRotation();
        UpdateAnimation();
    }

    private void FixedUpdate()
    {
        if (_owner.PhotonView != null && !_owner.PhotonView.IsMine)
        {
            return;
        }

        if (IsMovementLocked)
        {
            _currentSpeed = 0f;
            Vector3 velocity = _rigidbody.linearVelocity;
            velocity.x = 0f;
            velocity.z = 0f;
            _rigidbody.linearVelocity = velocity;
            return;
        }

        HandleMovement();
    }

    private void HandleInput()
    {
        float h = Input.GetAxis(HorizontalAxis);
        float v = Input.GetAxis(VerticalAxis);
        _moveDirection = new Vector3(h, 0, v).normalized;
    }

    private void HandleRotation()
    {
        if (_moveDirection.sqrMagnitude > MinMoveSqrMagnitude)
        {
            Quaternion targetRotation = Quaternion.LookRotation(_moveDirection);
            float rotSpeed = _rotationSpeed * _rotationSpeedMultiplier;
            if (IsOnIce) rotSpeed *= _iceRotationMultiplier;
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotSpeed * Time.deltaTime);
        }
    }

    private void HandleMovement()
    {
        // 속도 가속/감속
        float targetSpeed = _moveDirection.sqrMagnitude > MinMoveSqrMagnitude ? _moveSpeed : 0f;

        // 얼음 위에서 감속 시에만 낮은 감속 적용
        float accel = _acceleration;
        if (IsOnIce && targetSpeed < _currentSpeed)
        {
            accel = _iceDeceleration;
        }

        _currentSpeed = Mathf.MoveTowards(_currentSpeed, targetSpeed, accel * Time.fixedDeltaTime);

        // 얼음 위에서 입력 없을 때 관성 유지
        Vector3 moveDir = _moveDirection;
        if (IsOnIce && moveDir.sqrMagnitude < MinMoveSqrMagnitude && _currentSpeed > 0f)
        {
            moveDir = transform.forward;
        }

        Vector3 velocity = moveDir * _currentSpeed * _moveSpeedMultiplier;
        velocity.y = _rigidbody.linearVelocity.y;
        _rigidbody.linearVelocity = velocity;
    }

    public void SetSpeedMultiplier(float moveSpeedMultiplier, float rotationSpeedMultiplier)
    {
        _moveSpeedMultiplier = moveSpeedMultiplier;
        _rotationSpeedMultiplier = rotationSpeedMultiplier;
    }

    public void SetMovementLocked(object lockSource, bool isLocked)
    {
        if (lockSource == null)
        {
            Debug.LogWarning("[PlayerMovementAbility] 이동 잠금 토큰이 없음.");
            return;
        }

        if (isLocked)
        {
            _movementLockSources.Add(lockSource);
        }
        else
        {
            _movementLockSources.Remove(lockSource);
        }

        if (IsMovementLocked)
        {
            _moveDirection = Vector3.zero;
            _currentSpeed = 0f;
        }
    }

    private void UpdateAnimation()
    {
        bool isWalking = _moveDirection.sqrMagnitude > MinMoveSqrMagnitude;
        _playerAnimator.PlayWalkAnimation(isWalking);
    }
}
