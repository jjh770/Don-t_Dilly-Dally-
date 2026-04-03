using Cysharp.Threading.Tasks;
using DontDillyDally.MiniGame;
using Photon.Pun;
using System;
using System.Threading;
using UnityEngine;

namespace DontDillyDally.StageFlow
{
    // 레시피 성공 뒤 필요한 미니게임 요청과 결과 수신을 전담합니다.
    public sealed class StageMiniGameCoordinator : IDisposable
    {
        private readonly StageFlowRpcHandler _rpc;
        private readonly MiniGameLauncher _miniGameLauncher;
        private readonly Func<bool> _isLocalSurgeon;

        private UniTaskCompletionSource<bool> _miniGameResultTcs;

        public StageMiniGameCoordinator(
            StageFlowRpcHandler rpc,
            MiniGameLauncher miniGameLauncher,
            Func<bool> isLocalSurgeon)
        {
            _rpc = rpc;
            _miniGameLauncher = miniGameLauncher;
            _isLocalSurgeon = isLocalSurgeon;

            if (_rpc != null)
            {
                _rpc.OnMiniGameRequested += HandleMiniGameRequested;
                _rpc.OnMiniGameResultReceived += HandleMiniGameResultReceived;
            }
        }

        // ── 공개 흐름 진입점 ─────────────────────────────────────────

        // 대상 Actor가 로컬이면 직접 실행하고, 원격이면 RPC 요청 후 결과를 기다립니다.
        public async UniTask<bool> RunRecipeMiniGame(int targetActorNumber, MiniGameType type, CancellationToken ct)
        {
            if (PhotonNetwork.LocalPlayer != null && targetActorNumber == PhotonNetwork.LocalPlayer.ActorNumber)
            {
                return await LaunchLocalMiniGame(type, ct);
            }

            return await LaunchRemoteMiniGame(targetActorNumber, type, ct);
        }

        // ── 정리 ────────────────────────────────────────────────────

        public void Dispose()
        {
            _miniGameResultTcs?.TrySetCanceled();
            _miniGameResultTcs = null;

            if (_rpc != null)
            {
                _rpc.OnMiniGameRequested -= HandleMiniGameRequested;
                _rpc.OnMiniGameResultReceived -= HandleMiniGameResultReceived;
            }
        }

        // ── 내부 실행 / 수신 처리 ────────────────────────────────────

        private async UniTask<bool> LaunchLocalMiniGame(MiniGameType type, CancellationToken ct)
        {
            if (_miniGameLauncher == null)
            {
                Debug.LogWarning("[StageFlow] 로컬 미니게임 런처가 없어 실패로 처리합니다.");
                return false;
            }

            if (_miniGameLauncher.IsPlaying)
            {
                Debug.LogWarning("[StageFlow] 이미 진행 중인 미니게임이 있어 실패로 처리합니다.");
                return false;
            }

            var tcs = new UniTaskCompletionSource<MiniGameResult>();
            using (ct.Register(() => tcs.TrySetCanceled()))
            {
                _miniGameLauncher.Launch(type, result => tcs.TrySetResult(result));
                MiniGameResult result = await tcs.Task;
                return result.IsSuccess;
            }
        }

        // 원격 집도의에게 미니게임을 요청한 뒤 성공 여부 응답을 기다립니다.
        private async UniTask<bool> LaunchRemoteMiniGame(int targetActorNumber, MiniGameType type, CancellationToken ct)
        {
            if (targetActorNumber <= 0)
            {
                Debug.LogWarning("[StageFlow] 원격 미니게임 대상 Actor가 유효하지 않아 실패로 처리합니다.");
                return false;
            }

            _miniGameResultTcs?.TrySetCanceled();
            _miniGameResultTcs = new UniTaskCompletionSource<bool>();

            using (ct.Register(() => _miniGameResultTcs.TrySetCanceled()))
            {
                _rpc?.RequestMiniGame(targetActorNumber, type);
                return await _miniGameResultTcs.Task;
            }
        }

        // RPC로 전달된 미니게임 요청을 로컬 집도의가 직접 실행합니다.
        private void HandleMiniGameRequested(MiniGameType type)
        {
            if (_isLocalSurgeon == null || !_isLocalSurgeon())
            {
                Debug.LogWarning("[StageFlow] 집도의가 아닌 클라이언트가 미니게임 요청을 받아 실패로 응답합니다.");
                _rpc?.SendMiniGameResult(false);
                return;
            }

            if (_miniGameLauncher == null)
            {
                Debug.LogWarning("[StageFlow] 로컬 미니게임 런처가 없어 실패로 처리합니다.");
                _rpc?.SendMiniGameResult(false);
                return;
            }

            if (_miniGameLauncher.IsPlaying)
            {
                Debug.LogWarning("[StageFlow] 이미 진행 중인 미니게임이 있어 실패로 처리합니다.");
                _rpc?.SendMiniGameResult(false);
                return;
            }

            Debug.Log($"[StageFlow] 미니게임 요청을 수신해 로컬에서 실행합니다: {type}");
            _miniGameLauncher.Launch(type, result =>
            {
                _rpc?.SendMiniGameResult(result.IsSuccess);
            });
        }

        private void HandleMiniGameResultReceived(bool success)
        {
            _miniGameResultTcs?.TrySetResult(success);
        }
    }
}

