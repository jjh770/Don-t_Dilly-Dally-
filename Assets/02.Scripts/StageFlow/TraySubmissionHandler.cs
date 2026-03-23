using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using DontDillyDally.Data;
using Photon.Pun;
using UnityEngine;

namespace DontDillyDally.StageFlow
{
    /// <summary>
    /// 트레이 제출의 비동기 대기, 네트워크 분기, 트레이 리셋 동기화를 담당합니다.
    /// MonoBehaviour가 아닌 일반 C# 클래스입니다.
    /// </summary>
    public class TraySubmissionHandler : IDisposable
    {
        private readonly StageFlowRpcHandler _rpc;
        private readonly Func<bool> _isGameOverCheck;

        private UniTaskCompletionSource<SubmittedTray> _traySubmissionTcs;

        /// <summary>가장 최근 트레이를 제출한 플레이어의 ActorNumber</summary>
        public int LastSubmitterActorNumber { get; private set; } = -1;

        public TraySubmissionHandler(StageFlowRpcHandler rpc, Func<bool> isGameOverCheck)
        {
            _rpc = rpc;
            _isGameOverCheck = isGameOverCheck;

            _rpc.OnTraySubmittedReceived += HandleTraySubmittedReceived;
        }

        public bool CanSubmit =>
            !_isGameOverCheck() &&
            _rpc != null &&
            _rpc.CurrentPhase.Value == EStagePhase.Playing;

        public bool IsWaitingForSubmission => _traySubmissionTcs != null;

        // ── 비동기 대기 ───────────────────────────────────────────────

        public async UniTask<SubmittedTray> WaitForSubmission(CancellationToken ct)
        {
            _traySubmissionTcs = new UniTaskCompletionSource<SubmittedTray>();

            using (ct.Register(() => _traySubmissionTcs.TrySetCanceled()))
            {
                return await _traySubmissionTcs.Task;
            }
        }

        // ── 제출 요청 (외부 → 마스터) ─────────────────────────────────

        public void OnTraySubmitted(SubmittedTray tray)
        {
            RequestSubmission(tray);
        }

        public bool RequestSubmission(SubmittedTray tray, int trayViewId = -1)
        {
            if (tray == null)
            {
                Debug.LogWarning("[StageFlow] 제출할 트레이가 없습니다.");
                return false;
            }

            if (!CanSubmit)
            {
                Debug.LogWarning("[StageFlow] 지금은 트레이를 제출할 수 없는 상태입니다.");
                return false;
            }

            if (PhotonNetwork.IsMasterClient)
            {
                return TryAccept(tray, trayViewId, PhotonNetwork.LocalPlayer.ActorNumber);
            }

            _rpc.SubmitTray(tray, trayViewId);
            return true;
        }

        // ── 마스터 측 수락 ────────────────────────────────────────────

        private void HandleTraySubmittedReceived(SubmittedTray tray, int trayViewId, int submitterActorNumber)
        {
            if (!PhotonNetwork.IsMasterClient)
            {
                return;
            }

            TryAccept(tray, trayViewId, submitterActorNumber);
        }

        private bool TryAccept(SubmittedTray tray, int trayViewId, int submitterActorNumber)
        {
            if (tray == null)
            {
                return false;
            }

            if (_traySubmissionTcs == null)
            {
                Debug.LogWarning("[StageFlow] 현재는 트레이 제출을 기다리고 있지 않습니다.");
                return false;
            }

            LastSubmitterActorNumber = submitterActorNumber;
            SyncSubmittedTrayReset(trayViewId);
            return _traySubmissionTcs.TrySetResult(tray);
        }

        private void SyncSubmittedTrayReset(int trayViewId)
        {
            if (trayViewId < 0)
            {
                return;
            }

            PhotonView trayView = PhotonView.Find(trayViewId);
            if (trayView == null || !trayView.TryGetComponent(out TrayItem trayItem))
            {
                return;
            }

            trayItem.ResetTrayDataAndSync(trayItem.IsSterilizedTray);
        }

        // ── 정리 ──────────────────────────────────────────────────────

        public void Dispose()
        {
            if (_rpc != null)
            {
                _rpc.OnTraySubmittedReceived -= HandleTraySubmittedReceived;
            }
        }
    }
}
