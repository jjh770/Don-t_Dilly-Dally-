using System;
using System.Collections.Generic;
using UnityEngine;

namespace DontDillyDally.Data
{
    public static class StagePromptHelper
    {
        private static readonly CraftedMaterialType[] DefaultMaterials =
        {
            CraftedMaterialType.SterilizedScalpelGreen,
            CraftedMaterialType.SterilizedScalpelGray,
            CraftedMaterialType.SterilizedPincetteCurved,
            CraftedMaterialType.SterilizedPincetteStraight,
            CraftedMaterialType.SterilizedScissorsSmall,
            CraftedMaterialType.SterilizedScissorsLarge,
            CraftedMaterialType.SterilizedScissorsClamp,
            CraftedMaterialType.SterilizedBoneSaw,
            CraftedMaterialType.AnestheticSyringe,
            CraftedMaterialType.FilledPotionCyan,
            CraftedMaterialType.FilledPotionMagenta,
            CraftedMaterialType.FilledPotionYellow,
            CraftedMaterialType.MixedPotionBlue,
            CraftedMaterialType.MixedPotionRed,
            CraftedMaterialType.MixedPotionGreen,
            CraftedMaterialType.MixedPotionBlack,
            CraftedMaterialType.Bandage,
            CraftedMaterialType.Disinfectant,
            CraftedMaterialType.Stethoscope,
            CraftedMaterialType.AmbuBag,
            CraftedMaterialType.GauzeBox,
            CraftedMaterialType.RedMedicine,
            CraftedMaterialType.OrganLiver,
            CraftedMaterialType.OrganStomach,
            CraftedMaterialType.OrganLung,
            CraftedMaterialType.OrganIntestine,
        };

        private static readonly CraftedMaterialType[] Stage1Materials =
        {
            CraftedMaterialType.SterilizedScalpelGreen,
            CraftedMaterialType.SterilizedScissorsSmall,
            CraftedMaterialType.SterilizedBoneSaw,
            CraftedMaterialType.SterilizedPincetteStraight,
            CraftedMaterialType.AnestheticSyringe,
            CraftedMaterialType.FilledPotionCyan,
            CraftedMaterialType.FilledPotionMagenta,
            CraftedMaterialType.FilledPotionYellow,
            CraftedMaterialType.MixedPotionBlue,
            CraftedMaterialType.MixedPotionRed,
            CraftedMaterialType.MixedPotionGreen,
            CraftedMaterialType.MixedPotionBlack,
            CraftedMaterialType.RedMedicine,
            CraftedMaterialType.Bandage,
            CraftedMaterialType.OrganIntestine,
        };

        private static readonly CraftedMaterialType[] Stage2Materials =
        {
            CraftedMaterialType.SterilizedScalpelGray,
            CraftedMaterialType.SterilizedScissorsLarge,
            CraftedMaterialType.AnestheticSyringe,
            CraftedMaterialType.FilledPotionCyan,
            CraftedMaterialType.FilledPotionMagenta,
            CraftedMaterialType.FilledPotionYellow,
            CraftedMaterialType.MixedPotionBlue,
            CraftedMaterialType.MixedPotionRed,
            CraftedMaterialType.MixedPotionGreen,
            CraftedMaterialType.MixedPotionBlack,
            CraftedMaterialType.Bandage,
        };

        private static readonly CraftedMaterialType[] Stage3Materials =
        {
            CraftedMaterialType.SterilizedScalpelGray,
            CraftedMaterialType.SterilizedPincetteStraight,
            CraftedMaterialType.SterilizedScissorsSmall,
            CraftedMaterialType.AnestheticSyringe,
            CraftedMaterialType.Bandage,
            CraftedMaterialType.Disinfectant,
            CraftedMaterialType.GauzeBox,
            CraftedMaterialType.OrganLung,
        };

        private static readonly CraftedMaterialType[] Stage4Materials =
        {
            CraftedMaterialType.SterilizedScalpelGreen,
            CraftedMaterialType.SterilizedPincetteCurved,
            CraftedMaterialType.SterilizedScissorsClamp,
            CraftedMaterialType.AnestheticSyringe,
            CraftedMaterialType.Bandage,
            CraftedMaterialType.Disinfectant,
            CraftedMaterialType.Stethoscope,
            CraftedMaterialType.AmbuBag,
            CraftedMaterialType.OrganLiver,
            CraftedMaterialType.OrganStomach,
        };

