public static class CustomizingTypeExtensions
{
    public static bool CanUnequip(this CustomizingType type)
    {
        switch (type)
        {
            // Unequip 가능
            case CustomizingType.Hat:
            case CustomizingType.FaceAccessory:
            case CustomizingType.Glasses:
            case CustomizingType.Shoes:
            case CustomizingType.Costumes:
                return true;

            // Unequip 불가
            case CustomizingType.SkinColor:
            case CustomizingType.HairStyle:
            case CustomizingType.Faces:
            default:
                return false;
        }
    }

    public static bool IsRequired(this CustomizingType type)
    {
        return !CanUnequip(type);
    }
}
