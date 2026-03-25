using UnityEngine;

[RequireComponent(typeof(Animator))]
public class LobbyPreviewAnimator : MonoBehaviour
{
    [Header("세팅")]
    [SerializeField] private float _idleDuration = 12f;
    [SerializeField] private int _poseCount = 3;

    private const int InvalidPoseIndex = -1;
    private const int DefaultAnimatorLayer = 0;
    private const int MinPoseCount = 1;
    private const float ResetTimer = 0f;

    private const string ParamPoseIndex = "PoseIndex";
    private const string ParamPose = "Pose";
    private const string ParamCustomizingSave = "CustomizingSave";
    private const string TagPose = "Pose";

    private static readonly int PoseIndexHash = Animator.StringToHash(ParamPoseIndex);
    private static readonly int PoseTriggerHash = Animator.StringToHash(ParamPose);
    private static readonly int CustomizingSaveHash = Animator.StringToHash(ParamCustomizingSave);
    private static readonly int PoseStateTagHash = Animator.StringToHash(TagPose);

    private float _idleTimer;
    private bool _isPlayingPose;
    private int _lastPoseIndex = InvalidPoseIndex;

    private Animator _animator;

    private void Awake()
    {
        _animator = GetComponent<Animator>();
    }

    private void Update()
    {
        if (_isPlayingPose)
        {
            CheckPoseFinished();
        }
        else
        {
            UpdateIdleTimer();
        }
    }

    private void UpdateIdleTimer()
    {
        _idleTimer += Time.deltaTime;

        if (_idleTimer >= _idleDuration)
        {
            PlayRandomPose();
        }
    }

    private void CheckPoseFinished()
    {
        if (_animator == null) return;

        var stateInfo = _animator.GetCurrentAnimatorStateInfo(DefaultAnimatorLayer);

        if (!stateInfo.tagHash.Equals(PoseStateTagHash) && _isPlayingPose)
        {
            OnPoseFinished();
        }
    }

    private void PlayRandomPose()
    {
        if (_animator == null) return;
        if (_poseCount < MinPoseCount) return;

        int randomIndex = GetRandomPoseIndex();
        _lastPoseIndex = randomIndex;

        _animator.SetInteger(PoseIndexHash, randomIndex);
        _animator.SetTrigger(PoseTriggerHash);
        _isPlayingPose = true;
        _idleTimer = ResetTimer;
    }

    private int GetRandomPoseIndex()
    {
        if (_poseCount == MinPoseCount) return 0;

        int randomIndex;
        do
        {
            randomIndex = Random.Range(0, _poseCount);
        } while (randomIndex == _lastPoseIndex);

        return randomIndex;
    }

    private void OnPoseFinished()
    {
        _isPlayingPose = false;
        _idleTimer = ResetTimer;
    }

    public void PlayCustomizingSave()
    {
        if (_animator == null) return;

        _animator.SetTrigger(CustomizingSaveHash);
        _isPlayingPose = true;
        _idleTimer = ResetTimer;
    }
}
