using _Project.Scripts.FlaskSequence;
using System;

namespace _Project.Scripts.Levels
{
    public class LevelsController : IDisposable
    {
        private readonly LevelsView LevelsView;

        private LevelCreator _levelCreator;

        public LevelsController(LevelsView levelsView)
        {
            LevelsView = levelsView;

            LevelsView.OnCloseLevelsViewButtonClicked += OnCloseLevelsViewButtonClicked;
            LevelsView.OnOpenLevelsViewButtonClicked += OnOpenLevelsViewButtonClicked;
            LevelsView.OnLevelButtonClicked += OnLevelButtonDown;
        }

        public void Initialize(LevelCreator levelCreator)
        {
            _levelCreator = levelCreator;

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
            LevelsView.Hide();
        }

        private void OnLevelButtonDown(int levelNumber)
        {
            if (levelNumber - 1 == _levelCreator.CurrentLevelIndex)
            {
                _levelCreator.ReloadCurrentLevel();
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
