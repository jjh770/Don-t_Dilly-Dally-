using System.Collections.Generic;
using UnityEngine;

namespace DontDillyDally.Data
{
    // 로컬 JSON에서 폴백 질병 데이터를 로드하는 유틸리티입니다.
    // AI API 통신 실패 시 이 로더를 통해 미리 준비된 질병 데이터를 사용합니다.
    public static class FallbackDiseaseLoader
    {
        private const string FallbackResourcePath = "FallbackRecipes/fallback_diseases";

        private static List<DiseaseData> s_cachedDiseases;

        // 모든 폴백 질병 데이터를 로드합니다. 결과는 캐싱됩니다.
        public static List<DiseaseData> LoadAll()
        {
            if (s_cachedDiseases != null)
                return s_cachedDiseases;

            var textAsset = Resources.Load<TextAsset>(FallbackResourcePath);
            if (textAsset == null)
            {
                Debug.LogError("[FallbackDiseaseLoader] 폴백 레시피 파일을 찾을 수 없습니다: " +
                               FallbackResourcePath);
                return new List<DiseaseData>();
            }

            var collection = JsonUtility.FromJson<FallbackDiseaseCollection>(textAsset.text);
            s_cachedDiseases = DiseaseConverter.ConvertAll(collection);

            Debug.Log($"[FallbackDiseaseLoader] 폴백 질병 {s_cachedDiseases.Count}개 로드 완료.");
            return s_cachedDiseases;
        }

        // 랜덤으로 1개의 폴백 질병 데이터를 반환합니다.
        public static DiseaseData GetRandom()
        {
            List<DiseaseData> all = LoadAll();
            if (all.Count == 0)
                return null;

            int index = Random.Range(0, all.Count);
            return all[index];
        }

        // ID로 특정 폴백 질병 데이터를 검색합니다.
        public static DiseaseData GetById(string diseaseId)
        {
            List<DiseaseData> all = LoadAll();

            for (int i = 0; i < all.Count; i++)
            {
                if (all[i].DiseaseId == diseaseId)
                    return all[i];
            }

            return null;
        }

        // 난이도로 필터링하여 폴백 질병 목록을 반환합니다.
        public static List<DiseaseData> GetByDifficulty(int difficulty)
        {
            List<DiseaseData> all = LoadAll();
            var results = new List<DiseaseData>();

            for (int i = 0; i < all.Count; i++)
            {
                if (all[i].Difficulty == difficulty)
                    results.Add(all[i]);
            }

            return results;
        }

        // 랜덤으로 count개의 폴백 질병 데이터를 반환합니다.
        public static List<DiseaseData> GetRandom(int count)
        {
            List<DiseaseData> all = LoadAll();
            if (all.Count == 0)
                return new List<DiseaseData>();

            // Fisher-Yates 셔플을 위해 복사본 생성
            var shuffled = new List<DiseaseData>(all);
            for (int i = shuffled.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (shuffled[i], shuffled[j]) = (shuffled[j], shuffled[i]);
            }

            int resultCount = Mathf.Min(count, shuffled.Count);
            return shuffled.GetRange(0, resultCount);
        }

        // 캐시를 초기화합니다. 에디터에서 JSON을 수정했을 때 사용합니다.
        public static void ClearCache()
        {
            s_cachedDiseases = null;
        }
    }
}
