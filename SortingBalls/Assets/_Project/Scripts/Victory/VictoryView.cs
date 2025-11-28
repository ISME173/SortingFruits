using _Project.Scripts.FlaskSequence;
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
        [SerializeField] private Button _buttonContinue;
        [SerializeField] private PopupAnimationPanelsSequence _viewAnimation;
        [SerializeField] private TextMeshProUGUI _levelNumberText;

        public event Action OnContinueButtonClick;

        public void Show(int levelNumber)
        {
            _levelNumberText.text = levelNumber.ToString();

            gameObject.SetActive(true);
            _viewAnimation.Show(null);
        }

        public void Hide()
        {
            _viewAnimation.Hide(() => gameObject.SetActive(false));
        }

        [Inject]
        private void Initialize(VictoryController controller, LevelCreator levelCreator)
        {
            controller.Initialize(levelCreator);

            _buttonContinue.onClick.AddListener(() => OnContinueButtonClick?.Invoke());
        }
    }
}
