public interface IPushable : IInteractable
{
}

public interface IPushInteractionHandler
{
    bool TryHandlePushInteract();
}
