using System;
using System.Linq;
using Firebase.Firestore;

[FirestoreData]
public class PlayerInformationDTO 
{
    [FirestoreProperty]
    public string Name { get; set; }

    [FirestoreProperty]
    public string[] Hospital { get; set; }

    [FirestoreProperty]
    public DateTime[] Time { get; set; }

    // DTO → Domain
    public PlayerInformation ToDomain() => new PlayerInformation
    (   Name,
        Hospital.Zip(Time, (hospital, time) =>
            new MyHospital(hospital, time))
        .ToArray()
    );
        

    // Domain → DTO
    public static PlayerInformationDTO FromDomain(PlayerInformation information) => new PlayerInformationDTO
    {
        Name = information.Name,
        Hospital = information
        .GetMyHospitals.Select(hospital => hospital.Name).ToArray(),
        Time = information
        .GetMyHospitals.Select(hospital => hospital.Time).ToArray()
    };
}