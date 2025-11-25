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
        }

        public void Dispose()
        {
            LevelsView.OnCloseLevelsViewButtonClicked -= OnCloseLevelsViewButtonClicked;
            LevelsView.OnOpenLevelsViewButtonClicked -= OnOpenLevelsViewButtonClicked;
            LevelsView.OnLevelButtonClicked -= OnLevelButtonDown;
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
            _levelCreator.LoadLevelByIndex(levelNumber - 1);
        }
    }
}
