using _Project.Scripts.Advertising;
using _Project.Scripts.Audio;
using _Project.Scripts.FlaskSequence;
using _Project.Scripts.GameEvents;
using System;

namespace _Project.Scripts.Levels
{
    public class LevelsController : IDisposable
    {
        private readonly LevelsView LevelsView;

        private LevelCreator _levelCreator;
        private IAdvertising _advertising;
        private IGameEvents _gameEvents;
        private IAudioService _audioService;

        private AudioEvent _buttonClick;

        public LevelsController(LevelsView levelsView)
        {
            LevelsView = levelsView;

            LevelsView.OnCloseLevelsViewButtonClicked += OnCloseLevelsViewButtonClicked;
            LevelsView.OnOpenLevelsViewButtonClicked += OnOpenLevelsViewButtonClicked;
            LevelsView.OnLevelButtonClicked += OnLevelButtonDown;
            LevelsView.OnRestartLevelButtonClicked += OnRestartLevelButtonClicked;
        }

        public void Initialize(LevelCreator levelCreator, IAdvertising advertising, IGameEvents gameEvents, IAudioService audioService, AudioEvent buttonClick)
        {
            _levelCreator = levelCreator;
            _advertising = advertising;
            _gameEvents = gameEvents;
            _audioService = audioService;
            _buttonClick = buttonClick;

            _levelCreator.LevelCompleted += OnLevelCompleted;
            _levelCreator.LevelLoaded += OnLevelLoaded;
            _levelCreator.LevelCreated += OnLevelCreated;

            LevelsView.UpdateView(_levelCreator.LevelsCount, 1, 0);
        }

        public void Dispose()
        {
            LevelsView.OnCloseLevelsViewButtonClicked -= OnCloseLevelsViewButtonClicked;
            LevelsView.OnOpenLevelsViewButtonClicked -= OnOpenLevelsViewButtonClicked;
            LevelsView.OnLevelButtonClicked -= OnLevelButtonDown;

            _levelCreator.LevelCompleted -= OnLevelCompleted;
            _levelCreator.LevelLoaded -= OnLevelLoaded;
            _levelCreator.LevelCreated -= OnLevelCreated;
            LevelsView.OnRestartLevelButtonClicked -= OnRestartLevelButtonClicked;
        }

        private void OnOpenLevelsViewButtonClicked()
        {
            if (_advertising.CanShowInterstitial())
                _advertising.ShowInterstitial(null, null);

            _audioService.PlayOneShot(_buttonClick);

            LevelsView.UpdateView(_levelCreator.LevelsCount, _levelCreator.LoadedLevelsCount, 0);
            LevelsView.Show();
        }

        private void OnCloseLevelsViewButtonClicked()
        {
            if (_advertising.CanShowInterstitial())
                _advertising.ShowInterstitial(null, null);

            _audioService.PlayOneShot(_buttonClick);
            LevelsView.Hide();
        }

        private void OnLevelButtonDown(int levelNumber)
        {
            if (_advertising.CanShowInterstitial())
                _advertising.ShowInterstitial(null, null);

            _audioService.PlayOneShot(_buttonClick);
            _gameEvents.GameStart();

            if (levelNumber - 1 == _levelCreator.CurrentLevelIndex)
            {
                _levelCreator.ReloadCurrentLevel();
                LevelsView.Hide();
                return;
            }

            LevelsView.Hide();
            _levelCreator.LoadLevelByIndex(levelNumber - 1);
        }

        private void OnRestartLevelButtonClicked()
        {
            _audioService.PlayOneShot(_buttonClick);
            _levelCreator.ReloadCurrentLevel();
        }

        private void OnLevelCompleted(LevelData levelData)
        {
            LevelsView.CompleteLevel(levelData.LevelIndex + 1);
        }

        private void OnLevelCreated(LevelData levelData)
        {
            if (levelData.LevelState == LevelState.Completed)
                return;

            LevelsView.OpenLevel(levelData.LevelIndex + 1);
        }

        private void OnLevelLoaded(LevelData levelData)
        {
            LevelState levelState = levelData.LevelState;
            switch (levelState)
            {
                case LevelState.Opened:
                    LevelsView.OpenLevel(levelData.LevelIndex + 1);
                    break;
                case LevelState.Locked:
                    LevelsView.LockLevel(levelData.LevelIndex + 1);
                    break;
                case LevelState.Completed:
                    LevelsView.CompleteLevel(levelData.LevelIndex + 1);
                    break;
            }
        }
    }
}
