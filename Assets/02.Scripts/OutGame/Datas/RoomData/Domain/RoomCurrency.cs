
using System;
using Firebase.Firestore;

public readonly struct RoomCurrency 
{
    public ERoomCurrencyType Type { get; }
    public int Value { get; }

    public RoomCurrency(ERoomCurrencyType type, int value)
    {
        Type = type;
        if (value < 0) { throw new Exception("Value 값은 0보다 작을 수 없습니다."); }
        ;
        Value = value;    
    }

    public static RoomCurrency Default(ERoomCurrencyType type) => new RoomCurrency(type, 0);

    public RoomCurrency Add(int value) => new RoomCurrency(Type, Value + value);

    public RoomCurrency Minus(int value) => new RoomCurrency(Type, Value - value);

}
