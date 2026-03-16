using UnityEngine;

namespace DontDillyDally.Data
{
    // 실제 트레이 오브젝트에 붙는 상태 보관 컴포넌트입니다.
    // 멸균 여부와 트레이 위 재료 목록을 함께 관리합니다.
    public class TrayItem : ItemObject
    {
        [Header("트레이 상태")]
        [Tooltip("이 트레이가 들고 있는 실제 제출 데이터")]
        public SubmittedTray TrayData = new SubmittedTray();

        public bool HasTrayData => TrayData != null;

        public override void Initialize(string displayName, GameObject modelPrefab = null)
        {
            base.Initialize(displayName, modelPrefab);
            EnsureTrayData();
        }

        public void InitializeTray(
            string displayName,
            GameObject modelPrefab = null,
            bool isSterilized = false)
        {
            base.Initialize(displayName, modelPrefab);
            ResetTrayData(isSterilized);
        }

        public void EnsureTrayData()
        {
            if (TrayData == null)
                TrayData = new SubmittedTray();
        }

        public void ResetTrayData(bool isSterilized = false)
        {
            TrayData = new SubmittedTray
            {
                IsSterilized = isSterilized
            };
        }

        public void LoadTrayData(SubmittedTray trayData)
        {
            TrayData = trayData ?? new SubmittedTray();
        }

        public bool TryAddItem(CraftedItem item)
        {
            EnsureTrayData();
            return TrayData.TryAddItem(item);
        }

        public CraftedItem TakeLastItem()
        {
            EnsureTrayData();
            return TrayData.TakeLastItem();
        }

        public void ClearItems()
        {
            EnsureTrayData();
            TrayData.ClearItems();
        }

        public SubmittedTray GetTraySnapshot()
        {
            EnsureTrayData();
            return TrayData.Clone();
        }
    }
}