        public static IReadOnlyList<CraftedMaterialType> GetAllowedMaterials(string stageId)
        {
            return stageId switch
            {
                "1" => Stage1Materials,
                "2" => Stage2Materials,
                "3" => Stage3Materials,
                "4" => Stage4Materials,
                _ => DefaultMaterials,
            };
        }

        // 환자 인덱스별로 순환하며 할당되는 신체 부위 힌트입니다.
        // 병렬 단건 호출 시 각 환자가 서로 다른 부위를 갖도록 강제합니다.
        private static readonly string[] s_bodyPartHints =
        {
            "머리/두부",
            "복부/소화기",
            "팔/다리/외상",
            "호흡기/흉부",
            "피부/감각",
            "정형외과/뼈",
        };

        // 스테이지별 테마 영감 키워드 배열입니다.
        // 인덱스별로 순환하며 각 환자에게 서로 다른 소재가 배정됩니다.
        private static readonly string[] s_stage1ThemeKeywords =
        {
            "호박/잭오랜턴",
            "좀비 바이러스",
            "미라 붕대",
            "마녀 주술/빗자루",
            "박쥐/거미줄",
            "유령/귀신",
            "사탕 과다 섭취",
            "코스튬 사고",
        };

        private static readonly string[] s_stage2ThemeKeywords =
        {
            "총상/탄피",
            "수류탄 파편",
            "낙하산/훈련 사고",
            "전투식량 중독",
            "탱크/전투기 동경병",
            "위장크림 알레르기",
            "군화 물집",
            "행군 탈진",
        };

        private static readonly string[] s_stage3ThemeKeywords =
        {
            "동상/저체온",
            "냉동참치병",
            "얼죽아 증후군",
            "눈싸움 사고",
            "스키/스노보드 골절",
            "고드름 낙하",
            "털옷 정전기",
            "핫팩 화상",
        };

        private static readonly string[] s_stage4ThemeKeywords =
        {
            "의사 번아웃",
            "주사 공포증",
            "병문안 피로",
            "대기실 답답병",
            "소독약 중독",
            "의료차트 손목터널",
            "청진기 귀막힘",
            "링거 알러지",
        };

        private static string[] GetThemeKeywords(string stageId)
        {
            return stageId switch
            {
                "1" => s_stage1ThemeKeywords,
                "2" => s_stage2ThemeKeywords,
                "3" => s_stage3ThemeKeywords,
                "4" => s_stage4ThemeKeywords,
                _ => null,
            };
        }

        // 주어진 인덱스에 해당하는 환자별 힌트(신체 부위 + 테마 소재)를 반환합니다.
        public static string GetPatientHint(string stageId, int patientIndex)
        {
            string bodyPart = s_bodyPartHints[patientIndex % s_bodyPartHints.Length];

            string[] themes = GetThemeKeywords(stageId);
            if (themes == null || themes.Length == 0)
                return $"신체 부위: {bodyPart}";

            string theme = themes[patientIndex % themes.Length];
            return $"신체 부위: {bodyPart} / 스테이지 소재: {theme}";
        }

        public static string GetStageContext(string stageId)
        {
            string stageName = stageId switch
            {
                "1" => "1스테이지 (할로윈 맵)",
                "2" => "2스테이지 (밀리터리 맵)",
                "3" => "3스테이지 (겨울 맵)",
                "4" => "4스테이지 (병원 맵)",
                _ => $"{stageId} (기본 맵)",
            };

            string[] themes = GetThemeKeywords(stageId);
            string themeLine = themes != null
                ? $"{stageName}의 테마 키워드 풀: {string.Join(", ", themes)}"
                : "일반 테마";

            return $"목표 스테이지: {stageName}\n" +
                   $"{themeLine}\n" +
                   $"사용 가능 재료: {string.Join(", ", GetAllowedMaterials(stageId))}";
        }

