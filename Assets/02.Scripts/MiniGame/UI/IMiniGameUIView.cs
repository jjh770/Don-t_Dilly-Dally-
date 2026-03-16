namespace DontDillyDally.MiniGame
{
    public interface IMiniGameUIView
    {
        void Initialize(IMiniGame game);
        void UpdateView();
        void SetVisible(bool visible);
    }
}
