using UnityEngine;

namespace DontDillyDally.Data
{
    // 트레이를 작업대에 올리고, 트레이에 재료를 적재하는 도메인 컴포넌트입니다.
    // 플레이어 입력이나 시각 연출은 다른 컴포넌트에서 담당합니다.
    public class TrayWorkbench : MonoBehaviour
    {
        [Header("작업대 상태")]
        [Tooltip("현재 작업대 위에 올라와 있는 트레이입니다.")]
        public TrayItem CurrentTrayItem;

        [Header("룰 설정")]
        [SerializeField] private CraftingRuleDatabase _ruleDatabase;

        public bool HasTray => GetResolvedTrayItem() != null;

        public void SetCurrentTrayItem(TrayItem trayItem)
        {
            CurrentTrayItem = trayItem;
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

        public bool CanPlaceItemOnTray(CraftedItem item)
        {
            TrayItem trayItem = GetResolvedTrayItem();
            if (item == null || trayItem == null)
            {
                return false;
            }

            if (!CanPlaceMaterialOnTray(item.MaterialType))
            {
                return false;
            }

            return trayItem.CanStoreItem(item);
        }

        public bool CanPlaceBasicMaterialOnTray(CraftedMaterialType materialType)
        {
            CraftedItem item = CraftedItem.CreateBasicMaterial(materialType);
            if (item == null)
            {
                return false;
            }

            return CanPlaceItemOnTray(item);
        }

        public void LoadTray(SubmittedTray tray)
        {
            TrayItem trayItem = GetResolvedTrayItem();
            if (trayItem == null)
            {
                return;
            }

            trayItem.ApplyTraySnapshot(tray);
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

        private bool CanPlaceMaterialOnTray(CraftedMaterialType materialType)
        {
            if (materialType == CraftedMaterialType.None || materialType == CraftedMaterialType.Unknown)
            {
                return false;
            }

            if (materialType.IsBasicMaterial())
            {
                return true;
            }

            if (_ruleDatabase == null)
            {
                return false;
            }

            return _ruleDatabase.ContainsResult(materialType);
        }
    }
}
