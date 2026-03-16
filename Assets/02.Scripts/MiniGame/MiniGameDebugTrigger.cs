using UnityEngine;

namespace DontDillyDally.MiniGame
{
    // 에디터에서 키 입력으로 미니게임을 테스트하는 디버그 도구.
    // F1: ButtonMash, F2: DirectionQTE, F3: PrecisionStop
    public sealed class MiniGameDebugTrigger : MonoBehaviour
    {
#if UNITY_EDITOR
        [SerializeField] private MiniGameLauncher _launcher;

        private void Update()
        {
            if (_launcher == null) return;

            if (Input.GetKeyDown(KeyCode.F1))
            {
                LaunchDebug(MiniGameType.ButtonMash);
            }
            else if (Input.GetKeyDown(KeyCode.F2))
            {
                LaunchDebug(MiniGameType.DirectionQTE);
            }
            else if (Input.GetKeyDown(KeyCode.F3))
            {
                LaunchDebug(MiniGameType.PrecisionStop);
            }
        }

        private void LaunchDebug(MiniGameType type)
        {
            Debug.Log($"[DebugTrigger] {type} 미니게임 시작");
            _launcher.Launch(type, result =>
            {
                string status = result.IsSuccess ? "성공" : "실패";
                Debug.Log($"[DebugTrigger] {result.GameType} 결과: {status}, 점수={result.Score:F2}, 소요={result.ElapsedTime:F1}초");
            });
        }
#endif
    }
}
