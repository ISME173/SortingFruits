using AnimationsUI.CoreScripts;
using Reflex.Attributes;
using System;
using UnityEngine;
using UnityEngine.UI;

namespace _Project.Scripts.Pause
{
    public class PauseView : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PopupAnimationPanelsSequence _pauseViewAnimations;
        [Space]
        [SerializeField] private Button _soundSwitchButton;
        [SerializeField] private Button _musicSwitchButton;
        [Space]
        [SerializeField] private Button _closePauseViewButton;
        [SerializeField] private Button _openPauseViewButton;
        [Space]
        [SerializeField] private Button _openLevelsViewButton;

        public event Action OnSoundSwitchClicked, OnMusicSwitchClicked;
        public event Action OnOpenPauseViewClicked, OnClosePauseViewClicked;
        public event Action OnOpenLevelsViewClicked;

        public void Show()
        {
            gameObject.SetActive(true);
            _pauseViewAnimations.Show(null);
        }

        public void Hide()
        {
            _pauseViewAnimations.Hide(() => gameObject.SetActive(false));
        }

        [Inject]
        private void Initialize(PauseController pauseController)
        {
            _closePauseViewButton.onClick.AddListener(() => OnClosePauseViewClicked?.Invoke());
            _openPauseViewButton.onClick.AddListener(() => OnOpenPauseViewClicked?.Invoke());

            _soundSwitchButton.onClick.AddListener(() => OnSoundSwitchClicked?.Invoke());
            _musicSwitchButton.onClick.AddListener(() => OnMusicSwitchClicked?.Invoke());

            _openLevelsViewButton.onClick.AddListener(() => OnOpenLevelsViewClicked?.Invoke());
        }
    }
}
