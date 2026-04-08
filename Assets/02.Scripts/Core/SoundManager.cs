using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;
using UnityEngine.Pool;

// ──────────────────────────────────────────
//  키 enum — 사운드 추가 시 여기에만 등록
// ──────────────────────────────────────────
public enum BGMKey
{
    None,
    Robby,
    WaitingRoom,
    Stage1,
    Stage2,
    Stage3,
}

public enum SFXKey
{
    None,
    UIClick = 1,
    UIConfirm = 2,
}

/// <summary>
/// 사운드 재생 타입
/// - BGM    : 로컬 전용 배경음악
/// - RPC    : PunRPC로 전체 클라이언트에 동기화되는 효과음
/// - Local  : 로컬 전용 효과음 (자신에게만 들림)
/// </summary>
public enum SoundType
{
    BGM,
    RPC,
    Local
}

// ──────────────────────────────────────────
//  인스펙터용 데이터 컨테이너
// ──────────────────────────────────────────
[System.Serializable]
public class BGMEntry
{
    public BGMKey Key;
    public AudioClip Clip;
    [Range(0f, 1f)] public float Volume = 1f;
    [Range(0.5f, 2f)] public float Pitch = 1f;
}

[System.Serializable]
public class SFXEntry
{
    public SFXKey Key;
    public AudioClip Clip;
    [Range(0f, 1f)] public float Volume = 1f;
    [Range(0.5f, 2f)] public float Pitch = 1f;
    public bool Loop = false;
}

public class SoundManager : PunPersistentSingleton<SoundManager>
{

    // ──────────────────────────────────────────
    //  인스펙터 설정
    // ──────────────────────────────────────────
    [Header("BGM")]
    [SerializeField] private BGMEntry[] _bgmEntries;
    [SerializeField][Range(0f, 1f)] private float _bgmVolume = 0.5f;
    [SerializeField] private float _bgmFadeDuration = 1.0f;

    [Header("SFX (RPC + Local)")]
    [SerializeField] private SFXEntry[] _sfxEntries;
    [SerializeField][Range(0f, 1f)] private float _sfxVolume = 1.0f;
    [SerializeField] private int _sfxPoolDefault = 8;
    [SerializeField] private int _sfxPoolMax = 20;

    // ──────────────────────────────────────────
    //  내부 상태
    // ──────────────────────────────────────────
    private AudioSource _bgmSource;
    private IObjectPool<AudioSource> _sfxPool;

    private Dictionary<BGMKey, BGMEntry> _bgmDict = new Dictionary<BGMKey, BGMEntry>();
    private Dictionary<SFXKey, SFXEntry> _sfxDict = new Dictionary<SFXKey, SFXEntry>();

    private Coroutine _bgmFadeCoroutine;

    // ──────────────────────────────────────────
    //  초기화
    // ──────────────────────────────────────────
    protected override void Awake()
    {
        base.Awake();

        InitBGMSource();
        InitSFXPool();
        BuildDictionaries();
    }

    private void InitBGMSource()
    {
        _bgmSource = gameObject.AddComponent<AudioSource>();
        _bgmSource.loop = true;
        _bgmSource.playOnAwake = false;
        _bgmSource.volume = _bgmVolume;
    }

    private void InitSFXPool()
    {
        _sfxPool = new ObjectPool<AudioSource>(
            createFunc: CreateSFXSource,
            actionOnGet: src => src.gameObject.SetActive(true),
            actionOnRelease: src => { src.Stop(); src.clip = null; src.gameObject.SetActive(false); },
            actionOnDestroy: src => Destroy(src.gameObject),
            collectionCheck: true,
            defaultCapacity: _sfxPoolDefault,
            maxSize: _sfxPoolMax
        );
    }

    private void BuildDictionaries()
    {
        foreach (var e in _bgmEntries)
            if (e.Key != BGMKey.None) _bgmDict[e.Key] = e;

        foreach (var e in _sfxEntries)
            if (e.Key != SFXKey.None) _sfxDict[e.Key] = e;
    }

    private AudioSource CreateSFXSource()
    {
        var go = new GameObject("SFX_Source");
        go.transform.SetParent(transform);
        var src = go.AddComponent<AudioSource>();
        src.playOnAwake = false;
        return src;
    }

    // ══════════════════════════════════════════
    //  PUBLIC API
    // ══════════════════════════════════════════

    /// <summary>BGM을 재생합니다. (페이드 전환 포함)</summary>
    public void Play(BGMKey key) => PlayBGM(key);

