using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(fileName = "Hospital Level Catalog", menuName = "DontDillyDally/Hospital/Hospital Level Catalog")]
public class HospitalLevelCatalogSO : ScriptableObject
{
    [SerializeField] private List<HospitalLevelDefinitionSO> _levels;
    public IReadOnlyList<HospitalLevelDefinitionSO> Levels => _levels;

    public HospitalLevelDefinitionSO GetLevel(int level)
        => _levels.FirstOrDefault(l => l.Level == level);

    public HospitalLevelDefinitionSO GetNextLevel(int currentLevel)
        => _levels.FirstOrDefault(l => l.Level == currentLevel + 1);
}