
// 장착/해제 시도 결과
public enum EEquipResult
{
    Equipped,               // 장착 성공
    Unequipped,             // 해제 성공
    AlreadyEquipped,        // 이미 장착됨
    Locked,                 // 잠금 상태
    InvalidItem,            // 유효하지 않은 아이템
    CannotUnequipRequired,  // 필수 카테고리 해제 불가
    CategoryMismatch,       // 카테고리 불일치
    AlreadyUnequipped       // 이미 해제됨
}

public static class EEquipResultExtensions
{
    // 성공 여부
    public static bool IsSuccess(this EEquipResult result)
    {
        return result == EEquipResult.Equipped ||
               result == EEquipResult.Unequipped ||
               result == EEquipResult.AlreadyEquipped ||
               result == EEquipResult.AlreadyUnequipped;
    }

    // 실제 변경 발생 여부
    public static bool HasChanged(this EEquipResult result)
    {
        return result == EEquipResult.Equipped ||
               result == EEquipResult.Unequipped;
    }
}
