using UnityEngine;

namespace DontDillyDally.Data
{
    // 트레이 위에 준비된 재료를 올리는 제조대입니다.
    // 실제 트레이 데이터는 TrayItem이 보관하고, 제조대는 적재만 담당합니다.
    public class TrayWorkbench : MonoBehaviour
    {
        [Header("제조대 상태")]
        [Tooltip("현재 이 제조대 위에 올라와 있는 트레이 아이템")]
        public TrayItem CurrentTrayItem;

        public bool HasTray => CurrentTrayItem != null;

        public SubmittedTray CurrentTray
        {
            get
            {
                return CurrentTrayItem != null ? CurrentTrayItem.TrayData : null;
            }
        }

        public void SetCurrentTrayItem(TrayItem trayItem)
        {
            CurrentTrayItem = trayItem;

            if (CurrentTrayItem != null)
                CurrentTrayItem.EnsureTrayData();
        }

        public bool TryPlaceItemOnTray(CraftedItem item)
        {
            if (item == null || CurrentTrayItem == null)
                return false;

            return CurrentTrayItem.TryAddItem(item);
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
            if (CurrentTrayItem == null)
                return null;

            return CurrentTrayItem.TakeLastItem();
        }

        public void ClearTray()
        {
            if (CurrentTrayItem == null)
                return;

            CurrentTrayItem.ClearItems();
            CurrentTrayItem.TrayData.MarkContaminated();
        }

        public void LoadTray(SubmittedTray tray)
        {
            if (CurrentTrayItem == null)
                return;

            CurrentTrayItem.LoadTrayData(tray);
        }

        public SubmittedTray TakeTraySnapshot()
        {
            if (CurrentTrayItem == null)
                return null;

            return CurrentTrayItem.GetTraySnapshot();
        }

        public SubmittedTray TakeTrayAndReset()
        {
            SubmittedTray trayToSubmit = TakeTraySnapshot();

            if (CurrentTrayItem != null)
                CurrentTrayItem.ResetTrayData();

            return trayToSubmit;
        }
    }
}
