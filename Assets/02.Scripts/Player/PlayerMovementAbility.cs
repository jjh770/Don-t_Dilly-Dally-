using System;
using UnityEngine;

public class PlayerMovementAbility : MonoBehaviour
{
    [SerializeField] private float _moveSpeed = 3f;
    [SerializeField] private float _rotationSpeed = 10f;

    private Vector3 _moveDirection;
    private Rigidbody _rigidbody;
    private PlayerAnimator _playerAnimator;

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
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, _rotationSpeed * Time.deltaTime);
        }
    }

    private void HandleMovement()
    {
        Vector3 velocity = _moveDirection * _moveSpeed;
        velocity.y = _rigidbody.linearVelocity.y;
        _rigidbody.linearVelocity = velocity;
    }

    private void UpdateAnimation()
    {
        float speed = _moveDirection.magnitude;
        _playerAnimator.PlayMoveAnimation(speed);
    }
}
