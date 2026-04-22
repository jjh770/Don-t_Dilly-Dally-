using Photon.Pun;
using Photon.Realtime;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;

[RequireComponent(typeof(PhotonView))]
public class VehicleEventSpawner : MonoBehaviourPunCallbacks
{
    [Serializable]
    public class VehicleDef
    {
        public GameObject prefab;
        public float spawnDelay;

        [Header("진입")]
        public SplineContainer enterSpline;
        public float enterSpeed = 10f;
        public bool enterReversed;
        [Tooltip("진행 방향 대비 차체 yaw 각도(도). 0이면 직선 주행, 25~35이면 드리프트 느낌.")]
        public float enterDriftAngle;
        [Tooltip("진행 축 기준 회전 속도(도/초). 양수면 시계 방향 배럴 롤. 360이면 1초 1회전.")]
        public float enterRollSpeed;

        [Header("대기")]
        public float waitDuration;

        [Header("퇴장")]
        [Tooltip("체크 시 exitSpline 대신 enterSpline을 역주행 (왔던 길 되돌아감). 중간 순간이동 없음.")]
        public bool exitReversesEnter;
        [Tooltip("exitReversesEnter가 false일 때 사용. 시작점을 enterSpline 끝에 스냅해야 순간이동 없음. 비우면 진입 완료 후 파괴.")]
        public SplineContainer exitSpline;
        public float exitSpeed = 10f;
        public bool exitReversed;
        [Tooltip("진행 방향 대비 차체 yaw 각도(도). 0이면 직선 주행, 25~35이면 드리프트 느낌.")]
        public float exitDriftAngle;
        [Tooltip("진행 축 기준 회전 속도(도/초). 양수면 시계 방향 배럴 롤.")]
        public float exitRollSpeed;

        [Header("사운드 - 페이드 (루프/원샷 모두 지원)")]
        [Tooltip("스폰 시 재생. Loop=true면 연속, Loop=false면 클립 1회 재생. 둘 다 페이드 인/아웃 적용. None이면 재생 안 함.")]
        public SFXKey loopSfxKey = SFXKey.None;
        [Tooltip("페이드 인 시간(초). 차량이 나타나며 볼륨 0→최대로 상승.")]
        public float sfxFadeIn = 1f;
        [Tooltip("퇴장(또는 진입 완료) 후 peak 볼륨 유지 시간(초). 여운을 남길 때 사용.")]
        public float sfxLingerAfter = 0f;
        [Tooltip("페이드 아웃 시간(초). 차량 파괴 후에도 sfxLingerAfter + sfxFadeOut 동안 사운드가 남음.")]
        public float sfxFadeOut = 1f;

        [Header("사운드 - 원샷 (선택)")]
        [Tooltip("진입 시작 시 1회 재생. SFX Entry는 Loop=false로 둘 것. None이면 재생 안 함.")]
        public SFXKey enterSfxKey = SFXKey.None;
        [Tooltip("퇴장 시작 시 1회 재생. None이면 재생 안 함.")]
        public SFXKey exitSfxKey = SFXKey.None;
    }

    [Serializable]
    public class VehicleEvent
    {
        public string name;
        public VehicleDef[] vehicles;
    }

    [Header("이벤트 목록")]
    [SerializeField] private VehicleEvent[] _events;

    [Header("스폰 주기")]
    [SerializeField] private float _startDelay = 5f;
    [SerializeField] private float _minInterval = 20f;
    [SerializeField] private float _maxInterval = 40f;

    [Header("디버그")]
    [Tooltip("켜면 자동 루프 대신 숫자키 1~9로 해당 인덱스 이벤트 수동 발동. 머지 전 OFF 필수.")]
    [SerializeField] private bool _debugKeyboardTest;

    // 현재 실행 중인 이벤트로 인한 이동/사운드 누수 방지용.
    private readonly List<AudioSource> _activeLoopSources = new List<AudioSource>();
    private Coroutine _triggerLoopCoroutine;
    private bool _keyboardBusy;

    private void Start()
    {
        TryStartTriggerLoop();
    }

    public override void OnMasterClientSwitched(Player newMasterClient)
    {
        // 이전 마스터 이탈 후 새 마스터가 루프 재개.
        TryStartTriggerLoop();
    }

