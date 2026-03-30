using Cysharp.Threading.Tasks;
using UnityEngine;

public interface IPlayerPortraitService
{
    UniTask<Sprite> GetOrCreateAsync(PlayerAppearanceSnapshot snapshot);
    bool TryGetCached(PlayerAppearanceSnapshot snapshot, out Sprite sprite);
    void Invalidate(int actorNumber);
    void ClearRoomCache();
}
