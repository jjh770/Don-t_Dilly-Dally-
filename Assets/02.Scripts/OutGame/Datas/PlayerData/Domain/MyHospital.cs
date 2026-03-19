using System;
using System.Collections;
using UnityEngine;

public readonly struct MyHospital
{
    public string Name { get; }

    public DateTime Time { get; }

    public MyHospital(string name, DateTime time)
    {
        if (name == null) { throw new Exception("병원 이름은 Null일 수 없습니다."); }
        ;
        Name = name;
        Time = time;
    }
}