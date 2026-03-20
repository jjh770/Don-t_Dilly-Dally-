using Cysharp.Threading.Tasks;
using Firebase.Firestore;
using Photon.Pun.Demo.Asteroids;
using UnityEngine;

public class PlayerInformationFirebaseRepository : IPlayerInformationRepository
{
    FirebaseFirestore _db;

    private string COLLECTION_NAME = "PlayerInformation";

    public PlayerInformationFirebaseRepository()
    {
        _db = FirebaseInitializer.Instance.Database;
    }

    public async UniTask<PlayerInformation> Load(string account)
    {
        try
        {
            var result = await _db.Collection(COLLECTION_NAME).Document(account).GetSnapshotAsync();

            PlayerInformationDTO dto = result.ConvertTo<PlayerInformationDTO>();
            Debug.LogFormat("[PlayerInformationFirebaseRepository] 불러오기 성공");
            if (dto == null)
            {
                Debug.LogWarning("[PlayerInformationFirebaseRepository] 불러온 데이터가 null 입니다. null을 반환합니다.");
                return null;
            }
            {
                return dto.ToDomain();
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError("[PlayerInformationFirebaseRepository] 불러오기 실패, null을 반환합니다. :" + e);
            return null;
        }
    }

    public async UniTask Save(string account, PlayerInformation saveData)
    {
        try
        {
            var dto = PlayerInformationDTO.FromDomain(saveData);
            await _db.Collection(COLLECTION_NAME).Document(account).SetAsync(dto);
            Debug.Log("[PlayerInformationFirebaseRepository] 저장 성공");
        }
        catch (System.Exception e)
        {
            Debug.LogError("[PlayerInformationFirebaseRepository] 저장 실패: " + e);
        }
    }  
}
