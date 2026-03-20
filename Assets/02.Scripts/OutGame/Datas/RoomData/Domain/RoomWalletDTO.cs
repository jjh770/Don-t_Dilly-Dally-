
using Firebase.Firestore;

[FirestoreData]
public class RoomWalletDTO  
{
    [FirestoreProperty]
    public int Money { get; set; }

    [FirestoreProperty]
    public int Star { get; set; }

    // DTO → Domain
    public RoomWallet ToDomain() => new RoomWallet(new[] {
        new RoomCurrency(ERoomCurrencyType.Money, Money),
        new RoomCurrency(ERoomCurrencyType.Star,  Star),
    });

    // Domain → DTO
    public static RoomWalletDTO FromDomain(RoomWallet wallet) => new RoomWalletDTO
    {
        Money = wallet.Get(ERoomCurrencyType.Money).Value,
        Star = wallet.Get(ERoomCurrencyType.Star).Value,
    };

}
