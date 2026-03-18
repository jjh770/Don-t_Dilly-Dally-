using Cysharp.Threading.Tasks;
using UnityEngine;

public class RoomDataFirebaseRepository : IRoomDataRepository
{
    public async UniTask<RoomSaveData> Load()
    {
        await UniTask.Yield();
        return null;
    }

    public async UniTask Save(RoomSaveData saveData)
    {
        await UniTask.Yield();
    }
}
