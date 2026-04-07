using UnityEngine;

namespace DontDillyDally.MiniGame
{
    [CreateAssetMenu(menuName = "DontDillyDally/MiniGame/PrecisionStopConfig")]
    public sealed class PrecisionStopConfig : MiniGameConfig
    {
        // 목표 구간 너비의 절대 한계값 (0~1 정규화 기준).
        // Inspector Range slider 및 런타임 클램프에서 공통으로 사용.
        // Range attribute는 컴파일 타임 상수가 필요해 const로 선언.
        public const float MinTargetZoneWidth = 0.03f;
        public const float MaxTargetZoneWidth = 0.6f;

        [Header("순발력 정지 설정")]
        [Tooltip("시도 횟수 (라운드)")]
        [Range(1, 10)]
        public int RoundCount = 3;

        [Tooltip("목표 구간의 너비 (0~1 기준). 작을수록 어려움")]
        [Range(MinTargetZoneWidth, MaxTargetZoneWidth)]
        public float TargetZoneWidth = 0.15f;

        [Tooltip("화살표 이동 속도 (0~1 기준 초당 이동량)")]
        [Range(0.1f, 5f)]
        public float CursorSpeed = 0.8f;

        [Tooltip("라운드마다 속도 증가 배율")]
        [Range(1f, 2f)]
        public float SpeedMultiplierPerRound = 1.15f;

        [Tooltip("라운드마다 목표 구간 축소 배율 (1 이하일수록 점점 좁아짐)")]
        [Range(0.5f, 1f)]
        public float TargetZoneMultiplierPerRound = 1f;

        [Tooltip("정지 입력 키")]
        public KeyCode InputKey = KeyCode.Space;

        [Tooltip("목표 구간 중심 배치 여백 (양 끝으로부터의 최소 거리)")]
        [Range(0.05f, 0.4f)]
        public float TargetZonePadding = 0.15f;

        [Tooltip("라운드 전환 대기 시간 (초)")]
        [Range(0.1f, 3f)]
        public float RoundTransitionDelay = 0.6f;
    }
}
