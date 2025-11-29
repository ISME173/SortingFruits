using _Project.Scripts.Advertising;
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

        public LevelsController(LevelsView levelsView)
        {
            LevelsView = levelsView;

            LevelsView.OnCloseLevelsViewButtonClicked += OnCloseLevelsViewButtonClicked;
            LevelsView.OnOpenLevelsViewButtonClicked += OnOpenLevelsViewButtonClicked;
            LevelsView.OnLevelButtonClicked += OnLevelButtonDown;
        }

        public void Initialize(LevelCreator levelCreator, IAdvertising advertising, IGameEvents gameEvents)
        {
            _levelCreator = levelCreator;
            _advertising = advertising;
            _gameEvents = gameEvents;

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
        }

        private void OnOpenLevelsViewButtonClicked()
        {
            LevelsView.UpdateView(_levelCreator.LevelsCount, _levelCreator.LoadedLevelsCount, 0);
            LevelsView.Show();
        }

        private void OnCloseLevelsViewButtonClicked()
        {
            if (_advertising.CanShowInterstitial())
                _advertising.ShowInterstitial(null, null);

            LevelsView.Hide();
        }

        private void OnLevelButtonDown(int levelNumber)
        {
            if (_advertising.CanShowInterstitial())
                _advertising.ShowInterstitial(null, null);

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

        private void OnLevelCompleted(LevelData levelData)
        {
            LevelsView.CompleteLevel(levelData.LevelIndex + 1);
        }

        private void OnLevelCreated(LevelData levelData)
        {
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