        public static bool IsDiseaseAllowedForStage(DiseaseData disease, string stageId)
        {
            if (disease?.Recipes == null || string.IsNullOrEmpty(stageId))
                return false;

            if (!string.IsNullOrEmpty(disease.StageId))
                return disease.StageId == stageId;

            IReadOnlyList<CraftedMaterialType> allowedMaterials = GetAllowedMaterials(stageId);

            for (int i = 0; i < disease.Recipes.Count; i++)
            {
                List<CraftedMaterialType> requiredMaterials = disease.Recipes[i].RequiredMaterials;
                if (requiredMaterials == null)
                    return false;

                for (int j = 0; j < requiredMaterials.Count; j++)
                {
                    CraftedMaterialType material = requiredMaterials[j];
                    if (material == CraftedMaterialType.SterilizedTray)
                        continue;

                    bool isAllowed = false;
                    for (int k = 0; k < allowedMaterials.Count; k++)
                    {
                        if (allowedMaterials[k] == material)
                        {
                            isAllowed = true;
                            break;
                        }
                    }

                    if (!isAllowed)
                        return false;
                }
            }

            return true;
        }
    }

    // AI 질병 생성을 오케스트레이션하는 매니저입니다.
    // AI 생성 → JSON 파싱 → 검증 → 폴백 순서로 처리합니다.
    public class DiseaseGenerationManager : MonoBehaviour
    {
        [Header("서비스 연결")]
        [SerializeField] private DiseaseGenerationService _generationService;

        [Header("재시도 설정")]
        [SerializeField] private int _maxRetries = 0;

        // 지정된 난이도로 질병 데이터를 생성합니다.
        // AI 생성에 실패하면 폴백 데이터를 반환합니다.
        // difficulty: 1~5 (0이면 랜덤)
        public async Awaitable<DiseaseData> GenerateDisease(
            int difficulty = 0, string stageId = null,
            int patientIndex = 0, int totalPatients = 1)
        {
            if (difficulty <= 0 || difficulty > 5)
                difficulty = UnityEngine.Random.Range(1, 6);

            string diseaseId = $"AI_{DateTime.Now:yyyyMMddHHmmss}_{patientIndex:D2}";
            string userPrompt = BuildUserPrompt(difficulty, diseaseId, stageId, patientIndex, totalPatients);

            Debug.Log($"[DiseaseGenerationManager] ===== 단건 요청 [{patientIndex + 1}/{totalPatients}] =====\n" +
                      $"stageId='{stageId}', difficulty={difficulty}\n" +
                      $"--- 유저 프롬프트 ---\n{userPrompt}\n" +
                      $"=====================");

            for (int attempt = 0; attempt <= _maxRetries; attempt++)
            {
                if (attempt > 0)
                {
                    Debug.Log($"[DiseaseGenerationManager] AI 생성 재시도 ({attempt}/{_maxRetries}), 0.8초 대기...");
                    await Awaitable.WaitForSecondsAsync(0.8f);
                }

                DiseaseData result = await TryGenerateFromAI(userPrompt, stageId);
                if (result != null)
                {
                    result.Source = RecipeSource.AIGenerated;
                    Debug.Log($"[DiseaseGenerationManager] AI 질병 생성 성공: {result.DiseaseName}");
                    return result;
                }
            }

            return GetFallback(difficulty, stageId);
        }

        private async Awaitable<DiseaseData> TryGenerateFromAI(string userPrompt, string stageId)
        {
            if (_generationService == null)
            {
                Debug.LogWarning("[DiseaseGenerationManager] DiseaseGenerationService가 연결되지 않았습니다.");
                return null;
            }

            string json = await _generationService.GenerateDiseaseJson(userPrompt);
            if (string.IsNullOrEmpty(json))
                return null;

            try
            {
                Debug.Log($"[DiseaseGenerationManager] AI 응답 JSON:\n{json}");

                // 시스템 프롬프트가 항상 {"diseases": [...]} 형식을 반환하도록 지시하므로
                // 단일 생성이라도 배치 래퍼로 파싱한 뒤 첫 요소를 사용한다.
                GeneratedDiseaseCollection collection =
                    JsonUtility.FromJson<GeneratedDiseaseCollection>(json);

                if (collection?.diseases == null || collection.diseases.Count == 0)
                {
                    Debug.LogWarning("[DiseaseGenerationManager] JSON 파싱 결과가 비어 있습니다.");
                    return null;
                }

                DiseaseData disease = DiseaseConverter.Convert(collection.diseases[0]);
                if (disease == null)
                {
                    Debug.LogWarning("[DiseaseGenerationManager] DiseaseConverter 검증 실패.");
                    return null;
                }

                // Stage 검증 — AI가 허용되지 않은 재료를 사용했는지 확인
                if (!string.IsNullOrEmpty(stageId) &&
                    !StagePromptHelper.IsDiseaseAllowedForStage(disease, stageId))
                {
                    Debug.LogWarning(
                        $"[DiseaseGenerationManager] Stage '{stageId}'에 허용되지 않은 재료 포함: {disease.DiseaseName}");
                    return null;
                }

                // AI 응답에는 stageId 필드가 없으므로 수동 할당
                disease.StageId = stageId;

                return disease;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[DiseaseGenerationManager] JSON 파싱 예외: {e.Message}");
                return null;
            }
        }

