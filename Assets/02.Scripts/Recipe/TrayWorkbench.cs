using UnityEngine;

namespace DontDillyDally.Data
{
    // 제작된 결과물을 트레이 위에 올리는 제조대입니다.
    // 트레이 생성, 적재, 회수, 제출 준비를 담당합니다.
    public class TrayWorkbench : MonoBehaviour
    {
        [Header("트레이 상태")]
        [Tooltip("제조대 위 현재 트레이 상태")]
        public SubmittedTray CurrentTray = new SubmittedTray();

        public bool HasTray => CurrentTray != null;

        public SubmittedTray CreateNewTray(bool isSterilized = false)
        {
            CurrentTray = new SubmittedTray
            {
                IsSterilized = isSterilized
            };

            return CurrentTray;
        }

        public bool TryPlaceItemOnTray(CraftedItem item)
        {
            if (CurrentTray == null)
                CreateNewTray();

            return CurrentTray.TryAddItem(item);
        }

        public bool TryPlaceBasicMaterialOnTray(
            CraftedMaterialType materialType,
            int playerId = 0)
        {
            CraftedItem item = CraftedItem.CreateBasicMaterial(materialType, playerId);
            if (item == null)
                return false;

            return TryPlaceItemOnTray(item);
        }

        public CraftedItem TakeLastItemFromTray()
        {
            if (CurrentTray == null)
                return null;

            return CurrentTray.TakeLastItem();
        }

        public void ClearTray()
        {
            if (CurrentTray == null)
                return;

            CurrentTray.ClearItems();
            CurrentTray.MarkContaminated();
        }

        public void LoadTray(SubmittedTray tray)
        {
            CurrentTray = tray ?? new SubmittedTray();
        }

        public SubmittedTray TakeTraySnapshot()
        {
            if (CurrentTray == null)
                return null;

            return CurrentTray.Clone();
        }

        public SubmittedTray TakeTrayAndReset()
        {
            SubmittedTray trayToSubmit = TakeTraySnapshot();
            CreateNewTray();
            return trayToSubmit;
        }
    }
}
