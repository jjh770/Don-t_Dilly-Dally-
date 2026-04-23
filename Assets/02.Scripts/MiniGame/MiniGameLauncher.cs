using DontDillyDally.StageFlow;
using System;
using System.Collections;
using UnityEngine;

namespace DontDillyDally.MiniGame
{
    // 미니게임의 생성, 실행, 종료 라이프사이클을 관리한다.
    // 외부 시스템은 Launch()만 호출하면 된다.
    public sealed class MiniGameLauncher : MonoBehaviour
    {
        [Header("미니게임별 설정 에셋")]
        [SerializeField] private ButtonMashConfig _buttonMashConfig;
        [SerializeField] private DirectionQTEConfig _directionQTEConfig;
        [SerializeField] private PrecisionStopConfig _precisionStopConfig;
        [SerializeField] private ReCaptchaConfig _reCaptchaConfig;

        [Header("UI 참조")]
        [SerializeField] private MiniGameUIController _uiController;

        [Header("결과 연출")]
        [Tooltip("성공/실패 UI가 유지되는 시간(초)")]
        [SerializeField] private float _resultDisplayDuration = 0.5f;

        private IMiniGame _activeMiniGame;
        private IInputProvider _inputProvider;
        private Coroutine _resultCoroutine;
        private PlayerMovementAbility _playerMovementAbility;
        private readonly object _movementLockSource = new();
        private StageFlowRpcHandler _rpc;

        public bool IsPlaying =>
            _activeMiniGame != null && _activeMiniGame.CurrentState == EMiniGameState.Playing;

        private void Awake()
        {
            _inputProvider = new UnityInputProvider();
        }

        private void OnDisable()
        {
            UnlockLocalPlayerMovement();
        }

        // 외부에서 미니게임 실행을 요청하는 단일 진입점.
        public void Launch(MiniGameType type, Action<MiniGameResult> onComplete)
        {
            if (IsPlaying)
            {
                Debug.LogWarning("[MiniGameLauncher] 이미 진행 중인 미니게임이 있음");
                return;
            }

            LockLocalPlayerMovement();

            IMiniGame game = CreateMiniGame(type);
            MiniGameConfig config = GetConfig(type);

            _activeMiniGame = game;

            game.OnCompleted += result =>
            {
                _resultCoroutine = StartCoroutine(DelayedComplete(result, onComplete));
            };

            _uiController.ShowMiniGameUI(game);
            game.Begin(config);

            // 모든 클라이언트에 수술 VFX 시작 알림
            if (IsPlaying)
            {
                ResolveRpc()?.BroadcastMiniGameVFXStarted();
            }

            // Begin()에서 Config 캐스팅 실패 등으로 Playing 상태가 아니면 정리.
            if (!IsPlaying)
            {
                _uiController.HideMiniGameUI();
                _activeMiniGame = null;
                UnlockLocalPlayerMovement();
            }
        }

        // 진행 중인 미니게임 강제 중단.
        public void AbortCurrent()
        {
            // 결과 연출 코루틴이 진행 중이면 중복 호출 방지.
            if (_resultCoroutine != null)
            {
                return;
            }

            _activeMiniGame?.Abort();
        }

        private void Update()
        {
            if (IsPlaying)
            {
                _activeMiniGame.Tick(Time.deltaTime);
            }
        }

        private IMiniGame CreateMiniGame(MiniGameType type)
        {
            return type switch
            {
                MiniGameType.ButtonMash => new ButtonMashMiniGame(_inputProvider),
                MiniGameType.DirectionQTE => new DirectionQTEMiniGame(_inputProvider),
                MiniGameType.PrecisionStop => new PrecisionStopMiniGame(_inputProvider),
                MiniGameType.ReCaptcha => new ReCaptchaMiniGame(),
                _ => throw new ArgumentOutOfRangeException(nameof(type))
            };
        }

        private IEnumerator DelayedComplete(MiniGameResult result, Action<MiniGameResult> onComplete)
        {
            // 모든 클라이언트에 결과 VFX 알림 (성공/실패 이펙트)
            ResolveRpc()?.BroadcastMiniGameVFXResult(result.IsSuccess);

            // 결과 연출용 대기 (UI는 그대로 보여줌).
            _uiController.ShowResult(result.IsSuccess);
            yield return new WaitForSeconds(_resultDisplayDuration);

            _uiController.HideMiniGameUI();
            _activeMiniGame = null;
            _resultCoroutine = null;
            UnlockLocalPlayerMovement();
            onComplete?.Invoke(result);
        }

        private MiniGameConfig GetConfig(MiniGameType type)
        {
            return type switch
            {
                MiniGameType.ButtonMash => _buttonMashConfig,
                MiniGameType.DirectionQTE => _directionQTEConfig,
                MiniGameType.PrecisionStop => _precisionStopConfig,
                MiniGameType.ReCaptcha => _reCaptchaConfig,
                _ => throw new ArgumentOutOfRangeException(nameof(type))
            };
        }

        private StageFlowRpcHandler ResolveRpc()
        {
            if (_rpc != null)
            {
                return _rpc;
            }

            _rpc = FindObjectOfType<StageFlowRpcHandler>();
            return _rpc;
        }

        private void LockLocalPlayerMovement()
        {
            if (_playerMovementAbility != null)
            {
                _playerMovementAbility.SetMovementLocked(_movementLockSource, true);
                return;
            }

            _playerMovementAbility = ResolveLocalMovementAbility();
            _playerMovementAbility?.SetMovementLocked(_movementLockSource, true);
        }

        private void UnlockLocalPlayerMovement()
        {
            if (_playerMovementAbility == null)
            {
                return;
            }

            _playerMovementAbility.SetMovementLocked(_movementLockSource, false);
            _playerMovementAbility = null;
        }

        private static PlayerMovementAbility ResolveLocalMovementAbility()
        {
            if (!PlayerRegistry.TryGetLocalPlayer(out PlayerController player) || player == null)
            {
                return null;
            }

            return player.MovementAbility;
        }
    }
}