        // 지정된 수만큼 질병 데이터를 한 번의 API 호출로 배치 생성합니다.
        // 부분 성공을 허용하며, 검증 통과한 질병만 반환합니다.
        public async Awaitable<List<DiseaseData>> GenerateDiseases(
            int count, int difficulty = 0, string stageId = null)
        {
            if (difficulty <= 0 || difficulty > 5)
                difficulty = UnityEngine.Random.Range(1, 6);

            string baseId = $"AI_{DateTime.Now:yyyyMMddHHmmss}";
            string userPrompt = BuildBatchUserPrompt(count, difficulty, baseId, stageId);

            Debug.Log($"[DiseaseGenerationManager] ===== 배치 요청 =====\n" +
                      $"count={count}, difficulty={difficulty}, stageId='{stageId}'\n" +
                      $"--- 유저 프롬프트 ---\n{userPrompt}\n" +
                      $"=====================");

            for (int attempt = 0; attempt <= _maxRetries; attempt++)
            {
                if (attempt > 0)
                {
                    Debug.Log($"[DiseaseGenerationManager] 배치 생성 재시도 ({attempt}/{_maxRetries}), 1초 대기...");
                    await Awaitable.WaitForSecondsAsync(1f);
                }

                List<DiseaseData> results = await TryGenerateBatchFromAI(userPrompt, stageId);
                if (results != null && results.Count > 0)
                {
                    for (int i = 0; i < results.Count; i++)
                        results[i].Source = RecipeSource.AIGenerated;

                    Debug.Log($"[DiseaseGenerationManager] 배치 생성 성공: {results.Count}/{count}명");
                    return results;
                }
            }

            Debug.LogWarning($"[DiseaseGenerationManager] 배치 생성 실패, 빈 리스트 반환.");
            return new List<DiseaseData>();
        }

