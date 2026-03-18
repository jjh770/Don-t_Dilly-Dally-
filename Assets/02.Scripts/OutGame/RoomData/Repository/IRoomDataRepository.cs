using Cysharp.Threading.Tasks;
using UnityEngine;

public interface IRoomDataRepository 
{
    public UniTask Save(RoomSaveData saveData);

    public UniTask<RoomSaveData> Load();
}
