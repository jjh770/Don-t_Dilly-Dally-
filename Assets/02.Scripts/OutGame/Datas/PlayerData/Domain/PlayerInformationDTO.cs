using System;
using System.Linq;
using Firebase.Firestore;

[FirestoreData]
public class PlayerInformationDTO 
{
    [FirestoreProperty]
    public string Nickname { get; set; }

    [FirestoreProperty]
    public string[] Hospital { get; set; }

    [FirestoreProperty]
    public DateTime[] Time { get; set; }

    // DTO → Domain
    public PlayerInformation ToDomain() => new PlayerInformation
    (   Nickname,
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
