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
                {
                    Debug.Log($"[DiseaseGenerationManager] AI 생성 재시도 ({attempt}/{_maxRetries}), 3초 대기...");
                    await Awaitable.WaitForSecondsAsync(3f);
                }

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

        // 지정된 수만큼 질병 데이터를 한 번의 API 호출로 배치 생성합니다.
        // 부분 성공을 허용하며, 검증 통과한 질병만 반환합니다.
        public async Awaitable<List<DiseaseData>> GenerateDiseases(
            int count, int difficulty = 0, string category = null)
        {
            if (difficulty <= 0 || difficulty > 5)
                difficulty = UnityEngine.Random.Range(1, 6);

            string baseId = $"AI_{DateTime.Now:yyyyMMddHHmmss}";
            string userPrompt = BuildBatchUserPrompt(count, difficulty, category, baseId);

            for (int attempt = 0; attempt <= _maxRetries; attempt++)
            {
                if (attempt > 0)
                {
                    Debug.Log($"[DiseaseGenerationManager] 배치 생성 재시도 ({attempt}/{_maxRetries}), 3초 대기...");
                    await Awaitable.WaitForSecondsAsync(3f);
                }

                List<DiseaseData> results = await TryGenerateBatchFromAI(userPrompt);
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

        private async Awaitable<List<DiseaseData>> TryGenerateBatchFromAI(string userPrompt)
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
                    if (disease != null)
                        results.Add(disease);
                    else
                        Debug.LogWarning($"[DiseaseGenerationManager] 배치 내 {i}번째 환자 검증 실패, 건너뜀.");
                }

                return results;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[DiseaseGenerationManager] 배치 JSON 파싱 예외: {e.Message}");
                return null;
            }
        }

        private string BuildBatchUserPrompt(int count, int difficulty, string category, string baseId)
        {
            string categoryText = string.IsNullOrEmpty(category) ? "자유" : category;
            return $"환자 수: {count}명\n난이도: {difficulty}\n카테고리: {categoryText}\nbaseId: \"{baseId}\"\n" +
                   $"서로 다른 질병을 가진 환자 {count}명의 데이터를 diseases 배열에 생성해주세요.";
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