    /// <summary>SFX를 재생합니다.</summary>
    public void Play(SFXKey key, SoundType type)
    {
        switch (type)
        {
            case SoundType.RPC: PlaySFX_RPC(key); break;
            case SoundType.Local: PlaySFX_Local(key); break;
            default:
                Debug.LogWarning("[SoundManager] SFX에 SoundType.BGM은 사용할 수 없습니다.");
                break;
        }
    }

    /// <summary>현재 BGM을 정지합니다.</summary>
    public void StopBGM(bool fade = true)
    {
        if (fade) StartFade(_bgmSource, _bgmFadeDuration, 0f, () => _bgmSource.Stop());
        else _bgmSource.Stop();
    }

    /// <summary>루프 SFX를 명시적으로 정지하고 풀에 반납합니다.</summary>
    public void StopSFX(AudioSource source)
    {
        if (source != null) _sfxPool.Release(source);
    }

    public void SetBGMVolume(float volume) { _bgmVolume = Mathf.Clamp01(volume); _bgmSource.volume = _bgmVolume; }
    public void SetSFXVolume(float volume) { _sfxVolume = Mathf.Clamp01(volume); }

    // ══════════════════════════════════════════
    //  BGM
    // ══════════════════════════════════════════
    private void PlayBGM(BGMKey key)
    {
        if (!_bgmDict.TryGetValue(key, out var data))
        {
            Debug.LogWarning($"[SoundManager] BGM 키 없음: {key}");
            return;
        }

        if (_bgmSource.isPlaying && _bgmSource.clip == data.Clip) return;

        if (_bgmFadeCoroutine != null) StopCoroutine(_bgmFadeCoroutine);

        if (_bgmSource.isPlaying)
        {
            StartFade(_bgmSource, _bgmFadeDuration * 0.5f, 0f, () =>
            {
                SetBGMClip(data);
                StartFade(_bgmSource, _bgmFadeDuration * 0.5f, _bgmVolume);
            });
        }
        else
        {
            SetBGMClip(data);
            StartFade(_bgmSource, _bgmFadeDuration, _bgmVolume);
        }
    }

    private void SetBGMClip(BGMEntry data)
    {
        _bgmSource.clip = data.Clip;
        _bgmSource.pitch = data.Pitch;
        _bgmSource.volume = 0f;
        _bgmSource.Play();
    }

    // ══════════════════════════════════════════
    //  SFX RPC
    // ══════════════════════════════════════════
    private void PlaySFX_RPC(SFXKey key)
    {
        if (PhotonNetwork.IsConnected && PhotonNetwork.InRoom)
            photonView.RPC(nameof(RPC_PlaySFX), RpcTarget.All, (int)key);
        else
            PlaySFXInternal(key);
    }

    [PunRPC]
    private void RPC_PlaySFX(int keyInt) => PlaySFXInternal((SFXKey)keyInt);

    // ══════════════════════════════════════════
    //  SFX Local
    // ══════════════════════════════════════════
    private void PlaySFX_Local(SFXKey key) => PlaySFXInternal(key);

    // ══════════════════════════════════════════
    //  공통 SFX 재생
    // ══════════════════════════════════════════
    private void PlaySFXInternal(SFXKey key)
    {
        if (!_sfxDict.TryGetValue(key, out var data))
        {
            Debug.LogWarning($"[SoundManager] SFX 키 없음: {key}");
            return;
        }

        var source = _sfxPool.Get();
        source.clip = data.Clip;
        source.volume = data.Volume * _sfxVolume;
        source.pitch = data.Pitch;
        source.loop = data.Loop;
        source.Play();

        if (!data.Loop)
            StartCoroutine(ReleaseWhenDone(source, data.Clip.length / data.Pitch));
    }

    private IEnumerator ReleaseWhenDone(AudioSource source, float duration)
    {
        yield return new WaitForSeconds(duration);
        _sfxPool.Release(source);
    }

    // ══════════════════════════════════════════
    //  페이드 유틸리티
    // ══════════════════════════════════════════
    private void StartFade(AudioSource source, float duration, float targetVolume, System.Action onComplete = null)
    {
        if (_bgmFadeCoroutine != null) StopCoroutine(_bgmFadeCoroutine);
        _bgmFadeCoroutine = StartCoroutine(FadeRoutine(source, duration, targetVolume, onComplete));
    }

    private IEnumerator FadeRoutine(AudioSource source, float duration, float targetVolume, System.Action onComplete)
    {
        float start = source.volume, elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            source.volume = Mathf.Lerp(start, targetVolume, elapsed / duration);
            yield return null;
        }
        source.volume = targetVolume;
        onComplete?.Invoke();
    }
}
