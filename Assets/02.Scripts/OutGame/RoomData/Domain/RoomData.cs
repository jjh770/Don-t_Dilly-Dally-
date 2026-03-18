
using System;
using Unity.VisualScripting;

public class RoomData 
{
    public int Money {  get; private set; }
    public int Star { get; private set; }

    public int StageLevel { get; private set; }

    public RoomData(int money, int star, int stageLevel)
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
                Money =+ amount; 
                break;
            case ERoomCurrencyType.Star:
                Star = + amount;
                break;
            case ERoomCurrencyType.StageLevel:
                StageLevel = + amount;
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
