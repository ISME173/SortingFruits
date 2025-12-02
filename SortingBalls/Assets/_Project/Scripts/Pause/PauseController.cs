using _Project.Scripts.Advertising;
using _Project.Scripts.Audio;
using _Project.Scripts.GameEvents;
using _Project.Scripts.Saves;
using System;

namespace _Project.Scripts.Pause
{
    public class PauseController : IDisposable
    {
        private const string SaveMusicActivateKey = "MusicActive";
        private const string SaveSoundActivateKey = "SoundActive";

        private readonly PauseView PauseView;

        private IAdvertising _advertising;
        private IGameEvents _gameEvents;
        private IAudioService _audioService;
        private ISaves _saves;
        private AudioEvent _buttonClick;
        private AudioEvent _backgroundMusic;

        private bool _soundActive = true;
        private bool _musicActive = false;

        private float _lastSfxVolume = 1f;
        private float _lastUiVolume = 0.8f;
        private float _lastMusicVolume = 0.8f;

        public PauseController(PauseView pauseView)
        {
            PauseView = pauseView;

            PauseView.OnClosePauseViewClicked += OnCloseButtonClicked;
            PauseView.OnOpenPauseViewClicked += OnOpenButtonClicked;
            PauseView.OnOpenLevelsViewClicked += OnOpenLevelsViewButtonClicked;
            PauseView.OnMusicSwitchClicked += OnMusicSwitchButtonClicked;
            PauseView.OnSoundSwitchClicked += OnSoundSwitchButtonClicked;
        }

        public void Initialize(IAdvertising advertising, IGameEvents gameEvents, ISaves saves, IAudioService audioService, AudioEvent buttonClick, AudioEvent backgroundMusic)
        {
            _advertising = advertising;
            _gameEvents = gameEvents;
            _audioService = audioService;
            _buttonClick = buttonClick;
            _backgroundMusic = backgroundMusic;
            _saves = saves;

            _lastSfxVolume = Math.Max(_audioService.GetCategoryVolume(AudioCategory.Sfx), 0f);
            _lastUiVolume = Math.Max(_audioService.GetCategoryVolume(AudioCategory.Ui), 0f);
            _lastMusicVolume = Math.Max(_audioService.GetCategoryVolume(AudioCategory.Music), 0f);

            _soundActive = _saves.GetBool(SaveSoundActivateKey, _soundActive);
            _musicActive = _saves.GetBool(SaveMusicActivateKey, _musicActive);

            if (_soundActive) SoundOn(); else SoundOff();
            if (_musicActive) MusicOn(); else MusicOff();

            // FIX: запуск фоновой музыки только если она активна
            if (_musicActive)
                _audioService.Play(_backgroundMusic);
        }

        public void Dispose()
        {
            PauseView.OnClosePauseViewClicked -= OnCloseButtonClicked;
            PauseView.OnOpenPauseViewClicked -= OnOpenButtonClicked;
            PauseView.OnOpenLevelsViewClicked -= OnOpenLevelsViewButtonClicked;
            PauseView.OnMusicSwitchClicked -= OnMusicSwitchButtonClicked;
            PauseView.OnSoundSwitchClicked -= OnSoundSwitchButtonClicked;
        }

        private void OnCloseButtonClicked()
        {
            if (_advertising.CanShowInterstitial())
                _advertising.ShowInterstitial(null, null);

            // Если звук выключен, PlayOneShot будет загейтен AudioService'ом
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

        private void OnSoundSwitchButtonClicked()
        {
            if (_soundActive)
                SoundOff();
            else
                SoundOn();

            _saves.Save();
        }

        private void OnMusicSwitchButtonClicked()
        {
            if (_musicActive)
                MusicOff();
            else
                MusicOn();

            _saves.Save();
        }

        private void SoundOn()
        {
            _soundActive = true;
            _saves.SetBool(SaveSoundActivateKey, true);

            _audioService.SetCategoryMuted(AudioCategory.Sfx, false);
            _audioService.SetCategoryMuted(AudioCategory.Ui, false);

            PauseView.SetSoundActive(true);
        }

        private void SoundOff()
        {
            _soundActive = false;
            _saves.SetBool(SaveSoundActivateKey, false);

            _audioService.SetCategoryMuted(AudioCategory.Sfx, true);
            _audioService.SetCategoryMuted(AudioCategory.Ui, true);

            PauseView.SetSoundActive(false);
        }

        private void MusicOn()
        {
            _musicActive = true;
            _saves.SetBool(SaveMusicActivateKey, true);

            _audioService.SetCategoryMuted(AudioCategory.Music, false);

            // если фон. музыка ещё не играет — запустим
            _audioService.Play(_backgroundMusic);

            PauseView.SetMusicActive(true);
        }

        private void MusicOff()
        {
            _musicActive = false;
            _saves.SetBool(SaveMusicActivateKey, false);

            _audioService.SetCategoryMuted(AudioCategory.Music, true);

            PauseView.SetMusicActive(false);
        }
    }
}
