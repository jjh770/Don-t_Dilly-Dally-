using UnityEngine;
using System.Collections.Generic;

public class LobbyPreviewAnimator : MonoBehaviour
{
    [Header("Animator")]
    [SerializeField] private Animator _animator;

    [Header("Settings")]
    [SerializeField] private float _idleDurationBeforePose = 15f;

    [Header("Pose Animations")]
    [SerializeField] private List<AnimationClip> _poseClips = new();

    [Header("Parameters")]
    [SerializeField] private string _poseIndexParam = "PoseIndex";
    [SerializeField] private string _poseTriggerParam = "Pose";

    private float _idleTimer;
    private bool _isPlayingPose;

    private int _poseIndexHash;
    private int _poseTriggerHash;

    public IReadOnlyList<AnimationClip> PoseClips => _poseClips;
    public bool IsPlayingPose => _isPlayingPose;
    public float IdleTimer => _idleTimer;

    private void Awake()
    {
        if (_animator == null)
        {
            _animator = GetComponentInChildren<Animator>();
        }

        _poseIndexHash = Animator.StringToHash(_poseIndexParam);
        _poseTriggerHash = Animator.StringToHash(_poseTriggerParam);
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

        if (_idleTimer >= _idleDurationBeforePose)
        {
            PlayRandomPose();
        }
    }

    private void CheckPoseFinished()
    {
        if (_animator == null) return;

        var stateInfo = _animator.GetCurrentAnimatorStateInfo(0);

        if (!stateInfo.IsTag("Pose") && _isPlayingPose)
        {
            OnPoseFinished();
        }
    }

    private void PlayRandomPose()
    {
        if (_animator == null) return;
        if (_poseClips == null || _poseClips.Count == 0) return;

        int randomIndex = Random.Range(0, _poseClips.Count);

        _animator.SetInteger(_poseIndexHash, randomIndex);
        _animator.SetTrigger(_poseTriggerHash);
        _isPlayingPose = true;
        _idleTimer = 0f;

        string clipName = _poseClips[randomIndex] != null ? _poseClips[randomIndex].name : "Unknown";
        Debug.Log($"[LobbyPreviewAnimator] 포즈 재생: {randomIndex} ({clipName})");
    }

    private void OnPoseFinished()
    {
        _isPlayingPose = false;
        _idleTimer = 0f;
        Debug.Log("[LobbyPreviewAnimator] 포즈 종료, Idle 복귀");
    }

    public void ForceIdle()
    {
        _isPlayingPose = false;
        _idleTimer = 0f;
    }
}
