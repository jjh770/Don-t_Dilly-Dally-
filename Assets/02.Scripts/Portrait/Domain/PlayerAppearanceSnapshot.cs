using System.Collections.Generic;
using System.Linq;
using System.Text;

public sealed class PlayerAppearanceSnapshot
{
    private readonly Dictionary<CustomizingType, string> _equippedItemIds;

    public int ActorNumber { get; }
    public string Nickname { get; }
    public IReadOnlyDictionary<CustomizingType, string> EquippedItemIds => _equippedItemIds;

    public PlayerAppearanceSnapshot(int actorNumber, string nickname, Dictionary<CustomizingType, string> equippedItemIds)
    {
        ActorNumber = actorNumber;
        Nickname = nickname ?? string.Empty;
        _equippedItemIds = equippedItemIds != null
            ? new Dictionary<CustomizingType, string>(equippedItemIds)
            : new Dictionary<CustomizingType, string>();
    }

    public string CreateCacheKey()
    {
        StringBuilder builder = new StringBuilder();
        builder.Append(ActorNumber);
        builder.Append('|');

        foreach ((CustomizingType type, string itemId) in _equippedItemIds.OrderBy(pair => pair.Key))
        {
            builder.Append((int)type);
            builder.Append(':');
            builder.Append(itemId);
            builder.Append(';');
        }

        return builder.ToString();
    }
}
