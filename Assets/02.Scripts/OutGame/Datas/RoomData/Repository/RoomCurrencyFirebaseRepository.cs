using Cysharp.Threading.Tasks;
using Firebase.Firestore;
using UnityEngine;

public class RoomCurrencyFirebaseRepository : IRoomCurrencyRepository
{
    FirebaseFirestore _db;

    public RoomCurrencyFirebaseRepository(FirebaseFirestore db)
    {
        _db = db;
    }

    private string COLLECTION_NAME = "RoomCurrency";
    public async UniTask<RoomWallet> Load(string roomCode)
    {
        try
        {
            var result = await _db.Collection(COLLECTION_NAME).Document(roomCode).GetSnapshotAsync();

            RoomWalletDTO dto = result.ConvertTo<RoomWalletDTO >();
            Debug.LogFormat("[RoomDataRepository] 불러오기 성공");
            if (dto == null)
            {
                Debug.LogWarning("[RoomDataRepository] 불러온 데이터가 null 입니다. null을 반환합니다.");
                return null;
            }
            {
                return dto.ToDomain();
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError("[RoomDataRepository] 불러오기 실패, null을 반환합니다. :" + e);
            return null;
        }
    }

    public async UniTask<bool> IsExist(string roomCode)
    {
        try
        {
            var result = await _db.Collection(COLLECTION_NAME).Document(roomCode).GetSnapshotAsync();

            RoomWalletDTO  dto = result.ConvertTo<RoomWalletDTO >();
            if (dto == null)
            {
                return false;
            }
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("[RoomDataRepository] 불러온 데이터가 없습니다.:" + e);
            return false;
        }
    }

    public async UniTask Save(string roomCode, RoomWallet  wallet)
    {
        try
        {
            var dto = RoomWalletDTO.FromDomain(wallet);
            await _db.Collection(COLLECTION_NAME).Document(roomCode).SetAsync(dto);
            Debug.Log("[RoomDataRepository] 저장 성공");
        }
        catch (System.Exception e)
        {
            Debug.LogError("[RoomDataRepository] 저장 실패: " + e);
        }
    }

}
