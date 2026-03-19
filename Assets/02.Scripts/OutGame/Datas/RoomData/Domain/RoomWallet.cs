
using System;
using System.Collections.Generic;
using System.Linq;

public class RoomWallet
{
    private readonly Dictionary<ERoomCurrencyType, RoomCurrency> _currencies;

    private RoomWallet()
    {
        _currencies = Enum.GetValues(typeof(ERoomCurrencyType))
                          .Cast<ERoomCurrencyType>()
                          .ToDictionary(
                              type => type,
                              type => RoomCurrency.Default(type)
                          );
    }

    public RoomWallet(IEnumerable<RoomCurrency> currencies)
    {
        _currencies = currencies.ToDictionary(c => c.Type);
    }

    public static RoomWallet Default => new RoomWallet();

    public RoomCurrency Get(ERoomCurrencyType type) => _currencies[type];
    public bool Has(ERoomCurrencyType type) => _currencies.ContainsKey(type);
    public RoomWallet With(RoomCurrency updated)
    {
        var next = new Dictionary<ERoomCurrencyType, RoomCurrency>(_currencies);
        next[updated.Type] = updated;
        return new RoomWallet(next.Values); // 불변 갱신
    }
}