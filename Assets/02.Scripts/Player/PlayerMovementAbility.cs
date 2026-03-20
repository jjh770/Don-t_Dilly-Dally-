using System;
using UnityEngine;

public class PlayerMovementAbility : PlayerAbility
{
    [SerializeField] private float _moveSpeed = 3f;
    [SerializeField] private float _rotationSpeed = 10f;
    [SerializeField] private float _acceleration = 5f;

    private Vector3 _moveDirection;
    private float _currentSpeed;
    private float _moveSpeedMultiplier = 1f;
    private float _rotationSpeedMultiplier = 1f;
    private bool _isMovementLocked;
    private Rigidbody _rigidbody;
    private PlayerAnimator _playerAnimator;

    public Vector3 MoveDirection => _moveDirection;
    public float CurrentSpeed => _currentSpeed * _moveSpeedMultiplier;

    private const string HorizontalAxis = "Horizontal";
    private const string VerticalAxis = "Vertical";
    private const float MinMoveSqrMagnitude = 0.01f;

    protected override void Awake()
    {
        base.Awake();
        _rigidbody = GetComponent<Rigidbody>();
        _playerAnimator = GetComponent<PlayerAnimator>();
    }

    private void Update()
    {
        if (_owner.PhotonView != null && !_owner.PhotonView.IsMine)
        {
            return;
        }

        if (_isMovementLocked)
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
        if (_isMovementLocked)
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
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, _rotationSpeed * _rotationSpeedMultiplier * Time.deltaTime);
        }
    }

    private void HandleMovement()
    {
        // 속도 가속/감속
        float targetSpeed = _moveDirection.sqrMagnitude > MinMoveSqrMagnitude ? _moveSpeed : 0f;
        _currentSpeed = Mathf.MoveTowards(_currentSpeed, targetSpeed, _acceleration * Time.fixedDeltaTime);

        Vector3 velocity = _moveDirection * _currentSpeed * _moveSpeedMultiplier;
        velocity.y = _rigidbody.linearVelocity.y;
        _rigidbody.linearVelocity = velocity;
    }

    public void SetSpeedMultiplier(float moveSpeedMultiplier, float rotationSpeedMultiplier)
    {
        _moveSpeedMultiplier = moveSpeedMultiplier;
        _rotationSpeedMultiplier = rotationSpeedMultiplier;
    }

    public void SetMovementLocked(bool isLocked)
    {
        _isMovementLocked = isLocked;

        if (_isMovementLocked)
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
