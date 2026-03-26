using UnityEngine;

[CreateAssetMenu(fileName = "RoleVisualProfile", menuName = "SelectRole/RoleVisualProfile")]
public class RoleVisualProfileSO : ScriptableObject
{
    [Header("기본")]
    [SerializeField] private Material _defaultMaterial;
    [SerializeField] private Color _defaultNicknameColor = Color.white;

    [Header("집도의")]
    [SerializeField] private Material _surgeonMaterial;
    [SerializeField] private Color _surgeonNicknameColor = Color.red;

    [Header("어시스턴트")]
    [SerializeField] private Material[] _assistantMaterials;
    [SerializeField] private Color[] _assistantNicknameColors;

    public Material DefaultMaterial => _defaultMaterial;
    public Color DefaultNicknameColor => _defaultNicknameColor;

    public Material GetMaterial(RoleType role)
    {
        if (role == RoleType.Surgeon)
            return _surgeonMaterial;

        int index = role.GetAssistantIndex();
        if (index >= 0 && _assistantMaterials != null && index < _assistantMaterials.Length)
            return _assistantMaterials[index];

        return _defaultMaterial;
    }

    public Color GetNicknameColor(RoleType role)
    {
        if (role == RoleType.Surgeon)
            return _surgeonNicknameColor;

        int index = role.GetAssistantIndex();
        if (index >= 0 && _assistantNicknameColors != null && index < _assistantNicknameColors.Length)
            return _assistantNicknameColors[index];

        return _defaultNicknameColor;
    }
}
