using Cysharp.Threading.Tasks;
using Firebase.Firestore;
using UnityEngine;

public class PlayerInformationTestRepository : IPlayerInformationRepository
{
    FirebaseFirestore _db;

    private string COLLECTION_NAME = "PlayerInformation";

    public PlayerInformationTestRepository(FirebaseFirestore db)
    {
        _db = db;
    }

    public async UniTask<PlayerInformation> Load(string account)
    {
        try
        {
            var result = await _db.Collection(COLLECTION_NAME).Document(account).GetSnapshotAsync();

            PlayerInformationDTO dto = result.ConvertTo<PlayerInformationDTO>();
            Debug.LogFormat("[PlayerInformationTestRepository] 불러오기 성공");
            if (dto == null)
            {
                Debug.LogWarning("[PlayerInformationTestRepository] 불러온 데이터가 null 입니다. null을 반환합니다.");
                return null;
            }

            dto.Nickname = PlayerPrefs.GetString($"{account}.nickname", "Player");

            return dto.ToDomain();
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("[PlayerInformationTestRepository] 불러오기 실패, null을 반환합니다. :" + e);
            return null;
        }
    }

    public async UniTask Save(string account, PlayerInformation saveData)
    {
        try
        {
            var dto = PlayerInformationDTO.FromDomain(saveData);
            PlayerPrefs.SetString($"{account}.nickname", dto.Nickname);
            await _db.Collection(COLLECTION_NAME).Document(account).SetAsync(dto);
            Debug.Log("[PlayerInformationTestRepository] 저장 성공");
        }
        catch (System.Exception e)
        {
            Debug.LogError("[PlayerInformationTestRepository] 저장 실패: " + e);
        }
    }

}
