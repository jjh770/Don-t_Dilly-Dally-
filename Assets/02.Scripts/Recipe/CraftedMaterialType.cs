namespace DontDillyDally.Data
{
    // 조합을 통해 완성되거나 바로 제출 가능한 최종 재료 타입입니다.
    // 실제 레시피 판정은 이 결과 재료 단위로 진행합니다.
    public enum CraftedMaterialType
    {
        None = 0,

        // 멸균 결과물
        SterilizedTray,              // 멸균 트레이
        SterilizedScalpelGreen,      // 멸균 초록 메스
        SterilizedScalpelGray,       // 멸균 회색 메스
        SterilizedPincetteCurved,    // 멸균 곡선 핀셋
        SterilizedPincetteStraight,  // 멸균 직선 핀셋
        SterilizedScissorsSmall,     // 멸균 소형 가위
        SterilizedScissorsLarge,     // 멸균 대형 가위
        SterilizedScissorsClamp,     // 멸균 지혈 가위
        SterilizedBoneSaw,           // 멸균 뼈톱

        // 주사기 결과물
        AnestheticSyringe,           // 마취 주사기
        SedativeSyringe,             // 진정제 주사기

        // 단일 물약
        FilledPotionCyan,            // 청록 물약
        FilledPotionMagenta,         // 자주 물약
        FilledPotionYellow,          // 노랑 물약

        // 혼합 물약
        MixedPotionBlue,             // 파랑 물약
        MixedPotionRed,              // 빨강 물약
        MixedPotionGreen,            // 초록 물약

        // 기본 제출 재료
        Bandage,                     // 붕대
        Disinfectant,                // 소독약
        Stethoscope,                 // 청진기
        AmbuBag,                     // 앰부백
        GauzeBox,                    // 거즈
        RedMedicine,                 // 빨강 약
        OrganLiver,                  // 간
        OrganStomach,                // 위
        OrganLung,                   // 폐
        OrganIntestine,              // 장

        // 긴급 이벤트용 재료
        Defibrillator,               // 제세동기
        BloodPack,                   // 혈액팩

        // 잘못된 조합 결과
        Unknown
    }
}
