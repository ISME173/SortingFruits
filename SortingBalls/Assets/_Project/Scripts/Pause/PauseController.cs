using _Project.Scripts.Advertising;
using _Project.Scripts.GameEvents;
using System;

namespace _Project.Scripts.Pause
{
    public class PauseController : IDisposable
    {
        private readonly PauseView PauseView;

        private IAdvertising _advertising;
        private IGameEvents _gameEvents;

        public PauseController(PauseView pauseView)
        {
            PauseView = pauseView;

            PauseView.OnClosePauseViewClicked += OnCloseButtonClicked;
            PauseView.OnOpenPauseViewClicked += OnOpenButtonClicked;
            PauseView.OnOpenLevelsViewClicked += OnOpenLevelsViewButtonClicked;
        }

        public void Initialize(IAdvertising advertising, IGameEvents gameEvents)
        {
            _advertising = advertising;
            _gameEvents = gameEvents;
        }

        public void Dispose()
        {
            PauseView.OnClosePauseViewClicked -= OnCloseButtonClicked;
            PauseView.OnOpenPauseViewClicked -= OnOpenButtonClicked;
            PauseView.OnOpenLevelsViewClicked -= OnOpenLevelsViewButtonClicked;
        }

        private void OnCloseButtonClicked()
        {
            if (_advertising.CanShowInterstitial())
                _advertising.ShowInterstitial(null, null);

            PauseView.Hide(_gameEvents.GameStart);
        }

        private void OnOpenButtonClicked()
        {
            PauseView.Show(_gameEvents.GameStop);
        }

        private void OnOpenLevelsViewButtonClicked()
        {
            if (_advertising.CanShowInterstitial())
                _advertising.ShowInterstitial(null, null);

            PauseView.Hide();
        }
    }
}
