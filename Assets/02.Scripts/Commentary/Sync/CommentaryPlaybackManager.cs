using System;
using System.Collections.Generic;
using UnityEngine;

public class CommentaryPlaybackManager : MonoBehaviour
{
    public event Action OnPlaybackCompleted;
    public event Action<string> OnSubtitleChanged;

    [Header("오디오")]
    [SerializeField] private AudioSource _audioSource;

    [Header("TTS (동적형 전용)")]
    [SerializeField] private TTSManager _ttsManager;

    [Header("고정형/템플릿형 클립")]
    [SerializeField] private EventTypeClipGroup[] _eventTypeClips;

    [Header("설정")]
    [SerializeField] private float _defaultDuration = 3f;

    public bool IsPlaying { get; private set; }

    // EventType → ClipGroup 빠른 조회용
    private Dictionary<EventType, EventTypeClipGroup> _clipGroupMap;

    // 동적형 TTS 캐시
    private readonly Dictionary<string, AudioClip> _ttsCache = new();

    private CommentarySyncData _currentData;
    private Coroutine _playbackCoroutine;

    private void Awake()
    {
        BuildClipGroupMap();
    }

    private void BuildClipGroupMap()
    {
        _clipGroupMap = new Dictionary<EventType, EventTypeClipGroup>();

        if (_eventTypeClips == null) return;

        foreach (var group in _eventTypeClips)
        {
            if (group.ClipData != null && group.ClipData.Length > 0)
            {
                _clipGroupMap[group.EventType] = group;
            }
        }
    }

    public void SetEventTypeClips(EventTypeClipGroup[] clipGroups)
    {
        _eventTypeClips = clipGroups;
        BuildClipGroupMap();
    }

    public void PlayCommentary(CommentarySyncData syncData)
    {
        if (syncData == null) return;

        if (IsPlaying)
        {
            StopPlayback();
        }

        _currentData = syncData;
        IsPlaying = true;

        // 동적형 vs 고정형/템플릿형
        if (syncData.IsDynamic)
        {
            _ = PlayDynamicCommentaryAsync(syncData);
        }
        else
        {
            PlayEventTypeClip(syncData);
        }
    }

    // 동적형 코멘터리 재생
    private async Awaitable PlayDynamicCommentaryAsync(CommentarySyncData syncData)
    {
        if (string.IsNullOrEmpty(syncData.FinalText)) return;

        AudioClip clip = null;
        string cacheKey = syncData.FinalText.GetHashCode().ToString();

        _ttsCache.TryGetValue(cacheKey, out clip);

        // 캐시에 없으면 TTS 생성
        if (clip == null && _ttsManager != null)
        {
            try
            {
                clip = await _ttsManager.GenerateSpeech(syncData.FinalText);
                if (clip != null)
                {
                    _ttsCache[cacheKey] = clip;
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[CommentaryPlaybackManager] TTS 생성 실패: {e.Message}");
            }
        }

        // 재생 중인 상태 확인 (비동기 중 StopPlayback이 호출됐을 수 있음)
        if (!IsPlaying || _currentData != syncData) return;

        // 자막 표시
        OnSubtitleChanged?.Invoke(syncData.FinalText);

        if (clip != null)
        {
            PlayAudioClip(clip);
        }
        else
        {
            float duration = syncData.EstimatedDuration > 0 ? syncData.EstimatedDuration : _defaultDuration;
            _playbackCoroutine = StartCoroutine(WaitForDuration(duration));
        }
    }

    // 고정형/템플릿형 코멘터리 재생 (EventType 기반 클립)
    private void PlayEventTypeClip(CommentarySyncData syncData)
    {
        if (!_clipGroupMap.TryGetValue(syncData.EventType, out var group))
        {
            Debug.LogWarning($"[CommentaryPlaybackManager] EventType {syncData.EventType}에 등록된 클립이 없습니다.");
            CompletePlayback();
            return;
        }

        if (group.ClipData == null || group.ClipData.Length == 0)
        {
            Debug.LogWarning($"[CommentaryPlaybackManager] EventType {syncData.EventType}의 클립 배열이 비어있습니다.");
            CompletePlayback();
            return;
        }

        var selectedData = group.ClipData[UnityEngine.Random.Range(0, group.ClipData.Length)];

        if (selectedData.Clip == null)
        {
            Debug.LogWarning($"[CommentaryPlaybackManager] 선택된 클립이 null입니다. EventType: {syncData.EventType}");
            CompletePlayback();
            return;
        }

        string subtitle = !string.IsNullOrEmpty(selectedData.Subtitle) ? selectedData.Subtitle : syncData.FinalText;
        OnSubtitleChanged?.Invoke(subtitle);

        PlayAudioClip(selectedData.Clip);
    }

    private void PlayAudioClip(AudioClip clip)
    {
        if (_audioSource == null)
        {
            float duration = clip != null ? clip.length : _defaultDuration;
            _playbackCoroutine = StartCoroutine(WaitForDuration(duration));
            return;
        }

        _audioSource.clip = clip;
        _audioSource.Play();
        _playbackCoroutine = StartCoroutine(WaitForAudioComplete());
    }

    public void StopPlayback()
    {
        if (_playbackCoroutine != null)
        {
            StopCoroutine(_playbackCoroutine);
            _playbackCoroutine = null;
        }

        if (_audioSource != null && _audioSource.isPlaying)
        {
            _audioSource.Stop();
        }

        IsPlaying = false;
        _currentData = null;
        OnSubtitleChanged?.Invoke(null);
    }

    private System.Collections.IEnumerator WaitForAudioComplete()
    {
        while (_audioSource != null && _audioSource.isPlaying)
        {
            yield return null;
        }
        CompletePlayback();
    }

    private System.Collections.IEnumerator WaitForDuration(float duration)
    {
        yield return new WaitForSeconds(duration);
        CompletePlayback();
    }

    private void CompletePlayback()
    {
        IsPlaying = false;
        _currentData = null;
        OnSubtitleChanged?.Invoke(null);
        OnPlaybackCompleted?.Invoke();
    }

    public void ClearTTSCache()
    {
        _ttsCache.Clear();
    }

    public async Awaitable<AudioClip> PreGenerateAndCache(string text)
    {
        if (string.IsNullOrEmpty(text) || _ttsManager == null) return null;

        string cacheKey = text.GetHashCode().ToString();

        if (_ttsCache.TryGetValue(cacheKey, out var cachedClip))
        {
            return cachedClip;
        }

        try
        {
            AudioClip clip = await _ttsManager.GenerateSpeech(text);
            if (clip != null)
            {
                _ttsCache[cacheKey] = clip;
            }
            return clip;
        }
        catch (Exception e)
        {
            Debug.LogError($"[CommentaryPlaybackManager] 사전 생성 실패: {e.Message}");
            return null;
        }
    }
}
