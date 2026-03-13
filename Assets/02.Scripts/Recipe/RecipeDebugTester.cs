using System.Collections.Generic;
using UnityEngine;

namespace DontDillyDally.Data
{
    // 레시피 시스템을 플레이 모드에서 빠르게 확인하기 위한 디버그 테스터입니다.
    // 키 입력이나 컨텍스트 메뉴로 멸균, 적재, 제출 흐름을 테스트할 수 있습니다.
    public class RecipeDebugTester : MonoBehaviour
    {
        [Header("연동 대상")]
        [Tooltip("현재 테스트할 질병 데이터")]
        public DiseaseSO DiseaseSo;

        [Tooltip("트레이를 관리하는 제조대")]
        public TrayWorkbench TrayWorkbench;

        [Tooltip("트레이 멸균을 담당하는 기계")]
        public SterilizationMachine SterilizationMachine;

        [Header("테스트 설정")]
        [Tooltip("제출 시 사용할 환자 체력")]
        public float PatientHealth = 100f;

        [Tooltip("테스트용 플레이어 ID")]
        public int PlayerId = 1;

        private readonly TreatmentJudgeManager judgeManager = new TreatmentJudgeManager();

        private void Start()
        {
            ResetTestState();
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Alpha1))
                ResetTestState();

            if (Input.GetKeyDown(KeyCode.Alpha2))
                SterilizeCurrentTray();

            if (Input.GetKeyDown(KeyCode.Alpha3))
                AddAnestheticRecipe();

            if (Input.GetKeyDown(KeyCode.Alpha4))
                AddSecondRecipeSamples();

            if (Input.GetKeyDown(KeyCode.Alpha5))
                SubmitCurrentTray();
        }

        [ContextMenu("테스트 상태 초기화")]
        public void ResetTestState()
        {
            if (DiseaseSo == null)
            {
                Debug.LogWarning("[RecipeDebugTester] DiseaseSO가 연결되지 않았습니다.");
                return;
            }

            judgeManager.SetDisease(DiseaseSo.data);

            if (TrayWorkbench != null)
                TrayWorkbench.CreateNewTray();

            Debug.Log("[RecipeDebugTester] 테스트 상태를 초기화했습니다.");
            Debug.Log("[RecipeDebugTester] 1: 초기화, 2: 트레이 멸균, 3: 마취약 추가, 4: 2단계 샘플 추가, 5: 제출");
        }

        [ContextMenu("현재 트레이 멸균")]
        public void SterilizeCurrentTray()
        {
            if (TrayWorkbench == null || SterilizationMachine == null)
            {
                Debug.LogWarning("[RecipeDebugTester] TrayWorkbench 또는 SterilizationMachine이 연결되지 않았습니다.");
                return;
            }

            if (TrayWorkbench.CurrentTray == null)
                TrayWorkbench.CreateNewTray();

            bool success = SterilizationMachine.TrySterilizeTray(TrayWorkbench.CurrentTray);
            Debug.Log(success
                ? "[RecipeDebugTester] 현재 트레이를 멸균했습니다."
                : "[RecipeDebugTester] 트레이 멸균에 실패했습니다. 비어 있는 트레이만 멸균할 수 있습니다.");
        }

        [ContextMenu("마취약 1단계 샘플 추가")]
        public void AddAnestheticRecipe()
        {
            ClearTrayItemsOnly();
            AddItemToTray(CraftedMaterialType.AnestheticSyringe);
        }

        [ContextMenu("2단계 샘플 재료 추가")]
        public void AddSecondRecipeSamples()
        {
            ClearTrayItemsOnly();
            AddItemToTray(CraftedMaterialType.SterilizedScalpelGreen);
            AddItemToTray(CraftedMaterialType.SterilizedPincetteStraight);
            AddItemToTray(CraftedMaterialType.GauzeBox);
        }

        [ContextMenu("현재 트레이 제출")]
        public void SubmitCurrentTray()
        {
            if (TrayWorkbench == null)
            {
                Debug.LogWarning("[RecipeDebugTester] TrayWorkbench가 연결되지 않았습니다.");
                return;
            }

            SubmittedTray submittedTray = TrayWorkbench.TakeTraySnapshot();
            if (submittedTray == null)
            {
                Debug.LogWarning("[RecipeDebugTester] 제출할 트레이가 없습니다.");
                return;
            }

            TreatmentJudgeResult result = judgeManager.JudgeNextRecipe(submittedTray, PatientHealth);

            Debug.Log(
                $"[RecipeDebugTester] 제출 결과 - Success: {result.Success}, " +
                $"DiseaseCured: {result.DiseaseCured}, " +
                $"CompletedRecipeId: {result.CompletedRecipeId}, " +
                $"OverallProgress: {result.OverallProgress:0.00}, " +
                $"FailureReason: {result.FailureReason}");
        }

        [ContextMenu("현재 트레이 비우기")]
        public void ClearTrayItemsOnly()
        {
            if (TrayWorkbench == null)
            {
                Debug.LogWarning("[RecipeDebugTester] TrayWorkbench가 연결되지 않았습니다.");
                return;
            }

            if (TrayWorkbench.CurrentTray == null)
                TrayWorkbench.CreateNewTray();

            TrayWorkbench.CurrentTray.ClearItems();
            Debug.Log("[RecipeDebugTester] 현재 트레이의 재료를 비웠습니다.");
        }

        [ContextMenu("현재 질병 검증")]
        public void ValidateCurrentDisease()
        {
            if (DiseaseSo == null || DiseaseSo.data == null)
            {
                Debug.LogWarning("[RecipeDebugTester] 검증할 질병 데이터가 없습니다.");
                return;
            }

            bool isValid = DiseaseSo.data.Validate();
            Debug.Log(isValid
                ? "[RecipeDebugTester] 현재 질병 데이터 검증에 성공했습니다."
                : "[RecipeDebugTester] 현재 질병 데이터 검증에 실패했습니다.");
        }

        private void AddItemToTray(CraftedMaterialType materialType)
        {
            if (TrayWorkbench == null)
            {
                Debug.LogWarning("[RecipeDebugTester] TrayWorkbench가 연결되지 않았습니다.");
                return;
            }

            if (TrayWorkbench.CurrentTray == null)
                TrayWorkbench.CreateNewTray();

            CraftedItem item = new CraftedItem
            {
                MaterialType = materialType,
                UsedTool = ToolType.None,
                UsedAction = ActionType.None,
                SecondaryTool = ToolType.None,
                PreparedTime = Time.time,
                PreparedByPlayerId = PlayerId
            };

            bool success = TrayWorkbench.TryPlaceItemOnTray(item);
            Debug.Log(success
                ? $"[RecipeDebugTester] 트레이에 '{materialType}'를 올렸습니다."
                : "[RecipeDebugTester] 트레이에 재료를 올리지 못했습니다.");
        }
    }
}
