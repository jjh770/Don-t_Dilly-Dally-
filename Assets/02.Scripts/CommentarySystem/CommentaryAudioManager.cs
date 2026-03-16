using System.Collections.Generic;
using UnityEngine;

public class CommentaryAudioManager : MonoBehaviour
{
    [Header("Audio Source")]
    [SerializeField] private AudioSource _voiceSource;

    [Header("Pre-generated Voice Clips")]
    [SerializeField] private List<PreGeneratedClip> _preGeneratedClips = new();

    private readonly Dictionary<string, AudioClip> _clipDictionary = new();

    private void Awake()
    {
        InitializeClipDictionary();
    }

    private void InitializeClipDictionary()
    {
        foreach (PreGeneratedClip clip in _preGeneratedClips)
        {
            if (clip.clip != null && !string.IsNullOrEmpty(clip.clipName))
            {
                _clipDictionary[clip.clipName] = clip.clip;
            }
        }
    }

    public void PlayVoice(AudioClip clip)
    {
        if (clip == null)
        {
            Debug.LogWarning("[CommentaryAudioManager] AudioClip is null");
            return;
        }

        if (_voiceSource == null)
        {
            Debug.LogError("[CommentaryAudioManager] AudioSource is not assigned");
            return;
        }

        // 현재 재생 중이면 중단
        if (_voiceSource.isPlaying)
        {
            _voiceSource.Stop();
        }

        _voiceSource.clip = clip;
        _voiceSource.Play();
    }

    public AudioClip GetPreGeneratedClip(string clipName)
    {
        if (string.IsNullOrEmpty(clipName)) return null;

        _clipDictionary.TryGetValue(clipName, out AudioClip clip);
        return clip;
    }

    public void StopVoice()
    {
        if (_voiceSource != null && _voiceSource.isPlaying)
        {
            _voiceSource.Stop();
        }
    }

    public bool IsPlaying => _voiceSource != null && _voiceSource.isPlaying;

    [System.Serializable]
    public class PreGeneratedClip
    {
        public string clipName;
        public AudioClip clip;
    }
}
