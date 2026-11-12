using LitMotion;
using LitMotion.Extensions;
using System;
using UnityEngine;

namespace AnimationsUI.CoreScripts.AnimationVariants
{
    public class PopupWithIncrease : PopupPanelForAnimate
    {
        [SerializeField] private bool _playOnAwake = false;
        [Space]

        [Header("Show animation settings")]
        [SerializeField] private AnimationSettings _showAnimationsSettings;
        [SerializeField] private Vector3 _showedLocalScale;

        [Header("Hide animation settings")]
        [SerializeField] private AnimationSettings _hideAnimationSettings;
        [SerializeField] private Vector3 _hidedLocalScale;

        private MotionHandle _showAnimationHandle;
        private MotionHandle _hideAnimationHandle;

        private void Awake()
        {
            if (_playOnAwake)
                ShowPanel(null);
        }

        public override MotionHandle HidePanel(Action callback)
        {
            _showAnimationHandle.TryCancel();

            if (_hideAnimationSettings.WithLoop == false)
            {
                _hideAnimationHandle = LMotion.Create(_showedLocalScale, _hidedLocalScale, _hideAnimationSettings.Time)
                    .WithEase(_hideAnimationSettings.Ease)
                    .WithCancelOnError()
                    .WithOnComplete(callback)
                    .BindToLocalScale(PanelForAnimate);
            }
            else
            {
                _hideAnimationHandle = LMotion.Create(_showedLocalScale, _hidedLocalScale, _hideAnimationSettings.Time)
                    .WithEase(_hideAnimationSettings.Ease)
                    .WithLoops(_hideAnimationSettings.LoopCount, _hideAnimationSettings.LoopType)
                    .WithCancelOnError()
                    .WithOnComplete(callback)
                    .BindToLocalScale(PanelForAnimate);
            }

            return _hideAnimationHandle;
        }

        public override MotionHandle ShowPanel(Action callback)
        {
            _hideAnimationHandle.TryCancel();

            if (_showAnimationsSettings.WithLoop == false)
            {
                _showAnimationHandle = LMotion.Create(_hidedLocalScale, _showedLocalScale, _showAnimationsSettings.Time)
                    .WithEase(_showAnimationsSettings.Ease)
                    .WithCancelOnError()
                    .WithOnComplete(callback)
                    .BindToLocalScale(PanelForAnimate);
            }
            else
            {
                _showAnimationHandle = LMotion.Create(_hidedLocalScale, _showedLocalScale, _showAnimationsSettings.Time)
                    .WithEase(_showAnimationsSettings.Ease)
                    .WithLoops(_showAnimationsSettings.LoopCount, _showAnimationsSettings.LoopType)
                    .WithCancelOnError()
                    .WithOnComplete(callback)
                    .BindToLocalScale(PanelForAnimate);
            }

            return _showAnimationHandle;
        }
    }
}
