using Cysharp.Threading.Tasks;
using Firebase.Firestore;
using UnityEngine;

public class RoomDataFirebaseRepository : IRoomDataRepository
{
    FirebaseFirestore _db;

    public RoomDataFirebaseRepository()
    {
        _db = FirebaseInitializer.Instance.Database;
    }

    private string COLLECTION_NAME = "RoomData";
    public async UniTask<RoomSaveData> Load(string roomCode)
    {
        try
        {
            var result = await _db.Collection(COLLECTION_NAME).Document(roomCode).GetSnapshotAsync();

            RoomSaveData data = result.ConvertTo<RoomSaveData>();
            Debug.LogFormat("불러오기 성공");
            if (data == null)
            {
                Debug.LogWarning("[RoomDataRepository] 불러온 데이터가 null 입니다. null을 반환합니다.");
                return null;
            }
            {
                return data;
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

            RoomSaveData data = result.ConvertTo<RoomSaveData>();
            Debug.LogFormat("불러오기 성공");
            if (data == null)
            {
                Debug.LogWarning("[RoomDataRepository] 존재하지 않는 방입니다.");
                return false;
            }
            {
                return true;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError("[RoomDataRepository] 불러온 데이터가 없습니다.:" + e);
            return false;
        }
    }

    public async UniTask Save(string roomCode, RoomSaveData saveData)
    {
        try
        {
            await _db.Collection(COLLECTION_NAME).Document(roomCode).SetAsync(saveData);
            Debug.Log("[RoomDataRepository] 저장 성공: ");
        }
        catch (System.Exception e)
        {
            Debug.LogError("[RoomDataRepository] 저장 실패: " + e);
        }
    }
}
