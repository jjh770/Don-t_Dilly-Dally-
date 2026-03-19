using System;
using System.Collections.Generic;

namespace DontDillyDally.Data
{
    // AI API 응답 또는 로컬 JSON을 파싱하는 데이터 전송 객체입니다.
    // JsonUtility로 역직렬화한 뒤 DiseaseConverter를 통해 DiseaseData로 변환합니다.
    [Serializable]
    public class DiseaseDTO
    {
        public string diseaseId;
        public string diseaseName;
        public string description;
        public string patientName;
        public string backstory;
        public string patientQuote;
        public string category;
        public int difficulty;
        public float timeLimitSec;
        public int recommendedPlayers;
        public string successLine;
        public string failLine;
        public float failHealthPenalty;
        public List<RecipeStepDTO> recipes;
    }

    // 레시피 단계 DTO입니다.
    // requiredMaterials는 CraftedMaterialType Enum의 문자열 이름 배열입니다.
    [Serializable]
    public class RecipeStepDTO
    {
        public string recipeId;
        public string displayName;
        public string[] requiredMaterials;
        public int order;
        public bool requiresSterilizedTray;
    }

    // JsonUtility용 래퍼입니다. 최상위 배열 직렬화를 지원하지 않으므로
    // { "diseases": [...] } 형태로 감싸야 합니다.
    [Serializable]
    public class FallbackDiseaseCollection
    {
        public List<DiseaseDTO> diseases;
    }
}
