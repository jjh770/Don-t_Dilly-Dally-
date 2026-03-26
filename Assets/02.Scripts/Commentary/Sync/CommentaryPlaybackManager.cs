using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 코멘터리 재생 및 오디오 캐싱 담당
/// </summary>
public class CommentaryPlaybackManager : MonoBehaviour
{
    public event Action OnPlaybackCompleted;
    public event Action<string> OnSubtitleChanged;

    [Header("오디오")]
    [SerializeField] private AudioSource _audioSource;

    [Header("설정")]
    [SerializeField] private float _defaultDuration = 3f;

    public bool IsPlaying { get; private set; }

    private readonly Dictionary<string, AudioClip> _clipCache = new();
    private CommentarySyncData _currentData;
    private Coroutine _playbackCoroutine;

    public void CacheClip(string clipName, AudioClip clip)
    {
        if (string.IsNullOrEmpty(clipName) || clip == null) return;
        _clipCache[clipName] = clip;
    }

    public AudioClip GetCachedClip(string clipName)
    {
        if (string.IsNullOrEmpty(clipName)) return null;
        _clipCache.TryGetValue(clipName, out AudioClip clip);
        return clip;
    }

    public bool HasCachedClip(string clipName)
    {
        return !string.IsNullOrEmpty(clipName) && _clipCache.ContainsKey(clipName);
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

        OnSubtitleChanged?.Invoke(syncData.FinalText);

        AudioClip clip = null;

        if (syncData.UsePreGeneratedVoice)
        {
            clip = GetClip(syncData.PreGeneratedClipId);
        }
        else if (!string.IsNullOrEmpty(syncData.TtsAudioKey))
        {
            clip = GetCachedClip(syncData.TtsAudioKey);
        }

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

    private AudioClip GetClip(string clipId)
    {
        if (string.IsNullOrEmpty(clipId)) return null;

        // 캐시에서 찾기
        var clip = GetCachedClip(clipId);
        if (clip != null) return clip;

        // Resources에서 로드
        return Resources.Load<AudioClip>($"Commentary/{clipId}");
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

    public CommentarySyncData GetCurrentCommentary() => _currentData;
}
