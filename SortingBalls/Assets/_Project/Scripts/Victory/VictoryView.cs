using _Project.Scripts.Advertising;
using _Project.Scripts.Audio;
using _Project.Scripts.FlaskSequence;
using _Project.Scripts.GameEvents;
using _Project.Scripts.Saves;
using AnimationsUI.CoreScripts;
using Reflex.Attributes;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace _Project.Scripts.Victory
{
    public class VictoryView : MonoBehaviour
    {
        [Header("View references")]
        [SerializeField] private Button _buttonContinue;
        [SerializeField] private PopupAnimationPanelsSequence _viewAnimation;
        [SerializeField] private TextMeshProUGUI _levelNumberText;

        [Header("Sfx references")]
        [SerializeField] private AudioEvent _buttonClick;
        [SerializeField] private AudioEvent _victoryEffect;

        public event Action OnContinueButtonClick;

        public void Show(int levelNumber, Action callback = null)
        {
            _levelNumberText.text = levelNumber.ToString();

            gameObject.SetActive(true);
            _viewAnimation.Show(callback);
        }

        public void Hide(Action callback = null)
        {
            _viewAnimation.Hide(() =>
            {
                if (callback != null)
                    callback();

                gameObject.SetActive(false);
            });
        }

        [Inject]
        private void Initialize(VictoryController controller, LevelCreator levelCreator, IAdvertising advertising, IGameEvents gameEvents, ISaves saves, IAudioService audioService)
        {
            controller.Initialize(levelCreator, advertising, gameEvents, saves, audioService, _buttonClick, _victoryEffect);

            _buttonContinue.onClick.AddListener(() => OnContinueButtonClick?.Invoke());
        }
    }
}
