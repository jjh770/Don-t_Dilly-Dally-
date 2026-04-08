using System;
using System.Collections.Generic;

namespace DontDillyDally.Data
{
    // AI API 응답 또는 로컬 JSON을 파싱하는 데이터 전송 객체입니다.
    // JsonUtility로 역직렬화한 뒤 DiseaseConverter를 통해 DiseaseData로 변환합니다.
    //
    // [명명 규칙 예외] public 필드가 camelCase인 이유:
    // Unity JsonUtility는 필드 이름과 JSON 키를 1:1 매칭하므로
    // JSON 키 형식(camelCase)을 그대로 따라야 합니다.
    [Serializable]
    public class DiseaseDTO
    {
        public string stageId;
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
    //
    // [명명 규칙 예외] JsonUtility 직렬화를 위해 camelCase 필드명을 사용합니다.
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
    //
    // [명명 규칙 예외] JsonUtility 직렬화를 위해 camelCase 필드명을 사용합니다.
    [Serializable]
    public class FallbackDiseaseCollection
    {
        public List<DiseaseDTO> diseases;
    }

    // AI 다건 생성 응답용 래퍼입니다.
    // 폴백 래퍼와 동일한 구조이지만 용도가 다르므로 별도 타입으로 분리합니다.
    //
    // [명명 규칙 예외] JsonUtility 직렬화를 위해 camelCase 필드명을 사용합니다.
    [Serializable]
    public class GeneratedDiseaseCollection
    {
        public List<DiseaseDTO> diseases;
    }
}
