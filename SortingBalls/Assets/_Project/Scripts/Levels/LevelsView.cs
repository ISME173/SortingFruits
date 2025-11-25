using _Project.Scripts.FlaskSequence;
using Reflex.Attributes;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace _Project.Scripts.Levels
{
    public class LevelsView : MonoBehaviour
    {
        private readonly List<LevelButton> LevelButtons = new();

        [Header("References")]
        [SerializeField] private RectTransform _parentForLevelButtons;
        [Space]
        [SerializeField] private Button _closeLevelsViewButton;
        [SerializeField] private Button _openLevelsViewButton;

        [Header("Assets")]
        [SerializeField] private LevelButton _levelButtonPrefab;

        private GridLayoutGroup _grid;

        public event Action OnCloseLevelsViewButtonClicked, OnOpenLevelsViewButtonClicked;
        public event Action<int> OnLevelButtonClicked;

        private void OnDestroy()
        {
            foreach (var levelButton in LevelButtons)
                levelButton.OnLevelButtonDown -= OnLevelButtonDown;
        }

        public void Show()
        {
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        public void UpdateView(int levelsCount, int openedLevelsCount, int completedLevelsCount)
        {
            for (int i = 0; i < levelsCount; i++)
            {
                LevelButton newLevelButton = Instantiate(_levelButtonPrefab);
                newLevelButton.transform.SetParent(_parentForLevelButtons, false);

                LevelState levelState = LevelState.Locked;

                if (openedLevelsCount - 1 >= i)
                    levelState = LevelState.Opened;
                if (completedLevelsCount - 1 >= i)
                    levelState = LevelState.Completed;

                newLevelButton.Initialize(i + 1, levelState);
                newLevelButton.OnLevelButtonDown += OnLevelButtonDown;

                LevelButtons.Add(newLevelButton);
            }

            ExpandParentHeight(levelsCount);
        }

        public void OpenLevel(int levelNumber)
        {
            if (levelNumber < 1 || levelNumber > LevelButtons.Count)
            {
                Debug.LogWarning($"Invalid {nameof(levelNumber)} value: {levelNumber}");
                return;
            }

			//Debug.Log($"Open level: {levelNumber}");

			LevelButtons[levelNumber - 1].Open();
        }

        public void LockLevel(int levelNumber)
        {
            if (levelNumber < 1 || levelNumber > LevelButtons.Count)
            {
                Debug.LogWarning($"Invalid {nameof(levelNumber)} value: {levelNumber}");
                return;
            }

            //Debug.Log($"Lock level: {levelNumber}");

            LevelButtons[levelNumber - 1].Lock();
        }

		public void CompleteLevel(int levelNumber)
		{
			if (levelNumber < 1 || levelNumber > LevelButtons.Count)
			{
				Debug.LogWarning($"Invalid {nameof(levelNumber)} value: {levelNumber}");
				return;
			}

			//Debug.Log($"Complete level: {levelNumber}");

			LevelButtons[levelNumber - 1].Complete();
		}

		private void OnLevelButtonDown(int levelNumber)
        {
            OnLevelButtonClicked?.Invoke(levelNumber);
        }

        private void ExpandParentHeight(int totalButtons)
        {
			_grid ??= _parentForLevelButtons.GetComponent<GridLayoutGroup>();

			int columns = 1;
            switch (_grid.constraint)
            {
                case GridLayoutGroup.Constraint.FixedColumnCount:
                    columns = _grid.constraintCount;
                    break;
                case GridLayoutGroup.Constraint.FixedRowCount:
                    // В этом режиме автоматически высота уже достаточна, можно выйти.
                    return;
                case GridLayoutGroup.Constraint.Flexible:
                    // Падение в простой расчет одной колонки (или можно попытаться вычислить по ширине).
                    columns = Mathf.Max(1, Mathf.FloorToInt(
                        (_parentForLevelButtons.rect.width + _grid.spacing.x + _grid.padding.left + _grid.padding.right) /
                        (_grid.cellSize.x + _grid.spacing.x)));
                    break;
            }

            int rows = Mathf.CeilToInt((float)totalButtons / columns);
            float height =
                _grid.padding.top +
                _grid.padding.bottom +
                rows * _grid.cellSize.y +
                (rows - 1) * _grid.spacing.y;

            _parentForLevelButtons.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
        }

        [Inject]
        private void Initialize(LevelsController levelsController, LevelCreator levelCreator)
        {
            levelsController.Initialize(levelCreator);

            _grid ??= _parentForLevelButtons.GetComponent<GridLayoutGroup>();

            _closeLevelsViewButton.onClick.AddListener(() => OnCloseLevelsViewButtonClicked?.Invoke());
            _openLevelsViewButton.onClick.AddListener(() => OnOpenLevelsViewButtonClicked?.Invoke());
        }
    }
}
