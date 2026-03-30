using System.Collections.Generic;
using Photon.Pun;
using Photon.Realtime;

public class PhotonPlayerAppearanceSource : IPlayerAppearanceSource
{
    private readonly ICustomizingManager _customizingManager;

    public PhotonPlayerAppearanceSource(ICustomizingManager customizingManager)
    {
        _customizingManager = customizingManager;
    }

    public PlayerAppearanceSnapshot Create(Player player)
    {
        if (player == null)
        {
            return null;
        }

        Dictionary<CustomizingType, string> equippedItemIds;
        if (player == PhotonNetwork.LocalPlayer && _customizingManager != null && _customizingManager.IsInitialized)
        {
            equippedItemIds = _customizingManager.GetEquippedItemIds();
        }
        else
        {
            equippedItemIds = CustomizingProperties.GetPlayerCustomizing(player);
        }

        return new PlayerAppearanceSnapshot(
            player.ActorNumber,
            PlayerProperty.GetNickname(player),
            equippedItemIds);
    }
}
