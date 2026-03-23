namespace DontDillyDally.MiniGame
{
    public interface IMiniGameUIView
    {
        void Initialize(IMiniGame game);
        void UpdateView();
        void SetVisible(bool visible);

        // 성공/실패 결과를 화면에 표시 (Launcher의 딜레이 동안 보여줌)
        void ShowResult(bool isSuccess);
    }
}
