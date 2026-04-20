using DontDillyDally.Data;
using UnityEngine;

namespace DontDillyDally.StageFlow
{
    // 긴급 이벤트 진행/결과에 따른 환자 위 VFX를 관리한다.
    // VFX 인스턴스를 자식으로 미리 배치해두고 런타임에는 Play/Stop만 호출.
    // 위치 조정은 에디터에서 자식 Transform을 직접 맞추면 된다.
    // Diagnosis 기계 작동 여부는 Update에서 StageFlowManager 상태를 폴링한다 (게이지 UI와 동일 방식).
    public sealed class EmergencyVFXController : MonoBehaviour
    {
        [Header("VFX 인스턴스 (자식으로 미리 배치, 위치는 직접 조정)")]
        [Tooltip("Tray 긴급 이벤트(재료 필요) 시 재생")]
        [SerializeField] private ParticleSystem _bloodVfx;
        [Tooltip("Diagnosis 긴급 이벤트(기계 필요) 시 재생")]
        [SerializeField] private ParticleSystem _starVfx;
        [Tooltip("치료 이펙트: Tray 성공 시 원샷, Diagnosis 기계 작동 중 루프")]
        [SerializeField] private ParticleSystem _healVfx;

        // 현재 진행 중인 긴급 이벤트 종류.
        private EmergencyEventKind _currentKind;
        private bool _diagnosisOperatingDetected;

        // StageFlowRpcHandler 구독용.
        private StageFlowRpcHandler _rpc;

        private void Start()
        {
            TryBind();

            // 시작 시 모든 VFX를 정지 상태로 초기화 (에디터에서 자동 재생 설정되어도 게임 시작 시 리셋).
            FxHelper.Clear(_bloodVfx);
            FxHelper.Clear(_starVfx);
            FxHelper.Clear(_healVfx);
        }

        private void Update()
        {
            // late-bind: StageFlowRpcHandler는 스테이지 프리팹에 있어 늦게 생성될 수 있음.
            if (_rpc == null)
            {
                TryBind();
            }

            // Diagnosis 기계 작동 게이지가 뜨는 시점을 감지하여 Star → Heal(loop)로 전환.
            if (_currentKind == EmergencyEventKind.Diagnosis
                && !_diagnosisOperatingDetected
                && StageFlowManager.Instance != null
                && StageFlowManager.Instance.IsEmergencyDiagnosisOperating)
            {
                _diagnosisOperatingDetected = true;
                FxHelper.Stop(_starVfx);
                PlayHealLoop();
            }
        }

        private void OnDestroy()
        {
            Unbind();
        }

        // ── 이벤트 바인딩 ───────────────────────────────────────────

        private void TryBind()
        {
            if (StageFlowManager.Instance == null)
            {
                return;
            }

            _rpc = FindFirstObjectByType<StageFlowRpcHandler>();
            if (_rpc == null)
            {
                return;
            }

            _rpc.OnEmergencyStartedReceived += HandleEmergencyStarted;
            _rpc.OnEmergencyEndedReceived += HandleEmergencyEnded;
        }

        private void Unbind()
        {
            if (_rpc != null)
            {
                _rpc.OnEmergencyStartedReceived -= HandleEmergencyStarted;
                _rpc.OnEmergencyEndedReceived -= HandleEmergencyEnded;
                _rpc = null;
            }
        }

        // ── RPC 이벤트 핸들러 ───────────────────────────────────────

        private void HandleEmergencyStarted(
            EmergencyEventKind kind,
            EmergencyTriggerSource triggerSource,
            CraftedMaterialType trayTarget,
            DiagnosisScanType diagnosisTarget)
        {
            _currentKind = kind;
            _diagnosisOperatingDetected = false;

            // 이전 이벤트 잔여 이펙트 초기화.
            FxHelper.Clear(_healVfx);

            if (kind == EmergencyEventKind.Tray)
            {
                FxHelper.Clear(_starVfx);
                PlayBloodLoop();
            }
            else if (kind == EmergencyEventKind.Diagnosis)
            {
                FxHelper.Clear(_bloodVfx);
                PlayStarLoop();
            }
        }

        private void HandleEmergencyEnded(bool isSuccess)
        {
            EmergencyEventKind endedKind = _currentKind;
            _currentKind = EmergencyEventKind.None;
            _diagnosisOperatingDetected = false;

            // 시작용 VFX(Blood/Star)는 항상 중단. 자연스럽게 페이드되도록 Stop.
            FxHelper.Stop(_bloodVfx);
            FxHelper.Stop(_starVfx);

            if (isSuccess && endedKind == EmergencyEventKind.Tray)
            {
                // Tray 성공: 치료 원샷 연출.
                FxHelper.Clear(_healVfx);
                PlayHealOneShot();
            }
            else if (endedKind == EmergencyEventKind.Diagnosis)
            {
                // Diagnosis는 기계 작동 중 Heal이 이미 재생되고 있었음.
                // 성공/실패 모두 자연 페이드로 마무리.
                FxHelper.Stop(_healVfx);
            }
            else
            {
                // Tray 실패 또는 kind 없음: Heal 없음.
                FxHelper.Clear(_healVfx);
            }
        }

        // ── VFX 제어 ────────────────────────────────────────────────

        private void PlayBloodLoop()
        {
            if (_bloodVfx == null)
            {
                return;
            }

            ForceLoopAll(_bloodVfx, true);
            FxHelper.Clear(_bloodVfx);
            FxHelper.Play(_bloodVfx);
        }

        private void PlayStarLoop()
        {
            if (_starVfx == null)
            {
                return;
            }

            ForceLoopAll(_starVfx, true);
            FxHelper.Clear(_starVfx);
            FxHelper.Play(_starVfx);
        }

        private void PlayHealLoop()
        {
            if (_healVfx == null)
            {
                return;
            }

            ForceLoopAll(_healVfx, true);
            FxHelper.Clear(_healVfx);
            FxHelper.Play(_healVfx);
        }

        private void PlayHealOneShot()
        {
            if (_healVfx == null)
            {
                return;
            }

            ForceLoopAll(_healVfx, false);
            FxHelper.Clear(_healVfx);
            FxHelper.Play(_healVfx);
        }

        // 자식 파티클까지 모두 loop 상태를 통일한다.
        // 프리팹이 여러 파티클 시스템(연기, 별, 스파클 등)으로 구성되었을 때 사용.
        private static void ForceLoopAll(ParticleSystem root, bool loop)
        {
            if (root == null)
            {
                return;
            }

            foreach (ParticleSystem ps in root.GetComponentsInChildren<ParticleSystem>(true))
            {
                var main = ps.main;
                main.loop = loop;
            }
        }
    }
}
