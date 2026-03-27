using System;
using System.Collections.Generic;
using UnityEngine;

public class CommentaryPlaybackManager : MonoBehaviour
{
    public event Action OnPlaybackCompleted;
    public event Action<string> OnSubtitleChanged;

    [Header("오디오")]
    [SerializeField] private AudioSource _audioSource;

    [Header("TTS")]
    [SerializeField] private TTSManager _ttsManager;

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

        // 비동기로 오디오 재생 시작
        _ = PlayCommentaryAsync(syncData);
    }

    private async Awaitable PlayCommentaryAsync(CommentarySyncData syncData)
    {
        if (string.IsNullOrEmpty(syncData.FinalText)) return;

        // 캐시 키: 동일 텍스트는 동일 음성 재사용
        string cacheKey = syncData.FinalText.GetHashCode().ToString();
        AudioClip clip = GetCachedClip(cacheKey);

        // 캐시에 없으면 TTS 생성 (모든 타입 동일 처리)
        if (clip == null && _ttsManager != null)
        {
            try
            {
                clip = await _ttsManager.GenerateSpeech(syncData.FinalText);
                if (clip != null)
                {
                    CacheClip(cacheKey, clip);
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[CommentaryPlaybackManager] TTS 생성 실패: {e.Message}");
            }
        }

        // 재생 중인 상태 확인 (비동기 중 StopPlayback이 호출됐을 수 있음)
        if (!IsPlaying || _currentData != syncData) return;

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
