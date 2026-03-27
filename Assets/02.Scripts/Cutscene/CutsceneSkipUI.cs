using UnityEngine;

// 컷씬 스킵 안내 UI
public class CutsceneSkipUI : MonoBehaviour
{
    [SerializeField] private GameObject _panel;

    public bool IsVisible => _panel != null && _panel.activeSelf;

    private void Awake()
    {
        if (_panel != null)
        {
            _panel.SetActive(false);
        }
    }

    public void Show()
    {
        if (_panel != null)
        {
            _panel.SetActive(true);
        }
    }

    public void Hide()
    {
        if (_panel != null)
        {
            _panel.SetActive(false);
        }
    }
}
