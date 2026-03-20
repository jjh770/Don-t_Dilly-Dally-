using System;
using System.Collections.Generic;
using UnityEngine;

namespace DontDillyDally.Data
{
    // AI 질병 생성을 오케스트레이션하는 매니저입니다.
    // AI 생성 → JSON 파싱 → 검증 → 폴백 순서로 처리합니다.
    public class DiseaseGenerationManager : MonoBehaviour
    {
        [Header("서비스 연결")]
        [SerializeField] private DiseaseGenerationService _generationService;

        [Header("재시도 설정")]
        [SerializeField] private int _maxRetries = 1;

        // 지정된 난이도와 카테고리로 질병 데이터를 생성합니다.
        // AI 생성에 실패하면 폴백 데이터를 반환합니다.
        // difficulty: 1~5 (0이면 랜덤), category: "외과"/"내과"/"피부과"/"정형외과" (null이면 자유)
        public async Awaitable<DiseaseData> GenerateDisease(int difficulty = 0, string category = null)
        {
            if (difficulty <= 0 || difficulty > 5)
                difficulty = UnityEngine.Random.Range(1, 6);

            string diseaseId = $"AI_{DateTime.Now:yyyyMMddHHmmss}";
            string userPrompt = BuildUserPrompt(difficulty, category, diseaseId);

            for (int attempt = 0; attempt <= _maxRetries; attempt++)
            {
                if (attempt > 0)
                    Debug.Log($"[DiseaseGenerationManager] AI 생성 재시도 ({attempt}/{_maxRetries})...");

                DiseaseData result = await TryGenerateFromAI(userPrompt);
                if (result != null)
                {
                    result.Source = RecipeSource.AIGenerated;
                    Debug.Log($"[DiseaseGenerationManager] AI 질병 생성 성공: {result.DiseaseName}");
                    return result;
                }
            }

            return GetFallback(difficulty);
        }

        private async Awaitable<DiseaseData> TryGenerateFromAI(string userPrompt)
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

                DiseaseDTO dto = JsonUtility.FromJson<DiseaseDTO>(json);
                if (dto == null)
                {
                    Debug.LogWarning("[DiseaseGenerationManager] JSON 파싱 결과가 null입니다.");
                    return null;
                }

                DiseaseData disease = DiseaseConverter.Convert(dto);
                if (disease == null)
                {
                    Debug.LogWarning("[DiseaseGenerationManager] DiseaseConverter 검증 실패.");
                    return null;
                }

                return disease;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[DiseaseGenerationManager] JSON 파싱 예외: {e.Message}");
                return null;
            }
        }

        private DiseaseData GetFallback(int difficulty)
        {
            Debug.LogWarning("[DiseaseGenerationManager] AI 생성 실패, 폴백 데이터 사용.");

            List<DiseaseData> fallbacks = FallbackDiseaseLoader.GetByDifficulty(difficulty);
            if (fallbacks.Count > 0)
                return fallbacks[UnityEngine.Random.Range(0, fallbacks.Count)];

            return FallbackDiseaseLoader.GetRandom();
        }

        private string BuildUserPrompt(int difficulty, string category, string diseaseId)
        {
            string categoryText = string.IsNullOrEmpty(category) ? "자유" : category;
            return $"난이도: {difficulty}\n카테고리: {categoryText}\ndiseaseId: \"{diseaseId}\"\n새로운 질병 데이터를 생성해주세요.";
        }
    }
}
