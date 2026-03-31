public interface ICustomizingItemSpec
{
    string ItemId { get; }
    string DisplayName { get; }
    CustomizingType Category { get; }
    bool IsDefault { get; }
    int SortOrder { get; }
}
