using DontDillyDally.StageFlow;
using UnityEngine;

namespace DontDillyDally.MiniGame
{
    /// <summary>
    /// 미니게임 진행/결과에 따른 환자 위 VFX를 관리한다.
    /// StageFlowRpcHandler의 VFX 이벤트를 구독하여 모든 클라이언트에서 동일하게 재생.
    /// 환자 Transform을 런타임에 캐싱하므로 맵 프리팹 수정 불필요.
    /// MiniGameManager 프리팹에 배치.
    /// </summary>
    public sealed class MiniGameVFXController : MonoBehaviour
    {
        [Header("VFX 프리팹 (환자 위에 생성)")]
        [Tooltip("수술 중 루프 이펙트 프리팹")]
        [SerializeField] private ParticleSystem _surgeryLoopPrefab;
        [Tooltip("미니게임 성공 이펙트 프리팹")]
        [SerializeField] private ParticleSystem _successPrefab;
        [Tooltip("미니게임 실패 이펙트 프리팹 (출혈 등)")]
        [SerializeField] private ParticleSystem _failPrefab;

        [Header("위치 오프셋")]
        [Tooltip("환자 위치 기준 이펙트 오프셋")]
        [SerializeField] private Vector3 _vfxOffset = new(0f, 1.0f, 0f);

        // 런타임 인스턴스 (한 번만 생성, 재사용)
        private ParticleSystem _surgeryLoopInstance;
        private ParticleSystem _successInstance;
        private ParticleSystem _failInstance;

        // 캐싱된 환자 Transform
        private Transform _cachedPatientTransform;

        // StageFlowRpcHandler 구독용
        private StageFlowRpcHandler _rpc;

        private void Start()
        {
            TryBind();
        }

        private void Update()
        {
            // late-bind: StageFlowRpcHandler는 스테이지 프리팹에 있어 늦게 생성될 수 있음
            if (_rpc == null)
            {
                TryBind();
            }
        }

        private void OnDestroy()
        {
            Unbind();
            DestroyInstance(ref _surgeryLoopInstance);
            DestroyInstance(ref _successInstance);
            DestroyInstance(ref _failInstance);
        }

        // ── 이벤트 바인딩 ───────────────────────────────────────────

        private void TryBind()
        {
            if (StageFlowManager.Instance == null)
            {
                return;
            }

            // StageFlowManager에서 RpcHandler를 가져올 수 없으므로 직접 탐색
            _rpc = FindObjectOfType<StageFlowRpcHandler>();
            if (_rpc == null)
            {
                return;
            }

            _rpc.OnMiniGameVFXStarted += HandleVFXStarted;
            _rpc.OnMiniGameVFXResult += HandleVFXResult;
        }

        private void Unbind()
        {
            if (_rpc != null)
            {
                _rpc.OnMiniGameVFXStarted -= HandleVFXStarted;
                _rpc.OnMiniGameVFXResult -= HandleVFXResult;
                _rpc = null;
            }
        }

        // ── RPC 이벤트 핸들러 ───────────────────────────────────────

        private void HandleVFXStarted()
        {
            PlaySurgeryLoop();
        }

        private void HandleVFXResult(bool isSuccess)
        {
            StopSurgeryLoop();

            if (isSuccess)
            {
                PlaySuccess();
            }
            else
            {
                PlayFail();
            }
        }

        // ── VFX 제어 ────────────────────────────────────────────────

        public void PlaySurgeryLoop()
        {
            EnsureInstances();
            FxHelper.Play(_surgeryLoopInstance);
        }

        public void StopSurgeryLoop()
        {
            FxHelper.Clear(_surgeryLoopInstance);
        }

        public void PlaySuccess()
        {
            EnsureInstances();
            FxHelper.Play(_successInstance);
        }

        public void PlayFail()
        {
            EnsureInstances();
            FxHelper.Play(_failInstance);
        }

        /// <summary>
        /// 모든 이펙트를 즉시 정지. 환자 전환 시 호출.
        /// </summary>
        public void ClearAll()
        {
            FxHelper.Clear(_surgeryLoopInstance);
            FxHelper.Clear(_successInstance);
            FxHelper.Clear(_failInstance);

            // 다음 환자에서 새로 ResolvePatientTransform()하도록 캐시 초기화.
            _cachedPatientTransform = null;
        }

        // ── 인스턴스 관리 ───────────────────────────────────────────

        /// <summary>
        /// 환자 Transform을 찾고, VFX 인스턴스가 없으면 생성한다.
        /// 미니게임 시작/결과 시점에만 호출되므로 성능 부담 없음.
        /// </summary>
        private void EnsureInstances()
        {
            Transform patient = ResolvePatientTransform();
            if (patient == null)
            {
                return;
            }

            // 환자가 바뀌었으면 위치 갱신
            if (_cachedPatientTransform != patient)
            {
                _cachedPatientTransform = patient;
                UpdateInstancePositions();
            }

            Vector3 spawnPos = patient.position + _vfxOffset;

            if (_surgeryLoopInstance == null && _surgeryLoopPrefab != null)
            {
                // 프리팹의 원본 회전값을 그대로 유지 (에디터에서 설정한 X=-90 등)
                _surgeryLoopInstance = Instantiate(_surgeryLoopPrefab, spawnPos, _surgeryLoopPrefab.transform.rotation);

                // 프리팹이 루프가 아니어도 미니게임 종료까지 지속되도록 강제 루프 설정
                // 루트뿐 아니라 자식 파티클(별, 스파클 등)도 전부 루프로 설정
                ForceLoopAll(_surgeryLoopInstance);

                _surgeryLoopInstance.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }

            if (_successInstance == null && _successPrefab != null)
            {
                _successInstance = Instantiate(_successPrefab, spawnPos, _successPrefab.transform.rotation);
                _successInstance.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }

            if (_failInstance == null && _failPrefab != null)
            {
                _failInstance = Instantiate(_failPrefab, spawnPos, _failPrefab.transform.rotation);
                _failInstance.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
        }

        private void UpdateInstancePositions()
        {
            if (_cachedPatientTransform == null)
            {
                return;
            }

            Vector3 pos = _cachedPatientTransform.position + _vfxOffset;
            SetPosition(_surgeryLoopInstance, pos);
            SetPosition(_successInstance, pos);
            SetPosition(_failInstance, pos);
        }

        private Transform ResolvePatientTransform()
        {
            if (_cachedPatientTransform != null)
            {
                return _cachedPatientTransform;
            }

            PatientInteractable patient = FindObjectOfType<PatientInteractable>();
            if (patient != null)
            {
                _cachedPatientTransform = patient.transform;
            }

            return _cachedPatientTransform;
        }

        private static void SetPosition(ParticleSystem instance, Vector3 pos)
        {
            if (instance != null)
            {
                instance.transform.position = pos;
            }
        }

        /// <summary>
        /// 자식 파티클까지 모두 loop=true로 설정한다.
        /// 프리팹이 여러 파티클 시스템(연기, 별, 스파클 등)으로 구성되었을 때 사용.
        /// </summary>
        private static void ForceLoopAll(ParticleSystem root)
        {
            if (root == null)
            {
                return;
            }

            foreach (ParticleSystem ps in root.GetComponentsInChildren<ParticleSystem>(true))
            {
                var main = ps.main;
                main.loop = true;
            }
        }

        private static void DestroyInstance(ref ParticleSystem instance)
        {
            if (instance != null)
            {
                Destroy(instance.gameObject);
                instance = null;
            }
        }
    }
}
