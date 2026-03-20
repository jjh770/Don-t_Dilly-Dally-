using UnityEngine;

namespace DontDillyDally.Data
{
    // AI 질병 생성 테스트용 컴포넌트입니다.
    // Inspector에서 난이도/카테고리를 설정하고 [ContextMenu]로 테스트할 수 있습니다.
    public class DiseaseGenerationTester : MonoBehaviour
    {
        [Header("매니저 연결")]
        [SerializeField] private DiseaseGenerationManager _manager;

        [Header("테스트 설정")]
        [SerializeField] private int _testDifficulty = 2;
        [SerializeField] private string _testCategory = "";

        // Inspector에서 우클릭 → "AI 질병 생성 테스트" 클릭
        [ContextMenu("AI 질병 생성 테스트")]
        private async void TestGenerate()
        {
            if (_manager == null)
            {
                Debug.LogError("[Tester] DiseaseGenerationManager가 연결되지 않았습니다.");
                return;
            }

            Debug.Log($"[Tester] AI 질병 생성 시작... (난이도: {_testDifficulty}, 카테고리: {(string.IsNullOrEmpty(_testCategory) ? "자유" : _testCategory)})");

            string category = string.IsNullOrEmpty(_testCategory) ? null : _testCategory;
            DiseaseData disease = await _manager.GenerateDisease(_testDifficulty, category);

            if (disease == null)
            {
                Debug.LogError("[Tester] 질병 생성 실패 (null 반환).");
                return;
            }

            Debug.Log("========== 생성 결과 ==========");
            Debug.Log($"ID: {disease.DiseaseId}");
            Debug.Log($"질병명: {disease.DiseaseName}");
            Debug.Log($"환자: {disease.PatientName}");
            Debug.Log($"배경: {disease.Backstory}");
            Debug.Log($"환자 대사: {disease.PatientQuote}");
            Debug.Log($"카테고리: {disease.Category} | 난이도: {disease.Difficulty} | 제한시간: {disease.TimeLimitSec}초");
            Debug.Log($"권장인원: {disease.RecommendedPlayers} | 체력감소: {disease.FailHealthPenalty}");
            Debug.Log($"성공: {disease.SuccessLine}");
            Debug.Log($"실패: {disease.FailLine}");
            Debug.Log($"출처: {disease.Source}");
            Debug.Log($"레시피 수: {disease.Recipes.Count}");

            for (int i = 0; i < disease.Recipes.Count; i++)
            {
                RecipeData recipe = disease.Recipes[i];
                string materials = string.Join(", ", recipe.RequiredMaterials);
                Debug.Log($"  [{recipe.Order}] {recipe.DisplayName}: {materials}");
            }

            Debug.Log("================================");
        }

        // 폴백 데이터만 테스트 (API 키 없이도 가능)
        [ContextMenu("폴백 데이터 테스트")]
        private void TestFallback()
        {
            DiseaseData disease = FallbackDiseaseLoader.GetRandom();

            if (disease == null)
            {
                Debug.LogError("[Tester] 폴백 데이터 로드 실패.");
                return;
            }

            Debug.Log("========== 폴백 데이터 ==========");
            Debug.Log($"ID: {disease.DiseaseId} | 질병명: {disease.DiseaseName}");
            Debug.Log($"환자: {disease.PatientName} | 난이도: {disease.Difficulty}");
            Debug.Log($"레시피 수: {disease.Recipes.Count}");

            for (int i = 0; i < disease.Recipes.Count; i++)
            {
                RecipeData recipe = disease.Recipes[i];
                string materials = string.Join(", ", recipe.RequiredMaterials);
                Debug.Log($"  [{recipe.Order}] {recipe.DisplayName}: {materials}");
            }

            Debug.Log("==================================");
        }
    }
}
