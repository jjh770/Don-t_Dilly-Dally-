using System;
using System.Linq;
using Cysharp.Threading.Tasks;
using Firebase.Firestore;
using UnityEngine;

public class FirebasePlayerInformationRepository : IPlayerInformationRepository
{
    FirebaseFirestore _db;

    private readonly string _id;

    private const string CollectionName = "users";
    private const string SubCollectionName = "data";
    private const string DataDocumentId = "player-information";

    public FirebasePlayerInformationRepository(FirebaseFirestore db, string userId)
    {
        _db = db;
        _id = userId;
    }

    public async UniTask<PlayerInformation> Load()
    {
        try
        {
            var result = await _db.Collection(CollectionName)
                          .Document(_id)
                          .Collection(SubCollectionName)
                          .Document(DataDocumentId)
                          .GetSnapshotAsync();

            PlayerInformationDTO dto = result.ConvertTo<PlayerInformationDTO>();
            Debug.LogFormat("[FirebasePlayerInformationRepository] 불러오기 성공");
            if (dto == null)
            {
                Debug.LogWarning("[FirebasePlayerInformationRepository] 불러온 데이터가 null 입니다. null을 반환합니다.");
                return null;
            }
            {
                return dto.ToDomain();
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("[FirebasePlayerInformationRepository] 불러오기 실패, null을 반환합니다. :" + e);
            return null;
        }
    }

    public async UniTask Save(PlayerInformation saveData)
    {
        try
        {
            var dto = PlayerInformationDTO.FromDomain(saveData);
            await _db.Collection(CollectionName)
                          .Document(_id)
                          .Collection(SubCollectionName)
                          .Document(DataDocumentId)
                          .SetAsync(dto);
            Debug.Log("[FirebasePlayerInformationRepository] 저장 성공");
        }
        catch (System.Exception e)
        {
            Debug.LogError("[FirebasePlayerInformationRepository] 저장 실패: " + e);
        }
    }

    [FirestoreData]
    private class PlayerInformationDTO
    {
        [FirestoreProperty]
        public string Nickname { get; set; }

        [FirestoreProperty]
        public string[] Hospital { get; set; }

        [FirestoreProperty]
        public DateTime[] Time { get; set; }

        // DTO → Domain
        public PlayerInformation ToDomain() => new PlayerInformation
        (Nickname,
            Hospital.Zip(Time, (hospital, time) =>
                new MyHospital(hospital, time))
            .ToArray()
        );


        // Domain → DTO
        public static PlayerInformationDTO FromDomain(PlayerInformation information) => new PlayerInformationDTO
        {
            Nickname = information.Nickname,
            Hospital = information
            .MyHospitals.Select(hospital => hospital.Name).ToArray(),
            Time = information
            .MyHospitals.Select(hospital => hospital.Time).ToArray()
        };
    }


}
