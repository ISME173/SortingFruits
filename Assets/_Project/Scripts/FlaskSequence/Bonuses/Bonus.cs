using _Project.Scripts.Advertising;
using _Project.Scripts.Audio;
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
        [Header("Settings")]
        [SerializeField, Min(0)] private int _startBonusesCount;
        [SerializeField, Min(0)] private float _timerWithoutTriggerDownForAnimate = 5;

        [Header("View references")]
        [SerializeField] private Button _buttonForUse;
        [SerializeField] private TextMeshProUGUI _bonusesCountText;
        [SerializeField] private Image _advImage;
        [SerializeField] private PopupPanelForAnimate _bonusAnimation;

        [Header("Sfx references")]
        [SerializeField] private AudioEvent _buttonClick;

        private IInput _Input;
        private IAdvertising _advertising;
        private ISaves _saves;
        private IAudioService _audioService;

        private MotionHandle _timerForAnimate;
        private MotionHandle _bonusAnimationHandle;
        private int _bonusesCount;
        private string _saveBonusesCountKey = null;

        private void Awake()
        {
            _bonusesCount = _saves.GetInt(GetSaveBonusesCountKey(), _startBonusesCount);
            _bonusesCountText.text = _bonusesCount.ToString();

            _advImage.gameObject.SetActive(_bonusesCount == 0);
            _buttonForUse.onClick.AddListener(TryUseBonus);

            _Input.OnTriggerDown += OnTriggerDown;

            _timerForAnimate = Timer.After(_timerWithoutTriggerDownForAnimate, () =>
            {
                _bonusAnimationHandle = _bonusAnimation.ShowPanel(null);
            });
        }

        private void OnDestroy()
        {
            _Input.OnTriggerDown -= OnTriggerDown;
        }

        private void TryUseBonus()
        {
            _audioService.PlayOneShot(_buttonClick);

            if (CanUseBonus() == false)
                return;

            _bonusesCount--;
            _saves.SetInt(GetSaveBonusesCountKey(), _bonusesCount);

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
                _advertising.ShowRewarded(() =>
                {
                    _bonusesCount++;
                    _saves.SetInt(GetSaveBonusesCountKey(), _bonusesCount);

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

        [Inject]
        private void Initialize(ISaves saves, IAdvertising advertising, IInput input, IAudioService audioService)
        {
            _saves = saves;
            _Input = input;
            _advertising = advertising;
            _audioService = audioService;
        }
    }
}
