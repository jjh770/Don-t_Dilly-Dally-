using System.Collections.Generic;
using UnityEngine;

public class CommentaryAudioManager : MonoBehaviour
{
    [Header("오디오 소스")]
    [SerializeField] private AudioSource _voiceSource;

    private readonly Dictionary<string, AudioClip> _clipCache = new();

    public void CacheClip(string clipName, AudioClip clip)
    {
        if (string.IsNullOrEmpty(clipName) || clip == null) return;

        _clipCache[clipName] = clip;
        Debug.Log($"[CommentaryAudioManager] 클립 캐싱 완료: {clipName}");
    }

    public void PlayVoice(AudioClip clip)
    {
        if (clip == null)
        {
            Debug.LogWarning("[CommentaryAudioManager] 오디오 클립이 비었습니다.");
            return;
        }

        if (_voiceSource == null)
        {
            Debug.LogError("[CommentaryAudioManager] 오디오 소스가 할당되지 않았습니다.");
            return;
        }

        if (_voiceSource.isPlaying)
        {
            _voiceSource.Stop();
        }

        _voiceSource.clip = clip;
        _voiceSource.Play();
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

    public void StopVoice()
    {
        if (_voiceSource != null && _voiceSource.isPlaying)
        {
            _voiceSource.Stop();
        }
    }

    public bool IsPlaying => _voiceSource != null && _voiceSource.isPlaying;
}
