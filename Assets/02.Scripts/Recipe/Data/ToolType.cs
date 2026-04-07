using System;

namespace DontDillyDally.Data
{
    // 조합 시스템에서 사용하는 원본 도구와 원재료 타입입니다.
    // 행동을 거쳐 결과물을 만드는 대상만 포함합니다.
    [Flags]
    public enum ToolType : int
    {
        None = 0,

        // 준비 재료
        Syringe = 1 << 0,            // 빈 주사기

        // 수술 도구
        ScalpelGreen = 1 << 1,       // 초록 메스
        ScalpelGray = 1 << 2,        // 회색 메스
        PincetteCurved = 1 << 3,     // 곡선 핀셋
        PincetteStraight = 1 << 4,   // 직선 핀셋
        ScissorsSmall = 1 << 5,      // 소형 가위
        ScissorsLarge = 1 << 6,      // 대형 가위
        ScissorsClamp = 1 << 7,      // 지혈 가위
        BoneSaw = 1 << 8,            // 뼈톱

        // 물약 원재료
        PotionCyan = 1 << 9,         // 청록 원액
        PotionMagenta = 1 << 10,     // 자주 원액
        PotionYellow = 1 << 11,      // 노랑 원액

        // 액체 원재료
        AnestheticFluid = 1 << 12,   // 마취액
        SedativeFluid = 1 << 13      // 진정액
    }
}
