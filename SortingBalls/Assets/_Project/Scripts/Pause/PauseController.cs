using _Project.Scripts.Advertising;
using _Project.Scripts.Audio;
using _Project.Scripts.GameEvents;
using System;

namespace _Project.Scripts.Pause
{
    public class PauseController : IDisposable
    {
        private readonly PauseView PauseView;

        private IAdvertising _advertising;
        private IGameEvents _gameEvents;
        private IAudioService _audioService;

        private AudioEvent _buttonClick;

        public PauseController(PauseView pauseView)
        {
            PauseView = pauseView;

            PauseView.OnClosePauseViewClicked += OnCloseButtonClicked;
            PauseView.OnOpenPauseViewClicked += OnOpenButtonClicked;
            PauseView.OnOpenLevelsViewClicked += OnOpenLevelsViewButtonClicked;
        }

        public void Initialize(IAdvertising advertising, IGameEvents gameEvents, IAudioService audioService, AudioEvent buttonClick)
        {
            _advertising = advertising;
            _gameEvents = gameEvents;
            _audioService = audioService;
            _buttonClick = buttonClick;
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

            _audioService.PlayOneShot(_buttonClick);

            PauseView.Hide(_gameEvents.GameStart);
        }

        private void OnOpenButtonClicked()
        {
            _audioService.PlayOneShot(_buttonClick);
            PauseView.Show(_gameEvents.GameStop);
        }

        private void OnOpenLevelsViewButtonClicked()
        {
            PauseView.Hide();
        }
    }
}
