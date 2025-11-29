using _Project.Scripts.Advertising;
using _Project.Scripts.Audio;
using _Project.Scripts.GameEvents;
using AnimationsUI.CoreScripts;
using Reflex.Attributes;
using System;
using UnityEngine;
using UnityEngine.UI;

namespace _Project.Scripts.Pause
{
    public class PauseView : MonoBehaviour
    {
        [Header("View references")]
        [SerializeField] private PopupAnimationPanelsSequence _pauseViewAnimations;
        [Space]
        [SerializeField] private Button _soundSwitchButton;
        [SerializeField] private Button _musicSwitchButton;
        [Space]
        [SerializeField] private Button _closePauseViewButton;
        [SerializeField] private Button _openPauseViewButton;
        [Space]
        [SerializeField] private Button _openLevelsViewButton;

        [Header("Sfx references")]
        [SerializeField] private AudioEvent _buttonClickEvent;

        public event Action OnSoundSwitchClicked, OnMusicSwitchClicked;
        public event Action OnOpenPauseViewClicked, OnClosePauseViewClicked;
        public event Action OnOpenLevelsViewClicked;

        public void Show(Action callback = null)
        {
            gameObject.SetActive(true);
            _pauseViewAnimations.Show(callback);
        }

        public void Hide(Action callback = null)
        {
            _pauseViewAnimations.Hide(() =>
            {
                if (callback != null)
                    callback();

                gameObject.SetActive(false);
            });
        }

        [Inject]
        private void Initialize(PauseController pauseController, IAdvertising advertising, IGameEvents gameEvents, IAudioService audioService)
        {
            pauseController.Initialize(advertising, gameEvents, audioService, _buttonClickEvent);

            _closePauseViewButton.onClick.AddListener(() => OnClosePauseViewClicked?.Invoke());
            _openPauseViewButton.onClick.AddListener(() => OnOpenPauseViewClicked?.Invoke());

            _soundSwitchButton.onClick.AddListener(() => OnSoundSwitchClicked?.Invoke());
            _musicSwitchButton.onClick.AddListener(() => OnMusicSwitchClicked?.Invoke());

            _openLevelsViewButton.onClick.AddListener(() => OnOpenLevelsViewClicked?.Invoke());
        }
    }
}
