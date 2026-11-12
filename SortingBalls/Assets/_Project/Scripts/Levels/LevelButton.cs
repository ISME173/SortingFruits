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

        [Header("Current level")]
        [SerializeField, Min(1f)] private float _currentLevelScale = 1.15f;

        private Button _button;
        private int _levelNumber;
        private Vector3 _defaultScale = Vector3.one;

        public event Action<int> OnLevelButtonDown;

        public int LevelNumber => _levelNumber;

        public void Initialize(int levelNumber, LevelState levelButtonState)
        {
            _button = GetComponent<Button>();
            _defaultScale = transform.localScale;
            _levelNumber = levelNumber;
            _levelNumberText.text = _levelNumber.ToString();

            _button.onClick.AddListener(() => OnLevelButtonDown?.Invoke(_levelNumber));

            switch (levelButtonState)
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
        }

        public void Lock()
        {
            _levelNumberText.enabled = true;
            _button.interactable = false;
            _lockIcon.gameObject.SetActive(true);
            _completeIcon.gameObject.SetActive(false);
            SetHighlighted(false);
        }

        public void Open()
        {
            _levelNumberText.enabled = true;
            _button.interactable = true;
            _lockIcon.gameObject.SetActive(false);
            _completeIcon.gameObject.SetActive(false);
            SetHighlighted(true);
        }

        public void Complete()
        {
            _lockIcon.gameObject.SetActive(false);
            _levelNumberText.enabled = true;
            _button.interactable = true;
            _completeIcon.gameObject.SetActive(true);
            SetHighlighted(false);
        }

        public void SetHighlighted(bool isCurrent)
        {
            transform.localScale = _defaultScale * (isCurrent ? _currentLevelScale : 1f);
        }
    }
}
