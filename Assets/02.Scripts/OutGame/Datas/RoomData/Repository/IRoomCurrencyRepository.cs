using Cysharp.Threading.Tasks;
using UnityEngine;

public interface IRoomCurrencyRepository 
{
    public UniTask Save(string roomCode, RoomWallet  wallet);

    public UniTask<RoomWallet> Load(string roomCode);

    public UniTask<bool> IsExist(string roomCode);

}
