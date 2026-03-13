using System;

namespace DontDillyDally.Data
{
    // 조합 시스템에서 사용하는 원본 도구와 원재료 타입입니다.
    // 행동을 거쳐 결과물을 만드는 대상만 포함합니다.
    [Flags]
    public enum ToolType : long
    {
        None = 0,

        // 준비 재료
        Tray = 1L << 0,               // 빈 트레이
        Syringe = 1L << 1,            // 빈 주사기
        EmptyBeaker = 1L << 2,        // 빈 비커

        // 수술 도구
        ScalpelGreen = 1L << 3,       // 초록 메스
        ScalpelGray = 1L << 4,        // 회색 메스
        PincetteCurved = 1L << 5,     // 곡선 핀셋
        PincetteStraight = 1L << 6,   // 직선 핀셋
        ScissorsSmall = 1L << 7,      // 소형 가위
        ScissorsLarge = 1L << 8,      // 대형 가위
        ScissorsClamp = 1L << 9,      // 지혈 가위
        BoneSaw = 1L << 10,           // 뼈톱

        // 물약 원재료
        PotionCyan = 1L << 11,        // 청록 원액
        PotionMagenta = 1L << 12,     // 자주 원액
        PotionYellow = 1L << 13,      // 노랑 원액

        // 액체 원재료
        AnestheticFluid = 1L << 14,   // 마취액
        SedativeFluid = 1L << 15      // 진정액
    }
}
