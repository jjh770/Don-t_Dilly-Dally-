using System;
using UnityEngine;

public class PlayerMovementAbility : MonoBehaviour
{
    [SerializeField] private float _moveSpeed = 3f;
    [SerializeField] private float _rotationSpeed = 10f;
    [SerializeField] private float _acceleration = 5f;

    private Vector3 _moveDirection;
    private float _currentSpeed;
    private float _moveSpeedMultiplier = 1f;
    private float _rotationSpeedMultiplier = 1f;
    private Rigidbody _rigidbody;
    private PlayerAnimator _playerAnimator;

    public Vector3 MoveDirection => _moveDirection;
    public float CurrentSpeed => _currentSpeed * _moveSpeedMultiplier;

    private const string HorizontalAxis = "Horizontal";
    private const string VerticalAxis = "Vertical";
    private const float MinMoveSqrMagnitude = 0.01f;

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
        _playerAnimator = GetComponent<PlayerAnimator>();
    }

    private void Update()
    {
        HandleInput();
        HandleRotation();
        UpdateAnimation();
    }

    private void FixedUpdate()
    {
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

    private void UpdateAnimation()
    {
        bool isWalking = _moveDirection.sqrMagnitude > MinMoveSqrMagnitude;
        _playerAnimator.PlayWalkAnimation(isWalking);
    }
}
