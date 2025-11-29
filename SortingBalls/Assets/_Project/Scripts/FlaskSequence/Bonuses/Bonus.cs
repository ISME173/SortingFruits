using _Project.Scripts.Advertising;
using _Project.Scripts.FruitsSequence.Input;
using _Project.Scripts.Saves;
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
        [Inject] private readonly IInput Input;
        [Inject] private readonly IAdvertising Advertising;
        [Inject] private readonly ISaves Saves;

        [Header("Base parameters")]
        [SerializeField] private Button _buttonForUse;
        [SerializeField] private TextMeshProUGUI _bonusesCountText;
        [SerializeField] private Image _advImage;
        [SerializeField, Min(0)] private int _startBonusesCount;

        [Header("Animation")]
        [SerializeField] private PopupPanelForAnimate _bonusAnimation;
        [SerializeField, Min(0)] private float _timerWithoutTriggerDownForAnimate = 5;

        private int _bonusesCount;
        private MotionHandle _timerForAnimate;
        private MotionHandle _bonusAnimationHandle;
        private string _saveBonusesCountKey = null;

        private void Awake()
        {
            _bonusesCount = _startBonusesCount;
            _bonusesCountText.text = _bonusesCount.ToString();

            _advImage.gameObject.SetActive(_bonusesCount == 0);
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
            Saves.SetInt(GetSaveBonusesCountKey(), _bonusesCount);

            _bonusesCountText.text = _bonusesCount.ToString();

            if (_bonusesCount == 0)
            {
                _advImage.gameObject.SetActive(true);
            }

            UseBonus();
        }

        protected abstract void UseBonus();
        protected abstract string BuildSaveBonusesCountKey();

        protected virtual bool CanUseBonus()
        {
            if (_bonusesCount == 0)
            {
                Advertising.ShowRewarded(() =>
                {
                    _bonusesCount++;
                    Saves.SetInt(GetSaveBonusesCountKey(), _bonusesCount);

                    _bonusesCountText.text = _bonusesCount.ToString();

                    _advImage.gameObject.SetActive(false);
                }, null);
                return false;
            }

            return true;
        }

        private string GetSaveBonusesCountKey()
        {
            if (_saveBonusesCountKey == null)
                _saveBonusesCountKey = BuildSaveBonusesCountKey();

            return _saveBonusesCountKey;
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
