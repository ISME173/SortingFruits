using _Project.Scripts.FlaskSequence;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace _Project.Scripts.Levels
{
    [RequireComponent(typeof(Button))]
    public class LevelButton : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Image _lockIcon;
        [SerializeField] private Image _completeIcon;
        [SerializeField] private TextMeshProUGUI _levelNumberText;

        private Button _button;
        private int _levelNumber;
        private LevelState _levelButtonState;

        public event Action<int> OnLevelButtonDown;

        public int LevelNumber => _levelNumber;

        public void Initialize(int levelNumber, LevelState levelButtonState)
        {
            _button = GetComponent<Button>();
            _button.onClick.AddListener(() => OnLevelButtonDown?.Invoke(_levelNumber));

            _levelNumber = levelNumber;

            switch (_levelButtonState)
            {
                case LevelState.Opened:
                    Open();
                    break;
                case LevelState.Locked:
                    Lock();
                    break;
                case LevelState.Completed:
                    Complete();
                    break;
            }

            _levelNumberText.text = _levelNumber.ToString();
        }

        public void Lock()
        {
            if (_levelButtonState == LevelState.Opened)
                return;

            _levelButtonState = LevelState.Opened;

            _levelNumberText.enabled = true;
            _button.interactable = false;
            _lockIcon.gameObject.SetActive(true);
            _completeIcon.gameObject.SetActive(false);
        }

        public void Open()
        {
            if (_levelButtonState == LevelState.Locked)
                return;

            _levelButtonState = LevelState.Locked;

            _levelNumberText.enabled = true;
            _button.interactable = true;
            _lockIcon.gameObject.SetActive(false);
            _completeIcon.gameObject.SetActive(false);
        }

        public void Complete()
        {
            if (_levelButtonState == LevelState.Completed)
                return;

            _levelButtonState = LevelState.Completed;

            _lockIcon.gameObject.SetActive(false);
            _levelNumberText.enabled = false;
            _button.interactable = false;
            _completeIcon.gameObject.SetActive(true);
        }
    }
}
