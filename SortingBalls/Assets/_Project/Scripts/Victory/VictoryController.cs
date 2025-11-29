using _Project.Scripts.Advertising;
using _Project.Scripts.FlaskSequence;
using _Project.Scripts.GameEvents;
using System;

namespace _Project.Scripts.Victory
{
    public class VictoryController : IDisposable
    {
        private readonly VictoryView VictoryView;

        private LevelCreator _levelCreator;
        private IGameEvents _gameEvents;
        private IAdvertising _advertising;

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

        public void Initialize(LevelCreator levelCreator, IAdvertising advertising, IGameEvents gameEvents)
        {
            _levelCreator = levelCreator;
            _gameEvents = gameEvents;
            _advertising = advertising;

            _levelCreator.LevelCompleted += OnLevelCompleted;
        }

        private void OnContinueButtonClicked()
        {
            if (_advertising.CanShowInterstitial())
                _advertising.ShowInterstitial(null, null);

            VictoryView.Hide(_gameEvents.GameStart);
            _levelCreator.LoadNextLevel();
        }

        private void OnLevelCompleted(LevelData levelData)
        {
            VictoryView.Show(_levelCreator.CurrentLevelIndex + 1, _gameEvents.GameStop);
        }
    }
}
