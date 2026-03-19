using System.Linq;
using Firebase.Firestore;

[FirestoreData]
public class PlayerInformationDTO 
{
    [FirestoreProperty]
    public string Name { get; set; }

    [FirestoreProperty]
    public string[] Hospital { get; set; }

    // DTO → Domain
    public PlayerInformation ToDomain() => new PlayerInformation
    ( Name, Hospital
        .Select(hospital => new MyHospital(hospital))
        .ToArray() 
    );
        

    // Domain → DTO
    public static PlayerInformationDTO FromDomain(PlayerInformation information) => new PlayerInformationDTO
    {
        Name = information.Name,
        Hospital = information
        .GetMyHospitals.Select(hospital => hospital.Name).ToArray()
    };
}