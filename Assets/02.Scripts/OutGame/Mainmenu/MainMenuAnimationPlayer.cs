using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

public class MainMenuAnimationPlayer : MonoBehaviour
{
    [SerializeField] private PlayableDirector _director;
    [SerializeField] private TimelineAsset[] _timelines;
    [SerializeField] private bool _playOnStart = true;
    [SerializeField] private bool _avoidRepeat = true;
    [SerializeField] private DirectorWrapMode _wrapMode = DirectorWrapMode.None;

    private int _lastTimelineIndex = -1;
    private bool _isStopping;
    private readonly List<int> _playableTimelineIndexes = new List<int>();

    private void Awake()
    {
        if (_director == null)
        {
            _director = GetComponent<PlayableDirector>();
        }

        BuildPlayableTimelineCache();
    }

    private void OnValidate()
    {
        BuildPlayableTimelineCache();
    }

    private void OnEnable()
    {
        if (_director != null)
        {
            _director.stopped += HandleTimelineStopped;
        }
    }

    private void Start()
    {
        if (_playOnStart)
        {
            PlayRandomTimeline();
        }
    }

    private void OnDisable()
    {
        if (_director != null)
        {
            _director.stopped -= HandleTimelineStopped;
        }
    }

    public void PlayRandomTimeline()
    {
        if (_director == null || !HasPlayableTimeline())
        {
            return;
        }

        int timelineIndex = GetRandomTimelineIndex();
        TimelineAsset timeline = _timelines[timelineIndex];

        if (timeline == null)
        {
            return;
        }

        _lastTimelineIndex = timelineIndex;
        _director.playableAsset = timeline;
        _director.extrapolationMode = _wrapMode;
        _director.time = 0;
        _director.Play();
    }

    public void Stop()
    {
        if (_director == null)
        {
            return;
        }

        _isStopping = true;
        _director.Stop();
        _isStopping = false;
    }

    private void HandleTimelineStopped(PlayableDirector director)
    {
        if (_isStopping || !isActiveAndEnabled)
        {
            return;
        }

        PlayRandomTimeline();
    }

    private int GetRandomTimelineIndex()
    {
        int timelineCount = _playableTimelineIndexes.Count;
        if (!_avoidRepeat || timelineCount <= 1 || _lastTimelineIndex < 0)
        {
            return _playableTimelineIndexes[Random.Range(0, timelineCount)];
        }

        int randomPlayableIndex = Random.Range(0, timelineCount - 1);

        for (int i = 0; i < _playableTimelineIndexes.Count; i++)
        {
            int timelineIndex = _playableTimelineIndexes[i];
            if (timelineIndex == _lastTimelineIndex)
            {
                continue;
            }

            if (randomPlayableIndex == 0)
            {
                return timelineIndex;
            }

            randomPlayableIndex--;
        }

        return _playableTimelineIndexes[0];
    }

    private bool HasPlayableTimeline()
    {
        return _playableTimelineIndexes.Count > 0;
    }

    private void BuildPlayableTimelineCache()
    {
        _playableTimelineIndexes.Clear();

        if (_timelines == null)
        {
            return;
        }

        for (int i = 0; i < _timelines.Length; i++)
        {
            if (_timelines[i] != null)
            {
                _playableTimelineIndexes.Add(i);
            }
        }
    }
}
