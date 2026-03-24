using UnityEngine;

public class Lobby : MonoBehaviour
{
    [Header("프리뷰 캐릭터")]
    [SerializeField] private GameObject _previewCharacterPrefab;

    private GameObject _previewCharacter;

    private void Start()
    {
        SpawnPreviewCharacter();
    }

    private void OnDestroy()
    {
        if (_previewCharacter != null)
        {
            Destroy(_previewCharacter);
        }
    }

    private void SpawnPreviewCharacter()
    {
        if (_previewCharacterPrefab == null) return;

        Vector3 position = Vector3.zero;
        Quaternion rotation = Quaternion.identity;

        _previewCharacter = Instantiate(_previewCharacterPrefab, position, rotation);

        var photonController = _previewCharacter.GetComponent<PlayerCustomizingController>();
        if (photonController != null)
        {
            Destroy(photonController);
        }
    }
}
