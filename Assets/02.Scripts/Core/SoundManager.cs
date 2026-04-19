using ExternPropertyAttributes;
using Photon.Pun;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

// ──────────────────────────────────────────
//  키 enum — 사운드 추가 시 여기에만 등록
// ──────────────────────────────────────────
public enum BGMKey
{
    None,
    Main,
    Lobby,
    WaitingRoom,
    Halloween,
    Military,
    Winter,
    Hospital,
}

public enum SFXKey
{
    None,
    UIButtonClick = 1,
    UIButtonConfirm = 2,
    UIPanelOpen = 3,
    UIPanelClose = 4,
    AmbHelicopter1 = 5,
    AmbJetFly1 = 6,
    AmbPenguin1 = 7,
    AmbPenguin2 = 8,
    AmbPenguin3 = 9,
    PlayerThrow = 10,
    PlayerPickUp = 11,
    PlayerDrop = 12,
    PatientEmergencyBeep = 13,
    PotionMixerOpen = 14,
    PotionMixerClose = 15,
    PotionMixerInProgress = 16,
    PotionMixerComplete = 17,
    SterilizerOpen = 18,
    SterilizerClose = 19,
    SterilizerInProgress = 20,
    SterilizerComplete = 21,
    SterilizerSteam = 22,
    RecycleBin = 23,
    PatientBowlingStrike = 24,
    PatientBrake = 25,
    PatinetRocketHovering = 26,
    PatientRocketLanding = 27,
    PatientDeathSkeleton = 28,
    PatientDeathSkeletonPoof = 29,
    PatientDeathCoffinRise = 30,
    PatientDeathCoffinClose = 31,
    PatinetDeathAngel = 32,
    UIGoToWorkButton = 33,
    AmbHelicopter2 = 34,
    AmbJetFly2 = 35,
    AmbJetFly3 = 36,
    PatientWirePullDown = 37,
    PatientWirePullUp = 38,
    PatientWireHook = 39,
    PatientRecipeSuccess = 40,
    PatientSurgeryComplete = 41,
    SurgeryFail = 42,
    PatientCatapultFling = 43,
    PlayerWaterSplash = 44,
    Result2Star = 45,
    Result3Star = 46,
    AmbHelicopterLoop = 47,
    PatientBurnout = 48,
    PlayerHeavyMachineMove = 49,
}

// 사운드 재생 타입.
// - BGM    : 로컬 전용 배경음악.
// - RPC    : PunRPC로 전체 클라이언트에 동기화되는 효과음.
// - Local  : 로컬 전용 효과음 (자신에게만 들림).
public enum SoundType
{
    BGM,
    RPC,
    Local
}

// ──────────────────────────────────────────
//  인스펙터용 데이터 컨테이너
// ──────────────────────────────────────────
[Serializable]
public class BGMEntry
{
    public BGMKey Key;
    public AudioClip Clip;
    [Range(0f, 1f)] public float Volume = 1f;
    [Range(0.5f, 2f)] public float Pitch = 1f;
}

[Serializable]
public class SFXEntry
{
    public SFXKey Key;
    public AudioClip Clip;
    [Range(0f, 1f)] public float Volume = 1f;
    [Range(0.5f, 2f)] public float Pitch = 1f;
    public bool Loop = false;
    [AllowNesting]
    [ShowIf(nameof(Loop))]
    [Min(0f)] public float FadeOutDuration = 0.15f;

    public float Duration => (Clip != null && Pitch > 0) ? Clip.length / Pitch : 0f;
}
public class SoundManager : PunPersistentSingleton<SoundManager>
{
    // ──────────────────────────────────────────
    //  인스펙터 설정
    // ──────────────────────────────────────────
    [Header("BGM")]
    [SerializeField] private BGMEntry[] _bgmEntries = Array.Empty<BGMEntry>();
    [SerializeField][Range(0f, 1f)] private float _bgmVolume = 0.5f;
    [SerializeField] private float _bgmFadeDuration = 1.0f;

    [Header("SFX (RPC + Local)")]
    [SerializeField] private SFXEntry[] _sfxEntries = Array.Empty<SFXEntry>();
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
    private readonly Dictionary<AudioSource, Coroutine> _sfxFadeCoroutines = new Dictionary<AudioSource, Coroutine>();
    private readonly Dictionary<AudioSource, float> _sfxFadeDurations = new Dictionary<AudioSource, float>();
    private float _currentBgmEntryVolume = 1f;

