
using System.Collections.Generic;
using System.Linq;
using Firebase.Firestore;

[FirestoreData]
public class RoomWalletDTO  
{
    [FirestoreProperty]
    public int Money { get; set; }

    [FirestoreProperty]
    public Dictionary<string, int> StageStars { get; set; } = new Dictionary<string, int>();

    // DTO → Domain
    public RoomWallet ToDomain()
    {
        var stars = StageStars.ToDictionary(
            kvp => kvp.Key,
            kvp => new StageStars(kvp.Value)
        );

        return new RoomWallet(
        new RoomCurrency(ERoomCurrencyType.Money, Money),
        stars
        );
    }

    // Domain → DTO
    public static RoomWalletDTO FromDomain(RoomWallet wallet) => new RoomWalletDTO
    {
        Money = wallet.Money.Value,
        StageStars = wallet.StagesStars.ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value.Best
            )
    };

}
