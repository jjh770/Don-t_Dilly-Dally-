using System;
using UnityEngine;

public class PlayerMovementAbility : MonoBehaviour
{
    [SerializeField] private float _moveSpeed = 3f;
    [SerializeField] private float _rotationSpeed = 10f;

    public event Action<float> OnMoveSpeedChanged;

    private Rigidbody rb;
    private Vector3 moveDirection;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void Update()
    {
        HandleInput();
        HandleRotation();
        OnMoveSpeedChanged?.Invoke(moveDirection.magnitude);
    }

    private void FixedUpdate()
    {
        HandleMovement();
    }

    private void HandleInput()
    {
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");
        moveDirection = new Vector3(h, 0, v).normalized;
    }

    private void HandleRotation()
    {
        if (moveDirection.sqrMagnitude > 0.01f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, _rotationSpeed * Time.deltaTime);
        }
    }

    private void HandleMovement()
    {
        Vector3 velocity = moveDirection * _moveSpeed;
        velocity.y = rb.linearVelocity.y;
        rb.linearVelocity = velocity;

    }
}
