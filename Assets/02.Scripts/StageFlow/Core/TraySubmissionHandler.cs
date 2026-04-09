using Cysharp.Threading.Tasks;
using DontDillyDally.Data;
using Photon.Pun;
using Photon.Realtime;
using System;
using System.Threading;
using UnityEngine;

namespace DontDillyDally.StageFlow
{
    /// <summary>
    /// 트레이 제출의 비동기 대기, 네트워크 분기, 트레이 리셋 동기화를 담당합니다.
    /// MonoBehaviour가 아닌 일반 C# 클래스입니다.
    /// </summary>
    public class TraySubmissionHandler : IDisposable
    {
        private const int SubmissionResponseTimeoutMs = 5000;

        private readonly StageFlowRpcHandler _rpc;
        private readonly Func<bool> _isGameOverCheck;

        private UniTaskCompletionSource<SubmittedTray> _traySubmissionTcs;
        private int _pendingSubmissionTrayViewId = -1;
        private Action _onPendingSubmissionAccepted;
        private Action _onPendingSubmissionRejected;
        private CancellationTokenSource _pendingSubmissionTimeoutCts;

        /// <summary>가장 최근 트레이를 제출한 플레이어의 ActorNumber</summary>
        public int LastSubmitterActorNumber { get; private set; } = -1;

        public TraySubmissionHandler(StageFlowRpcHandler rpc, Func<bool> isGameOverCheck)
        {
            _rpc = rpc;
            _isGameOverCheck = isGameOverCheck;

            _rpc.OnTraySubmissionRequestedReceived += HandleTraySubmissionRequestedReceived;
            _rpc.OnTraySubmissionResponseReceived += HandleTraySubmissionResponseReceived;
        }

        public bool CanSubmit =>
            !_isGameOverCheck() &&
            _rpc != null &&
            _rpc.CurrentPhase.Value == EStagePhase.Playing;

        public bool IsWaitingForSubmission => _traySubmissionTcs != null;

        // ── 비동기 대기 ───────────────────────────────────────────────

        public async UniTask<SubmittedTray> WaitForSubmission(CancellationToken ct)
        {
            _traySubmissionTcs?.TrySetCanceled();
            _traySubmissionTcs = new UniTaskCompletionSource<SubmittedTray>();

            using (ct.Register(() => _traySubmissionTcs.TrySetCanceled()))
            {
                return await _traySubmissionTcs.Task;
            }
        }

        // ── 제출 요청 (외부 → 마스터) ─────────────────────────────────

        public void OnTraySubmitted(TrayItem trayItem)
        {
            RequestSubmission(trayItem, null, null);
        }

        public bool RequestSubmission(TrayItem trayItem, Action onAccepted, Action onRejected = null)
        {
            if (trayItem == null)
            {
                Debug.LogWarning("[StageFlow] 제출할 트레이가 없습니다.");
                return false;
            }

            if (!CanSubmit)
            {
                Debug.LogWarning("[StageFlow] 지금은 트레이를 제출할 수 없는 상태입니다.");
                return false;
            }

            int trayViewId = trayItem.ViewId;
            if (trayViewId < 0)
            {
                Debug.LogWarning("[StageFlow] 제출할 트레이 ViewId가 올바르지 않습니다.");
                return false;
            }

            if (_pendingSubmissionTrayViewId >= 0)
            {
                Debug.LogWarning("[StageFlow] 이미 처리 중인 트레이 제출 요청이 있습니다.");
                return false;
            }

            if (PhotonNetwork.IsMasterClient)
            {
                bool accepted = TryAcceptAuthoritative(
                    trayViewId,
                    PhotonNetwork.LocalPlayer?.ActorNumber ?? -1,
                    PhotonNetwork.LocalPlayer);

                if (accepted)
                {
                    onAccepted?.Invoke();
                }
                else
                {
                    onRejected?.Invoke();
                }

                return accepted;
            }

            _pendingSubmissionTrayViewId = trayViewId;
            _onPendingSubmissionAccepted = onAccepted;
            _onPendingSubmissionRejected = onRejected;
            ResetPendingSubmissionTimeout(trayViewId).Forget();
            _rpc.SubmitTrayRequest(trayViewId);
            return true;
        }

        // ── 마스터 측 수락 ────────────────────────────────────────────

