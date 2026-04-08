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
        private static Dictionary<int, List<DiseaseData>> s_cachedByDifficulty;

        // 모든 폴백 질병 데이터를 로드합니다. 결과는 캐싱됩니다.
        public static List<DiseaseData> LoadAll()
        {
            if (s_cachedDiseases != null)
                return s_cachedDiseases;

            TextAsset textAsset = Resources.Load<TextAsset>(FallbackResourcePath);
            if (textAsset == null)
            {
                Debug.LogError("[FallbackDiseaseLoader] 폴백 레시피 파일을 찾을 수 없습니다: " +
                               FallbackResourcePath);
                return new List<DiseaseData>();
            }

            FallbackDiseaseCollection collection = JsonUtility.FromJson<FallbackDiseaseCollection>(textAsset.text);
            s_cachedDiseases = DiseaseConverter.ConvertAll(collection);
            BuildDifficultyCache();

            Debug.Log($"[FallbackDiseaseLoader] 폴백 질병 {s_cachedDiseases.Count}개 로드 완료.");
            return s_cachedDiseases;
        }

        // 랜덤으로 1개의 폴백 질병 데이터를 반환합니다.
        public static DiseaseData GetRandom()
        {
            List<DiseaseData> all = LoadAll();
            if (all.Count == 0)
                return null;

            return all[Random.Range(0, all.Count)];
        }

        public static DiseaseData GetRandom(string stageId)
        {
            List<DiseaseData> filtered = GetByStage(stageId);
            if (filtered.Count > 0)
            {
                return filtered[Random.Range(0, filtered.Count)];
            }

            return GetRandom();
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

        // 난이도로 필터링하여 폴백 질병 목록을 반환합니다. 결과는 캐싱됩니다.
        public static List<DiseaseData> GetByDifficulty(int difficulty)
        {
            LoadAll();

            if (s_cachedByDifficulty != null && s_cachedByDifficulty.TryGetValue(difficulty, out List<DiseaseData> cached))
                return cached;

            return new List<DiseaseData>();
        }

        public static List<DiseaseData> GetByStage(string stageId)
        {
            List<DiseaseData> all = LoadAll();
            if (string.IsNullOrEmpty(stageId))
                return all;

            var results = new List<DiseaseData>();
            for (int i = 0; i < all.Count; i++)
            {
                if (StagePromptHelper.IsDiseaseAllowedForStage(all[i], stageId))
                {
                    results.Add(all[i]);
                }
            }

            return results;
        }

        public static List<DiseaseData> GetByStageAndDifficulty(string stageId, int difficulty)
        {
            List<DiseaseData> stageDiseases = GetByStage(stageId);
            var results = new List<DiseaseData>();

            for (int i = 0; i < stageDiseases.Count; i++)
            {
                if (stageDiseases[i].Difficulty == difficulty)
                {
                    results.Add(stageDiseases[i]);
                }
            }

            return results;
        }

        // 랜덤으로 count개의 폴백 질병 데이터를 반환합니다.
        public static List<DiseaseData> GetRandom(int count)
        {
            List<DiseaseData> all = LoadAll();
            if (all.Count == 0)
                return new List<DiseaseData>();

            int resultCount = Mathf.Min(count, all.Count);

            // Fisher-Yates 부분 셔플 — 전체 복사 없이 필요한 수만 셔플합니다.
            var indices = new int[all.Count];
            for (int i = 0; i < indices.Length; i++)
                indices[i] = i;

            var results = new List<DiseaseData>(resultCount);
            for (int i = 0; i < resultCount; i++)
            {
                int j = Random.Range(i, indices.Length);
                (indices[i], indices[j]) = (indices[j], indices[i]);
                results.Add(all[indices[i]]);
            }

            return results;
        }

        // 캐시를 초기화합니다. 에디터에서 JSON을 수정했을 때 사용합니다.
        public static void ClearCache()
        {
            s_cachedDiseases = null;
            s_cachedByDifficulty = null;
        }

        private static void BuildDifficultyCache()
        {
            s_cachedByDifficulty = new Dictionary<int, List<DiseaseData>>();

            if (s_cachedDiseases == null)
                return;

            for (int i = 0; i < s_cachedDiseases.Count; i++)
            {
                DiseaseData disease = s_cachedDiseases[i];
                if (!s_cachedByDifficulty.TryGetValue(disease.Difficulty, out List<DiseaseData> list))
                {
                    list = new List<DiseaseData>();
                    s_cachedByDifficulty[disease.Difficulty] = list;
                }

                list.Add(disease);
            }
        }
    }
}
