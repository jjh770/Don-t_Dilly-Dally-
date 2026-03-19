using System;
using System.Collections;
using UnityEngine;

public readonly struct MyHospital
{
    public string Name { get; }

    public MyHospital(string name)
    {
        if (name == null) { throw new Exception("병원 이름은 Null일 수 없습니다."); }
        ;
        Name = name;
    }
}