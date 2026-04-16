using System;

public enum ELoadingStep
{
    FirebaseInit,
    PlayerDataLoad,
    AttendanceLoad,
    NoInternet,
}

public static class LoadingUIService
{
    public static event Action<ELoadingStep> OnShowRequested;
    public static event Action OnHideRequested;

    public static bool IsVisible { get; private set; }
    public static ELoadingStep CurrentStep { get; private set; } = ELoadingStep.FirebaseInit;

    public static void Show(ELoadingStep step)
    {
        CurrentStep = step;
        IsVisible = true;
        OnShowRequested?.Invoke(step);
    }

    public static void Hide()
    {
        IsVisible = false;
        OnHideRequested?.Invoke();
    }
}