using UnityEngine;

public class PlayerAnimator : MonoBehaviour
{
    private Animator _animator;

    private static readonly int Speed = Animator.StringToHash("Speed");
    private static readonly int IsCarrying = Animator.StringToHash("IsCarrying");
    private static readonly int IsPushing = Animator.StringToHash("IsPushing");
    private static readonly int IsPulling = Animator.StringToHash("IsPulling");
    private static readonly int Throw = Animator.StringToHash("Throw");

    private void Awake()
    {
        _animator = GetComponent<Animator>();
    }

    public void PlayMoveAnimation(float speed)
    {
        _animator.SetFloat(Speed, Mathf.Clamp01(speed));
    }

    public void PlayCarryingAnimation(bool isCarrying)
    {
        _animator.SetBool(IsCarrying, isCarrying);
    }

    public void PlayThrowAnimation()
    {
        _animator.SetTrigger(Throw);
    }

    public void ResetThrowAnimation()
    {
        _animator.ResetTrigger(Throw);
    }

    public void PlayPushingAnimation(bool isPushing)
    {
        _animator.SetBool(IsPushing, isPushing);
    }

    public void PlayPullingAnimation(bool isPulling)
    {
        _animator.SetBool(IsPulling, isPulling);
    }
}
