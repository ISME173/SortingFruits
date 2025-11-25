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
        private LevelButtonState _levelButtonState;

        public event Action<int> OnLevelButtonDown;

        public enum LevelButtonState
        {
            Opened, Locked, Completed
        }

        public int LevelNumber => _levelNumber;

        public void Initialize(int levelNumber, LevelButtonState levelButtonState)
        {
            _button = GetComponent<Button>();
            _button.onClick.AddListener(() => OnLevelButtonDown?.Invoke(_levelNumber));

            _levelNumber = levelNumber;

            switch (_levelButtonState)
            {
                case LevelButtonState.Opened:
                    Open();
                    break;
                case LevelButtonState.Locked:
                    Lock();
                    break;
                case LevelButtonState.Completed:
                    Complete();
                    break;
            }

            _levelNumberText.text = _levelNumber.ToString();
        }

        public void Lock()
        {
            if (_levelButtonState == LevelButtonState.Opened)
                return;

            _levelButtonState = LevelButtonState.Opened;

            _levelNumberText.enabled = false;
            _button.interactable = false;
            _lockIcon.gameObject.SetActive(true);
            _completeIcon.gameObject.SetActive(false);
        }

        public void Open()
        {
            if (_levelButtonState == LevelButtonState.Locked)
                return;

            _levelButtonState = LevelButtonState.Locked;

            _levelNumberText.enabled = true;
            _button.interactable = true;
            _lockIcon.gameObject.SetActive(false);
            _completeIcon.gameObject.SetActive(false);
        }

        public void Complete()
        {
            if (_levelButtonState == LevelButtonState.Completed)
                return;

            _levelButtonState = LevelButtonState.Completed;

            _lockIcon.gameObject.SetActive(false);
            _levelNumberText.enabled = false;
            _button.interactable = false;
            _completeIcon.gameObject.SetActive(true);
        }
    }
}
