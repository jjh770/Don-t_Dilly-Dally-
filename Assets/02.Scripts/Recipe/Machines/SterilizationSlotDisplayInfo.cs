namespace DontDillyDally.Data
{
    public enum SterilizationSlotDisplayKind
    {
        Empty = 0,
        Material = 1,
        Tray = 2
    }

    // 멸균기 슬롯 UI 표시용 불변 정보입니다.
    // UI는 이 정보를 아이콘 스프라이트로 매핑합니다.
    public readonly struct SterilizationSlotDisplayInfo
    {
        public static readonly SterilizationSlotDisplayInfo Empty =
            new SterilizationSlotDisplayInfo(SterilizationSlotDisplayKind.Empty, CraftedMaterialType.None, TrayKind.Normal);

        public readonly SterilizationSlotDisplayKind Kind;
        public readonly CraftedMaterialType Material;
        public readonly TrayKind TrayKind;

        private SterilizationSlotDisplayInfo(
            SterilizationSlotDisplayKind kind,
            CraftedMaterialType material,
            TrayKind trayKind)
        {
            Kind = kind;
            Material = material;
            TrayKind = trayKind;
        }

        public static SterilizationSlotDisplayInfo CreateMaterial(CraftedMaterialType material)
        {
            return new SterilizationSlotDisplayInfo(
                SterilizationSlotDisplayKind.Material,
                material,
                TrayKind.Normal);
        }

        public static SterilizationSlotDisplayInfo CreateTray(TrayKind trayKind)
        {
            return new SterilizationSlotDisplayInfo(
                SterilizationSlotDisplayKind.Tray,
                CraftedMaterialType.None,
                trayKind);
        }
    }
}
