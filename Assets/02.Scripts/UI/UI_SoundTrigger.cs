using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class UI_SoundTrigger : MonoBehaviour, IPointerEnterHandler
{
    [Header("Common")]
    [SerializeField] private SoundType _soundType = SoundType.Local;
    [SerializeField] private SFXKey _buttonSfxKey = SFXKey.UIButtonClick;

    [Header("Hover")]
    [SerializeField] private bool _playHoverSound = false;
    [SerializeField] private SFXKey _hoverSfxKey = SFXKey.UIButtonClick;

    private Button _button;

    private void Awake()
    {
        CacheComponents();
    }

    private void OnEnable()
    {
        CacheComponents();

        if (_button != null)
        {
            _button.onClick.AddListener(HandleButtonClick);
        }
    }

    private void OnDisable()
    {
        if (_button != null)
        {
            _button.onClick.RemoveListener(HandleButtonClick);
        }
    }

    private void Reset()
    {
        CacheComponents();
    }

    private void HandleButtonClick()
    {
        Play(_buttonSfxKey);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (_playHoverSound)
        {
            Play(_hoverSfxKey);
        }
    }

    private void Play(SFXKey key)
    {
        if (key == SFXKey.None || SoundManager.Instance == null)
        {
            return;
        }

        SoundManager.Instance.Play(key, _soundType);
    }

    private void CacheComponents()
    {
        _button = GetComponent<Button>();
    }
}
