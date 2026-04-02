
using System.Collections.Generic;
using System.Linq;
using Firebase.Firestore;

[FirestoreData]
public class RoomWalletDTO  
{
    [FirestoreProperty]
    public int Coin { get; set; }

    [FirestoreProperty]
    public Dictionary<string, int> StageStars { get; set; } = new Dictionary<string, int>();

    [FirestoreProperty]
    public int Level { get; set; }

    // DTO → Domain
    public RoomWallet ToDomain()
    {
        var stars = StageStars.ToDictionary(
            kvp => kvp.Key,
            kvp => new StageStars(kvp.Value)
        );

        return new RoomWallet(
        new RoomCurrency(ERoomCurrencyType.Coin, Coin),
        stars,
        new HospitalLevel(Level));
    }

    // Domain → DTO
    public static RoomWalletDTO FromDomain(RoomWallet wallet) => new RoomWalletDTO
    {
        Coin = wallet.Coin.Value,
        StageStars = wallet.StagesStars.ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value.Best
            ),
        Level = wallet.HospitalLevel.Value 
    };

}
