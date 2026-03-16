using UnityEngine;

namespace DontDillyDally.Data
{
    // 레시피 시스템을 플레이 모드에서 빠르게 확인하기 위한 디버그 테스트입니다.
    public class RecipeDebugTester : MonoBehaviour
    {
        [Header("연동 대상")]
        [Tooltip("현재 테스트할 질병 데이터")]
        public DiseaseSO DiseaseSo;

        [Tooltip("트레이 위에 재료를 올리는 제조대")]
        public TrayWorkbench TrayWorkbench;

        [Tooltip("실제 제출 데이터를 들고 있는 트레이 아이템")]
        public TrayItem TrayItem;

        [Tooltip("트레이 멸균에 사용하는 기계")]
        public SterilizationMachine SterilizationMachine;

        [Header("테스트 설정")]
        [Tooltip("제출 때 사용할 환자 체력")]
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
            if (Input.GetKeyDown(KeyCode.Q))
                ResetTestState();

            if (Input.GetKeyDown(KeyCode.W))
                SterilizeCurrentTray();

            if (Input.GetKeyDown(KeyCode.E))
                AddAnestheticRecipe();

            if (Input.GetKeyDown(KeyCode.R))
                AddSecondRecipeSamples();

            if (Input.GetKeyDown(KeyCode.T))
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

            if (TrayItem != null)
                TrayItem.ResetTrayData();

            BindTrayToWorkbench();
        }

        [ContextMenu("현재 트레이 멸균")]
        public void SterilizeCurrentTray()
        {
            if (TrayItem == null || SterilizationMachine == null)
            {
                Debug.LogWarning("[RecipeDebugTester] TrayItem 또는 SterilizationMachine이 연결되지 않았습니다.");
                return;
            }

            TrayItem.EnsureTrayData();
            SterilizationMachine.TrySterilizeTray(TrayItem);
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
            if (TrayItem == null)
            {
                Debug.LogWarning("[RecipeDebugTester] TrayItem이 연결되지 않았습니다.");
                return;
            }

            SubmittedTray submittedTray = TrayItem.GetTraySnapshot();
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
            if (TrayItem == null)
            {
                Debug.LogWarning("[RecipeDebugTester] TrayItem이 연결되지 않았습니다.");
                return;
            }

            TrayItem.EnsureTrayData();
            TrayItem.ClearItems();
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

        private void BindTrayToWorkbench()
        {
            if (TrayWorkbench == null || TrayItem == null)
                return;

            TrayWorkbench.SetCurrentTrayItem(TrayItem);
        }

        private void AddItemToTray(CraftedMaterialType materialType)
        {
            if (TrayWorkbench == null || TrayItem == null)
            {
                Debug.LogWarning("[RecipeDebugTester] TrayWorkbench 또는 TrayItem이 연결되지 않았습니다.");
                return;
            }

            BindTrayToWorkbench();

            CraftedItem item = new CraftedItem
            {
                MaterialType = materialType,
                UsedTool = ToolType.None,
                UsedAction = ActionType.None,
                SecondaryTool = ToolType.None,
                PreparedTime = Time.time,
                PreparedByPlayerId = PlayerId
            };

            TrayWorkbench.TryPlaceItemOnTray(item);
        }
    }
}
