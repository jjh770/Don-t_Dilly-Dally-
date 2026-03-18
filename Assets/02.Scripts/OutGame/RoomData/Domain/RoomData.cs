
using System;
using Firebase.Firestore;

[FirestoreData]
public class RoomData 
{
    [FirestoreProperty]
    public int Money {  get; set; }

    [FirestoreProperty]
    public int Star { get; set; }

    [FirestoreProperty]
    public int StageLevel { get;  set; }

    public RoomData() { }
    public RoomData(int money = 0, int star = 0, int stageLevel = 0)
    {
        if (money < 0) { throw new Exception("Money 값은 0보다 작을 수 없습니다."); }

        if (star < 0) { throw new Exception("Star 값은 0보다 작을 수 없습니다."); }
        if (stageLevel < 0) { throw new Exception("StageLevel 값은 0보다 작을 수 없습니다."); }


        Money = money;
        Star = star;
        StageLevel = stageLevel;
    }

    public void Add(ERoomCurrencyType type, int amount)
    {
        switch (type)
        {
            case ERoomCurrencyType.Money:
                Money += amount; 
                break;
            case ERoomCurrencyType.Star:
                Star += amount;
                break;
            case ERoomCurrencyType.StageLevel:
                StageLevel += amount;
                break;
        }
    }

    public void Minus(ERoomCurrencyType type, int amount)
    {
        switch (type)
        {
            case ERoomCurrencyType.Money:
                Money = Math.Max(Money + amount, 0);
                break;
            case ERoomCurrencyType.Star:
                Star = Math.Max(Star + amount, 0);
                break;
            case ERoomCurrencyType.StageLevel:
                StageLevel = Math.Max(StageLevel + amount, 0);
                break;
        }
    }  
}
