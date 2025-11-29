using _Project.Scripts.Advertising;
using _Project.Scripts.Audio;
using _Project.Scripts.GameEvents;
using _Project.Scripts.Saves;
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
        [Space]
        [SerializeField] private Image _soundButtonImage;
        [SerializeField] private Image _musicButtonImage;

        [Header("Assets")]
        [SerializeField] private Sprite _soundOnSprite;
        [SerializeField] private Sprite _soundOffSprite;
        [Space]
        [SerializeField] private Sprite _musicOnSprite;
        [SerializeField] private Sprite _musicOffSprite;

        [Header("Sfx references")]
        [SerializeField] private AudioEvent _buttonClickEvent;
        [SerializeField] private AudioEvent _backgroundMusic;

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

        public void SetSoundActive(bool active)
        {
            _soundButtonImage.sprite = active ? _soundOnSprite : _soundOffSprite;
        }

        public void SetMusicActive(bool active)
        {
            _musicButtonImage.sprite = active ? _musicOnSprite : _musicOffSprite;
        }

        [Inject]
        private void Initialize(PauseController pauseController, IAdvertising advertising, ISaves saves, IGameEvents gameEvents, IAudioService audioService)
        {
            pauseController.Initialize(advertising, gameEvents, saves, audioService, _buttonClickEvent, _backgroundMusic);

            _closePauseViewButton.onClick.AddListener(() => OnClosePauseViewClicked?.Invoke());
            _openPauseViewButton.onClick.AddListener(() => OnOpenPauseViewClicked?.Invoke());

            _soundSwitchButton.onClick.AddListener(() => OnSoundSwitchClicked?.Invoke());
            _musicSwitchButton.onClick.AddListener(() => OnMusicSwitchClicked?.Invoke());

            _openLevelsViewButton.onClick.AddListener(() => OnOpenLevelsViewClicked?.Invoke());
        }
    }
}
