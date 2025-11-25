using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace _Project.Scripts.FlaskSequence.Bonuses
{
    public abstract class Bonus : MonoBehaviour
    {
        [Header("Base parameters")]
        [SerializeField] private Button _buttonForUse;
        [SerializeField] private TextMeshProUGUI _bonusesCountText;
        [SerializeField, Min(0)] private int _startBonusesCount;

        private int _bonusesCount;

        private void Awake()
        {
            _bonusesCount = _startBonusesCount;
            _bonusesCountText.text = _bonusesCount.ToString();

            _buttonForUse.onClick.AddListener(TryUseBonus);
        }

        private void TryUseBonus()
        {
            if (CanUseBonus() == false)
                return;

            _bonusesCount--;
            _bonusesCountText.text = _bonusesCount.ToString();

            UseBonus();
        }

        protected abstract void UseBonus();

        protected virtual bool CanUseBonus()
        {
            if (_bonusesCount == 0)
                return false;

            return true;
        }
    }
}
