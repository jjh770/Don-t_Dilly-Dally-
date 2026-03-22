using System;
using Photon.Pun;
using UniRx;
using UnityEngine;

namespace DontDillyDally.StageFlow
{
    /// <summary>
    /// 스테이지 흐름 상태를 소유하고 네트워크 동기화(RPC)를 담당합니다.
    /// 같은 GameObject에 StageFlowManager와 함께 부착합니다.
    /// </summary>
    [RequireComponent(typeof(PhotonView))]
    public class StageFlowRpcHandler : MonoBehaviourPun
    {
        // ── ReactiveProperty (외부 구독용) ──────────────────────────
        private readonly ReactiveProperty<EStagePhase> _currentPhase = new(EStagePhase.None);
        private readonly ReactiveProperty<float> _patientHealth = new(100f);
        private readonly ReactiveProperty<float> _stageTimer = new(0f);
        private readonly ReactiveProperty<int> _currentPatientIndex = new(0);
        private readonly ReactiveProperty<int> _currentRecipeIndex = new(0);
        private readonly ReactiveProperty<int> _surgeonActorNumber = new(-1);

        public IReadOnlyReactiveProperty<EStagePhase> CurrentPhase => _currentPhase;
        public IReadOnlyReactiveProperty<float> PatientHealth => _patientHealth;
        public IReadOnlyReactiveProperty<float> StageTimer => _stageTimer;
        public IReadOnlyReactiveProperty<int> CurrentPatientIndex => _currentPatientIndex;
        public IReadOnlyReactiveProperty<int> CurrentRecipeIndex => _currentRecipeIndex;
        public IReadOnlyReactiveProperty<int> SurgeonActorNumber => _surgeonActorNumber;

        // ── 이벤트 (StageFlowManager가 구독) ────────────────────────
        public event Action<EGameOverReason> OnGameOverReceived;

        private bool _isGameOver;

        // ================================================================
        //  마스터 → 클라이언트 동기화 메서드
        // ================================================================

        public void SetPhase(EStagePhase phase)
        {
            Debug.Log($"[StageFlow] 페이즈 변경: {_currentPhase.Value} → {phase}");
            _currentPhase.Value = phase;
            if (PhotonNetwork.IsMasterClient)
            {
                photonView.RPC(nameof(RPC_SetPhase), RpcTarget.Others, (int)phase);
            }
        }

        public void SetHealth(float health)
        {
            _patientHealth.Value = health;
            if (PhotonNetwork.IsMasterClient)
            {
                photonView.RPC(nameof(RPC_SyncHealth), RpcTarget.Others, health);
            }
        }

        public void SetTimer(float time)
        {
            _stageTimer.Value = time;
            if (PhotonNetwork.IsMasterClient)
            {
                photonView.RPC(nameof(RPC_SyncTimer), RpcTarget.Others, time);
            }
        }

        public void SetPatientIndex(int index)
        {
            _currentPatientIndex.Value = index;
            if (PhotonNetwork.IsMasterClient)
            {
                photonView.RPC(nameof(RPC_SyncPatientIndex), RpcTarget.Others, index);
            }
        }

        public void SetRecipeIndex(int index)
        {
            _currentRecipeIndex.Value = index;
        }

        public void SetSurgeon(int actorNumber)
        {
            _surgeonActorNumber.Value = actorNumber;
            if (PhotonNetwork.IsMasterClient)
            {
                photonView.RPC(nameof(RPC_SetSurgeon), RpcTarget.Others, actorNumber);
            }
        }

        public void BroadcastStageData(string json)
        {
            if (PhotonNetwork.IsMasterClient)
            {
                photonView.RPC(nameof(RPC_ReceiveStageData), RpcTarget.Others, json);
            }
        }

        public void BroadcastGameOver(EGameOverReason reason)
        {
            if (PhotonNetwork.IsMasterClient)
            {
                photonView.RPC(nameof(RPC_GameOver), RpcTarget.Others, (int)reason);
            }
        }

        public void BroadcastEmergency()
        {
            if (PhotonNetwork.IsMasterClient)
            {
                photonView.RPC(nameof(RPC_TriggerEmergency), RpcTarget.Others);
            }
        }

        // ================================================================
        //  RPC 수신 (클라이언트 측)
        // ================================================================

        [PunRPC]
        private void RPC_SetPhase(int phase)
        {
            Debug.Log($"[StageFlow] [RPC] 페이즈 수신: {(EStagePhase)phase}");
            _currentPhase.Value = (EStagePhase)phase;
        }

        [PunRPC]
        private void RPC_SyncHealth(float health)
        {
            _patientHealth.Value = health;
        }

        [PunRPC]
        private void RPC_SyncTimer(float time)
        {
            _stageTimer.Value = time;
        }

        [PunRPC]
        private void RPC_SyncPatientIndex(int index)
        {
            _currentPatientIndex.Value = index;
        }

        [PunRPC]
        private void RPC_SetSurgeon(int actorNumber)
        {
            _surgeonActorNumber.Value = actorNumber;
        }

        [PunRPC]
        private void RPC_ReceiveStageData(string json)
        {
            var stageData = JsonUtility.FromJson<StageData>(json);
            Debug.Log($"[StageFlow] [RPC] 스테이지 데이터 수신: 환자 {stageData.Patients.Count}명");
            OnStageDataReceived?.Invoke(stageData);
        }

        public event Action<StageData> OnStageDataReceived;

        [PunRPC]
        private void RPC_GameOver(int reason)
        {
            if (_isGameOver) return;
            _isGameOver = true;

            Debug.Log($"[StageFlow] [RPC] 게임 오버 수신: {(EGameOverReason)reason}");
            _currentPhase.Value = EStagePhase.GameOver;
            OnGameOverReceived?.Invoke((EGameOverReason)reason);
        }

        [PunRPC]
        private void RPC_TriggerEmergency()
        {
            EventManager.Instance?.Publish(EventType.PatientCritical, "긴급 처치가 필요합니다!");
        }

        // ================================================================
        //  상태 리셋
        // ================================================================

        public void ResetState()
        {
            _isGameOver = false;
        }

        // ================================================================
        //  정리
        // ================================================================

        private void OnDestroy()
        {
            _currentPhase.Dispose();
            _patientHealth.Dispose();
            _stageTimer.Dispose();
            _currentPatientIndex.Dispose();
            _currentRecipeIndex.Dispose();
            _surgeonActorNumber.Dispose();
        }
    }
}
