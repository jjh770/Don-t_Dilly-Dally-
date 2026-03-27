using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 코멘터리 재생 및 오디오 캐싱 담당
/// - 원격 클라이언트도 로컬 TTS 생성 지원
/// </summary>
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
    [SerializeField] private bool _generateTtsOnRemote = true;

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
        AudioClip clip = null;

        Debug.Log($"[CommentaryPlaybackManager] 재생 시작 - UsePreGenerated: {syncData.UsePreGeneratedVoice}, TtsKey: {syncData.TtsAudioKey}, Text: {syncData.FinalText?.Substring(0, Mathf.Min(20, syncData.FinalText?.Length ?? 0))}");

        // 1. 사전 생성된 음성 (고정형)
        if (syncData.UsePreGeneratedVoice)
        {
            clip = GetClip(syncData.PreGeneratedClipId);
            Debug.Log($"[CommentaryPlaybackManager] 고정형 클립 로드: {(clip != null ? "성공" : "실패")}");
        }
        // 2. TTS 캐시 확인 (템플릿형/동적형)
        else if (!string.IsNullOrEmpty(syncData.TtsAudioKey))
        {
            clip = GetCachedClip(syncData.TtsAudioKey);
            Debug.Log($"[CommentaryPlaybackManager] 캐시 확인: {(clip != null ? "있음" : "없음")}");
        }

        // 3. 캐시에 없으면 로컬에서 TTS 생성 (템플릿형/동적형 - 모든 클라이언트)
        if (clip == null && !syncData.UsePreGeneratedVoice && !string.IsNullOrEmpty(syncData.FinalText))
        {
            if (_ttsManager == null)
            {
                Debug.LogError("[CommentaryPlaybackManager] TTSManager가 할당되지 않았습니다! Inspector에서 연결해주세요.");
            }
            else if (!_generateTtsOnRemote)
            {
                Debug.LogWarning("[CommentaryPlaybackManager] _generateTtsOnRemote가 비활성화되어 있습니다.");
            }
            else
            {
                Debug.Log($"[CommentaryPlaybackManager] 로컬 TTS 생성 시작: {syncData.FinalText.Substring(0, Mathf.Min(20, syncData.FinalText.Length))}...");
                try
                {
                    clip = await _ttsManager.GenerateSpeech(syncData.FinalText);
                    if (clip != null)
                    {
                        Debug.Log("[CommentaryPlaybackManager] TTS 생성 성공");
                        if (!string.IsNullOrEmpty(syncData.TtsAudioKey))
                        {
                            CacheClip(syncData.TtsAudioKey, clip);
                        }
                    }
                    else
                    {
                        Debug.LogWarning("[CommentaryPlaybackManager] TTS 생성 결과가 null입니다.");
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"[CommentaryPlaybackManager] 로컬 TTS 생성 실패: {e.Message}");
                }
            }
        }

        // 재생 중인 상태 확인 (비동기 중 StopPlayback이 호출됐을 수 있음)
        if (!IsPlaying || _currentData != syncData)
        {
            Debug.Log("[CommentaryPlaybackManager] 재생 상태 변경됨, 중단");
            return;
        }

        if (clip != null)
        {
            Debug.Log("[CommentaryPlaybackManager] 오디오 재생 시작");
            PlayAudioClip(clip);
        }
        else
        {
            Debug.LogWarning($"[CommentaryPlaybackManager] 클립 없음, 텍스트만 표시 (duration: {syncData.EstimatedDuration}s)");
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
