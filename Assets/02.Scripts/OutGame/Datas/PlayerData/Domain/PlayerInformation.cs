using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

public class PlayerInformation
{
    private string _name;

    private readonly List<MyHospital> _hospitals;

    private const int DefaultMaxHospitalCount = 5;

    private int _maxHospitals;
    public string Name => _name;

    private PlayerInformation()
    {
        _hospitals = new List<MyHospital>();
        _name = "Player";
        _maxHospitals = DefaultMaxHospitalCount;
    }

    public PlayerInformation(string name,IEnumerable<MyHospital> hospitals)
    {
        _name = name;
        _hospitals = hospitals.ToList<MyHospital>();
        _maxHospitals = DefaultMaxHospitalCount;
    }

    public static PlayerInformation Default => new PlayerInformation();

    public string name => _name;

    public bool CanAdd(string name)
    {
        return _hospitals.Any(x => x.Name == name) || (_hospitals.Count < _maxHospitals);
    }
    public void SetName(string name)
    {
        _name = name;
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