    public float BGMVolume => _bgmVolume;
    public float SFXVolume => _sfxVolume;

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
            actionOnGet: src =>
            {
                StopTrackedSFXFade(src);
                _sfxFadeDurations.Remove(src);
                src.gameObject.SetActive(true);
            },
            actionOnRelease: src =>
            {
                StopTrackedSFXFade(src);
                _sfxFadeDurations.Remove(src);
                src.Stop();
                src.clip = null;
                src.loop = false;
                src.pitch = 1f;
                src.gameObject.SetActive(false);
            },
            actionOnDestroy: src => Destroy(src.gameObject),
            collectionCheck: true,
            defaultCapacity: _sfxPoolDefault,
            maxSize: _sfxPoolMax
        );
    }

    private void BuildDictionaries()
    {
        foreach (var e in _bgmEntries)
        {
            if (e.Key != BGMKey.None)
            {
                _bgmDict[e.Key] = e;
            }
        }

        foreach (var e in _sfxEntries)
        {
            if (e.Key != SFXKey.None)
            {
                _sfxDict[e.Key] = e;
            }
        }
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

    /// <summary>Loop가 켜진 SFX만 핸들을 반환하며 재생합니다.</summary>
    public AudioSource PlayLoop(SFXKey key)
    {
        return PlaySFX_Local(key, requireLoop: true);
    }

    /// <summary>현재 BGM을 정지합니다.</summary>
    public void StopBGM(bool fade = true)
    {
        if (fade) StartFade(_bgmSource, _bgmFadeDuration, 0f, () => _bgmSource.Stop());
        else _bgmSource.Stop();
    }

    /// <summary>루프 SFX를 명시적으로 정지하고 풀에 반납합니다.</summary>
    public void StopSFX(AudioSource source, bool fade = true)
    {
        if (source == null)
        {
            return;
        }

        float fadeDuration = 0f;
        _sfxFadeDurations.TryGetValue(source, out fadeDuration);

        if (!fade || fadeDuration <= 0f || !source.gameObject.activeSelf)
        {
            _sfxPool.Release(source);
            return;
        }

        StartSFXFade(source, fadeDuration, 0f, () => _sfxPool.Release(source));
    }

    public void SetBGMVolume(float volume) { _bgmVolume = Mathf.Clamp01(volume); _bgmSource.volume = _bgmVolume * _currentBgmEntryVolume; }
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

        if (_bgmSource.isPlaying)
        {
            StartFade(_bgmSource, _bgmFadeDuration * 0.5f, 0f, () =>
            {
                SetBGMClip(data);
                StartFade(_bgmSource, _bgmFadeDuration * 0.5f, _bgmVolume * data.Volume);
            });
        }
        else
        {
            SetBGMClip(data);
            StartFade(_bgmSource, _bgmFadeDuration, _bgmVolume * data.Volume);
        }
    }

    private void SetBGMClip(BGMEntry data)
    {
        _currentBgmEntryVolume = Mathf.Clamp01(data.Volume);
        _bgmSource.clip = data.Clip;
        _bgmSource.pitch = data.Pitch;
        _bgmSource.volume = 0f;
        _bgmSource.Play();
    }

    // ══════════════════════════════════════════
    //  SFX RPC
    // ══════════════════════════════════════════
    private AudioSource PlaySFX_RPC(SFXKey key, bool requireLoop = false)
    {
        if (requireLoop)
        {
            Debug.LogWarning("[SoundManager] Loop SFX는 현재 Local 재생만 지원합니다.");
            return null;
        }

        if (PhotonNetwork.IsConnected && PhotonNetwork.InRoom)
        {
            if (SceneRPCView.Instance == null)
            {
                Debug.LogWarning("[SoundManager] SceneRPCView가 씬에 존재하지 않습니다. RPC SFX를 재생할 수 없습니다.");
                return null;
            }
            SceneRPCView.Instance.PlaySfxForAll(key);
            return null;
        }
        else
        {
            return PlaySFXInternal(key, requireLoop);
        }
    }


    // ══════════════════════════════════════════
    //  SFX Local
    // ══════════════════════════════════════════
    private AudioSource PlaySFX_Local(SFXKey key, bool requireLoop = false) => PlaySFXInternal(key, requireLoop);

    // ══════════════════════════════════════════
    //  공통 SFX 재생
    // ══════════════════════════════════════════
    private AudioSource PlaySFXInternal(SFXKey key, bool requireLoop = false)
    {
        if (!_sfxDict.TryGetValue(key, out var data))
        {
            Debug.LogWarning($"[SoundManager] SFX 키 없음: {key}");
            return null;
        }

        if (requireLoop && !data.Loop)
        {
            Debug.LogWarning($"[SoundManager] Loop로 재생하려면 SFX Entry의 Loop를 켜야 합니다: {key}");
            return null;
        }

        var source = _sfxPool.Get();
        source.clip = data.Clip;
        source.volume = data.Volume * _sfxVolume;
        source.pitch = data.Pitch;
        source.loop = data.Loop;
        _sfxFadeDurations[source] = data.Loop ? data.FadeOutDuration : 0f;
        source.Play();

        if (!data.Loop)
            StartCoroutine(ReleaseWhenDone(source, data.Duration));

        return source;
    }

    private IEnumerator ReleaseWhenDone(AudioSource source, float duration)
    {
        yield return new WaitForSeconds(duration);
        _sfxPool.Release(source);
    }

    // ══════════════════════════════════════════
    //  페이드 유틸리티
    // ══════════════════════════════════════════
    private void StartFade(AudioSource source, float duration, float targetVolume, Action onComplete = null)
    {
        if (_bgmFadeCoroutine != null) StopCoroutine(_bgmFadeCoroutine);
        _bgmFadeCoroutine = StartCoroutine(FadeRoutine(source, duration, targetVolume, onComplete));
    }

    private void StartSFXFade(AudioSource source, float duration, float targetVolume, Action onComplete = null)
    {
        if (source == null)
        {
            return;
        }

        StopTrackedSFXFade(source);
        Coroutine coroutine = StartCoroutine(FadeRoutine(source, duration, targetVolume, () =>
        {
            _sfxFadeCoroutines.Remove(source);
            onComplete?.Invoke();
        }));

        _sfxFadeCoroutines[source] = coroutine;
    }

    private void StopTrackedSFXFade(AudioSource source)
    {
        if (source == null)
        {
            return;
        }

        if (_sfxFadeCoroutines.TryGetValue(source, out Coroutine coroutine))
        {
            StopCoroutine(coroutine);
            _sfxFadeCoroutines.Remove(source);
        }
    }

    private IEnumerator FadeRoutine(AudioSource source, float duration, float targetVolume, System.Action onComplete)
    {
        float start = source.volume;
        float elapsed = 0f;
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
