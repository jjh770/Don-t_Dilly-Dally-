using System;
using System.Collections.Generic;
using System.Linq;

namespace DontDillyDally.Data
{
    // 환자에게 제출되는 트레이 데이터입니다.
    // 트레이 종류와 결과물 목록을 함께 관리합니다.
    [Serializable]
    public class SubmittedTray
    {
        public const int MaxContainedItems = 4;

        public TrayKind Kind = TrayKind.Normal;
        public List<CraftedItem> ContainedItems = new List<CraftedItem>();

        public int ContainedItemCount => ContainedItems?.Count ?? 0;
        public bool IsSterilizedTray => Kind == TrayKind.Sterilized;

        public bool CanAddItem()
        {
            return ContainedItems != null && ContainedItems.Count < MaxContainedItems;
        }

        public bool TryAddItem(CraftedItem item)
        {
            if (item == null || !CanAddItem())
            {
                return false;
            }

            ContainedItems.Add(item);
            return true;
        }

        public CraftedItem TakeLastItem()
        {
            if (ContainedItems == null || ContainedItems.Count == 0)
            {
                return null;
            }

            int lastIndex = ContainedItems.Count - 1;
            CraftedItem item = ContainedItems[lastIndex];
            ContainedItems.RemoveAt(lastIndex);
            return item;
        }

        public void SetTrayKind(TrayKind trayKind)
        {
            Kind = trayKind;
        }

        public void MarkSterilized()
        {
            Kind = TrayKind.Sterilized;
        }

        public void MarkContaminated()
        {
            Kind = TrayKind.Normal;
        }

        public bool HasAnyItems()
        {
            return ContainedItems != null && ContainedItems.Count > 0;
        }

        public List<CraftedMaterialType> GetContainedMaterialTypes()
        {
            return ContainedItems?
                .Select(item => item.MaterialType)
                .ToList() ?? new List<CraftedMaterialType>();
        }

        public SubmittedTray Clone()
        {
            return new SubmittedTray
            {
                Kind = Kind,
                ContainedItems = ContainedItems != null
                    ? new List<CraftedItem>(ContainedItems)
                    : new List<CraftedItem>()
            };
        }
    }
}
