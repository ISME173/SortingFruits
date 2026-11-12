using LitMotion;
using LitMotion.Extensions;
using System;
using UnityEngine;

namespace AnimationsUI.CoreScripts.AnimationVariants
{
    public class PopupWithSlidingPanel : PopupPanelForAnimate
    {
        [Header("Show settings")]
        [SerializeField] private AnimationSettings _showAnimationSettings;
        [Space]
        [SerializeField] private Vector3 _startLocalPositionInShow;
        [SerializeField] private Vector3 _endLocalPositionInShow;

        [Header("Hide settings")]
        [SerializeField] private AnimationSettings _hideAnimationSettings;
        [Space]
        [SerializeField] private Vector3 _startLocalPositionInHide;
        [SerializeField] private Vector3 _endLocalPositionInHide;

        private MotionHandle _showAnimationHandle;
        private MotionHandle _hideAnimationHandle;

        public override MotionHandle HidePanel(Action callback)
        {
            _showAnimationHandle.TryCancel();

            if (_hideAnimationSettings.WithLoop == false)
            {
                _hideAnimationHandle = LMotion.Create(_startLocalPositionInHide, _endLocalPositionInHide, _hideAnimationSettings.Time)
                    .WithEase(_hideAnimationSettings.Ease)
                    .WithCancelOnError()
                    .WithOnComplete(callback)
                    .BindToLocalPosition(PanelForAnimate);
            }
            else
            {
                _hideAnimationHandle = LMotion.Create(_startLocalPositionInHide, _endLocalPositionInHide, _hideAnimationSettings.Time)
                    .WithEase(_hideAnimationSettings.Ease)
                    .WithLoops(_hideAnimationSettings.LoopCount, _hideAnimationSettings.LoopType)
                    .WithCancelOnError()
                    .WithOnComplete(callback)
                    .BindToLocalPosition(PanelForAnimate);
            }

            return _hideAnimationHandle;
        }

        public override MotionHandle ShowPanel(Action callback)
        {
            _hideAnimationHandle.TryCancel();

            if (_showAnimationSettings.WithLoop == false)
            {
                _showAnimationHandle = LMotion.Create(_startLocalPositionInShow, _endLocalPositionInShow, _showAnimationSettings.Time)
                    .WithEase(_showAnimationSettings.Ease)
                    .WithCancelOnError()
                    .WithOnComplete(callback)
                    .BindToLocalPosition(PanelForAnimate);
            }
            else
            {
                _showAnimationHandle = LMotion.Create(_startLocalPositionInShow, _endLocalPositionInShow, _showAnimationSettings.Time)
                    .WithEase(_showAnimationSettings.Ease)
                    .WithLoops(_showAnimationSettings.LoopCount, _showAnimationSettings.LoopType)
                    .WithCancelOnError()
                    .WithOnComplete(callback)
                    .BindToLocalPosition(PanelForAnimate);
            }

            return _showAnimationHandle;
        }
    }
}
