using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

public class PlayerInformation
{
    private string _nickname;

    private readonly List<MyHospital> _hospitals;

    private const int DefaultMaxHospitalCount = 5;

    private int _maxHospitals;
    public string Nickname => _nickname;

    private PlayerInformation()
    {
        _hospitals = new List<MyHospital>();
        _nickname = "Player";
        _maxHospitals = DefaultMaxHospitalCount;
    }

    public PlayerInformation(string name,IEnumerable<MyHospital> hospitals)
    {
        _nickname = name;
        _hospitals = hospitals.ToList<MyHospital>();
        _maxHospitals = DefaultMaxHospitalCount;
    }

    public static PlayerInformation Default => new PlayerInformation();

    public string name => _nickname;

    public bool CanAdd(string name)
    {
        return _hospitals.Any(x => x.Name == name) || (_hospitals.Count < _maxHospitals);
    }
    public void SetName(string name)
    {
        _nickname = name;
    }
    public MyHospital[] MyHospitals => _hospitals.ToArray();
    public void TryAddHospital(MyHospital hospital)
    {
        if (!CanAdd(hospital.Name))
        {
            throw new Exception($"근무 병원은 {_maxHospitals}개를 초과할 수 없습니다.");
        }
        _hospitals.RemoveAll(x => x.Name == hospital.Name);
        _hospitals.Insert(0, hospital);
    }

    public void TryRemoveHospital(string code)
    {
        foreach (MyHospital hospital in _hospitals)
        {
            if (hospital.Name != code) continue;
            _hospitals.Remove(hospital);
            return;
        }

        throw new Exception($"{code} : 병원이 존재하지 않습니다.");
    }
}
