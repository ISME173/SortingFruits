using Reflex.Attributes;
using TMPro;
using UnityEngine;

namespace _Project.Scripts.FlaskSequence
{
    public class LevelNumberView : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _levelNumberText;

        [Inject] private readonly LevelCreator LevelCreator;

        private void OnEnable()
        {
            _levelNumberText.text = (LevelCreator.CurrentLevelIndex + 1).ToString();
            LevelCreator.LevelCreated += LevelCreated;
        }

        private void OnDisable()
        {
            LevelCreator.LevelCreated -= LevelCreated;
        }

        private void LevelCreated(LevelData levelData)
        {
            _levelNumberText.text = (LevelCreator.CurrentLevelIndex + 1).ToString();
        }
    }
}
