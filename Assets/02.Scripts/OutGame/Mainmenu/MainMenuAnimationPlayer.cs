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

    private void Awake()
    {
        if (_director == null)
        {
            _director = GetComponent<PlayableDirector>();
        }
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
        int timelineCount = CountPlayableTimelines();
        if (!_avoidRepeat || timelineCount <= 1 || _lastTimelineIndex < 0)
        {
            return GetPlayableTimelineIndexAt(Random.Range(0, timelineCount));
        }

        int randomPlayableIndex = Random.Range(0, timelineCount - 1);
        int selectedPlayableIndex = 0;

        for (int i = 0; i < _timelines.Length; i++)
        {
            if (_timelines[i] == null || i == _lastTimelineIndex)
            {
                continue;
            }

            if (selectedPlayableIndex == randomPlayableIndex)
            {
                return i;
            }

            selectedPlayableIndex++;
        }

        return GetPlayableTimelineIndexAt(0);
    }

    private bool HasPlayableTimeline()
    {
        return CountPlayableTimelines() > 0;
    }

    private int CountPlayableTimelines()
    {
        if (_timelines == null)
        {
            return 0;
        }

        int count = 0;
        for (int i = 0; i < _timelines.Length; i++)
        {
            if (_timelines[i] != null)
            {
                count++;
            }
        }

        return count;
    }

    private int GetPlayableTimelineIndexAt(int playableIndex)
    {
        for (int i = 0; i < _timelines.Length; i++)
        {
            if (_timelines[i] == null)
            {
                continue;
            }

            if (playableIndex == 0)
            {
                return i;
            }

            playableIndex--;
        }

        return 0;
    }
}
