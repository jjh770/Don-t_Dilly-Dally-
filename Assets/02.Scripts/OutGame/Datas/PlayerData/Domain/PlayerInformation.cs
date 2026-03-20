using System.Collections.Generic;
using System.Linq;

public class PlayerInformation
{
    private string _name;

    private readonly List<MyHospital> _hospitals;

    public string Name => _name;

    private PlayerInformation()
    {
        _hospitals = new List<MyHospital>();
        _name = "Player";
    }

    public PlayerInformation(string name,IEnumerable<MyHospital> hospitals)
    {
        _name = name;
        _hospitals = hospitals.ToList<MyHospital>();
    }

    public static PlayerInformation Default => new PlayerInformation();

    public string name => _name;

    public void SetName(string name)
    {
        _name = name;
    }
    public MyHospital[] MyHospitals => _hospitals.ToArray();
    public void AddHospital(MyHospital hospital)
    {
        _hospitals.RemoveAll(x => x.Name == hospital.Name);
        _hospitals.Insert(0, hospital);
    }

    public void RemoveHospital(MyHospital hospital)
    {
        _hospitals.Remove(hospital);
    }
}
