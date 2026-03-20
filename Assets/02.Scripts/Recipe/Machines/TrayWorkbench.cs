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

        public bool HasTray => GetResolvedTrayItem() != null;

        public SubmittedTray CurrentTray
        {
            get
            {
                TrayItem trayItem = GetResolvedTrayItem();
                return trayItem != null ? trayItem.TrayData : null;
            }
        }

        public void SetCurrentTrayItem(TrayItem trayItem)
        {
            CurrentTrayItem = trayItem;

            if (CurrentTrayItem != null)
            {
                CurrentTrayItem.EnsureTrayData();
            }
        }

        public bool CanPlaceTrayItem(TrayItem trayItem)
        {
            if (trayItem == null)
            {
                return false;
            }

            if (!trayItem.IsSterilizedTray)
            {
                return false;
            }

            if (HasTray)
            {
                return false;
            }

            return true;
        }

        public bool TrySetCurrentTrayItem(TrayItem trayItem)
        {
            if (!CanPlaceTrayItem(trayItem))
            {
                return false;
            }

            SetCurrentTrayItem(trayItem);
            return true;
        }

        public void ClearCurrentTrayItem(TrayItem trayItem = null)
        {
            if (trayItem == null || CurrentTrayItem == trayItem)
            {
                CurrentTrayItem = null;
            }
        }

        public bool TryPlaceItemOnTray(CraftedItem item)
        {
            TrayItem trayItem = GetResolvedTrayItem();
            if (item == null || trayItem == null)
            {
                return false;
            }

            return trayItem.TryAddItem(item);
        }

        public bool TryPlaceBasicMaterialOnTray(
            CraftedMaterialType materialType,
            int playerId = 0)
        {
            CraftedItem item = CraftedItem.CreateBasicMaterial(materialType, playerId);
            if (item == null)
            {
                return false;
            }

            return TryPlaceItemOnTray(item);
        }

        public CraftedItem TakeLastItemFromTray()
        {
            TrayItem trayItem = GetResolvedTrayItem();
            if (trayItem == null)
            {
                return null;
            }

            return trayItem.TakeLastItem();
        }

        public void ClearTray()
        {
            TrayItem trayItem = GetResolvedTrayItem();
            if (trayItem == null)
            {
                return;
            }

            trayItem.ClearItems();
            trayItem.SetTrayKindAndSync(TrayKind.Normal);
        }

        public void LoadTray(SubmittedTray tray)
        {
            TrayItem trayItem = GetResolvedTrayItem();
            if (trayItem == null)
            {
                return;
            }

            trayItem.LoadTrayData(tray);
        }

        public SubmittedTray TakeTraySnapshot()
        {
            TrayItem trayItem = GetResolvedTrayItem();
            if (trayItem == null)
            {
                return null;
            }

            return trayItem.GetTraySnapshot();
        }

        public SubmittedTray TakeTrayAndReset()
        {
            SubmittedTray trayToSubmit = TakeTraySnapshot();

            TrayItem trayItem = GetResolvedTrayItem();
            if (trayItem != null)
            {
                trayItem.ResetTrayData();
            }

            return trayToSubmit;
        }

        private TrayItem GetResolvedTrayItem()
        {
            if (CurrentTrayItem == null)
            {
                return null;
            }

            NetworkItemOwnership ownership = CurrentTrayItem.NetworkOwnership;
            if (ownership != null && ownership.IsHeld)
            {
                CurrentTrayItem = null;
                return null;
            }

            return CurrentTrayItem;
        }
    }
}
