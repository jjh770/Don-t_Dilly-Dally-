using UnityEngine;

namespace DontDillyDally.MiniGame
{
    [CreateAssetMenu(menuName = "DontDillyDally/MiniGame/PrecisionStopConfig")]
    public sealed class PrecisionStopConfig : MiniGameConfig
    {
        [Header("순발력 정지 설정")]
        [Tooltip("시도 횟수 (라운드)")]
        [Range(1, 5)]
        public int roundCount = 3;

        [Tooltip("목표 구간의 너비 (0~1 기준). 작을수록 어려움")]
        [Range(0.05f, 0.3f)]
        public float targetZoneWidth = 0.15f;

        [Tooltip("화살표 이동 속도 (0~1 기준 초당 이동량)")]
        public float cursorSpeed = 0.8f;

        [Tooltip("라운드마다 속도 증가 배율")]
        public float speedMultiplierPerRound = 1.15f;

        [Tooltip("정지 입력 키")]
        public KeyCode inputKey = KeyCode.Space;

        [Tooltip("목표 구간 중심 배치 여백")]
        [Range(0.1f, 0.3f)]
        public float targetZonePadding = 0.15f;
    }
}
