using Photon.Realtime;

public interface IPlayerAppearanceSource
{
    PlayerAppearanceSnapshot Create(Player player);
}
