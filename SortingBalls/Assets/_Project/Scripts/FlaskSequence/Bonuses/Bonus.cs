using _Project.Scripts.FruitsSequence.Input;
using _Project.Scripts.Utils;
using AnimationsUI.CoreScripts;
using LitMotion;
using Reflex.Attributes;
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

        [Header("Animation")]
        [SerializeField] private PopupPanelForAnimate _bonusAnimation;
        [SerializeField, Min(0)] private float _timerWithoutTriggerDownForAnimate = 5;

        private int _bonusesCount;
        [Inject] private readonly IInput Input;

        private MotionHandle _timerForAnimate;
        private MotionHandle _bonusAnimationHandle;

        private void Awake()
        {
            _bonusesCount = _startBonusesCount;
            _bonusesCountText.text = _bonusesCount.ToString();

            _buttonForUse.onClick.AddListener(TryUseBonus);

            Input.OnTriggerDown += OnTriggerDown;

            _timerForAnimate = Timer.After(_timerWithoutTriggerDownForAnimate, () =>
            {
                _bonusAnimationHandle = _bonusAnimation.ShowPanel(null);
            });
        }

        private void OnDestroy()
        {
            Input.OnTriggerDown -= OnTriggerDown;
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

        private void OnTriggerDown(Vector3 position)
        {
            _timerForAnimate.TryCancel();
            _bonusAnimationHandle.TryCancel();

            transform.localScale = Vector3.one;

            _timerForAnimate = Timer.After(_timerWithoutTriggerDownForAnimate, () =>
            {
                _bonusAnimationHandle = _bonusAnimation.ShowPanel(null);
            });
        }
    }
}
