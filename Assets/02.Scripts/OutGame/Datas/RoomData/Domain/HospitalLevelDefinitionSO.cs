using UnityEngine;

[CreateAssetMenu(fileName = "Hospital Level Definition", menuName = "DontDillyDally/Hospital/Hospital Level Definition")]
public class HospitalLevelDefinitionSO : ScriptableObject
{
    [Header("기본 정보")]
    [SerializeField] private int _level;
    [SerializeField] private string _hospitalName;
    [SerializeField] private Sprite _hospitalIcon;

    [Header("업그레이드 조건")]
    [SerializeField] private int _requiredStars;
    [SerializeField] private int _upgradeCost;

    public int Level => _level;
    public string HospitalName => _hospitalName;
    public Sprite HospitalIcon => _hospitalIcon;
    public int RequiredStars => _requiredStars;
    public int UpgradeCost => _upgradeCost;
    public bool IsDefault => _level == 0;
}