using _Project.Scripts.FlaskSequence;
using System;

namespace _Project.Scripts.Victory
{
    public class VictoryController : IDisposable
    {
        private readonly VictoryView VictoryView;

        private LevelCreator _levelCreator;

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

        public void Initialize(LevelCreator levelCreator)
        {
            _levelCreator = levelCreator;
            _levelCreator.LevelCompleted += OnLevelCompleted;
        }

        private void OnContinueButtonClicked()
        {
            VictoryView.Hide();
            _levelCreator.LoadNextLevel();
        }

        private void OnLevelCompleted(LevelData levelData)
        {
            VictoryView.Show(_levelCreator.CurrentLevelIndex + 1);
        }
    }
}
