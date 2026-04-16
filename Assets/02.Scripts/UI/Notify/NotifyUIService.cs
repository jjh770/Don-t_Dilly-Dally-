using System;

public enum ENotifyType
{
    OtherPlayerLeft,
    KickedByHost,
}

public static class NotifyUIService
{
    public static event Action<ENotifyType> OnShowRequested;
    public static ENotifyType? PendingNotify { get; private set; }

    public static void QueueForNextScene(ENotifyType type)
    {
        PendingNotify = type;
    }

    public static bool TryConsumePending(out ENotifyType type)
    {
        if (PendingNotify.HasValue)
        {
            type = PendingNotify.Value;
            PendingNotify = null;
            return true;
        }

        type = default;
        return false;
    }

    public static void Show(ENotifyType type)
    {
        OnShowRequested?.Invoke(type);
    }

}