using UnityEngine;

public class PlayerAnimator : MonoBehaviour
{
    private Animator _animator;

    private static readonly int Speed = Animator.StringToHash("Speed");
    private static readonly int IsCarrying = Animator.StringToHash("IsCarrying");

    private void Awake()
    {
        _animator = GetComponent<Animator>();
    }

    public void PlayMoveAnimation(float speed)
    {
        _animator.SetFloat(Speed, Mathf.Clamp01(speed));
    }

    public void SetCarrying(bool isCarrying)
    {
        _animator.SetBool(IsCarrying, isCarrying);
    }
}