        private async Awaitable<List<DiseaseData>> TryGenerateBatchFromAI(string userPrompt, string stageId)
        {
            if (_generationService == null)
            {
                Debug.LogWarning("[DiseaseGenerationManager] DiseaseGenerationService가 연결되지 않았습니다.");
                return null;
            }

            string json = await _generationService.GenerateDiseaseJson(userPrompt);
            if (string.IsNullOrEmpty(json))
                return null;

            try
            {
                Debug.Log($"[DiseaseGenerationManager] 배치 AI 응답 JSON:\n{json}");

                GeneratedDiseaseCollection collection =
                    JsonUtility.FromJson<GeneratedDiseaseCollection>(json);

                if (collection?.diseases == null || collection.diseases.Count == 0)
                {
                    Debug.LogWarning("[DiseaseGenerationManager] 배치 JSON 파싱 결과가 비어 있습니다.");
                    return null;
                }

                // 개별 검증 — 실패한 환자는 건너뛰고 성공한 것만 반환
                var results = new List<DiseaseData>();
                for (int i = 0; i < collection.diseases.Count; i++)
                {
                    DiseaseData disease = DiseaseConverter.Convert(collection.diseases[i]);
                    if (disease == null)
                    {
                        Debug.LogWarning($"[DiseaseGenerationManager] 배치 내 {i}번째 환자 DTO 변환 실패, 건너뜀.");
                        continue;
                    }

                    // Stage 검증 — AI가 허용되지 않은 재료를 사용했는지 확인
                    if (!string.IsNullOrEmpty(stageId) &&
                        !StagePromptHelper.IsDiseaseAllowedForStage(disease, stageId))
                    {
                        Debug.LogWarning(
                            $"[DiseaseGenerationManager] 배치 내 {i}번째 환자 '{disease.DiseaseName}' — Stage '{stageId}'에 허용되지 않은 재료 포함, 건너뜀.");
                        continue;
                    }

                    // AI 응답에는 stageId 필드가 없으므로 수동 할당
                    disease.StageId = stageId;

                    results.Add(disease);
                }

                return results;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[DiseaseGenerationManager] 배치 JSON 파싱 예외: {e.Message}");
                return null;
            }
        }
        private string BuildUserPrompt(int difficulty, string diseaseId, string stageId,
            int patientIndex, int totalPatients)
        {
            string stageContext = StagePromptHelper.GetStageContext(stageId);
            string patientHint = StagePromptHelper.GetPatientHint(stageId, patientIndex);

            // 시스템 프롬프트가 diseases 배열 래퍼를 요구하므로 단일 생성도 "환자 수: 1명" 을 명시한다.
            return $"[생성 조건]\n" +
                   $"{stageContext}\n" +
                   $"- 환자 수: 1명 (전체 {totalPatients}명 중 {patientIndex + 1}번째)\n" +
                   $"- 난이도: {difficulty}\n" +
                   $"- diseaseId: \"{diseaseId}\"\n\n" +
                   $"[이번 환자 전용 힌트]\n" +
                   $"{patientHint}\n" +
                   $"→ 위 힌트의 '신체 부위'를 중심으로 한 질병으로 만들고, '스테이지 소재'를 질병의 원인·배경·증상에 자연스럽게 녹여내세요. " +
                   $"스테이지 소재가 잘 어울리지 않으면 평범한 일상 질병으로 풀어도 괜찮습니다.\n\n" +
                   $"[금지]\n" +
                   $"1. [긴급 이벤트 전용 재료] 레시피에 포함 금지.\n" +
                   $"2. 레시피(recipes)는 반드시 '사용 가능 재료'에 명시된 재료만 사용.\n\n" +
                   $"diseases 배열에 환자 1명을 생성해주세요.";
        }

        private string BuildBatchUserPrompt(int count, int difficulty, string baseId, string stageId)
        {
            string stageContext = StagePromptHelper.GetStageContext(stageId);

            return $"[생성 조건]\n" +
                   $"{stageContext}\n" +
                   $"- 환자 수: {count}명\n" +
                   $"- 난이도: {difficulty}\n" +
                   $"- baseId: \"{baseId}\"\n\n" +
                   $"'테마 영감'에 나열된 여러 소재 중에서 서로 다른 소재를 골라 환자마다 다르게 변주하세요. 일부는 평범한 일상 질병을 스테이지 배경 속 사고로 풀어내도 좋습니다.\n" +
                   $"[다양성 필수] 환자 {count}명은 반드시 다음을 모두 만족:\n" +
                   $"  1. 서로 다른 신체 부위(머리/복부/사지/호흡기/소화기/피부 등)\n" +
                   $"  2. 서로 다른 증상 유형(외상/감염/내과/정형외과/피부과 등)\n" +
                   $"  3. 같은 소재(예: 호박, 좀비, 동상 등)를 2명 이상 반복 사용 금지 — 각 환자는 '테마 영감'의 서로 다른 키워드를 사용\n" +
                   $"레시피(recipes) 구성 시 반드시 '사용 가능 재료'에 명시된 재료만 사용해야 합니다. ([긴급 이벤트 전용 재료] 절대 포함 금지)\n" +
                   $"서로 다른 질병을 가진 환자 {count}명의 데이터를 diseases 배열에 생성해주세요.";
        }

        private DiseaseData GetFallback(int difficulty, string stageId)
        {
            Debug.LogWarning("[DiseaseGenerationManager] AI 생성 실패, 폴백 데이터 사용.");

            List<DiseaseData> fallbacks = FallbackDiseaseLoader.GetByStageAndDifficulty(stageId, difficulty);
            if (fallbacks.Count > 0)
                return fallbacks[UnityEngine.Random.Range(0, fallbacks.Count)];

            return FallbackDiseaseLoader.GetRandom(stageId);
        }
    }
}
