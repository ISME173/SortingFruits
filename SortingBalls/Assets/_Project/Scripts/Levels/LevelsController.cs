using _Project.Scripts.Advertising;
using _Project.Scripts.Audio;
using _Project.Scripts.FlaskSequence;
using _Project.Scripts.GameEvents;
using System;
using UnityEngine;

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

            _levelCreator.OnLevelStateChanged += OnLevelStateChanged;

            LevelsView.UpdateView(_levelCreator.LevelsCount, 1, 0);
        }

        public void Dispose()
        {
            LevelsView.OnCloseLevelsViewButtonClicked -= OnCloseLevelsViewButtonClicked;
            LevelsView.OnOpenLevelsViewButtonClicked -= OnOpenLevelsViewButtonClicked;
            LevelsView.OnLevelButtonClicked -= OnLevelButtonDown;
            LevelsView.OnRestartLevelButtonClicked -= OnRestartLevelButtonClicked;

            _levelCreator.OnLevelStateChanged -= OnLevelStateChanged;
        }

        private void OnOpenLevelsViewButtonClicked()
        {
            if (_advertising.CanShowInterstitial())
                _advertising.ShowInterstitial(null, null);

            _audioService.PlayOneShot(_buttonClick);

            //LevelsView.UpdateView(_levelCreator.LevelsCount, _levelCreator.LoadedLevelsCount, 0);
            LevelsView.Show();
        }

        private void OnLevelStateChanged(int levelIndex, LevelState levelState)
        {
            switch (levelState)
            {
                case LevelState.Opened:
                    LevelsView.OpenLevel(levelIndex + 1);
                    break;
                case LevelState.Locked:
                    LevelsView.LockLevel(levelIndex + 1);
                    break;
                case LevelState.Completed:
                    Debug.Log($"Complete level: {levelIndex + 1}");
                    LevelsView.CompleteLevel(levelIndex + 1);
                    break;
            }
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
    }
}
