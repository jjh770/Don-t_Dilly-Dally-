using UnityEngine;

namespace DontDillyDally.MiniGame
{
    public sealed class MiniGameUIController : MonoBehaviour
    {
        [SerializeField] private ButtonMashUIView _buttonMashView;
        [SerializeField] private DirectionQTEUIView _directionQTEView;
        [SerializeField] private PrecisionStopUIView _precisionStopView;

        [Header("공통 UI")]
        [SerializeField] private GameObject _overlayPanel;

        private IMiniGameUIView _activeView;

        public void ShowMiniGameUI(IMiniGame game)
        {
            _overlayPanel.SetActive(true);

            _activeView = game.GameType switch
            {
                MiniGameType.ButtonMash => _buttonMashView,
                MiniGameType.DirectionQTE => _directionQTEView,
                MiniGameType.PrecisionStop => _precisionStopView,
                _ => null
            };

            _activeView?.Initialize(game);
            _activeView?.SetVisible(true);
        }

        public void ShowResult(bool isSuccess)
        {
            _activeView?.ShowResult(isSuccess);
        }

        public void HideMiniGameUI()
        {
            _activeView?.SetVisible(false);
            _activeView = null;
            _overlayPanel.SetActive(false);
        }

        private void Update()
        {
            _activeView?.UpdateView();
        }
    }
}
