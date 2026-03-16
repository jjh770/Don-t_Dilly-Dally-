using UnityEngine;

public class PlayerAnimator : MonoBehaviour
{
    private Animator _animator;

    private static readonly int IsHolding = Animator.StringToHash("IsHolding");
    private static readonly int IsGrabbing = Animator.StringToHash("IsGrabbing");
    private static readonly int IsPushing = Animator.StringToHash("IsPushing");
    private static readonly int IsWalking = Animator.StringToHash("IsWalking");
    private static readonly int Throw = Animator.StringToHash("Throw");

    private void Awake()
    {
        _animator = GetComponent<Animator>();
    }

    public void PlayWalkAnimation(bool isWalking)
    {
        _animator.SetBool(IsWalking, isWalking);
    }

    public void PlayHoldAnimation(bool isHolding)
    {
        _animator.SetBool(IsHolding, isHolding);
    }

    public void PlayGrabAnimation(bool isGrabbing)
    {
        _animator.SetBool(IsGrabbing, isGrabbing);
    }

    public void PlayThrowAnimation()
    {
        _animator.SetTrigger(Throw);
    }

    public void ResetThrowAnimation()
    {
        _animator.ResetTrigger(Throw);
    }

    public void PlayPushAnimation(bool isPushing)
    {
        _animator.SetBool(IsPushing, isPushing);
    }
}
