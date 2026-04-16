using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum ENotifyType
{
    OtherPlayerLeft,
    KickedByHost,
}

public static class NotifyUIService
{
    public static event Action<ENotifyType> OnShowRequested;
    public static ENotifyType CurrentNotify { get; private set; }

    public static void Show(ENotifyType type)
    {
        CurrentNotify = type;
        OnShowRequested?.Invoke(CurrentNotify);
    }

}

public class NotifyUI : UIPopupBase
{
    private static NotifyUI _instance;

    [SerializeField] private TMP_Text _messageText;
    [SerializeField] private Button _closeButton;

    private readonly Dictionary<ENotifyType, string> _messages = new()
    {
        { ENotifyType.KickedByHost,    "방장에 의해 \n 강퇴 당했습니다." },
        { ENotifyType.OtherPlayerLeft,  "다른 플레이어가 \n 게임을 이탈하여 \n 스테이지를 종료합니다." },
    };

    private void OnEnable()
    {
        NotifyUIService.OnShowRequested += Show;

        if (_closeButton != null)
        {
            _closeButton.onClick.AddListener(Hide);
        }
    }

    protected override void Awake()
    {
        base.Awake();

        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnDisable()
    {
        NotifyUIService.OnShowRequested -= Show;
 
        if (_closeButton != null)
        {
            _closeButton.onClick.RemoveListener(Hide);
        }
    }

    public void Show(ENotifyType type)
    {
        if (_messages.TryGetValue(type, out string message))
        {
            _messageText.text = message;
        }
        Show();
    }

    protected override void OnShow()
    {
       
    }
}
