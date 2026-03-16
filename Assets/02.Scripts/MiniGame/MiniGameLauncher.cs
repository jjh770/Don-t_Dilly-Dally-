using System;
using System.Collections;
using UnityEngine;

namespace DontDillyDally.MiniGame
{
    // 미니게임의 생성, 실행, 종료 라이프사이클을 관리한다.
    // 외부 시스템은 Launch()만 호출하면 된다.
    //
    // [통합 시 필요한 작업]
    // 1. Launch() 호출 전에 플레이어 입력 비활성화
    // 2. onComplete 콜백에서 플레이어 입력 재활성화
    public sealed class MiniGameLauncher : MonoBehaviour
    {
        [Header("미니게임별 설정 에셋")]
        [SerializeField] private ButtonMashConfig _buttonMashConfig;
        [SerializeField] private DirectionQTEConfig _directionQTEConfig;
        [SerializeField] private PrecisionStopConfig _precisionStopConfig;

        [Header("UI 참조")]
        [SerializeField] private MiniGameUIController _uiController;

        [Header("카운트다운 설정")]
        [SerializeField] private float _countdownDuration = 3f;

        private IMiniGame _activeMiniGame;
        private IInputProvider _inputProvider;
        private bool _isCountingDown;

        private void Awake()
        {
            _inputProvider = new UnityInputProvider();
        }

        // 외부에서 미니게임 실행을 요청하는 단일 진입점
        public void Launch(MiniGameType type, Action<MiniGameResult> onComplete)
        {
            if (_activeMiniGame != null && _activeMiniGame.CurrentState == MiniGameState.Playing)
            {
                Debug.LogWarning("[MiniGameLauncher] 이미 진행 중인 미니게임이 있음");
                return;
            }

            if (_isCountingDown)
            {
                Debug.LogWarning("[MiniGameLauncher] 카운트다운 진행 중");
                return;
            }

            IMiniGame game = CreateMiniGame(type);
            MiniGameConfig config = GetConfig(type);

            _activeMiniGame = game;

            game.OnCompleted += result =>
            {
                _uiController.HideMiniGameUI();
                _activeMiniGame = null;
                onComplete?.Invoke(result);
            };

            _uiController.ShowMiniGameUI(game);
            StartCoroutine(CountdownThenBegin(game, config));
        }

        // 진행 중인 미니게임 강제 중단
        public void AbortCurrent()
        {
            _activeMiniGame?.Abort();
        }

        public bool IsPlaying =>
            _activeMiniGame != null && _activeMiniGame.CurrentState == MiniGameState.Playing;

        private void Update()
        {
            if (_activeMiniGame?.CurrentState == MiniGameState.Playing)
            {
                _activeMiniGame.Tick(Time.deltaTime);
            }
        }

        private IEnumerator CountdownThenBegin(IMiniGame game, MiniGameConfig config)
        {
            _isCountingDown = true;
            _uiController.StartCountdown(_countdownDuration);

            yield return new WaitForSeconds(_countdownDuration);

            _isCountingDown = false;
            game.Begin(config);
        }

        private IMiniGame CreateMiniGame(MiniGameType type)
        {
            return type switch
            {
                MiniGameType.ButtonMash => new ButtonMashMiniGame(_inputProvider),
                MiniGameType.DirectionQTE => new DirectionQTEMiniGame(_inputProvider),
                MiniGameType.PrecisionStop => new PrecisionStopMiniGame(_inputProvider),
                _ => throw new ArgumentOutOfRangeException(nameof(type))
            };
        }

        private MiniGameConfig GetConfig(MiniGameType type)
        {
            return type switch
            {
                MiniGameType.ButtonMash => _buttonMashConfig,
                MiniGameType.DirectionQTE => _directionQTEConfig,
                MiniGameType.PrecisionStop => _precisionStopConfig,
                _ => throw new ArgumentOutOfRangeException(nameof(type))
            };
        }
    }
}
