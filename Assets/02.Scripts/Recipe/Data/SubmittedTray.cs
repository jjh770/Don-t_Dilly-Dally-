using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Serialization;

namespace DontDillyDally.Data
{
    // 환자에게 제출되는 트레이 데이터입니다.
    // 트레이 종류와 결과물 목록을 함께 관리합니다.
    [Serializable]
    public class SubmittedTray
    {
        public const int MaxContainedItems = 4;

        [FormerlySerializedAs("Kind")]
        private TrayKind _kind = TrayKind.Normal;

        [FormerlySerializedAs("ContainedItems")]
        private List<CraftedItem> _containedItems = new List<CraftedItem>();

        public TrayKind Kind => _kind;
        public IReadOnlyList<CraftedItem> ContainedItems => _containedItems;
        public bool IsSterilizedTray => _kind == TrayKind.Sterilized;

        public bool CanAddItem()
        {
            return _containedItems != null && _containedItems.Count < MaxContainedItems;
        }

        public bool TryAddItem(CraftedItem item)
        {
            if (item == null || !CanAddItem())
            {
                return false;
            }

            _containedItems.Add(item);
            return true;
        }

        public CraftedItem TakeLastItem()
        {
            if (_containedItems == null || _containedItems.Count == 0)
            {
                return null;
            }

            int lastIndex = _containedItems.Count - 1;
            CraftedItem item = _containedItems[lastIndex];
            _containedItems.RemoveAt(lastIndex);
            return item;
        }

        public void SetTrayKind(TrayKind trayKind)
        {
            _kind = trayKind;
        }

        public void MarkSterilized()
        {
            _kind = TrayKind.Sterilized;
        }

        public bool HasAnyItems()
        {
            return _containedItems != null && _containedItems.Count > 0;
        }

        public List<CraftedMaterialType> GetContainedMaterialTypes()
        {
            return _containedItems?
                .Select(item => item.MaterialType)
                .ToList() ?? new List<CraftedMaterialType>();
        }

        public SubmittedTray Clone()
        {
            return new SubmittedTray
            {
                _kind = _kind,
                _containedItems = _containedItems != null
                    ? new List<CraftedItem>(_containedItems)
                    : new List<CraftedItem>()
            };
        }
    }
}