        private void HandleTraySubmissionRequestedReceived(int trayViewId, int submitterActorNumber)
        {
            if (!PhotonNetwork.IsMasterClient)
            {
                return;
            }

            Player submitterPlayer = PhotonNetwork.CurrentRoom?.GetPlayer(submitterActorNumber);
            TryAcceptAuthoritative(trayViewId, submitterActorNumber, submitterPlayer);
        }

        private bool TryAcceptAuthoritative(int trayViewId, int submitterActorNumber, Player submitterPlayer)
        {
            bool accepted = false;

            // 제출 판정은 제출자가 보내온 JSON이 아니라,
            // 마스터가 실제 트레이 오브젝트에서 읽은 최신 스냅샷을 기준으로 합니다.
            if (!TryResolveAuthoritativeTraySnapshot(trayViewId, out SubmittedTray tray))
            {
                _rpc?.SendTraySubmissionResponse(submitterPlayer, trayViewId, false);
                return false;
            }

            if (_traySubmissionTcs == null)
            {
                Debug.LogWarning("[StageFlow] 현재는 트레이 제출을 기다리고 있지 않습니다.");
                _rpc?.SendTraySubmissionResponse(submitterPlayer, trayViewId, false);
                return false;
            }

            LastSubmitterActorNumber = submitterActorNumber;
            accepted = _traySubmissionTcs.TrySetResult(tray);
            _rpc?.SendTraySubmissionResponse(submitterPlayer, trayViewId, accepted);
            return accepted;
        }

        private static bool TryResolveAuthoritativeTraySnapshot(int trayViewId, out SubmittedTray tray)
        {
            tray = null;

            PhotonView trayView = PhotonView.Find(trayViewId);
            if (trayView == null || !trayView.TryGetComponent(out TrayItem trayItem))
            {
                Debug.LogWarning($"[StageFlow] 제출된 트레이를 찾을 수 없습니다. ViewId={trayViewId}");
                return false;
            }

            tray = trayItem.GetTraySnapshot();
            if (tray == null)
            {
                Debug.LogWarning($"[StageFlow] 트레이 스냅샷 생성에 실패했습니다. ViewId={trayViewId}");
                return false;
            }

            return true;
        }

        private void HandleTraySubmissionResponseReceived(int trayViewId, bool accepted)
        {
            if (_pendingSubmissionTrayViewId != trayViewId)
            {
                return;
            }

            Action callback = accepted
                ? _onPendingSubmissionAccepted
                : _onPendingSubmissionRejected;

            ClearPendingSubmission();
            callback?.Invoke();
        }

        private void ClearPendingSubmission()
        {
            _pendingSubmissionTimeoutCts?.Cancel();
            _pendingSubmissionTimeoutCts?.Dispose();
            _pendingSubmissionTimeoutCts = null;
            _pendingSubmissionTrayViewId = -1;
            _onPendingSubmissionAccepted = null;
            _onPendingSubmissionRejected = null;
        }

        private async UniTaskVoid ResetPendingSubmissionTimeout(int trayViewId)
        {
            _pendingSubmissionTimeoutCts?.Cancel();
            _pendingSubmissionTimeoutCts?.Dispose();
            _pendingSubmissionTimeoutCts = new CancellationTokenSource();
            CancellationToken token = _pendingSubmissionTimeoutCts.Token;

            try
            {
                await UniTask.Delay(SubmissionResponseTimeoutMs, cancellationToken: token);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            if (_pendingSubmissionTrayViewId != trayViewId)
            {
                return;
            }

            Debug.LogWarning($"[StageFlow] 트레이 제출 응답이 제한 시간 내에 도착하지 않았습니다. ViewId={trayViewId}");
            Action rejectedCallback = _onPendingSubmissionRejected;
            ClearPendingSubmission();
            rejectedCallback?.Invoke();
        }

        // ── 정리 ──────────────────────────────────────────────────────

        public void Dispose()
        {
            if (_rpc != null)
            {
                _rpc.OnTraySubmissionRequestedReceived -= HandleTraySubmissionRequestedReceived;
                _rpc.OnTraySubmissionResponseReceived -= HandleTraySubmissionResponseReceived;
            }

            ClearPendingSubmission();
        }
    }
}
