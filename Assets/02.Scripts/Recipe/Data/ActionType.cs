using System;

namespace DontDillyDally.Data
{
    // 기계나 스테이션에서 수행하는 행동 타입입니다.
    // 조합은 도구와 행동 조합으로 판정합니다.
    [Flags]
    public enum ActionType : int
    {
        None = 0,

        // 멸균 행동
        Sterilize = 1 << 0,       // 멸균기 사용

        // 채우기 행동
        Fill = 1 << 1,            // 주사기나 비커 채우기

        // 물약 혼합 행동
        MixPotion = 1 << 2,       // 두 물약 혼합

        // 집도의 전용 스캔 행동
        UltrasoundScan = 1 << 3,  // 초음파 검사
        XRayScan = 1 << 4,        // X-Ray 검사
        ECGScan = 1 << 5          // ECG 검사
    }
}
