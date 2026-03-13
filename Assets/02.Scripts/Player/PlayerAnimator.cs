using UnityEngine;

public class PlayerAnimator : MonoBehaviour
{
    private Animator _animator;
    private PlayerMovementAbility _movementAbility;

    private static readonly int Speed = Animator.StringToHash("Speed");

    private void Awake()
    {
        _animator = GetComponent<Animator>();
        _movementAbility = GetComponent<PlayerMovementAbility>();
    }

    private void OnEnable()
    {
        _movementAbility.OnMoveSpeedChanged += UpdateMoveAnimation;
    }

    private void OnDisable()
    {
        _movementAbility.OnMoveSpeedChanged -= UpdateMoveAnimation;
    }

    private void UpdateMoveAnimation(float speed)
    {
        _animator.SetFloat(Speed, Mathf.Clamp01(speed));
    }
}
