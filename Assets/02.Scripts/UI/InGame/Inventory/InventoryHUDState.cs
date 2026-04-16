namespace DontDillyDally.UI
{
    // 인벤토리 HUD 상태
    public enum InventoryHUDState
    {
        Default,           // 기본 상태
        CanPickup,         // 집기 가능
        CanPush,           // 밀기 가능
        HoldingSmallItem,  // 작은 아이템 소지
        HoldingLargeItem   // 큰 아이템 소지 (밀기)
    }
}