    private void TryStartTriggerLoop()
    {
        if (_debugKeyboardTest)
        {
            return;
        }

        if (_triggerLoopCoroutine != null)
        {
            return;
        }

        // 오프라인(테스트씬) 또는 마스터 클라이언트만 트리거 주도.
        if (!PhotonNetwork.InRoom || PhotonNetwork.IsMasterClient)
        {
            _triggerLoopCoroutine = StartCoroutine(TriggerLoop());
        }
    }

    private void Update()
    {
        if (!_debugKeyboardTest || _events == null || _keyboardBusy)
        {
            return;
        }

        int maxKeys = Mathf.Min(_events.Length, 9);
        for (int i = 0; i < maxKeys; i++)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1 + i))
            {
                StartCoroutine(KeyboardDispatchRoutine(i));
                break;
            }
        }
    }

    private IEnumerator KeyboardDispatchRoutine(int eventIndex)
    {
        _keyboardBusy = true;
        float duration = ComputeEventDuration(eventIndex);
        DispatchEvent(eventIndex);
        if (duration > 0f)
        {
            yield return new WaitForSeconds(duration);
        }
        _keyboardBusy = false;
    }

    private IEnumerator TriggerLoop()
    {
        if (_startDelay > 0f)
        {
            yield return new WaitForSeconds(_startDelay);
        }

        while (true)
        {
            if (_events == null || _events.Length == 0)
            {
                yield break;
            }

            int eventIndex = UnityEngine.Random.Range(0, _events.Length);
            float eventDuration = ComputeEventDuration(eventIndex);
            DispatchEvent(eventIndex);

            // 인터벌보다 이벤트가 길면 이벤트 끝날 때까지만 대기 (충돌 방지).
            // 이벤트가 인터벌 안에 끝나면 인터벌 그대로 사용.
            float interval = UnityEngine.Random.Range(_minInterval, _maxInterval);
            float waitTime = Mathf.Max(interval, eventDuration);
            yield return new WaitForSeconds(waitTime);
        }
    }

    private float ComputeEventDuration(int eventIndex)
    {
        if (_events == null || eventIndex < 0 || eventIndex >= _events.Length)
        {
            return 0f;
        }

        VehicleEvent evt = _events[eventIndex];
        if (evt == null || evt.vehicles == null)
        {
            return 0f;
        }

        float maxDuration = 0f;
        foreach (VehicleDef def in evt.vehicles)
        {
            if (def == null || def.enterSpline == null)
            {
                continue;
            }

            float duration = def.spawnDelay;
            duration += def.enterSpline.CalculateLength() / Mathf.Max(def.enterSpeed, 0.001f);
            duration += Mathf.Max(0f, def.waitDuration);

            if (def.exitReversesEnter)
            {
                duration += def.enterSpline.CalculateLength() / Mathf.Max(def.exitSpeed, 0.001f);
            }
            else if (def.exitSpline != null)
            {
                duration += def.exitSpline.CalculateLength() / Mathf.Max(def.exitSpeed, 0.001f);
            }

            maxDuration = Mathf.Max(maxDuration, duration);
        }

        return maxDuration;
    }

    private void DispatchEvent(int eventIndex)
    {
        if (_events == null || eventIndex < 0 || eventIndex >= _events.Length)
        {
            return;
        }

        if (PhotonNetwork.InRoom)
        {
            photonView.RPC(nameof(RPC_RunEvent), RpcTarget.All, eventIndex);
        }
        else
        {
            // 오프라인 테스트: 로컬에서만 실행.
            RPC_RunEvent(eventIndex);
        }
    }

    [PunRPC]
    private void RPC_RunEvent(int eventIndex)
    {
        if (_events == null || eventIndex < 0 || eventIndex >= _events.Length)
        {
            return;
        }

        VehicleEvent evt = _events[eventIndex];
        if (evt == null || evt.vehicles == null)
        {
            return;
        }

        foreach (VehicleDef def in evt.vehicles)
        {
            if (def == null || def.prefab == null || def.enterSpline == null)
            {
                continue;
            }

            StartCoroutine(RunVehicle(def));
        }
    }

    private IEnumerator RunVehicle(VehicleDef def)
    {
        if (def.spawnDelay > 0f)
        {
            yield return new WaitForSeconds(def.spawnDelay);
        }

        Vector3 startPosition = (Vector3)def.enterSpline.EvaluatePosition(0f);
        GameObject spawned = Instantiate(def.prefab, startPosition, Quaternion.identity, transform);

        PlayOneShot(def.enterSfxKey);

        AudioSource loopSource = StartLoopSfx(def, out Coroutine fadeInCoroutine);

        bool hasExit = def.exitReversesEnter || def.exitSpline != null;

        // 퇴장 없으면 진입 끝난 시점에서 sfxLingerAfter 대기 후 페이드아웃 예약.
        // 사운드 총 수명 = 진입 시간 + sfxLingerAfter + sfxFadeOut.
        if (!hasExit && loopSource != null && (def.sfxFadeOut > 0f || def.sfxLingerAfter > 0f))
        {
            float enterLength = def.enterSpline.CalculateLength();
            float enterDuration = enterLength / Mathf.Max(def.enterSpeed, 0.001f);
            float waitBeforeFade = enterDuration + Mathf.Max(0f, def.sfxLingerAfter);
            StartCoroutine(ScheduledFadeOutAndRelease(loopSource, fadeInCoroutine, waitBeforeFade, def.sfxFadeOut));
            loopSource = null;
            fadeInCoroutine = null;
        }

        yield return MoveAlong(spawned, def.enterSpline, def.enterSpeed, def.enterReversed, false, def.enterDriftAngle, def.enterRollSpeed);

        if (spawned == null)
        {
            StopSfxImmediate(loopSource, fadeInCoroutine);
            yield break;
        }

        if (def.waitDuration > 0f)
        {
            yield return new WaitForSeconds(def.waitDuration);
        }

        if (hasExit && spawned != null)
        {
            PlayOneShot(def.exitSfxKey);

            // 퇴장 시작 시점에 페이드아웃 시작(퇴장 이동과 병렬).
            StartCoroutine(FadeOutAndRelease(loopSource, fadeInCoroutine, def.sfxFadeOut));
            loopSource = null;
            fadeInCoroutine = null;

            if (def.exitReversesEnter)
            {
                yield return MoveAlong(spawned, def.enterSpline, def.exitSpeed, def.exitReversed, true, def.exitDriftAngle, def.exitRollSpeed);
            }
            else
            {
                WarnIfSplineGap(def);
                yield return MoveAlong(spawned, def.exitSpline, def.exitSpeed, def.exitReversed, false, def.exitDriftAngle, def.exitRollSpeed);
            }
        }

        // sfxFadeOut=0 이고 퇴장도 없는 경우 여기서 즉시 중단.
        StopSfxImmediate(loopSource, fadeInCoroutine);

        if (spawned != null)
        {
            Destroy(spawned);
        }
    }

    private void PlayOneShot(SFXKey key)
    {
        if (key == SFXKey.None || SoundManager.Instance == null)
        {
            return;
        }

        SoundManager.Instance.Play(key, SoundType.Local);
    }

    private AudioSource StartLoopSfx(VehicleDef def, out Coroutine fadeInCoroutine)
    {
        fadeInCoroutine = null;

        if (def.loopSfxKey == SFXKey.None || SoundManager.Instance == null)
        {
            return null;
        }

        // PlayLocalWithHandle은 Entry의 Loop 여부와 무관하게 핸들 반환.
        // Loop=true면 source.loop=true, Loop=false면 source.loop=false로 설정되고 풀이 클립 종료 시 자동 반환.
        AudioSource source = SoundManager.Instance.PlayLocalWithHandle(def.loopSfxKey);
        if (source == null)
        {
            return null;
        }

        if (source.loop)
        {
            // Loop=true 소스만 누수 위험. 스폰너 파괴 시 정리 대상으로 등록.
            _activeLoopSources.Add(source);
        }

        float targetVolume = source.volume;
        if (def.sfxFadeIn > 0f)
        {
            source.volume = 0f;
            fadeInCoroutine = StartCoroutine(FadeVolumeTo(source, targetVolume, def.sfxFadeIn));
        }

        return source;
    }

    private IEnumerator FadeVolumeTo(AudioSource source, float target, float duration)
    {
        if (source == null)
        {
            yield break;
        }

        if (duration <= 0f)
        {
            source.volume = target;
            yield break;
        }

        float start = source.volume;
        float elapsed = 0f;
        while (elapsed < duration && source != null && source.gameObject.activeSelf)
        {
            elapsed += Time.deltaTime;
            source.volume = Mathf.Lerp(start, target, elapsed / duration);
            yield return null;
        }

        if (source != null && source.gameObject.activeSelf)
        {
            source.volume = target;
        }
    }

    private IEnumerator FadeOutAndRelease(AudioSource source, Coroutine fadeInToCancel, float duration)
    {
        if (fadeInToCancel != null)
        {
            StopCoroutine(fadeInToCancel);
        }

        if (source == null)
        {
            yield break;
        }

        // Loop=false면 SoundManager.ReleaseWhenDone이 자동 반환하므로 여기서 StopSFX 금지(이중 Release 방지).
        bool wasLoop = source.loop;

        yield return FadeVolumeTo(source, 0f, duration);

        if (source != null && wasLoop && SoundManager.Instance != null)
        {
            _activeLoopSources.Remove(source);
            SoundManager.Instance.StopSFX(source, fade: false);
        }
    }

    private IEnumerator ScheduledFadeOutAndRelease(AudioSource source, Coroutine fadeInToCancel, float waitSeconds, float fadeDuration)
    {
        if (waitSeconds > 0f)
        {
            yield return new WaitForSeconds(waitSeconds);
        }

        yield return FadeOutAndRelease(source, fadeInToCancel, fadeDuration);
    }

    private void StopSfxImmediate(AudioSource source, Coroutine fadeInToCancel)
    {
        if (fadeInToCancel != null)
        {
            StopCoroutine(fadeInToCancel);
        }

        if (source == null || SoundManager.Instance == null)
        {
            return;
        }

        if (source.loop)
        {
            // Loop=true만 수동 Release. Loop=false는 SoundManager가 자연 종료 시 반환.
            _activeLoopSources.Remove(source);
            SoundManager.Instance.StopSFX(source, fade: false);
        }
        else
        {
            // Loop=false면 즉시 음소거만.
            source.volume = 0f;
        }
    }

    private void OnDestroy()
    {
        // 스테이지 전환/씬 언로드 시 진행 중이던 Loop SFX 소스 반환 (풀 누수 방지).
        if (SoundManager.Instance == null)
        {
            return;
        }

        foreach (AudioSource source in _activeLoopSources)
        {
            if (source != null)
            {
                SoundManager.Instance.StopSFX(source, fade: false);
            }
        }
        _activeLoopSources.Clear();
    }

    private void WarnIfSplineGap(VehicleDef def)
    {
        const float toleranceSqr = 0.25f;

        Vector3 enterEnd = (Vector3)def.enterSpline.EvaluatePosition(1f);
        Vector3 exitStart = (Vector3)def.exitSpline.EvaluatePosition(0f);

        if ((enterEnd - exitStart).sqrMagnitude > toleranceSqr)
        {
            Debug.LogWarning(
                $"[VehicleEventSpawner] enterSpline 끝({enterEnd})과 exitSpline 시작({exitStart}) 사이에 간격이 있어 " +
                $"차량이 순간이동합니다. 에디터에서 knot을 스냅하거나 exitReversesEnter 옵션을 사용하세요.");
        }
    }

    private IEnumerator MoveAlong(GameObject target, SplineContainer spline, float speed, bool reverseFacing, bool reverseDirection, float driftAngle, float rollSpeed)
    {
        if (target == null)
        {
            yield break;
        }

        bool finished = false;
        VehiclePathMover mover = target.AddComponent<VehiclePathMover>();
        mover.Initialize(spline, speed, reverseFacing, reverseDirection, driftAngle, rollSpeed, () => finished = true);

        while (!finished && target != null)
        {
            yield return null;
        }

        if (target != null)
        {
            Destroy(mover);
        }
    }
}
