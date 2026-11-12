using _Project.Scripts.Advertising;
using _Project.Scripts.Audio;
using _Project.Scripts.FlaskSequence;
using _Project.Scripts.GameEvents;
using _Project.Scripts.Saves;
using System;

namespace _Project.Scripts.Victory
{
    public class VictoryController : IDisposable
    {
        private readonly VictoryView VictoryView;

        private LevelCreator _levelCreator;
        private IGameEvents _gameEvents;
        private IAdvertising _advertising;
        private ISaves _saves;
        private IAudioService _audioService;

        private AudioEvent _buttonClick;
        private AudioEvent _victoryEffect;

        public VictoryController(VictoryView victoryView)
        {
            VictoryView = victoryView;
            VictoryView.OnContinueButtonClick += OnContinueButtonClicked;
        }

        public void Dispose()
        {
            VictoryView.OnContinueButtonClick -= OnContinueButtonClicked;
            _levelCreator.LevelCompleted -= OnLevelCompleted;
        }

        public void Initialize(LevelCreator levelCreator, IAdvertising advertising, IGameEvents gameEvents, ISaves saves, IAudioService audioService, AudioEvent buttonClick, AudioEvent victoryEffect)
        {
            _levelCreator = levelCreator;
            _gameEvents = gameEvents;
            _advertising = advertising;
            _saves = saves;
            _audioService = audioService;
            _buttonClick = buttonClick;
            _victoryEffect = victoryEffect;

            _levelCreator.LevelCompleted += OnLevelCompleted;
        }

        private void OnContinueButtonClicked()
        {
            if (_advertising.CanShowInterstitial())
                _advertising.ShowInterstitial(null, null);

            _audioService.PlayOneShot(_buttonClick);

            VictoryView.Hide(_gameEvents.GameStart);
            _levelCreator.LoadNextLevel();
        }

        private void OnLevelCompleted(LevelData levelData)
        {
            _audioService.PlayOneShot(_victoryEffect);
            VictoryView.Show(_levelCreator.CurrentLevelIndex + 1, _gameEvents.GameStop);

            _saves.Save();
        }
    }
}
